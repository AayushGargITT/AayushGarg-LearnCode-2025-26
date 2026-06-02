using ResourceMindAI.Domain.Enums;

namespace ResourceMindAI.Application.DTOs.User;

public class CreateUserDto
{
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
    public Role Role { get; set; }
    public bool ForcePasswordChange { get; set; } = true;
}
