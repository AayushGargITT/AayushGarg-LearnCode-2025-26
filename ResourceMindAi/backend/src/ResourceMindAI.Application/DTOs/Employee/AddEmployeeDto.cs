namespace ResourceMindAI.Application.DTOs.Employee;
public class AddEmployeeDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Department { get; set; }
    public string? Designation { get; set; }
}
