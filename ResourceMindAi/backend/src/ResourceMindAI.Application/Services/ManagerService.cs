using System.Globalization;
using Microsoft.Extensions.Logging;
using ResourceMindAI.Application.Abstractions.Repositories;
using ResourceMindAI.Application.Abstractions.Services;
using ResourceMindAI.Application.DTOs.Manager;
using ResourceMindAI.Domain.Entities;
using ResourceMindAI.Domain.Enums;
using ResourceMindAI.Domain.Exceptions;

namespace ResourceMindAI.Application.Services;

public class ManagerService : IManagerService
{
    private const int MaximumAiCandidates = 10;
    private const decimal MaximumWeeklyHours = 40m;

    private static readonly IReadOnlyDictionary<string, string> SkillAliases =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["back end"] = "backend",
            ["front end"] = "frontend",
            ["dev ops"] = "devops",
            ["quality assurance"] = "qa",
            ["dotnet"] = ".net",
            ["asp.net core"] = ".net",
            ["c sharp"] = "c#",
            ["js"] = "javascript",
            ["ts"] = "typescript",
            ["reactjs"] = "react",
            ["angularjs"] = "angular",
            ["spring"] = "spring boot"
        };

    private static readonly IReadOnlySet<string> GenericJobWords =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "developer",
            "engineer",
            "specialist",
            "resource",
            "person"
        };

    private readonly IManagerRepository _managerRepository;
    private readonly ISystemConfigRepository _systemConfigRepository;
    private readonly ILlmClient _llmClient;
    private readonly ILogger<ManagerService> _logger;

    public ManagerService(
        IManagerRepository managerRepository,
        ISystemConfigRepository systemConfigRepository,
        ILlmClient llmClient,
        ILogger<ManagerService> logger)
    {
        _managerRepository = managerRepository;
        _systemConfigRepository = systemConfigRepository;
        _llmClient = llmClient;
        _logger = logger;
    }

    public async Task<ManagerResourceDashboardDto> GetResourceDashboardAsync(Guid managerId)
    {
        var employees = await _managerRepository.GetTeamEmployeesAsync(managerId);
        var resources = employees.Select(MapResource).ToList();

        return new ManagerResourceDashboardDto
        {
            OnBench = resources.Where(x => x.AllocationPercent == 0).ToList(),
            ActiveEmployees = resources.Where(x => x.AllocationPercent > 0).ToList(),
        };
    }

    public async Task<ManagerResourceDto> GetResourceDetailAsync(Guid managerId, Guid employeeId)
    {
        var employee = await _managerRepository.GetTeamEmployeeAsync(managerId, employeeId);
        if (employee is null)
        {
            throw new EntityNotFoundException("Employee", employeeId);
        }

        return MapResource(employee);
    }

    public async Task<IReadOnlyList<ManagerProjectDto>> GetProjectsAsync(Guid managerId)
    {
        var projects = await _managerRepository.GetProjectsAsync(managerId);
        return projects.Select(MapProject).ToList();
    }

    public async Task<ManagerProjectDetailDto> GetProjectDetailAsync(Guid managerId, Guid projectId)
    {
        var project = await GetOwnedProjectAsync(managerId, projectId);
        var health = CalculateHealth(project);

        return new ManagerProjectDetailDto
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            StartDate = project.StartDate,
            EndDate = project.EndDate,
            Status = project.Status,
            HealthStatus = health.Health,
            TeamSize = project.Allocations.Where(IsActiveAllocation).Select(x => x.UserId).Distinct().Count(),
            Milestones = project.Milestones.OrderBy(x => x.DueDate).Select(MapMilestone).ToList(),
            AllocatedResources = project.Allocations.Where(IsActiveAllocation).Select(MapAllocation).ToList(),
            RiskFlags = health.Flags,
            RiskSummary = ProjectRiskSummarySerializer.Deserialize(project.RiskFlagsJson),
        };
    }

    public async Task<ProjectRiskSummaryDto> GenerateProjectRiskSummaryAsync(
        Guid managerId,
        Guid projectId)
    {
        var project = await GetRiskSummaryProjectAsync(managerId, projectId);

        try
        {
            return await GenerateAndSaveRiskSummaryAsync(project);
        }
        catch (ExternalServiceException exception)
        {
            var previousSummary = ProjectRiskSummarySerializer.Deserialize(project.RiskFlagsJson);
            if (previousSummary is not null)
            {
                _logger.LogWarning(
                    exception,
                    "AI risk generation failed for project {ProjectId}; returning saved summary",
                    projectId);
                return previousSummary;
            }

            throw;
        }
    }

    public async Task<ProjectRiskSummaryDto> GenerateScheduledProjectRiskSummaryAsync(
        Guid managerId,
        Guid projectId)
    {
        var project = await GetRiskSummaryProjectAsync(managerId, projectId);
        return await GenerateAndSaveRiskSummaryAsync(project);
    }

    public async Task<IReadOnlyList<ManagerTimesheetDto>> GetSubmittedTimesheetsAsync(Guid managerId)
    {
        var timesheets = await _managerRepository.GetSubmittedTimesheetsAsync(managerId);

        return timesheets.Select(x => new ManagerTimesheetDto
        {
            Id = x.Id,
            EmployeeId = x.UserId,
            EmployeeName = x.User.FullName,
            ProjectName = x.Project.Name,
            WeekStartDate = x.WeekStartDate,
            HoursLogged = x.HoursLogged,
            Status = x.Status,
            Tags = x.ActivityTags.Select(tag => tag.TagName).ToList(),
        }).ToList();
    }

    public async Task<ResourceMatchResponseDto> FindResourcesAsync(Guid managerId, FindResourceRequestDto request)
    {
        await GetOwnedProjectAsync(managerId, request.ProjectId!.Value);

        var extractedIntent = await _llmClient.ExtractResourceIntentAsync(request.Requirement);
        var intent = NormalizeIntent(extractedIntent);
        var requestedDates = ValidateAndParseIntent(intent);
        var employees = await _managerRepository.GetOrganizationSearchCandidatesAsync();

        var matches = new List<ResourceMatchDto>();
        foreach (var employee in employees)
        {
            if (employee.Allocations.Any(allocation =>
                allocation.ProjectId == request.ProjectId.Value
                && IsActiveAllocation(allocation)))
            {
                continue;
            }

            var availablePercent = Math.Max(0m, requestedDates.HasValue
                ? 100 - await _managerRepository.GetOverlappingAllocationPercentAsync(
                    employee.Id,
                    requestedDates.Value.FromDate,
                    requestedDates.Value.ToDate)
                : 100 - MapResource(employee).AllocationPercent);

            if (availablePercent <= 0
                || (intent.AvailabilityRequirement.HasValue
                    && availablePercent < intent.AvailabilityRequirement.Value))
            {
                continue;
            }

            if (!HasRequiredSkills(employee, intent.RequiredSkills)
                || MatchesExclusion(employee, intent.ExclusionConstraints))
            {
                continue;
            }

            var match = ScoreEmployee(
                employee,
                intent,
                availablePercent,
                employee.ResourceProfile?.ManagerId == managerId);
            matches.Add(match);
        }

        matches = matches
            .OrderByDescending(match => match.Score)
            .ThenBy(match => match.Employee.FullName)
            .Take(MaximumAiCandidates)
            .ToList();

        await AddAiCandidateExplanationsAsync(request.Requirement, intent, matches);
        foreach (var match in matches.Where(match => string.IsNullOrWhiteSpace(match.AiReason)))
        {
            match.AiReason = string.Join(" ", match.Reasons);
        }

        return new ResourceMatchResponseDto
        {
            Intent = intent,
            Matches = matches
                .OrderBy(match => match.AiRank ?? int.MaxValue)
                .ThenByDescending(match => match.Score)
                .ThenBy(match => match.Employee.FullName)
                .ToList(),
        };
    }

    public async Task<TeamBuilderResponseDto> BuildTeamAsync(
        Guid managerId,
        BuildTeamRequestDto request)
    {
        var project = await GetOwnedProjectAsync(managerId, request.ProjectId!.Value);
        var extractedIntent = await _llmClient.ExtractResourceIntentAsync(request.Requirement);
        var intent = NormalizeIntent(extractedIntent);
        ValidateAndParseIntent(intent);

        var employees = await _managerRepository.GetOrganizationSearchCandidatesAsync();
        var candidates = employees
            .Select(employee => ScoreEmployee(
                employee,
                intent,
                Math.Max(0m, 100m - MapResource(employee).AllocationPercent),
                employee.ResourceProfile?.ManagerId == managerId))
            .OrderByDescending(match => match.Score)
            .ThenBy(match => match.Employee.FullName)
            .ToList();

        if (candidates.Count == 0)
        {
            return new TeamBuilderResponseDto
            {
                Intent = intent,
                TeamSummary = "No active employees are available to evaluate for this team requirement.",
                Members = [],
                UnavailableMatches = [],
                MissingSkills = intent.RequiredSkills
            };
        }

        var aiRequest = new TeamBuilderAiRequestDto
        {
            ProjectName = project.Name,
            Requirement = request.Requirement,
            Intent = intent,
            Candidates = candidates.Select(BuildTeamCandidateSummary).ToList()
        };
        var aiResponse = await _llmClient.BuildTeamAsync(aiRequest);

        return MapTeamBuilderResponse(intent, candidates, aiResponse);
    }

    public async Task<ManagerAllocationDto> AllocateAsync(Guid managerId, CreateManagerAllocationDto request)
    {
        var fromDate = request.FromDate!.Value.Date;
        var project = await GetOwnedProjectAsync(managerId, request.ProjectId!.Value);
        var toDate = request.ToDate?.Date
            ?? project.EndDate?.Date
            ?? DateTime.MaxValue.Date;

        ValidateDateRange(fromDate, toDate);
        ValidateProjectDateRange(project, toDate);

        if (project.Status is not (ProjectStatus.Active or ProjectStatus.Planned))
        {
            throw new ValidationException("Project must be Active or Planned before allocating resources.");
        }

        var employee = await _managerRepository.GetTeamEmployeeAsync(managerId, request.EmployeeId!.Value);
        if (employee is null)
        {
            throw new ForbiddenException("You can allocate only employees in your team.", "MANAGER_SCOPE_VIOLATION");
        }

        var existingPercent = await _managerRepository.GetOverlappingAllocationPercentAsync(employee.Id, fromDate, toDate);
        if (existingPercent + request.UtilisationPercent!.Value > 100)
        {
            throw new ValidationException("Total allocation across overlapping dates cannot exceed 100%.");
        }

        var allocation = new Allocation
        {
            Id = Guid.NewGuid(),
            UserId = employee.Id,
            ProjectId = project.Id,
            UtilisationPercent = request.UtilisationPercent.Value,
            FromDate = fromDate,
            ToDate = toDate,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        var createdAllocation = await _managerRepository.AddAllocationAsync(allocation);
        createdAllocation.User = employee.User;
        createdAllocation.Project = project;

        return MapAllocation(createdAllocation);
    }

    public async Task<ManagerAllocationDto> EndAllocationAsync(Guid managerId, Guid allocationId)
    {
        var allocation = await _managerRepository.GetAllocationAsync(managerId, allocationId);
        if (allocation is null)
        {
            throw new EntityNotFoundException("Allocation", allocationId);
        }

        allocation.ToDate = DateTime.UtcNow.Date;
        allocation.IsActive = false;

        await _managerRepository.SaveChangesAsync();
        return MapAllocation(allocation);
    }

    private async Task<Project> GetOwnedProjectAsync(Guid managerId, Guid projectId)
    {
        var project = await _managerRepository.GetProjectAsync(managerId, projectId);
        if (project is null)
        {
            throw new ForbiddenException("You can access only projects managed by you.", "MANAGER_SCOPE_VIOLATION");
        }

        return project;
    }

    private async Task<Project> GetRiskSummaryProjectAsync(Guid managerId, Guid projectId)
    {
        var project = await _managerRepository.GetProjectForRiskSummaryAsync(managerId, projectId);
        if (project is null)
        {
            throw new ForbiddenException(
                "You can generate risk summaries only for projects managed by you.",
                "MANAGER_SCOPE_VIOLATION");
        }

        return project;
    }

    private async Task<ProjectRiskSummaryDto> GenerateAndSaveRiskSummaryAsync(Project project)
    {
        var configuredHours = await _systemConfigRepository.GetMaxWeeklyHoursAsync();
        var maximumWeeklyHours = configuredHours is > 0
            ? configuredHours.Value
            : MaximumWeeklyHours;
        var facts = BuildRiskFacts(project, maximumWeeklyHours);
        var generated = await _llmClient.GenerateProjectRiskSummaryAsync(facts);
        var validated = ValidateRiskSummary(generated);

        project.RiskFlagsJson = ProjectRiskSummarySerializer.Serialize(validated);
        await _managerRepository.SaveChangesAsync();

        return validated;
    }

    private static void ValidateDateRange(DateTime fromDate, DateTime toDate)
    {
        if (fromDate >= toDate)
        {
            throw new ValidationException("From date must be before to date.");
        }
    }

    private static void ValidateProjectDateRange(Project project, DateTime allocationToDate)
    {
        if (project.EndDate.HasValue && allocationToDate > project.EndDate.Value.Date)
        {
            throw new ValidationException(
                $"Allocation end date cannot be after the project end date ({project.EndDate.Value:dd-MM-yyyy}).");
        }
    }

    private static ManagerResourceDto MapResource(ResourceProfile employee)
    {
        return MapResource(employee.User);
    }

    private static ManagerResourceDto MapResource(User employee)
    {
        var activeAllocations = employee.Allocations.Where(IsActiveAllocation).ToList();
        var allocationPercent = activeAllocations.Sum(x => x.UtilisationPercent);

        return new ManagerResourceDto
        {
            Id = employee.Id,
            UserId = employee.Id,
            FullName = employee.FullName,
            Department = employee.Department ?? string.Empty,
            Designation = employee.Designation ?? string.Empty,
            AllocationPercent = allocationPercent,
            CurrentStatus = allocationPercent > 0
                ? ResourceStatus.Allocated
                : ResourceStatus.Bench,
            Skills = employee.ResourceProfile?.Skills
                .OrderBy(x => x.SkillName)
                .Select(x => x.SkillName)
                .ToList() ?? [],
            ActiveAllocations = activeAllocations.Select(MapAllocation).ToList(),
            RecentActivityTags = employee.Timesheets
                .OrderByDescending(x => x.WeekStartDate)
                .Take(6)
                .SelectMany(x => x.ActivityTags)
                .Select(x => x.TagName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(8)
                .ToList(),
        };
    }

    private static ManagerAllocationDto MapAllocation(Allocation allocation)
    {
        return new ManagerAllocationDto
        {
            Id = allocation.Id,
            EmployeeId = allocation.UserId,
            EmployeeName = allocation.User.FullName,
            ProjectId = allocation.ProjectId,
            ProjectName = allocation.Project.Name,
            UtilisationPercent = allocation.UtilisationPercent,
            FromDate = allocation.FromDate,
            ToDate = allocation.ToDate.Date == DateTime.MaxValue.Date ? null : allocation.ToDate,
            IsActive = allocation.IsActive,
        };
    }

    private static ManagerProjectDto MapProject(Project project)
    {
        var health = CalculateHealth(project);

        return new ManagerProjectDto
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            StartDate = project.StartDate,
            EndDate = project.EndDate,
            Status = project.Status,
            HealthStatus = health.Health,
            TeamSize = project.Allocations.Where(IsActiveAllocation).Select(x => x.UserId).Distinct().Count(),
        };
    }

    private static ManagerMilestoneDto MapMilestone(Milestone milestone)
    {
        return new ManagerMilestoneDto
        {
            Id = milestone.Id,
            Title = milestone.Title,
            DueDate = milestone.DueDate,
            Status = milestone.Status,
        };
    }

    private static bool IsActiveAllocation(Allocation allocation)
    {
        var today = DateTime.UtcNow.Date;
        return allocation.IsActive && allocation.FromDate.Date <= today && allocation.ToDate.Date >= today;
    }

    private static ResourceMatchDto ScoreEmployee(
        User employee,
        ResourceIntentDto intent,
        decimal availablePercent,
        bool isUnderCurrentManager)
    {
        var resource = MapResource(employee);
        var reasons = new List<string>();
        var score = 0;

        var employeeSkills = resource.Skills
            .Select(NormalizeSkill)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var matchedSkills = intent.RequiredSkills
            .Where(employeeSkills.Contains)
            .ToList();
        if (matchedSkills.Count > 0)
        {
            score += matchedSkills.Count * 15;
            reasons.Add($"Matched skills: {string.Join(", ", matchedSkills)}.");
        }

        var matchedActivity = intent.RequiredSkills
            .Where(skill => resource.RecentActivityTags
                .Select(NormalizeSearchValue)
                .Any(tag => ContainsWholeTerm(tag, skill)))
            .ToList();
        if (matchedActivity.Count > 0)
        {
            score += matchedActivity.Count * 10;
            reasons.Add($"Recent activity includes: {string.Join(", ", matchedActivity)}.");
        }

        if (availablePercent >= 100)
        {
            score += 25;
            reasons.Add("Employee has 100% availability during the requested period.");
        }
        else if (availablePercent > 0)
        {
            score += 15;
            reasons.Add($"{availablePercent}% availability remains during the requested period.");
        }

        if (intent.SoftConstraints.Any(x => x.Contains("bench", StringComparison.OrdinalIgnoreCase))
            && resource.AllocationPercent == 0)
        {
            score += 10;
            reasons.Add("Satisfies bench availability preference.");
        }

        if (reasons.Count == 0 && availablePercent > 0)
        {
            reasons.Add("Employee has allocation capacity available.");
        }

        return new ResourceMatchDto
        {
            Employee = resource,
            Score = Math.Min(score, 100),
            AvailablePercent = availablePercent,
            IsUnderCurrentManager = isUnderCurrentManager,
            Reasons = reasons,
        };
    }

    private async Task AddAiCandidateExplanationsAsync(
        string requirement,
        ResourceIntentDto intent,
        IReadOnlyList<ResourceMatchDto> matches)
    {
        if (matches.Count == 0)
        {
            return;
        }

        var request = new ResourceCandidateExplanationRequestDto
        {
            Requirement = requirement,
            Intent = intent,
            Candidates = matches.Select(match => new ResourceCandidateDto
            {
                EmployeeId = match.Employee.Id,
                Name = match.Employee.FullName,
                Designation = match.Employee.Designation,
                Skills = match.Employee.Skills,
                RecentActivityTags = match.Employee.RecentActivityTags,
                AvailablePercent = match.AvailablePercent,
                BackendScore = match.Score,
                BackendReasons = match.Reasons
            }).ToList()
        };

        try
        {
            var response = await _llmClient.ExplainResourceMatchesAsync(request);
            var knownMatches = matches.ToDictionary(match => match.Employee.Id);
            var validExplanations = response.Matches 
                .Where(explanation =>
                    explanation.AiRank > 0
                    && knownMatches.ContainsKey(explanation.EmployeeId))
                .GroupBy(explanation => explanation.EmployeeId)
                .Where(group => group.Count() == 1)
                .Select(group => group.Single())
                .OrderBy(explanation => explanation.AiRank)
                .ToList();

            if (validExplanations.Count != matches.Count
                || validExplanations.Select(item => item.AiRank).Distinct().Count() != matches.Count)
            {
                _logger.LogWarning(
                    "AI candidate explanation returned an incomplete or invalid candidate ranking");
                return;
            }

            foreach (var explanation in validExplanations)
            {
                var match = knownMatches[explanation.EmployeeId];
                match.AiRank = explanation.AiRank;
                match.AiReason = explanation.AiReason.Trim();
                match.Strengths = CleanValues(explanation.Strengths);
                match.Concerns = CleanValues(explanation.Concerns);
            }
        }
        catch (ExternalServiceException exception)
        {
            _logger.LogWarning(
                exception,
                "AI candidate explanation failed; returning backend-ranked resource matches");
        }
    }

    private static TeamBuilderCandidateDto BuildTeamCandidateSummary(ResourceMatchDto match)
    {
        var isEligible = match.Employee.CurrentStatus == ResourceStatus.Bench;
        return new TeamBuilderCandidateDto
        {
            EmployeeId = match.Employee.Id,
            Name = match.Employee.FullName,
            Department = match.Employee.Department,
            Designation = match.Employee.Designation,
            Skills = match.Employee.Skills,
            RecentActivityTags = match.Employee.RecentActivityTags,
            Status = match.Employee.CurrentStatus,
            IsEligible = isEligible,
            EligibilityReason = isEligible
                ? "Eligible because the employee is currently on bench."
                : "Not eligible because Team Builder allows only bench employees.",
            BackendScore = match.Score,
            BackendReasons = match.Reasons
        };
    }

    private static TeamBuilderResponseDto MapTeamBuilderResponse(
        ResourceIntentDto intent,
        IReadOnlyList<ResourceMatchDto> candidates,
        TeamBuilderAiResponseDto response)
    {
        if (string.IsNullOrWhiteSpace(response.TeamSummary))
        {
            throw new ExternalServiceException("AI team recommendation returned an invalid summary.");
        }

        var knownCandidates = candidates.ToDictionary(candidate => candidate.Employee.Id);
        var duplicateMemberIds = response.Members
            .GroupBy(member => member.EmployeeId)
            .Any(group => group.Count() > 1);
        var duplicateUnavailableIds = response.UnavailableMatches
            .GroupBy(member => member.EmployeeId)
            .Any(group => group.Count() > 1);
        if (duplicateMemberIds
            || duplicateUnavailableIds
            || response.Members.Any(member =>
                !knownCandidates.ContainsKey(member.EmployeeId)
                || knownCandidates[member.EmployeeId].Employee.CurrentStatus != ResourceStatus.Bench
                || string.IsNullOrWhiteSpace(member.SuggestedRole)
                || string.IsNullOrWhiteSpace(member.Reason))
            || response.UnavailableMatches.Any(member =>
                !knownCandidates.ContainsKey(member.EmployeeId)
                || knownCandidates[member.EmployeeId].Employee.CurrentStatus != ResourceStatus.Allocated
                || string.IsNullOrWhiteSpace(member.MatchedRole)
                || string.IsNullOrWhiteSpace(member.Reason))
            || response.Members.Select(member => member.EmployeeId)
                .Intersect(response.UnavailableMatches.Select(member => member.EmployeeId))
                .Any())
        {
            throw new ExternalServiceException("AI team recommendation returned invalid members.");
        }

        var members = response.Members
            .Select(member =>
            {
                var candidate = knownCandidates[member.EmployeeId];
                return new TeamBuilderMemberDto
                {
                    Employee = candidate.Employee,
                    SuggestedRole = member.SuggestedRole.Trim(),
                    Reason = member.Reason.Trim(),
                    MatchedSkills = GetVerifiedMatchedSkills(
                        candidate.Employee.Skills,
                        member.MatchedSkills)
                };
            })
            .ToList();

        var unavailableMatches = response.UnavailableMatches
            .Select(member =>
            {
                var candidate = knownCandidates[member.EmployeeId];
                return new TeamBuilderUnavailableMemberDto
                {
                    Employee = candidate.Employee,
                    MatchedRole = member.MatchedRole.Trim(),
                    Reason = member.Reason.Trim(),
                    MatchedSkills = GetVerifiedMatchedSkills(
                        candidate.Employee.Skills,
                        member.MatchedSkills)
                };
            })
            .ToList();

        return new TeamBuilderResponseDto
        {
            Intent = intent,
            TeamSummary = response.TeamSummary.Trim(),
            Members = members,
            UnavailableMatches = unavailableMatches,
            MissingSkills = CleanValues(response.MissingSkills)
        };
    }

    private static IReadOnlyList<string> GetVerifiedMatchedSkills(
        IReadOnlyList<string> candidateSkills,
        IEnumerable<string> matchedSkills)
    {
        var actualSkills = candidateSkills
            .GroupBy(NormalizeSkill, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.First(),
                StringComparer.OrdinalIgnoreCase);

        return matchedSkills
            .Select(NormalizeSkill)
            .Where(actualSkills.ContainsKey)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(skill => actualSkills[skill])
            .ToList();
    }

    private static ResourceIntentDto NormalizeIntent(ResourceIntentDto intent)
    {
        return new ResourceIntentDto
        {
            RequiredSkills = intent.RequiredSkills
                .Select(NormalizeSkill)
                .Where(value => value.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            ExperienceHint = NormalizeOptionalValue(intent.ExperienceHint),
            AvailabilityRequirement = intent.AvailabilityRequirement,
            FromDate = NormalizeOptionalValue(intent.FromDate),
            ToDate = NormalizeOptionalValue(intent.ToDate),
            PrioritySignals = CleanValues(intent.PrioritySignals),
            SoftConstraints = CleanValues(intent.SoftConstraints),
            ExclusionConstraints = CleanValues(intent.ExclusionConstraints)
        };
    }

    private static bool HasRequiredSkills(
        User employee,
        IReadOnlyList<string> requiredSkills)
    {
        if (requiredSkills.Count == 0)
        {
            return true;
        }

        var employeeSkills = employee.ResourceProfile?.Skills
            .Select(skill => NormalizeSkill(skill.SkillName))
            .ToHashSet(StringComparer.OrdinalIgnoreCase)
            ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        return requiredSkills.All(employeeSkills.Contains);
    }

    private static bool MatchesExclusion(
        User employee,
        IReadOnlyList<string> exclusions)
    {
        if (exclusions.Count == 0)
        {
            return false;
        }

        var searchableValues = (employee.ResourceProfile?.Skills ?? [])
            .Select(skill => NormalizeSearchValue(skill.SkillName))
            .Append(NormalizeSearchValue(employee.Designation))
            .Append(NormalizeSearchValue(employee.Department))
            .ToList();

        return exclusions
            .Select(NormalizeSearchValue)
            .Any(exclusion => searchableValues.Any(value => ContainsWholeTerm(value, exclusion)));
    }

    private static string NormalizeSkill(string value)
    {
        var normalized = NormalizeSearchValue(value);
        var withoutJobWords = string.Join(
            ' ',
            normalized
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(word => !GenericJobWords.Contains(word)));

        return SkillAliases.TryGetValue(withoutJobWords, out var canonical)
            ? canonical
            : withoutJobWords;
    }

    private static string NormalizeSearchValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return string.Join(
            ' ',
            value.Trim()
                .ToLowerInvariant()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static string? NormalizeOptionalValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : NormalizeSearchValue(value);
    }

    private static IReadOnlyList<string> CleanValues(IEnumerable<string> values)
    {
        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool ContainsWholeTerm(string value, string term)
    {
        if (value.Equals(term, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var valueWords = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var termWords = term.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return termWords.Length > 0
            && termWords.All(termWord => valueWords.Contains(
                termWord,
                StringComparer.OrdinalIgnoreCase));
    }

    private static (DateTime FromDate, DateTime ToDate)? ValidateAndParseIntent(ResourceIntentDto intent)
    {
        if (intent.AvailabilityRequirement is < 1 or > 100)
        {
            throw new ExternalServiceException("AI intent detection returned an invalid availability requirement.");
        }

        var hasFromDate = !string.IsNullOrWhiteSpace(intent.FromDate);
        var hasToDate = !string.IsNullOrWhiteSpace(intent.ToDate);
        if (!hasFromDate && !hasToDate)
        {
            return null;
        }

        if (!hasFromDate || !hasToDate)
        {
            throw new ExternalServiceException("AI intent detection returned an incomplete date range.");
        }

        if (!TryParseIntentDate(intent.FromDate, out var fromDate)
            || !TryParseIntentDate(intent.ToDate, out var toDate)
            || fromDate >= toDate)
        {
            throw new ExternalServiceException("AI intent detection returned an invalid date range.");
        }

        return (fromDate, toDate);
    }

    private static bool TryParseIntentDate(string? value, out DateTime date)
    {
        return DateTime.TryParseExact(
            value,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out date);
    }

    private static ProjectRiskFactsDto BuildRiskFacts(
        Project project,
        decimal maximumWeeklyHours)
    {
        var health = CalculateHealth(project);
        var recentCutoff = DateTime.UtcNow.Date.AddDays(-56);

        return new ProjectRiskFactsDto
        {
            ProjectName = project.Name,
            ProjectStatus = project.Status.ToString(),
            StartDate = project.StartDate,
            EndDate = project.EndDate,
            Milestones = project.Milestones
                .OrderBy(milestone => milestone.DueDate)
                .Select(milestone => new ProjectRiskMilestoneFactDto
                {
                    Title = milestone.Title,
                    DueDate = milestone.DueDate,
                    Status = milestone.Status.ToString()
                })
                .ToList(),
            ActiveAllocations = project.Allocations
                .Where(IsActiveAllocation)
                .Select(allocation => new ProjectRiskAllocationFactDto
                {
                    EmployeeName = allocation.User.FullName,
                    AllocationPercent = allocation.UtilisationPercent,
                    FromDate = allocation.FromDate,
                    ToDate = allocation.ToDate.Date == DateTime.MaxValue.Date
                        ? null
                        : allocation.ToDate,
                    ExpectedWeeklyHours = maximumWeeklyHours
                        * allocation.UtilisationPercent
                        / 100m
                })
                .ToList(),
            RecentTimesheets = BuildRecentTimesheetFacts(
                project,
                recentCutoff,
                maximumWeeklyHours),
            SystemRiskFlags = health.Flags
        };
    }

    private static IReadOnlyList<ProjectRiskTimesheetFactDto> BuildRecentTimesheetFacts(
        Project project,
        DateTime recentCutoff,
        decimal maximumWeeklyHours)
    {
        var facts = project.Timesheets
            .Where(timesheet => timesheet.WeekStartDate >= recentCutoff)
            .Select(timesheet => new ProjectRiskTimesheetFactDto
            {
                EmployeeName = timesheet.User.FullName,
                WeekStart = timesheet.WeekStartDate,
                LoggedHours = timesheet.HoursLogged,
                ExpectedHours = GetExpectedHours(project, timesheet, maximumWeeklyHours),
                Status = "Submitted"
            })
            .ToList();

        var submittedWeeks = project.Timesheets
            .Select(timesheet => (timesheet.UserId, Week: timesheet.WeekStartDate.Date))
            .ToHashSet();
        var firstWeek = StartOfWeek(recentCutoff);
        var lastCompletedWeek = StartOfWeek(DateTime.UtcNow.Date).AddDays(-7);

        foreach (var employeeAllocations in project.Allocations.GroupBy(allocation => allocation.UserId))
        {
            for (var week = firstWeek; week <= lastCompletedWeek; week = week.AddDays(7))
            {
                var weeklyAllocations = employeeAllocations
                    .Where(allocation =>
                        allocation.FromDate.Date <= week.AddDays(6)
                        && allocation.ToDate.Date >= week)
                    .ToList();

                if (weeklyAllocations.Count == 0
                    || submittedWeeks.Contains((employeeAllocations.Key, week)))
                {
                    continue;
                }

                facts.Add(new ProjectRiskTimesheetFactDto
                {
                    EmployeeName = weeklyAllocations[0].User.FullName,
                    WeekStart = week,
                    LoggedHours = 0m,
                    ExpectedHours = maximumWeeklyHours
                        * Math.Min(weeklyAllocations.Sum(allocation => allocation.UtilisationPercent), 100m)
                        / 100m,
                    Status = "Missed"
                });
            }
        }

        return facts
            .OrderByDescending(fact => fact.WeekStart)
            .ThenBy(fact => fact.EmployeeName)
            .ToList();
    }

    private static decimal GetExpectedHours(
        Project project,
        Timesheet timesheet,
        decimal maximumWeeklyHours)
    {
        var allocationPercent = project.Allocations
            .Where(allocation =>
                allocation.UserId == timesheet.UserId
                && allocation.FromDate.Date <= timesheet.WeekStartDate.Date.AddDays(6)
                && allocation.ToDate.Date >= timesheet.WeekStartDate.Date)
            .Sum(allocation => allocation.UtilisationPercent);

        return maximumWeeklyHours * Math.Min(allocationPercent, 100m) / 100m;
    }

    private static ProjectRiskSummaryDto ValidateRiskSummary(ProjectRiskSummaryDto summary)
    {
        var validHealthValues = new[] { "ON_TRACK", "ATTENTION", "AT_RISK" };
        var overallHealth = summary.OverallHealth?.Trim().ToUpperInvariant();
        if (!validHealthValues.Contains(overallHealth)
            || string.IsNullOrWhiteSpace(summary.Summary))
        {
            throw new ExternalServiceException("AI project risk summary returned invalid content.");
        }

        var validSeverities = new[] { "LOW", "MEDIUM", "HIGH" };
        var riskPoints = summary.RiskPoints
            .Where(point =>
                validSeverities.Contains(point.Severity?.Trim().ToUpperInvariant())
                && !string.IsNullOrWhiteSpace(point.Title)
                && !string.IsNullOrWhiteSpace(point.Description))
            .Select(point => new ProjectRiskPointDto
            {
                Severity = point.Severity.Trim().ToUpperInvariant(),
                Title = point.Title.Trim(),
                Description = point.Description.Trim()
            })
            .ToList();

        if (riskPoints.Count != summary.RiskPoints.Count)
        {
            throw new ExternalServiceException("AI project risk summary returned invalid risk points.");
        }

        return new ProjectRiskSummaryDto
        {
            OverallHealth = overallHealth!,
            Summary = summary.Summary.Trim(),
            RiskPoints = riskPoints,
            RecommendedActions = CleanValues(summary.RecommendedActions),
            SuggestedSkills = CleanValues(summary.SuggestedSkills),
            GeneratedAt = DateTime.UtcNow
        };
    }

    private static DateTime StartOfWeek(DateTime date)
    {
        var daysSinceMonday = ((int)date.DayOfWeek + 6) % 7;
        return date.Date.AddDays(-daysSinceMonday);
    }

    private static (HealthStatus Health, IReadOnlyList<string> Flags, IReadOnlyList<string> Summary) CalculateHealth(Project project)
    {
        var today = DateTime.UtcNow.Date;
        var flags = new List<string>();
        var score = 100;

        var overdueMilestones = project.Milestones.Count(x => x.DueDate.Date < today && x.Status != MilestoneStatus.Completed);
        if (overdueMilestones > 0)
        {
            score -= overdueMilestones * 20;
            flags.Add($"{overdueMilestones} milestone(s) are overdue.");
        }

        if (!project.Allocations.Any(IsActiveAllocation))
        {
            score -= 15;
            flags.Add("Project has no active resource allocation.");
        }

        if (project.EndDate.HasValue && project.EndDate.Value.Date < today && project.Status != ProjectStatus.Completed)
        {
            score -= 25;
            flags.Add("Project end date has passed but project is not completed.");
        }

        var health = score >= 80 ? HealthStatus.Green : score >= 50 ? HealthStatus.Amber : HealthStatus.Red;
        var summary = flags.Count == 0
            ? new List<string> { "No major delivery risks detected from current milestones and allocations." }
            : flags.Select(flag => $"Risk: {flag}").ToList();

        return (health, flags, summary);
    }
}
