using ResourceMindAI.Domain.Enums;

namespace ResourceMindAI.Application.DTOs.Project;

public class MilestoneDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Title { get; set; } = null!;
    public DateTime DueDate { get; set; }
    public MilestoneStatus Status { get; set; }
}
