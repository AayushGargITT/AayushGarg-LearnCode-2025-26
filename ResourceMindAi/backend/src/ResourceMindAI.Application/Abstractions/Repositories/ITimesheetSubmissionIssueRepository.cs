using ResourceMindAI.Domain.Entities;

namespace ResourceMindAI.Application.Abstractions.Repositories;

public interface ITimesheetSubmissionIssueRepository
{
    Task<IReadOnlyList<User>> GetActiveEmployeesWithManagersAsync(
        CancellationToken cancellationToken);

    Task<TimesheetSubmissionIssue?> GetAsync(
        Guid employeeUserId,
        DateTime weekStartDate,
        CancellationToken cancellationToken);

    Task<bool> HasSubmittedTimesheetAsync(
        Guid employeeUserId,
        DateTime weekStartDate,
        CancellationToken cancellationToken);

    Task<bool> IsFrozenAsync(
        Guid employeeUserId,
        DateTime weekStartDate,
        CancellationToken cancellationToken = default);

    Task<User?> GetEmployeeWithManagerAsync(
        Guid employeeUserId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<TimesheetSubmissionIssue>> GetFrozenForManagerAsync(
        Guid managerUserId,
        CancellationToken cancellationToken);

    Task AddAsync(
        TimesheetSubmissionIssue issue,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
