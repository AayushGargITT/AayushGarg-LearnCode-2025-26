using Microsoft.EntityFrameworkCore;
using ResourceMindAI.Application.Abstractions.Repositories;
using ResourceMindAI.Domain.Entities;

namespace ResourceMindAI.Infrastructure.Persistence.Repositories;

public class TimesheetRepository : ITimesheetRepository
{
    private readonly AppDbContext _dbContext;

    public TimesheetRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<ResourceProfile?> GetResourceProfileByUserIdAsync(Guid userId)
    {
        return _dbContext.ResourceProfiles
            .AsNoTracking()
            .Include(profile => profile.User)
            .SingleOrDefaultAsync(profile => profile.Id == userId && profile.User.IsActive);
    }

    public async Task<IReadOnlyList<Allocation>> GetAllocationsAsync(Guid employeeId)
    {
        return await _dbContext.Allocations
            .AsNoTracking()
            .Include(allocation => allocation.Project)
            .Where(allocation => allocation.ResourceProfileId == employeeId)
            .OrderByDescending(allocation => allocation.FromDate)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Allocation>> GetAllocationsForWeekAsync(
        Guid employeeId,
        DateTime weekStart,
        DateTime weekEnd)
    {
        return await _dbContext.Allocations
            .AsNoTracking()
            .Include(allocation => allocation.Project)
            .Where(allocation =>
                allocation.ResourceProfileId == employeeId
                && allocation.FromDate <= weekEnd
                && allocation.ToDate >= weekStart)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Timesheet>> GetTimesheetsAsync(Guid employeeId)
    {
        return await _dbContext.Timesheets
            .AsNoTracking()
            .Include(timesheet => timesheet.Project)
            .Include(timesheet => timesheet.ActivityTags)
            .Where(timesheet => timesheet.ResourceProfileId == employeeId)
            .OrderByDescending(timesheet => timesheet.WeekStartDate)
            .ToListAsync();
    }

    public Task<bool> HasTimesheetForWeekAsync(Guid employeeId, DateTime weekStart)
    {
        return _dbContext.Timesheets.AnyAsync(timesheet =>
            timesheet.ResourceProfileId == employeeId
            && timesheet.WeekStartDate == weekStart);
    }

    public async Task AddRangeAsync(IReadOnlyCollection<Timesheet> timesheets)
    {
        await _dbContext.Timesheets.AddRangeAsync(timesheets);
        await _dbContext.SaveChangesAsync();
    }
}
