using ResourceMindAI.Domain.Entities;

namespace ResourceMindAI.Application.Abstractions.Repositories;

public interface ISchedulerRepository
{
    Task<IReadOnlyList<User>> GetActiveEmployeesAsync(
        DateTime evaluationDate,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Project>> GetRiskSummaryProjectsAsync(
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Project>> GetProjectHealthNotificationCandidatesAsync(
        CancellationToken cancellationToken);
}
