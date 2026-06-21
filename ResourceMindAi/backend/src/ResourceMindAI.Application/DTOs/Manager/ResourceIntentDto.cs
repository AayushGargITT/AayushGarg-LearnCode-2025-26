namespace ResourceMindAI.Application.DTOs.Manager;

public class ResourceIntentDto
{
    public IReadOnlyList<string> RequiredSkills { get; set; } = [];
    public string? ExperienceHint { get; set; }
    public int? AvailabilityRequirement { get; set; }
    public string? FromDate { get; set; }
    public string? ToDate { get; set; }
    public IReadOnlyList<string> PrioritySignals { get; set; } = [];
    public IReadOnlyList<string> SoftConstraints { get; set; } = [];
    public IReadOnlyList<string> ExclusionConstraints { get; set; } = [];
}
