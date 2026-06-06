namespace ResourceMindAI.Application.DTOs.Manager;

public class ManagerAllocationDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = null!;
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = null!;
    public decimal UtilisationPercent { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public bool IsActive { get; set; }
}
