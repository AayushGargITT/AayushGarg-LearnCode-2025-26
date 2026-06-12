namespace ResourceMindAI.Domain.Entities;

public class NotificationLog
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid RecipientUserId { get; set; }
    public string RecipientEmail { get; set; } = null!;
    public string NotificationType { get; set; } = null!;
    public DateTime SentAtUtc { get; set; }
    public bool IsSuccess { get; set; }
    public string? FailureReason { get; set; }

    public Project Project { get; set; } = null!;
    public User RecipientUser { get; set; } = null!;
}
