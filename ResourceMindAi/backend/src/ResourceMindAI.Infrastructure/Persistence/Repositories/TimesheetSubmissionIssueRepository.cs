using Microsoft.EntityFrameworkCore;
using ResourceMindAI.Application.Abstractions.Repositories;
using ResourceMindAI.Domain.Entities;
using ResourceMindAI.Domain.Enums;

namespace ResourceMindAI.Infrastructure.Persistence.Repositories;

public sealed class TimesheetSubmissionIssueRepository
    : ITimesheetSubmissionIssueRepository
{
    private readonly AppDbContext _dbContext;

    public TimesheetSubmissionIssueRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<User>> GetActiveEmployeesWithManagersAsync(
        CancellationToken cancellationToken)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .Include(user => user.ResourceProfile)
                .ThenInclude(profile => profile!.Manager)
            .Where(user => user.IsActive && user.Role == Role.Resource)
            .ToListAsync(cancellationToken);
    }

    public Task<TimesheetSubmissionIssue?> GetAsync(
        Guid employeeUserId,
        DateTime weekStartDate,
        CancellationToken cancellationToken)
    {
        return _dbContext.TimesheetSubmissionIssues
            .SingleOrDefaultAsync(issue =>
                issue.EmployeeUserId == employeeUserId
                && issue.WeekStartDate == weekStartDate,
                cancellationToken);
    }

    public Task<bool> HasSubmittedTimesheetAsync(
        Guid employeeUserId,
        DateTime weekStartDate,
        CancellationToken cancellationToken)
    {
        return _dbContext.Timesheets.AnyAsync(timesheet =>
            timesheet.UserId == employeeUserId
            && timesheet.WeekStartDate == weekStartDate
            && timesheet.Status == TimesheetStatus.Submitted,
            cancellationToken);
    }

    public Task<bool> IsFrozenAsync(
        Guid employeeUserId,
        DateTime weekStartDate,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.TimesheetSubmissionIssues.AnyAsync(issue =>
            issue.EmployeeUserId == employeeUserId
            && issue.WeekStartDate == weekStartDate
            && issue.Status == TimesheetSubmissionIssueStatus.Frozen,
            cancellationToken);
    }

    public Task<User?> GetEmployeeWithManagerAsync(
        Guid employeeUserId,
        CancellationToken cancellationToken)
    {
        return _dbContext.Users
            .Include(user => user.ResourceProfile)
                .ThenInclude(profile => profile!.Manager)
            .SingleOrDefaultAsync(user =>
                user.Id == employeeUserId
                && user.IsActive
                && user.Role == Role.Resource,
                cancellationToken);
    }

    public async Task<IReadOnlyList<TimesheetSubmissionIssue>> GetFrozenForManagerAsync(
        Guid managerUserId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.TimesheetSubmissionIssues
            .AsNoTracking()
            .Include(issue => issue.EmployeeUser)
            .Where(issue =>
                issue.Status == TimesheetSubmissionIssueStatus.Frozen
                && issue.EmployeeUser.ResourceProfile != null
                && issue.EmployeeUser.ResourceProfile.ManagerId == managerUserId)
            .OrderBy(issue => issue.WeekStartDate)
            .ThenBy(issue => issue.EmployeeUser.FullName)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        TimesheetSubmissionIssue issue,
        CancellationToken cancellationToken)
    {
        await _dbContext.TimesheetSubmissionIssues.AddAsync(
            issue,
            cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
