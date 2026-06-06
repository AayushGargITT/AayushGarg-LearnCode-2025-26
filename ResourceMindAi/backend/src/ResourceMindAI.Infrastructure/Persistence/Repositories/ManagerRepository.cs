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

    public async Task<IReadOnlyList<Employee>> GetTeamEmployeesAsync(Guid managerId)
    {
        _logger.LogDebug("Querying team employees for manager {ManagerId}", managerId);

        return await EmployeeGraph()
            .Where(x => x.ManagerId == managerId && x.IsActive && x.User.IsActive && x.User.Role == Role.Employee)
            .OrderBy(x => x.User.FullName)
            .ToListAsync();
    }

    public async Task<Employee?> GetTeamEmployeeAsync(Guid managerId, Guid employeeId)
    {
        return await EmployeeGraph()
            .FirstOrDefaultAsync(x =>
                x.Id == employeeId
                && x.ManagerId == managerId
                && x.IsActive
                && x.User.IsActive
                && x.User.Role == Role.Employee);
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

    public async Task<IReadOnlyList<Timesheet>> GetSubmittedTimesheetsAsync(Guid managerId)
    {
        return await _dbContext.Timesheets
            .Include(x => x.Employee)
                .ThenInclude(x => x.User)
            .Include(x => x.Project)
            .Include(x => x.ActivityTags)
            .Where(x => x.Project.ManagerId == managerId && x.Status == TimesheetStatus.Submitted)
            .OrderByDescending(x => x.WeekStartDate)
            .ToListAsync();
    }

    public async Task<Allocation?> GetAllocationAsync(Guid managerId, Guid allocationId)
    {
        return await _dbContext.Allocations
            .Include(x => x.Employee)
                .ThenInclude(x => x.User)
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
                x.EmployeeId == employeeId
                && x.IsActive
                && x.FromDate <= toDate
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

    private IQueryable<Employee> EmployeeGraph()
    {
        return _dbContext.Employees
            .Include(x => x.User)
            .Include(x => x.Skills)
            .Include(x => x.Allocations)
                .ThenInclude(x => x.Project)
            .Include(x => x.Timesheets)
                .ThenInclude(x => x.ActivityTags);
    }

    private IQueryable<Project> ProjectGraph()
    {
        return _dbContext.Projects
            .Include(x => x.Milestones)
            .Include(x => x.Allocations)
                .ThenInclude(x => x.Employee)
                    .ThenInclude(x => x.User);
    }
}
