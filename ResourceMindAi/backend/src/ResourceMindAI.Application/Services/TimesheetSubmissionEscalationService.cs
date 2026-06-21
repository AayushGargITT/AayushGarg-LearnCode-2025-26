using Microsoft.Extensions.Logging;
using ResourceMindAI.Application.Abstractions.Repositories;
using ResourceMindAI.Application.Abstractions.Services;
using ResourceMindAI.Application.DTOs.Manager;
using ResourceMindAI.Domain.Entities;
using ResourceMindAI.Domain.Enums;
using ResourceMindAI.Domain.Exceptions;

namespace ResourceMindAI.Application.Services;

public sealed class TimesheetSubmissionEscalationService
    : ITimesheetSubmissionEscalationService
{
    private readonly ITimesheetSubmissionIssueRepository _issueRepository;
    private readonly ITimesheetSubmissionNotificationService _notificationService;
    private readonly ILogger<TimesheetSubmissionEscalationService> _logger;

    public TimesheetSubmissionEscalationService(
        ITimesheetSubmissionIssueRepository issueRepository,
        ITimesheetSubmissionNotificationService notificationService,
        ILogger<TimesheetSubmissionEscalationService> logger)
    {
        _issueRepository = issueRepository;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task ProcessAsync(
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var today = utcNow.Date;
        if (!IsEscalationDay(today.DayOfWeek))
        {
            _logger.LogInformation(
                "Timesheet submission escalation skipped for {EvaluationDate}; it is not an escalation day",
                today);
            return;
        }

        var targetWeek = StartOfWeek(today).AddDays(-7);
        var resources = await _issueRepository.GetActiveResourcesWithManagersAsync(
            cancellationToken);

        foreach (var Resource in resources)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await ProcessResourceAsync(
                    Resource,
                    targetWeek,
                    utcNow,
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Timesheet submission escalation failed for Resource {ResourceUserId} and week {WeekStartDate}",
                    Resource.Id,
                    targetWeek);
            }
        }
    }

    public async Task RestoreAccessAsync(
        Guid managerUserId,
        Guid ResourceUserId,
        DateTime weekStartDate,
        CancellationToken cancellationToken = default)
    {
        var normalizedWeek = StartOfWeek(weekStartDate);
        var Resource = await _issueRepository.GetResourceWithManagerAsync(
            ResourceUserId,
            cancellationToken)
            ?? throw new EntityNotFoundException("Active Resource", ResourceUserId);

        if (Resource.ResourceProfile?.ManagerId != managerUserId)
        {
            throw new ForbiddenException(
                "Only the Resource's current manager can restore timesheet submission access.");
        }

        var issue = await _issueRepository.GetAsync(
            ResourceUserId,
            normalizedWeek,
            cancellationToken)
            ?? throw new EntityNotFoundException(
                "A timesheet submission issue was not found for the selected week.");

        if (issue.Status != TimesheetSubmissionIssueStatus.Frozen)
        {
            throw new ValidationException(
                "Timesheet submission access can be restored only for a frozen week.");
        }

        var now = DateTime.UtcNow;
        issue.Status = TimesheetSubmissionIssueStatus.Restored;
        issue.RestoredAtUtc = now;
        issue.RestoredByManagerUserId = managerUserId;
        issue.UpdatedAtUtc = now;
        await _issueRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Timesheet submission access restored by manager {ManagerUserId} for Resource {ResourceUserId} and week {WeekStartDate}",
            managerUserId,
            ResourceUserId,
            normalizedWeek);
    }

    public async Task<IReadOnlyList<FrozenTimesheetSubmissionDto>> GetFrozenAsync(
        Guid managerUserId,
        CancellationToken cancellationToken = default)
    {
        var issues = await _issueRepository.GetFrozenForManagerAsync(
            managerUserId,
            cancellationToken);

        return issues.Select(issue => new FrozenTimesheetSubmissionDto
        {
            ResourceUserId = issue.ResourceUserId,
            ResourceName = issue.ResourceUser.FullName,
            WeekStartDate = issue.WeekStartDate,
            FrozenAtUtc = issue.FrozenAtUtc!.Value
        }).ToList();
    }

    private async Task ProcessResourceAsync(
        User Resource,
        DateTime targetWeek,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        if (await _issueRepository.HasSubmittedTimesheetAsync(
                Resource.Id,
                targetWeek,
                cancellationToken))
        {
            return;
        }

        var issue = await _issueRepository.GetAsync(
            Resource.Id,
            targetWeek,
            cancellationToken);
        if (issue?.Status == TimesheetSubmissionIssueStatus.Restored)
        {
            return;
        }

        issue ??= await CreateIssueAsync(
            Resource,
            targetWeek,
            utcNow,
            cancellationToken);

        switch (utcNow.DayOfWeek)
        {
            case DayOfWeek.Monday:
                await SendFirstReminderAsync(Resource, issue, utcNow, cancellationToken);
                break;
            case DayOfWeek.Tuesday:
                await SendSecondReminderAsync(Resource, issue, utcNow, cancellationToken);
                break;
            case DayOfWeek.Wednesday:
                await FreezeAndEscalateAsync(Resource, issue, utcNow, cancellationToken);
                break;
        }
    }

    private async Task<TimesheetSubmissionIssue> CreateIssueAsync(
        User Resource,
        DateTime targetWeek,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var issue = new TimesheetSubmissionIssue
        {
            Id = Guid.NewGuid(),
            ResourceUserId = Resource.Id,
            ManagerUserId = Resource.ResourceProfile?.ManagerId,
            WeekStartDate = targetWeek,
            Status = TimesheetSubmissionIssueStatus.Missing,
            CreatedAtUtc = utcNow
        };

        await _issueRepository.AddAsync(issue, cancellationToken);
        await _issueRepository.SaveChangesAsync(cancellationToken);
        return issue;
    }

    private async Task SendFirstReminderAsync(
        User Resource,
        TimesheetSubmissionIssue issue,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        if (issue.FirstReminderSentAtUtc.HasValue
            || issue.Status != TimesheetSubmissionIssueStatus.Missing)
        {
            return;
        }

        await _notificationService.SendFirstReminderAsync(
            Resource,
            issue.WeekStartDate,
            cancellationToken);

        issue.Status = TimesheetSubmissionIssueStatus.FirstReminderSent;
        issue.FirstReminderSentAtUtc = utcNow;
        issue.UpdatedAtUtc = utcNow;
        await _issueRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "First missed-timesheet reminder sent to Resource {ResourceUserId} for week {WeekStartDate}",
            Resource.Id,
            issue.WeekStartDate);
    }

    private async Task SendSecondReminderAsync(
        User Resource,
        TimesheetSubmissionIssue issue,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        if (issue.SecondReminderSentAtUtc.HasValue
            || issue.Status is TimesheetSubmissionIssueStatus.SecondReminderSent
                or TimesheetSubmissionIssueStatus.Frozen)
        {
            return;
        }

        await _notificationService.SendSecondReminderAsync(
            Resource,
            issue.WeekStartDate,
            cancellationToken);

        issue.Status = TimesheetSubmissionIssueStatus.SecondReminderSent;
        issue.SecondReminderSentAtUtc = utcNow;
        issue.UpdatedAtUtc = utcNow;
        await _issueRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Second missed-timesheet reminder sent to Resource {ResourceUserId} for week {WeekStartDate}",
            Resource.Id,
            issue.WeekStartDate);
    }

    private async Task FreezeAndEscalateAsync(
        User Resource,
        TimesheetSubmissionIssue issue,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        if (issue.Status == TimesheetSubmissionIssueStatus.Frozen)
        {
            return;
        }

        var manager = Resource.ResourceProfile?.Manager is { IsActive: true } activeManager
            ? activeManager
            : null;
        issue.Status = TimesheetSubmissionIssueStatus.Frozen;
        issue.ManagerUserId = manager?.Id;
        issue.FrozenAtUtc = utcNow;
        issue.UpdatedAtUtc = utcNow;
        await _issueRepository.SaveChangesAsync(cancellationToken);

        _logger.LogWarning(
            "Timesheet submission frozen for Resource {ResourceUserId} and week {WeekStartDate}",
            Resource.Id,
            issue.WeekStartDate);

        await _notificationService.SendFrozenEscalationAsync(
            Resource,
            manager,
            issue.WeekStartDate,
            cancellationToken);
    }

    private static bool IsEscalationDay(DayOfWeek day)
    {
        return day is DayOfWeek.Monday
            or DayOfWeek.Tuesday
            or DayOfWeek.Wednesday;
    }

    private static DateTime StartOfWeek(DateTime date)
    {
        var daysSinceMonday = ((int)date.DayOfWeek + 6) % 7;
        return date.Date.AddDays(-daysSinceMonday);
    }
}
