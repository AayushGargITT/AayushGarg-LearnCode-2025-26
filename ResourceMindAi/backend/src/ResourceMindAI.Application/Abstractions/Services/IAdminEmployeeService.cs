using ResourceMindAI.Application.DTOs.Auth;
using ResourceMindAI.Application.DTOs.Employee;

namespace ResourceMindAI.Application.Abstractions.Services;

public interface IAdminEmployeeService
{
    Task<IReadOnlyList<EmployeeListDto>> GetAllAsync();
    Task<UserProfileDto> GetByIdAsync(Guid userId);
    Task<IReadOnlyList<EmployeeSkillDto>> GetSkillsAsync(Guid employeeId);
    Task<EmployeeSkillDto> AddSkillAsync(Guid employeeId, CreateEmployeeSkillDto request);
    Task<EmployeeSkillDto> UpdateSkillProficiencyAsync(Guid employeeId, Guid skillId, UpdateEmployeeSkillProficiencyDto request);
    Task<EmployeeListDto> AssignManagerAsync(Guid employeeId, AssignEmployeeManagerDto request);
}
