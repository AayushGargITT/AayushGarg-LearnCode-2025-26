using Microsoft.EntityFrameworkCore;
using ResourceMindAI.Application.Abstractions.Repositories;
using ResourceMindAI.Domain.Entities;
using ResourceMindAI.Domain.Enums;

namespace ResourceMindAI.Infrastructure.Persistence.Repositories;

public class SchedulerRepository : ISchedulerRepository
{
    private readonly AppDbContext _dbContext;

    public SchedulerRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<User>> GetActiveEmployeesAsync(
        DateTime evaluationDate,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .Include(user => user.Allocations.Where(allocation =>
                allocation.IsActive
                && allocation.FromDate <= evaluationDate
                && allocation.ToDate >= evaluationDate))
            .Where(user => user.IsActive && user.Role == Role.Employee)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Project>> GetRiskSummaryProjectsAsync(
        CancellationToken cancellationToken)
    {
        return await _dbContext.Projects
            .AsNoTracking()
            .Where(project =>
                project.Status == ProjectStatus.Active
                || project.Status == ProjectStatus.Planned)
            .Select(project => new Project
            {
                Id = project.Id,
                ManagerId = project.ManagerId,
                Name = project.Name,
                Description = project.Description
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Project>> GetProjectHealthNotificationCandidatesAsync(
        CancellationToken cancellationToken)
    {
        return await _dbContext.Projects
            .AsNoTracking()
            .Include(project => project.Manager)
            .Where(project =>
                !string.IsNullOrWhiteSpace(project.RiskFlagsJson)
                && (project.Status == ProjectStatus.Active
                    || project.Status == ProjectStatus.Planned))
            .ToListAsync(cancellationToken);
    }
}
