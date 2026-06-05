using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ResourceMindAI.Application.Abstractions.Repositories;
using ResourceMindAI.Domain.Entities;

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
}
