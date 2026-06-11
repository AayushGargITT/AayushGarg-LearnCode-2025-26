using System.ComponentModel.DataAnnotations;

namespace ResourceMindAI.Application.DTOs.Manager;

public sealed class BuildTeamRequestDto
{
    [Required]
    public Guid? ProjectId { get; init; }

    [Required]
    [StringLength(2000, MinimumLength = 5)]
    public string Requirement { get; init; } = null!;
}

public sealed class TeamBuilderAiRequestDto
{
    public string ProjectName { get; init; } = null!;
    public string Requirement { get; init; } = null!;
    public ResourceIntentDto Intent { get; init; } = null!;
    public IReadOnlyList<ResourceCandidateDto> Candidates { get; init; } = [];
}

public sealed class TeamBuilderAiResponseDto
{
    public string TeamSummary { get; init; } = null!;
    public IReadOnlyList<TeamBuilderAiMemberDto> Members { get; init; } = [];
    public IReadOnlyList<string> MissingSkills { get; init; } = [];
}

public sealed class TeamBuilderAiMemberDto
{
    public Guid EmployeeId { get; init; }
    public string SuggestedRole { get; init; } = null!;
    public string Reason { get; init; } = null!;
    public IReadOnlyList<string> MatchedSkills { get; init; } = [];
}

public sealed class TeamBuilderResponseDto
{
    public ResourceIntentDto Intent { get; init; } = null!;
    public string TeamSummary { get; init; } = null!;
    public IReadOnlyList<TeamBuilderMemberDto> Members { get; init; } = [];
    public IReadOnlyList<string> MissingSkills { get; init; } = [];
}

public sealed class TeamBuilderMemberDto
{
    public ManagerResourceDto Employee { get; init; } = null!;
    public string SuggestedRole { get; init; } = null!;
    public string Reason { get; init; } = null!;
    public IReadOnlyList<string> MatchedSkills { get; init; } = [];
}
