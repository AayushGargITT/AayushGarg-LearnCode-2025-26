using ResourceMindAI.Application.DTOs.Manager;

namespace ResourceMindAI.Application.Abstractions.Services;
public interface ITimesheetSubmissionEscalationService
{
    Task ProcessAsync(DateTime utcNow, CancellationToken cancellationToken);

    Task RestoreAccessAsync(
        Guid managerUserId,
        Guid ResourceUserId,
        DateTime weekStartDate,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FrozenTimesheetSubmissionDto>> GetFrozenAsync(
        Guid managerUserId,
        CancellationToken cancellationToken = default);
}
