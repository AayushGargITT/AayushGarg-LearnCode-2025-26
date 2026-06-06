using ResourceMindAI.Application.Abstractions.Repositories;
using ResourceMindAI.Application.Abstractions.Services;
using ResourceMindAI.Application.DTOs.Manager;
using ResourceMindAI.Domain.Entities;
using ResourceMindAI.Domain.Enums;
using ResourceMindAI.Domain.Exceptions;
using System.Globalization;

namespace ResourceMindAI.Application.Services;

public class ManagerService : IManagerService
{
    private readonly IManagerRepository _managerRepository;
    private readonly ILlmClient _llmClient;

    public ManagerService(IManagerRepository managerRepository, ILlmClient llmClient)
    {
        _managerRepository = managerRepository;
        _llmClient = llmClient;
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
            TeamSize = project.Allocations.Where(IsActiveAllocation).Select(x => x.EmployeeId).Distinct().Count(),
            Milestones = project.Milestones.OrderBy(x => x.DueDate).Select(MapMilestone).ToList(),
            AllocatedResources = project.Allocations.Where(IsActiveAllocation).Select(MapAllocation).ToList(),
            RiskFlags = health.Flags,
            RiskSummary = health.Summary,
        };
    }

    public async Task<IReadOnlyList<ManagerTimesheetDto>> GetSubmittedTimesheetsAsync(Guid managerId)
    {
        var timesheets = await _managerRepository.GetSubmittedTimesheetsAsync(managerId);

        return timesheets.Select(x => new ManagerTimesheetDto
        {
            Id = x.Id,
            EmployeeName = x.Employee.User.FullName,
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

        var intent = await _llmClient.ExtractResourceIntentAsync(request.Requirement);
        var requestedDates = ValidateAndParseIntent(intent);
        var employees = await _managerRepository.GetTeamEmployeesAsync(managerId);

        var matches = new List<ResourceMatchDto>();
        foreach (var employee in employees)
        {
            var availablePercent = requestedDates.HasValue
                ? 100 - await _managerRepository.GetOverlappingAllocationPercentAsync(
                    employee.Id,
                    requestedDates.Value.FromDate,
                    requestedDates.Value.ToDate)
                : 100 - MapResource(employee).AllocationPercent;

            if (intent.AvailabilityRequirement.HasValue
                && availablePercent < intent.AvailabilityRequirement.Value)
            {
                continue;
            }

            var match = ScoreEmployee(employee, intent, availablePercent);
            if (match.Score > 0)
            {
                matches.Add(match);
            }
        }

        matches = matches
            .OrderByDescending(match => match.Score)
            .ThenBy(match => match.Employee.FullName)
            .ToList();

        return new ResourceMatchResponseDto
        {
            Intent = intent,
            Matches = matches,
        };
    }

    public async Task<ManagerAllocationDto> AllocateAsync(Guid managerId, CreateManagerAllocationDto request)
    {
        var fromDate = request.FromDate!.Value.Date;
        var project = await GetOwnedProjectAsync(managerId, request.ProjectId!.Value);
        var toDate = request.ToDate?.Date
            ?? project.EndDate?.Date
            ?? DateTime.MaxValue.Date;

        ValidateDateRange(fromDate, toDate);

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
            EmployeeId = employee.Id,
            ProjectId = project.Id,
            UtilisationPercent = request.UtilisationPercent.Value,
            FromDate = fromDate,
            ToDate = toDate,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        var createdAllocation = await _managerRepository.AddAllocationAsync(allocation);
        createdAllocation.Employee = employee;
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

    private static void ValidateDateRange(DateTime fromDate, DateTime toDate)
    {
        if (fromDate >= toDate)
        {
            throw new ValidationException("From date must be before to date.");
        }
    }

    private static ManagerResourceDto MapResource(Employee employee)
    {
        var activeAllocations = employee.Allocations.Where(IsActiveAllocation).ToList();
        var allocationPercent = activeAllocations.Sum(x => x.UtilisationPercent);

        return new ManagerResourceDto
        {
            Id = employee.Id,
            UserId = employee.UserId,
            FullName = employee.User.FullName,
            Department = employee.Department,
            Designation = employee.Designation,
            AllocationPercent = allocationPercent,
            CurrentStatus = allocationPercent == 0 ? "Bench" : allocationPercent >= 100 ? "Full" : "Partial",
            Skills = employee.Skills.OrderBy(x => x.SkillName).Select(x => x.SkillName).ToList(),
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
            EmployeeId = allocation.EmployeeId,
            EmployeeName = allocation.Employee.User.FullName,
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
            TeamSize = project.Allocations.Where(IsActiveAllocation).Select(x => x.EmployeeId).Distinct().Count(),
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
        Employee employee,
        ResourceIntentDto intent,
        decimal availablePercent)
    {
        var resource = MapResource(employee);
        var reasons = new List<string>();
        var score = 0;

        if (!string.IsNullOrWhiteSpace(intent.RequiredRole)
            && employee.Designation.Contains(intent.RequiredRole.Split(' ')[0], StringComparison.OrdinalIgnoreCase))
        {
            score += 20;
            reasons.Add($"Designation aligns with {intent.RequiredRole}.");
        }

        var employeeSkills = resource.Skills.Select(x => x.ToLowerInvariant()).ToHashSet();
        var matchedSkills = intent.RequiredSkills.Where(skill => employeeSkills.Any(value => value.Contains(skill))).ToList();
        if (matchedSkills.Count > 0)
        {
            score += matchedSkills.Count * 15;
            reasons.Add($"Matched skills: {string.Join(", ", matchedSkills)}.");
        }

        var matchedActivity = intent.RequiredSkills
            .Where(skill => resource.RecentActivityTags.Any(tag => tag.Contains(skill, StringComparison.OrdinalIgnoreCase)))
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
            Reasons = reasons,
        };
    }

    private static (DateTime FromDate, DateTime ToDate)? ValidateAndParseIntent(ResourceIntentDto intent)
    {
        if (intent.AvailabilityRequirement is < 1 or > 100)
        {
            throw new ExternalServiceException("AI intent detection returned an invalid availability requirement.");
        }

        if (string.IsNullOrWhiteSpace(intent.FromDate) || string.IsNullOrWhiteSpace(intent.ToDate))
        {
            return null;
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
