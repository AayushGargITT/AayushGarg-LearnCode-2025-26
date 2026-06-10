using ResourceMindAI.Domain.Entities;

namespace ResourceMindAI.Application.Abstractions.Repositories;

public interface IProjectRepository
{
    Task<IReadOnlyList<Project>> GetAllAsync();
    Task<Project?> GetByIdAsync(Guid id);
    Task<Project?> GetForManagerUpdateAsync(Guid id);
    Task<IReadOnlyList<Allocation>> GetActiveAllocationsForEmployeesUnderManagerAsync(
        IReadOnlyCollection<Guid> employeeIds,
        Guid managerId);
    Task<Project> CreateAsync(Project project);
    Task<IReadOnlyList<Milestone>> GetMilestonesAsync(Guid projectId);
    Task<Milestone?> GetMilestoneAsync(Guid projectId, Guid milestoneId);
    Task<Milestone> AddMilestoneAsync(Milestone milestone);
    Task<Milestone> UpdateMilestoneAsync(Milestone milestone);
    Task SaveManagerUpdateAsync(Project project, IReadOnlyCollection<Employee> employees);
}
