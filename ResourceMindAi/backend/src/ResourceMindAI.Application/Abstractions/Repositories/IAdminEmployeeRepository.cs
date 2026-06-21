using ResourceMindAI.Domain.Entities;

namespace ResourceMindAI.Application.Abstractions.Repositories;

public interface IAdminEmployeeRepository
{
    Task<IReadOnlyList<User>> GetAllAsync();
    Task<User?> GetByIdAsync(Guid userId);
    Task<User?> GetForManagerUpdateAsync(Guid employeeId);
    Task<IReadOnlyList<Skill>> GetSkillsAsync(Guid employeeId);
    Task<Skill?> GetSkillAsync(Guid employeeId, Guid skillId);
    Task<Skill?> GetSkillByNameAsync(Guid employeeId, string skillName);
    Task<Skill> AddSkillAsync(Skill skill);
    Task<Skill> UpdateSkillAsync(Skill skill);
    Task SaveResourceProfileAsync(ResourceProfile resourceProfile);
}
