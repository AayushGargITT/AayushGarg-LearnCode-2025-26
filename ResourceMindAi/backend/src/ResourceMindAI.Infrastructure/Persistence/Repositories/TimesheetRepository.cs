using Microsoft.EntityFrameworkCore;
using ResourceMindAI.Application.Abstractions.Repositories;
using ResourceMindAI.Domain.Entities;
using ResourceMindAI.Domain.Enums;

namespace ResourceMindAI.Infrastructure.Persistence.Repositories;

public class TimesheetRepository : ITimesheetRepository
{
    private readonly AppDbContext _dbContext;

    public TimesheetRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<User?> GetEmployeeUserAsync(Guid userId)
    {
        return _dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(user =>
                user.Id == userId
                && user.IsActive
                && user.Role == Role.Resource);
    }

    public async Task<IReadOnlyList<Allocation>> GetAllocationsAsync(Guid employeeId)
    {
        return await _dbContext.Allocations
            .AsNoTracking()
            .Include(allocation => allocation.Project)
            .Where(allocation => allocation.UserId == employeeId)
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
                allocation.UserId == employeeId
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
            .Where(timesheet => timesheet.UserId == employeeId)
            .OrderByDescending(timesheet => timesheet.WeekStartDate)
            .ToListAsync();
    }

    public Task<bool> HasTimesheetForWeekAsync(Guid employeeId, DateTime weekStart)
    {
        return _dbContext.Timesheets.AnyAsync(timesheet =>
            timesheet.UserId == employeeId
            && timesheet.WeekStartDate == weekStart);
    }

    public async Task AddRangeAsync(IReadOnlyCollection<Timesheet> timesheets)
    {
        await _dbContext.Timesheets.AddRangeAsync(timesheets);
        await _dbContext.SaveChangesAsync();
    }
}
