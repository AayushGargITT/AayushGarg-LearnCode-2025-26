using ResourceMindAI.Domain.Enums;

namespace ResourceMindAI.Application.DTOs.Manager;

public class ManagerMilestoneDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public DateTime DueDate { get; set; }
    public MilestoneStatus Status { get; set; }
}
