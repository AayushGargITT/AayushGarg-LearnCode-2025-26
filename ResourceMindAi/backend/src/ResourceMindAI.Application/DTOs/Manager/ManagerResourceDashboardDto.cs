namespace ResourceMindAI.Application.DTOs.Manager;

public class ManagerResourceDashboardDto
{
    public IReadOnlyList<ManagerResourceDto> OnBench { get; set; } = [];
    public IReadOnlyList<ManagerResourceDto> ActiveResources { get; set; } = [];
}
