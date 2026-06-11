using ResourceMindAI.Application.DTOs.Auth;
using ResourceMindAI.Application.DTOs.User;

namespace ResourceMindAI.Application.Abstractions.Services;

public interface IUserService
{
    Task<IReadOnlyList<UserProfileDto>> GetAllAsync();
    Task<IReadOnlyList<UserProfileDto>> GetActiveManagersAsync();
    Task<UserProfileDto> GetByIdAsync(Guid id);
    Task<UserProfileDto> CreateAsync(CreateUserDto request);
    Task<UserProfileDto> ResetPasswordAsync(Guid id);
    Task<DeactivateUserResultDto> DeactivateAsync(Guid id);
    Task<UserProfileDto> ReactivateAsync(Guid id);
}
