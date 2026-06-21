namespace ResourceMindAI.Application.DTOs.Manager;

public sealed class FrozenTimesheetSubmissionDto
{
    public Guid ResourceUserId { get; init; }
    public string ResourceName { get; init; } = null!;
    public DateTime WeekStartDate { get; init; }
    public DateTime FrozenAtUtc { get; init; }
}
