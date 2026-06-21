using ResourceMindAI.Domain.Entities;

namespace ResourceMindAI.Application.Abstractions.Repositories;

public interface ITimesheetSubmissionIssueRepository
{
    Task<IReadOnlyList<User>> GetActiveResourcesWithManagersAsync(
        CancellationToken cancellationToken);

    Task<TimesheetSubmissionIssue?> GetAsync(
        Guid resourceUserId,
        DateTime weekStartDate,
        CancellationToken cancellationToken);

    Task<bool> HasSubmittedTimesheetAsync(
        Guid resourceUserId,
        DateTime weekStartDate,
        CancellationToken cancellationToken);

    Task<bool> IsFrozenAsync(
        Guid resourceUserId,
        DateTime weekStartDate,
        CancellationToken cancellationToken = default);

    Task<User?> GetResourceWithManagerAsync(
        Guid resourceUserId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<TimesheetSubmissionIssue>> GetFrozenForManagerAsync(
        Guid managerUserId,
        CancellationToken cancellationToken);

    Task AddAsync(
        TimesheetSubmissionIssue issue,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
