using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ResourceMindAI.Application.Abstractions.Repositories;
using ResourceMindAI.Application.Abstractions.Services;
using ResourceMindAI.Application.Constants;
using ResourceMindAI.Application.DTOs.Notifications;
using ResourceMindAI.Domain.Entities;
using ResourceMindAI.Domain.Exceptions;

namespace ResourceMindAI.Infrastructure.ExternalServices.Email;

public sealed class ProjectHealthNotificationService : IProjectHealthNotificationService
{
    private const int DefaultThrottleHours = 24;

    private readonly IEmailService _emailService;
    private readonly INotificationLogRepository _notificationLogRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ProjectHealthNotificationService> _logger;

    public ProjectHealthNotificationService(
        IEmailService emailService,
        INotificationLogRepository notificationLogRepository,
        IConfiguration configuration,
        ILogger<ProjectHealthNotificationService> logger)
    {
        _emailService = emailService;
        _notificationLogRepository = notificationLogRepository;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ProjectHealthNotificationResultDto> NotifyAsync(
        ProjectHealthNotificationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!IsUnhealthy(request.RiskSummary.OverallHealth))
        {
            return new ProjectHealthNotificationResultDto();
        }

        var now = DateTime.UtcNow;
        var latestSuccessful = await _notificationLogRepository.GetLatestSuccessfulAsync(
            request.ProjectId,
            request.ManagerId,
            NotificationTypes.ProjectHealthAlert,
            cancellationToken);
        if (IsThrottled(latestSuccessful, now))
        {
            _logger.LogInformation(
                "Project health email notification skipped due to throttling for project {ProjectId}",
                request.ProjectId);
            return new ProjectHealthNotificationResultDto();
        }

        try
        {
            await _emailService.SendAsync(
                ProjectHealthEmailBuilder.Build(request),
                cancellationToken);
            await AddNotificationLogAsync(
                request,
                now,
                isSuccess: true,
                failureReason: null,
                cancellationToken);

            _logger.LogInformation(
                "Project health email notification sent for project {ProjectId} to manager {ManagerId}",
                request.ProjectId,
                request.ManagerId);

            return new ProjectHealthNotificationResultDto
            {
                Sent = true,
                SentAt = now
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            await TryAddFailureLogAsync(request, now, exception, cancellationToken);
            _logger.LogError(
                exception,
                "Project health email notification failed for project {ProjectId}",
                request.ProjectId);
            return new ProjectHealthNotificationResultDto();
        }
    }

    private bool IsThrottled(
        NotificationLog? latestSuccessful,
        DateTime now)
    {
        if (latestSuccessful is null)
        {
            return false;
        }

        var configuredValue = _configuration["Notifications:ProjectHealth:ThrottleHours"];
        var throttleHours = int.TryParse(configuredValue, out var configuredHours)
            && configuredHours > 0
            ? configuredHours
            : DefaultThrottleHours;

        return latestSuccessful.SentAtUtc.AddHours(throttleHours) > now;
    }

    private static bool IsUnhealthy(string health)
    {
        return health is "ATTENTION" or "AT_RISK";
    }

    private Task AddNotificationLogAsync(
        ProjectHealthNotificationRequestDto request,
        DateTime sentAtUtc,
        bool isSuccess,
        string? failureReason,
        CancellationToken cancellationToken)
    {
        return _notificationLogRepository.AddAsync(
            new NotificationLog
            {
                Id = Guid.NewGuid(),
                ProjectId = request.ProjectId,
                RecipientUserId = request.ManagerId,
                RecipientEmail = request.ManagerEmail,
                NotificationType = NotificationTypes.ProjectHealthAlert,
                SentAtUtc = sentAtUtc,
                IsSuccess = isSuccess,
                FailureReason = failureReason
            },
            cancellationToken);
    }

    private async Task TryAddFailureLogAsync(
        ProjectHealthNotificationRequestDto request,
        DateTime sentAtUtc,
        Exception exception,
        CancellationToken cancellationToken)
    {
        try
        {
            await AddNotificationLogAsync(
                request,
                sentAtUtc,
                isSuccess: false,
                failureReason: GetSafeFailureReason(exception),
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception logException)
        {
            _logger.LogError(
                logException,
                "Failed to persist project health notification failure for project {ProjectId}",
                request.ProjectId);
        }
    }

    private static string GetSafeFailureReason(Exception exception)
    {
        var message = exception is ExternalServiceException
            && !string.IsNullOrWhiteSpace(exception.Message)
            ? exception.Message.Trim()
            : "Unexpected email delivery failure.";

        return message.Length <= 500
            ? message
            : message[..500];
    }
}
