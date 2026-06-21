using ResourceMindAI.Application.DTOs.Auth;

namespace ResourceMindAI.Application.DTOs.User;

public sealed class DeactivateUserResultDto
{
    public UserProfileDto User { get; init; } = null!;
    public string Message { get; init; } = null!;
    public int EndedAllocationCount { get; init; }
}

public sealed class ManagerDeactivationValidationDto
{
    public IReadOnlyList<string> Projects { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Employees { get; init; } = Array.Empty<string>();
}
