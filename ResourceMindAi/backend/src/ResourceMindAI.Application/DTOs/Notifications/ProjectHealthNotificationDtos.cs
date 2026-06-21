using ResourceMindAI.Application.DTOs.Manager;

namespace ResourceMindAI.Application.DTOs.Notifications;

public sealed class ProjectHealthNotificationRequestDto
{
    public Guid ProjectId { get; init; }
    public string ProjectName { get; init; } = null!;
    public string ProjectStatus { get; init; } = null!;
    public DateTime ProjectStartDate { get; init; }
    public DateTime? ProjectEndDate { get; init; }
    public Guid ManagerId { get; init; }
    public string ManagerName { get; init; } = null!;
    public string ManagerEmail { get; init; } = null!;
    public ProjectRiskSummaryDto RiskSummary { get; init; } = null!;
    public IReadOnlyList<ProjectHealthResourceRecommendationDto> MatchingResources { get; init; } = [];
}

public sealed class ProjectHealthNotificationResultDto
{
    public bool Sent { get; init; }
    public DateTime? SentAt { get; init; }
}

public sealed class ProjectHealthResourceRecommendationDto
{
    public string FullName { get; init; } = null!;
    public string? Department { get; init; }
    public string? Designation { get; init; }
    public IReadOnlyList<string> MatchedSkills { get; init; } = [];
}
