using ResourceMindAI.Application.DTOs.Auth;

namespace ResourceMindAI.Application.Abstractions.Services;

public interface IEmployeeService
{
    Task<IReadOnlyList<UserProfileDto>> GetAllAsync();
    Task<UserProfileDto> GetByIdAsync(Guid userId);
}
