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
        var employees = await _issueRepository.GetActiveEmployeesWithManagersAsync(
            cancellationToken);

        foreach (var employee in employees)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await ProcessEmployeeAsync(
                    employee,
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
                    "Timesheet submission escalation failed for employee {EmployeeUserId} and week {WeekStartDate}",
                    employee.Id,
                    targetWeek);
            }
        }
    }

    public async Task RestoreAccessAsync(
        Guid managerUserId,
        Guid employeeUserId,
        DateTime weekStartDate,
        CancellationToken cancellationToken = default)
    {
        var normalizedWeek = StartOfWeek(weekStartDate);
        var employee = await _issueRepository.GetEmployeeWithManagerAsync(
            employeeUserId,
            cancellationToken)
            ?? throw new EntityNotFoundException("Active employee", employeeUserId);

        if (employee.ResourceProfile?.ManagerId != managerUserId)
        {
            throw new ForbiddenException(
                "Only the employee's current manager can restore timesheet submission access.");
        }

        var issue = await _issueRepository.GetAsync(
            employeeUserId,
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
            "Timesheet submission access restored by manager {ManagerUserId} for employee {EmployeeUserId} and week {WeekStartDate}",
            managerUserId,
            employeeUserId,
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
            EmployeeUserId = issue.EmployeeUserId,
            EmployeeName = issue.EmployeeUser.FullName,
            WeekStartDate = issue.WeekStartDate,
            FrozenAtUtc = issue.FrozenAtUtc!.Value
        }).ToList();
    }

    private async Task ProcessEmployeeAsync(
        User employee,
        DateTime targetWeek,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        if (await _issueRepository.HasSubmittedTimesheetAsync(
                employee.Id,
                targetWeek,
                cancellationToken))
        {
            return;
        }

        var issue = await _issueRepository.GetAsync(
            employee.Id,
            targetWeek,
            cancellationToken);
        if (issue?.Status == TimesheetSubmissionIssueStatus.Restored)
        {
            return;
        }

        issue ??= await CreateIssueAsync(
            employee,
            targetWeek,
            utcNow,
            cancellationToken);

        switch (utcNow.DayOfWeek)
        {
            case DayOfWeek.Monday:
                await SendFirstReminderAsync(employee, issue, utcNow, cancellationToken);
                break;
            case DayOfWeek.Tuesday:
                await SendSecondReminderAsync(employee, issue, utcNow, cancellationToken);
                break;
            case DayOfWeek.Wednesday:
                await FreezeAndEscalateAsync(employee, issue, utcNow, cancellationToken);
                break;
        }
    }

    private async Task<TimesheetSubmissionIssue> CreateIssueAsync(
        User employee,
        DateTime targetWeek,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var issue = new TimesheetSubmissionIssue
        {
            Id = Guid.NewGuid(),
            EmployeeUserId = employee.Id,
            ManagerUserId = employee.ResourceProfile?.ManagerId,
            WeekStartDate = targetWeek,
            Status = TimesheetSubmissionIssueStatus.Missing,
            CreatedAtUtc = utcNow
        };

        await _issueRepository.AddAsync(issue, cancellationToken);
        await _issueRepository.SaveChangesAsync(cancellationToken);
        return issue;
    }

    private async Task SendFirstReminderAsync(
        User employee,
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
            employee,
            issue.WeekStartDate,
            cancellationToken);

        issue.Status = TimesheetSubmissionIssueStatus.FirstReminderSent;
        issue.FirstReminderSentAtUtc = utcNow;
        issue.UpdatedAtUtc = utcNow;
        await _issueRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "First missed-timesheet reminder sent to employee {EmployeeUserId} for week {WeekStartDate}",
            employee.Id,
            issue.WeekStartDate);
    }

    private async Task SendSecondReminderAsync(
        User employee,
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
            employee,
            issue.WeekStartDate,
            cancellationToken);

        issue.Status = TimesheetSubmissionIssueStatus.SecondReminderSent;
        issue.SecondReminderSentAtUtc = utcNow;
        issue.UpdatedAtUtc = utcNow;
        await _issueRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Second missed-timesheet reminder sent to employee {EmployeeUserId} for week {WeekStartDate}",
            employee.Id,
            issue.WeekStartDate);
    }

    private async Task FreezeAndEscalateAsync(
        User employee,
        TimesheetSubmissionIssue issue,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        if (issue.Status == TimesheetSubmissionIssueStatus.Frozen)
        {
            return;
        }

        var manager = employee.ResourceProfile?.Manager is { IsActive: true } activeManager
            ? activeManager
            : null;
        issue.Status = TimesheetSubmissionIssueStatus.Frozen;
        issue.ManagerUserId = manager?.Id;
        issue.FrozenAtUtc = utcNow;
        issue.UpdatedAtUtc = utcNow;
        await _issueRepository.SaveChangesAsync(cancellationToken);

        _logger.LogWarning(
            "Timesheet submission frozen for employee {EmployeeUserId} and week {WeekStartDate}",
            employee.Id,
            issue.WeekStartDate);

        await _notificationService.SendFrozenEscalationAsync(
            employee,
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
