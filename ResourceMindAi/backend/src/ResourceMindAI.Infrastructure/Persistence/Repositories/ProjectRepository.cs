using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ResourceMindAI.Application.Abstractions.Repositories;
using ResourceMindAI.Domain.Entities;
using ResourceMindAI.Domain.Enums;

namespace ResourceMindAI.Infrastructure.Persistence.Repositories;

public class ProjectRepository : IProjectRepository
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<ProjectRepository> _logger;

    public ProjectRepository(AppDbContext dbContext, ILogger<ProjectRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Project>> GetAllAsync()
    {
        _logger.LogDebug("Querying all projects with managers");

        return await _dbContext.Projects
            .Include(x => x.Manager)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<Project?> GetByIdAsync(Guid id)
    {
        _logger.LogDebug("Querying project {ProjectId}", id);

        return await _dbContext.Projects
            .Include(x => x.Manager)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public Task<Project?> GetForManagerUpdateAsync(Guid id)
    {
        return _dbContext.Projects
            .Include(project => project.Manager)
            .Include(project => project.Allocations.Where(allocation => allocation.IsActive))
                .ThenInclude(allocation => allocation.User)
                    .ThenInclude(user => user.ResourceProfile)
            .FirstOrDefaultAsync(project => project.Id == id);
    }

    public async Task<IReadOnlyList<Allocation>> GetActiveAllocationsForResourcesUnderManagerAsync(
        IReadOnlyCollection<Guid> resourceIds,
        Guid managerId)
    {
        return await _dbContext.Allocations
            .AsNoTracking()
            .Include(allocation => allocation.Project)
            .Include(allocation => allocation.User)
            .Where(allocation =>
                allocation.IsActive
                && resourceIds.Contains(allocation.UserId)
                && allocation.Project.ManagerId == managerId
                && (allocation.Project.Status == ProjectStatus.Active
                    || allocation.Project.Status == ProjectStatus.Planned))
            .ToListAsync();
    }

    public async Task<Project> CreateAsync(Project project)
    {
        _dbContext.Projects.Add(project);
        await _dbContext.SaveChangesAsync();
        return project;
    }

    public async Task<IReadOnlyList<Milestone>> GetMilestonesAsync(Guid projectId)
    {
        return await _dbContext.Milestones
            .Where(x => x.ProjectId == projectId)
            .OrderBy(x => x.DueDate)
            .ToListAsync();
    }

    public async Task<Milestone?> GetMilestoneAsync(Guid projectId, Guid milestoneId)
    {
        return await _dbContext.Milestones
            .FirstOrDefaultAsync(x => x.ProjectId == projectId && x.Id == milestoneId);
    }

    public async Task<Milestone> AddMilestoneAsync(Milestone milestone)
    {
        _dbContext.Milestones.Add(milestone);
        await _dbContext.SaveChangesAsync();
        return milestone;
    }

    public async Task<Milestone> UpdateMilestoneAsync(Milestone milestone)
    {
        _dbContext.Milestones.Update(milestone);
        await _dbContext.SaveChangesAsync();
        return milestone;
    }

    public async Task SaveManagerUpdateAsync(
        Project project,
        IReadOnlyCollection<ResourceProfile> resourceProfiles)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        _dbContext.Projects.Update(project);
        var profileIds = resourceProfiles.Select(profile => profile.Id).ToList();
        var existingProfileIds = await _dbContext.ResourceProfiles
            .Where(profile => profileIds.Contains(profile.Id))
            .Select(profile => profile.Id)
            .ToListAsync();

        _dbContext.ResourceProfiles.UpdateRange(
            resourceProfiles.Where(profile => existingProfileIds.Contains(profile.Id)));
        await _dbContext.ResourceProfiles.AddRangeAsync(
            resourceProfiles.Where(profile => !existingProfileIds.Contains(profile.Id)));
        await _dbContext.SaveChangesAsync();

        await transaction.CommitAsync();
    }
}
