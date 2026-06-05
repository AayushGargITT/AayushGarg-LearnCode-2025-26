using ResourceMindAI.Domain.Entities;

namespace ResourceMindAI.Application.Abstractions.Repositories;

public interface IEmployeeRepository
{
    Task<IReadOnlyList<Employee>> GetAllAsync();
    Task<Employee?> GetByIdAsync(Guid userId);
    Task<Employee?> GetByEmployeeIdAsync(Guid employeeId);
    Task<IReadOnlyList<Skill>> GetSkillsAsync(Guid employeeId);
    Task<Skill?> GetSkillAsync(Guid employeeId, Guid skillId);
    Task<Skill?> GetSkillByNameAsync(Guid employeeId, string skillName);
    Task<Skill> AddSkillAsync(Skill skill);
    Task<Skill> UpdateSkillAsync(Skill skill);
}
