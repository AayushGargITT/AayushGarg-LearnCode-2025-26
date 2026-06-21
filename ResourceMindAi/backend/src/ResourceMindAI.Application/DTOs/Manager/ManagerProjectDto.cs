using ResourceMindAI.Domain.Enums;

namespace ResourceMindAI.Application.DTOs.Manager;

public class ManagerProjectDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public ProjectStatus Status { get; set; }
    public HealthStatus HealthStatus { get; set; }
    public int TeamSize { get; set; }
}
