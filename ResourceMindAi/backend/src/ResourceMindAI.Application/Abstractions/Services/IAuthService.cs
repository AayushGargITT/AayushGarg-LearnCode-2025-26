using ResourceMindAI.Application.DTOs.Auth;

namespace ResourceMindAI.Application.Abstractions.Services;

public interface IAuthService
{
    Task<UserProfileDto> LoginAsync(LoginDto request);
    Task<UserProfileDto> ChangePasswordAsync(ChangePasswordDto request);
}
