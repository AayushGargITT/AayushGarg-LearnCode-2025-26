using ResourceMindAI.Domain.Enums;

namespace ResourceMindAI.Domain.Entities;

public sealed class TimesheetSubmissionIssue
{
    public Guid Id { get; set; }
    public Guid ResourceUserId { get; set; }
    public Guid? ManagerUserId { get; set; }
    public DateTime WeekStartDate { get; set; }
    public TimesheetSubmissionIssueStatus Status { get; set; }
    public DateTime? FirstReminderSentAtUtc { get; set; }
    public DateTime? SecondReminderSentAtUtc { get; set; }
    public DateTime? FrozenAtUtc { get; set; }
    public DateTime? RestoredAtUtc { get; set; }
    public Guid? RestoredByManagerUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    public User ResourceUser { get; set; } = null!;
    public User? ManagerUser { get; set; }
    public User? RestoredByManagerUser { get; set; }
}
