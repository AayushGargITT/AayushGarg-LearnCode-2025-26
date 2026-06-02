using System;

namespace ResourceMindAI.Domain.Entities;
public class Allocation
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid ProjectId { get; set; }
    public decimal UtilisationPercent { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public Employee Employee { get; set; } = null!;
    public Project Project { get; set; } = null!;
}
