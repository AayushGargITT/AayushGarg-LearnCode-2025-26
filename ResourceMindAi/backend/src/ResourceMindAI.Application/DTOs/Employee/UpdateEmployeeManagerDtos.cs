using System.ComponentModel.DataAnnotations;

namespace ResourceMindAI.Application.DTOs.Employee;

public sealed class UpdateEmployeeManagerDto
{
    [Required(ErrorMessage = "Manager is required.")]
    public Guid? NewManagerId { get; set; }
}

public sealed class EmployeeManagerUpdatePreviewDto
{
    public Guid EmployeeId { get; init; }
    public Guid? CurrentManagerId { get; init; }
    public Guid NewManagerId { get; init; }
    public IReadOnlyList<string> ActiveProjects { get; init; } = Array.Empty<string>();
}

public sealed class EmployeeManagerUpdateResultDto
{
    public EmployeeListDto Employee { get; init; } = null!;
    public IReadOnlyList<string> EndedProjects { get; init; } = Array.Empty<string>();
    public string Message { get; init; } = null!;
}
