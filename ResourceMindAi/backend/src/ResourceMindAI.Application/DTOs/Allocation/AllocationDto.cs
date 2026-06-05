namespace ResourceMindAI.Application.DTOs.Allocation;

public class AllocationDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = null!;
    public string EmployeeDesignation { get; set; } = null!;
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = null!;
    public string ProjectManager { get; set; } = null!;
    public decimal UtilisationPercent { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
