using ResourceMindAI.Domain.Enums;

namespace ResourceMindAI.Application.DTOs.Employee;

public class EmployeeListDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public Role Role { get; set; }
    public ResourceStatus AllocationStatus { get; set; }
    public string Department { get; set; } = null!;
    public string Designation { get; set; } = null!;
    public bool IsActive { get; set; }
    public Guid? ManagerId { get; set; }
    public string? ManagerName { get; set; }
}
