using Microsoft.Extensions.Logging;
using ResourceMindAI.Application.Abstractions.Services;
using ResourceMindAI.Domain.Entities;

namespace ResourceMindAI.Infrastructure.ExternalServices.Email;

public sealed class TimesheetSubmissionNotificationService
    : ITimesheetSubmissionNotificationService
{
    private readonly IEmailService _emailService;
    private readonly ILogger<TimesheetSubmissionNotificationService> _logger;

    public TimesheetSubmissionNotificationService(
        IEmailService emailService,
        ILogger<TimesheetSubmissionNotificationService> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public Task SendFirstReminderAsync(
        User Resource,
        DateTime weekStartDate,
        CancellationToken cancellationToken)
    {
        return _emailService.SendAsync(
            TimesheetSubmissionEmailBuilder.FirstReminder(Resource, weekStartDate),
            cancellationToken);
    }

    public Task SendSecondReminderAsync(
        User Resource,
        DateTime weekStartDate,
        CancellationToken cancellationToken)
    {
        return _emailService.SendAsync(
            TimesheetSubmissionEmailBuilder.SecondReminder(Resource, weekStartDate),
            cancellationToken);
    }

    public async Task SendFrozenEscalationAsync(
        User Resource,
        User? manager,
        DateTime weekStartDate,
        CancellationToken cancellationToken)
    {
        await TrySendAsync(
            TimesheetSubmissionEmailBuilder.Frozen(Resource, Resource, weekStartDate),
            Resource.Id,
            cancellationToken);

        if (manager is null)
        {
            _logger.LogWarning(
                "Timesheet frozen escalation has no manager recipient for Resource {ResourceUserId}",
                Resource.Id);
            return;
        }

        await TrySendAsync(
            TimesheetSubmissionEmailBuilder.Frozen(manager, Resource, weekStartDate),
            manager.Id,
            cancellationToken);
    }

    private async Task TrySendAsync(
        ResourceMindAI.Application.DTOs.Notifications.EmailMessageDto message,
        Guid recipientUserId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _emailService.SendAsync(message, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Timesheet frozen escalation email failed for recipient {RecipientUserId}",
                recipientUserId);
        }
    }
}
