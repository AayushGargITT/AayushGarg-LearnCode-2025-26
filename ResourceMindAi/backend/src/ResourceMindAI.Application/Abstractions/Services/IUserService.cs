using ResourceMindAI.Application.DTOs.Auth;
using ResourceMindAI.Application.DTOs.User;

namespace ResourceMindAI.Application.Abstractions.Services;

public interface IUserService
{
    Task<IReadOnlyList<UserProfileDto>> GetAllAsync();
    Task<UserProfileDto> GetByIdAsync(Guid id);
    Task<UserProfileDto> CreateAsync(CreateUserDto request);
}
