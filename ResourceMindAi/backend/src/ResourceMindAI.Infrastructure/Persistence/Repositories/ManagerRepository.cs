using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ResourceMindAI.Application.Abstractions.Repositories;
using ResourceMindAI.Domain.Entities;
using ResourceMindAI.Domain.Enums;

namespace ResourceMindAI.Infrastructure.Persistence.Repositories;

public class ManagerRepository : IManagerRepository
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<ManagerRepository> _logger;

    public ManagerRepository(AppDbContext dbContext, ILogger<ManagerRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ResourceProfile>> GetTeamEmployeesAsync(Guid managerId)
    {
        _logger.LogDebug("Querying team employees for manager {ManagerId}", managerId);

        return await ResourceProfileGraph()
            .Where(x => x.ManagerId == managerId && x.User.IsActive && x.User.Role == Role.Resource)
            .OrderBy(x => x.User.FullName)
            .ToListAsync();
    }

    public async Task<ResourceProfile?> GetTeamEmployeeAsync(Guid managerId, Guid employeeId)
    {
        return await ResourceProfileGraph()
            .FirstOrDefaultAsync(x =>
                x.Id == employeeId
                && x.ManagerId == managerId
                && x.User.IsActive
                && x.User.Role == Role.Resource);
    }

    public async Task<IReadOnlyList<User>> GetOrganizationSearchCandidatesAsync()
    {
        _logger.LogDebug("Querying organization-wide active resource search candidates");

        return await _dbContext.Users
            .Include(user => user.ResourceProfile)
                .ThenInclude(profile => profile!.Skills)
            .Include(user => user.ResourceProfile)
                .ThenInclude(profile => profile!.Manager)
            .Include(user => user.Allocations)
                .ThenInclude(allocation => allocation.Project)
            .Include(user => user.Timesheets)
                .ThenInclude(timesheet => timesheet.ActivityTags)
            .Where(user => user.IsActive && user.Role == Role.Resource)
            .OrderBy(user => user.FullName)
            .AsSplitQuery()
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Project>> GetProjectsAsync(Guid managerId)
    {
        return await ProjectGraph()
            .Where(x => x.ManagerId == managerId)
            .OrderBy(x => x.EndDate)
            .ToListAsync();
    }

    public async Task<Project?> GetProjectAsync(Guid managerId, Guid projectId)
    {
        return await ProjectGraph()
            .FirstOrDefaultAsync(x => x.Id == projectId && x.ManagerId == managerId);
    }

    public async Task<Project?> GetProjectForRiskSummaryAsync(Guid managerId, Guid projectId)
    {
        return await _dbContext.Projects
            .Include(x => x.Manager)
            .Include(x => x.Milestones)
            .Include(x => x.Allocations)
                .ThenInclude(x => x.User)
            .Include(x => x.Timesheets)
                .ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == projectId && x.ManagerId == managerId);
    }

    public async Task<IReadOnlyList<Timesheet>> GetSubmittedTimesheetsAsync(Guid managerId)
    {
        return await _dbContext.Timesheets
            .Include(x => x.User)
            .Include(x => x.Project)
            .Include(x => x.ActivityTags)
            .Where(x => x.Project.ManagerId == managerId && x.Status == TimesheetStatus.Submitted)
            .OrderByDescending(x => x.WeekStartDate)
            .ToListAsync();
    }

    public async Task<Allocation?> GetAllocationAsync(Guid managerId, Guid allocationId)
    {
        return await _dbContext.Allocations
            .Include(x => x.User)
            .Include(x => x.Project)
            .FirstOrDefaultAsync(x => x.Id == allocationId && x.Project.ManagerId == managerId);
    }

    public async Task<decimal> GetOverlappingAllocationPercentAsync(
        Guid employeeId,
        DateTime fromDate,
        DateTime toDate,
        Guid? excludedAllocationId = null)
    {
        return await _dbContext.Allocations
            .Where(x =>
                x.UserId == employeeId
                && x.IsActive
                && x.FromDate <= toDate
                && x.ToDate >= fromDate
                && (!excludedAllocationId.HasValue || x.Id != excludedAllocationId.Value))
            .SumAsync(x => x.UtilisationPercent);
    }

    public async Task<Allocation> AddAllocationAsync(Allocation allocation)
    {
        _dbContext.Allocations.Add(allocation);
        await _dbContext.SaveChangesAsync();
        return allocation;
    }

    public Task SaveChangesAsync()
    {
        return _dbContext.SaveChangesAsync();
    }

    private IQueryable<ResourceProfile> ResourceProfileGraph()
    {
        return _dbContext.ResourceProfiles
            .Include(x => x.User)
                .ThenInclude(user => user.Allocations)
                    .ThenInclude(allocation => allocation.Project)
            .Include(x => x.User)
                .ThenInclude(user => user.Timesheets)
                    .ThenInclude(timesheet => timesheet.ActivityTags)
            .Include(x => x.Skills)
            .AsSplitQuery();
    }

    private IQueryable<Project> ProjectGraph()
    {
        return _dbContext.Projects
            .Include(x => x.Milestones)
            .Include(x => x.Allocations)
                .ThenInclude(x => x.User);
    }
}
