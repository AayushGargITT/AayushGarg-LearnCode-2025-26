using ResourceMindAI.Domain.Enums;

namespace ResourceMindAI.Application.DTOs.Auth;

public class LoginResponseDto
{
    public UserProfileDto User { get; set; } = null!;
    public string? Token { get; set; }
}

public class UserProfileDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Username { get; set; } = null!;
    public Role Role { get; set; }
    public bool IsActive { get; set; }
    public bool ForcePasswordChange { get; set; }
    public Guid? EmployeeId { get; set; }
    public string? Department { get; set; }
    public string? Designation { get; set; }
}
