using System.ComponentModel.DataAnnotations;

namespace ResourceMindAI.Application.DTOs.Employee;

public class AssignEmployeeManagerDto
{
    [Required(ErrorMessage = "Manager is required.")]
    public Guid? ManagerId { get; set; }
}
