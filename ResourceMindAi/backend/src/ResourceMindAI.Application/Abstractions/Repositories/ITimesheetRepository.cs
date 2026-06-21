using ResourceMindAI.Domain.Entities;

namespace ResourceMindAI.Application.Abstractions.Repositories;

public interface ITimesheetRepository
{
    Task<User?> GetResourceUserAsync(Guid userId);
    Task<IReadOnlyList<Allocation>> GetAllocationsAsync(Guid resourceId);
    Task<IReadOnlyList<Allocation>> GetAllocationsForWeekAsync(
        Guid resourceId,
        DateTime weekStart,
        DateTime weekEnd);
    Task<IReadOnlyList<Timesheet>> GetTimesheetsAsync(Guid resourceId);
    Task<bool> HasTimesheetForWeekAsync(Guid resourceId, DateTime weekStart);
    Task AddRangeAsync(IReadOnlyCollection<Timesheet> timesheets);
}
