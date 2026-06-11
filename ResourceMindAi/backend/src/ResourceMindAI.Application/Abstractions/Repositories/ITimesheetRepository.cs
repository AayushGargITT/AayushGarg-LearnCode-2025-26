using ResourceMindAI.Domain.Entities;

namespace ResourceMindAI.Application.Abstractions.Repositories;

public interface ITimesheetRepository
{
    Task<User?> GetEmployeeUserAsync(Guid userId);
    Task<IReadOnlyList<Allocation>> GetAllocationsAsync(Guid employeeId);
    Task<IReadOnlyList<Allocation>> GetAllocationsForWeekAsync(
        Guid employeeId,
        DateTime weekStart,
        DateTime weekEnd);
    Task<IReadOnlyList<Timesheet>> GetTimesheetsAsync(Guid employeeId);
    Task<bool> HasTimesheetForWeekAsync(Guid employeeId, DateTime weekStart);
    Task AddRangeAsync(IReadOnlyCollection<Timesheet> timesheets);
}
