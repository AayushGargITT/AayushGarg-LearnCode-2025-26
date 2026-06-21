using ResourceMindAI.Domain.Entities;

namespace ResourceMindAI.Application.Abstractions.Repositories;

public interface ISchedulerRepository
{
    Task<IReadOnlyList<User>> GetActiveResourcesAsync(
        DateTime evaluationDate,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Project>> GetRiskSummaryProjectsAsync(
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Project>> GetProjectHealthNotificationCandidatesAsync(
        CancellationToken cancellationToken);

    Task<IReadOnlyList<User>> GetActiveResourcesUnderManagerAsync(
        Guid managerId,
        CancellationToken cancellationToken);
}
