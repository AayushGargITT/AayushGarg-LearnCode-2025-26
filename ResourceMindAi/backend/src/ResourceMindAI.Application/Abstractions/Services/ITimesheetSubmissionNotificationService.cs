using ResourceMindAI.Domain.Entities;

namespace ResourceMindAI.Application.Abstractions.Services;

public interface ITimesheetSubmissionNotificationService
{
    Task SendFirstReminderAsync(
        User employee,
        DateTime weekStartDate,
        CancellationToken cancellationToken);

    Task SendSecondReminderAsync(
        User employee,
        DateTime weekStartDate,
        CancellationToken cancellationToken);

    Task SendFrozenEscalationAsync(
        User employee,
        User? manager,
        DateTime weekStartDate,
        CancellationToken cancellationToken);
}
