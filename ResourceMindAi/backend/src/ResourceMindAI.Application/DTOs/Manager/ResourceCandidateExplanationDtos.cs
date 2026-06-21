namespace ResourceMindAI.Application.DTOs.Manager;

public sealed class ResourceCandidateExplanationRequestDto
{
    public string Requirement { get; init; } = null!;
    public ResourceIntentDto Intent { get; init; } = null!;
    public IReadOnlyList<ResourceCandidateDto> Candidates { get; init; } = [];
}

public sealed class ResourceCandidateDto
{
    public Guid ResourceId { get; init; }
    public string Name { get; init; } = null!;
    public string Designation { get; init; } = null!;
    public IReadOnlyList<string> Skills { get; init; } = [];
    public IReadOnlyList<string> RecentActivityTags { get; init; } = [];
    public decimal AvailablePercent { get; init; }
    public int BackendScore { get; init; }
    public IReadOnlyList<string> BackendReasons { get; init; } = [];
}

public sealed class ResourceCandidateExplanationResponseDto
{
    public IReadOnlyList<ResourceCandidateExplanationDto> Matches { get; init; } = [];
}

public sealed class ResourceCandidateExplanationDto
{
    public Guid ResourceId { get; init; }
    public int AiRank { get; init; }
    public string AiReason { get; init; } = null!;
    public IReadOnlyList<string> Strengths { get; init; } = [];
    public IReadOnlyList<string> Concerns { get; init; } = [];
}
