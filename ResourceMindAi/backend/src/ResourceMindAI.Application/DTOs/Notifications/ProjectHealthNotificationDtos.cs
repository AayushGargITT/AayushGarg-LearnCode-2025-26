using ResourceMindAI.Application.DTOs.Manager;

namespace ResourceMindAI.Application.DTOs.Notifications;

public sealed class ProjectHealthNotificationRequestDto
{
    public Guid ProjectId { get; init; }
    public string ProjectName { get; init; } = null!;
    public Guid ManagerId { get; init; }
    public string ManagerName { get; init; } = null!;
    public string ManagerEmail { get; init; } = null!;
    public ProjectRiskSummaryDto RiskSummary { get; init; } = null!;
}

public sealed class ProjectHealthNotificationResultDto
{
    public bool Sent { get; init; }
    public DateTime? SentAt { get; init; }
}
