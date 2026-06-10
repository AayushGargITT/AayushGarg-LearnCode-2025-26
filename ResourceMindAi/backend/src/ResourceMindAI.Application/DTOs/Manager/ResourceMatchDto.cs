namespace ResourceMindAI.Application.DTOs.Manager;

public class ResourceMatchDto
{
    public ManagerResourceDto Employee { get; set; } = null!;
    public int Score { get; set; }
    public decimal AvailablePercent { get; set; }
    public IReadOnlyList<string> Reasons { get; set; } = [];
    public int? AiRank { get; set; }
    public string? AiReason { get; set; }
    public IReadOnlyList<string> Strengths { get; set; } = [];
    public IReadOnlyList<string> Concerns { get; set; } = [];
}
