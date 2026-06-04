using ResourceMindAI.Application.DTOs.Auth;
using ResourceMindAI.Application.DTOs.User;
using ResourceMindAI.Application.DTOs.Employee;

namespace ResourceMindAI.Application.Abstractions.Services;

public interface IUserService
{
    Task<IReadOnlyList<UserProfileDto>> GetAllAsync();
    Task<UserProfileDto> GetByIdAsync(Guid id);
    Task<UserProfileDto> CreateAsync(CreateUserDto request);
    Task<UserProfileDto> ResetPasswordAsync(Guid id);
    Task<UserProfileDto> ToggleStatusAsync(Guid id);
     Task<UserProfileDto> AddEmployeeAsync(Guid userId, AddEmployeeDto request);
    
}
