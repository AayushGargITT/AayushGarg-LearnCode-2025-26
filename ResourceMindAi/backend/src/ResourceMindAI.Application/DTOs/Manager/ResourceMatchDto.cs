namespace ResourceMindAI.Application.DTOs.Manager;

public class ResourceMatchDto
{
    public ManagerResourceDto Employee { get; set; } = null!;
    public int Score { get; set; }
    public IReadOnlyList<string> Reasons { get; set; } = [];
}
