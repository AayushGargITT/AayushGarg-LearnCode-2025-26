using ResourceMindAI.Domain.Entities;

namespace ResourceMindAI.Application.Abstractions.Services;

public interface ITimesheetSubmissionNotificationService
{
    Task SendFirstReminderAsync(
        User Resource,
        DateTime weekStartDate,
        CancellationToken cancellationToken);

    Task SendSecondReminderAsync(
        User Resource,
        DateTime weekStartDate,
        CancellationToken cancellationToken);

    Task SendFrozenEscalationAsync(
        User Resource,
        User? manager,
        DateTime weekStartDate,
        CancellationToken cancellationToken);
}
