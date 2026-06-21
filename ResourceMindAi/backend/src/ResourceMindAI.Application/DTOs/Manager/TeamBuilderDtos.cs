using System.ComponentModel.DataAnnotations;
using ResourceMindAI.Domain.Enums;

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
    public IReadOnlyList<TeamBuilderCandidateDto> Candidates { get; init; } = [];
}

public sealed class TeamBuilderCandidateDto
{
    public Guid ResourceId { get; init; }
    public string Name { get; init; } = null!;
    public string Department { get; init; } = null!;
    public string Designation { get; init; } = null!;
    public IReadOnlyList<string> Skills { get; init; } = [];
    public IReadOnlyList<string> RecentActivityTags { get; init; } = [];
    public ResourceStatus Status { get; init; }
    public bool IsEligible { get; init; }
    public string EligibilityReason { get; init; } = null!;
    public int BackendScore { get; init; }
    public IReadOnlyList<string> BackendReasons { get; init; } = [];
}

public sealed class TeamBuilderAiResponseDto
{
    public string TeamSummary { get; init; } = null!;
    public IReadOnlyList<TeamBuilderAiMemberDto> Members { get; init; } = [];
    public IReadOnlyList<TeamBuilderAiUnavailableMemberDto> UnavailableMatches { get; init; } = [];
    public IReadOnlyList<string> MissingSkills { get; init; } = [];
}

public sealed class TeamBuilderAiMemberDto
{
    public Guid ResourceId { get; init; }
    public string SuggestedRole { get; init; } = null!;
    public string Reason { get; init; } = null!;
    public IReadOnlyList<string> MatchedSkills { get; init; } = [];
}

public sealed class TeamBuilderAiUnavailableMemberDto
{
    public Guid ResourceId { get; init; }
    public string MatchedRole { get; init; } = null!;
    public string Reason { get; init; } = null!;
    public IReadOnlyList<string> MatchedSkills { get; init; } = [];
}

public sealed class TeamBuilderResponseDto
{
    public ResourceIntentDto Intent { get; init; } = null!;
    public string TeamSummary { get; init; } = null!;
    public IReadOnlyList<TeamBuilderMemberDto> Members { get; init; } = [];
    public IReadOnlyList<TeamBuilderUnavailableMemberDto> UnavailableMatches { get; init; } = [];
    public IReadOnlyList<string> MissingSkills { get; init; } = [];
}

public sealed class TeamBuilderMemberDto
{
    public ManagerResourceDto Resource { get; init; } = null!;
    public string SuggestedRole { get; init; } = null!;
    public string Reason { get; init; } = null!;
    public IReadOnlyList<string> MatchedSkills { get; init; } = [];
}

public sealed class TeamBuilderUnavailableMemberDto
{
    public ManagerResourceDto Resource { get; init; } = null!;
    public string MatchedRole { get; init; } = null!;
    public string Reason { get; init; } = null!;
    public IReadOnlyList<string> MatchedSkills { get; init; } = [];
}
