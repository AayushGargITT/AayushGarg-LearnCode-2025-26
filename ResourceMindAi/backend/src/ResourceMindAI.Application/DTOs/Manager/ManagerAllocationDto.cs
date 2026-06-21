namespace ResourceMindAI.Application.DTOs.Manager;

public class ManagerAllocationDto
{
    public Guid Id { get; set; }
    public Guid ResourceId { get; set; }
    public string ResourceName { get; set; } = null!;
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = null!;
    public decimal UtilisationPercent { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public bool IsActive { get; set; }
}
