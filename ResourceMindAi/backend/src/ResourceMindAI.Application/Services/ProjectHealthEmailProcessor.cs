using Microsoft.Extensions.Logging;
using ResourceMindAI.Application.Abstractions.Repositories;
using ResourceMindAI.Application.Abstractions.Services;
using ResourceMindAI.Application.DTOs.Notifications;
using ResourceMindAI.Domain.Entities;

namespace ResourceMindAI.Application.Services;

public sealed class ProjectHealthEmailProcessor : IProjectHealthEmailProcessor
{
    private readonly ISchedulerRepository _schedulerRepository;
    private readonly IProjectHealthNotificationService _notificationService;
    private readonly ILogger<ProjectHealthEmailProcessor> _logger;

    public ProjectHealthEmailProcessor(
        ISchedulerRepository schedulerRepository,
        IProjectHealthNotificationService notificationService,
        ILogger<ProjectHealthEmailProcessor> logger)
    {
        _schedulerRepository = schedulerRepository;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        var projects = await _schedulerRepository
            .GetProjectHealthNotificationCandidatesAsync(cancellationToken);

        foreach (var project in projects)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!ProjectRiskSummarySerializer.TryDeserialize(
                    project.RiskFlagsJson,
                    out var summary))
            {
                _logger.LogWarning(
                    "Project health email skipped because saved risk JSON is invalid for project {ProjectId}",
                    project.Id);
                continue;
            }

            if (summary!.OverallHealth is not ("ATTENTION" or "AT_RISK"))
            {
                continue;
            }

            try
            {
                var teamResources = await _schedulerRepository.GetActiveResourcesUnderManagerAsync(
                    project.ManagerId,
                    cancellationToken);
                await _notificationService.NotifyAsync(
                    new ProjectHealthNotificationRequestDto
                    {
                        ProjectId = project.Id,
                        ProjectName = project.Name,
                        ProjectStatus = project.Status.ToString(),
                        ProjectStartDate = project.StartDate,
                        ProjectEndDate = project.EndDate,
                        ManagerId = project.ManagerId,
                        ManagerName = project.Manager.FullName,
                        ManagerEmail = project.Manager.Email,
                        RiskSummary = summary,
                        MatchingResources = FindMatchingResources(
                            teamResources,
                            summary.SuggestedSkills)
                    },
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Project health email processing failed for project {ProjectId}; continuing",
                    project.Id);
            }
        }
    }

    private static IReadOnlyList<ProjectHealthResourceRecommendationDto> FindMatchingResources(
        IEnumerable<User> resources,
        IEnumerable<string> suggestedSkills)
    {
        var requiredTerms = suggestedSkills
            .Select(NormalizeSearchValue)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (requiredTerms.Count == 0)
        {
            return [];
        }

        return resources
            .Select(resource => MapMatchingResource(resource, requiredTerms))
            .Where(resource => resource is not null)
            .Select(resource => resource!)
            .OrderBy(resource => resource.FullName)
            .Take(8)
            .ToList();
    }

    private static ProjectHealthResourceRecommendationDto? MapMatchingResource(
        User resource,
        IReadOnlyCollection<string> requiredTerms)
    {
        var skills = resource.ResourceProfile?.Skills
            .Select(skill => skill.SkillName)
            .Where(skill => !string.IsNullOrWhiteSpace(skill))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];
        var searchableValues = skills
            .Append(resource.Designation ?? string.Empty)
            .Select(NormalizeSearchValue)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList();
        var matchedSkills = skills
            .Where(skill => MatchesAnyRequiredTerm(
                NormalizeSearchValue(skill),
                requiredTerms))
            .ToList();
        var designationMatches = MatchesAnyRequiredTerm(
            NormalizeSearchValue(resource.Designation),
            requiredTerms);

        if (matchedSkills.Count == 0 && !designationMatches)
        {
            return null;
        }

        return new ProjectHealthResourceRecommendationDto
        {
            FullName = resource.FullName,
            Department = resource.Department,
            Designation = resource.Designation,
            MatchedSkills = matchedSkills.Count > 0
                ? matchedSkills
                : requiredTerms
                    .Where(term => searchableValues.Any(value => ContainsWholeTerm(value, term)))
                    .ToList()
        };
    }

    private static bool MatchesAnyRequiredTerm(
        string value,
        IEnumerable<string> requiredTerms)
    {
        return !string.IsNullOrWhiteSpace(value)
            && requiredTerms.Any(term =>
                ContainsWholeTerm(value, term)
                || ContainsWholeTerm(term, value));
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
}
