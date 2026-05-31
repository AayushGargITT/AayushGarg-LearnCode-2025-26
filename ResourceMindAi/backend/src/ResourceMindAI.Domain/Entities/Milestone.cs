using System;
using ResourceMindAI.Domain.Enums;

namespace ResourceMindAI.Domain.Entities;
public class Milestone
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Title { get; set; } = null!;
    public DateTime DueDate { get; set; }
    public MilestoneStatus Status { get; set; }

    public Project Project { get; set; } = null!;

    public Milestone() { }
}
