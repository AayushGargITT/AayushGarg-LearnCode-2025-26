namespace ResourceMindAI.Application.DTOs.Manager;

public class ResourceMatchResponseDto
{
    public ResourceIntentDto Intent { get; set; } = null!;
    public IReadOnlyList<ResourceMatchDto> Matches { get; set; } = [];
}
