namespace ResourceMindAI.Application.DTOs.Manager;

public class ManagerProjectDetailDto : ManagerProjectDto
{
    public IReadOnlyList<ManagerMilestoneDto> Milestones { get; set; } = [];
    public IReadOnlyList<ManagerAllocationDto> AllocatedResources { get; set; } = [];
    public IReadOnlyList<string> RiskFlags { get; set; } = [];
    public IReadOnlyList<string> RiskSummary { get; set; } = [];
}
