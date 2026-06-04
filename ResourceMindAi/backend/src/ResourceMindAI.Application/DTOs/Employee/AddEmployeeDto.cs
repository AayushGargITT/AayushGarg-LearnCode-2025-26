using System.ComponentModel.DataAnnotations;

namespace ResourceMindAI.Application.DTOs.Employee;

public class AddEmployeeDto
{
    [Required(ErrorMessage = "Designation is required.")]
    public string Designation { get; set; } = null!;

    [Required(ErrorMessage = "Department is required.")]
    public string Department { get; set; } = null!;
}
