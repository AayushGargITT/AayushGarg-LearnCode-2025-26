namespace ResourceMindAI.Application.DTOs.Manager;

public sealed class FrozenTimesheetSubmissionDto
{
    public Guid EmployeeUserId { get; init; }
    public string EmployeeName { get; init; } = null!;
    public DateTime WeekStartDate { get; init; }
    public DateTime FrozenAtUtc { get; init; }
}
