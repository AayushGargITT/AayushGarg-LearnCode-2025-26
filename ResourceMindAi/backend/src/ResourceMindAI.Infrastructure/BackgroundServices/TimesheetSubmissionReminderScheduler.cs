using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ResourceMindAI.Application.Abstractions.Services;

namespace ResourceMindAI.Infrastructure.BackgroundServices;

public sealed class TimesheetSubmissionReminderScheduler : BackgroundService
{
    private const int DefaultInitialDelaySeconds = 20;
    private const int DefaultIntervalHours = 24;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TimesheetSubmissionReminderScheduler> _logger;

    public TimesheetSubmissionReminderScheduler(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<TimesheetSubmissionReminderScheduler> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Timesheet submission reminder scheduler started");

        if (!await WaitAsync(GetInitialDelay(), stoppingToken))
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            if (IsEscalationDay(now.DayOfWeek))
            {
                await ExecuteEscalationAsync(now, stoppingToken);
            }
            else
            {
                _logger.LogInformation(
                    "Timesheet submission reminder scheduler skipped on {DayOfWeek}",
                    now.DayOfWeek);
            }

            if (!await WaitAsync(GetInterval(), stoppingToken))
            {
                return;
            }
        }
    }

    private async Task ExecuteEscalationAsync(
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var service = scope.ServiceProvider
                .GetRequiredService<ITimesheetSubmissionEscalationService>();

            _logger.LogInformation(
                "Timesheet submission reminder scheduler execution started for {DayOfWeek}",
                utcNow.DayOfWeek);
            await service.ProcessAsync(utcNow, cancellationToken);
            _logger.LogInformation(
                "Timesheet submission reminder scheduler execution completed for {DayOfWeek}",
                utcNow.DayOfWeek);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Timesheet submission reminder scheduler execution failed");
        }
    }

    private TimeSpan GetInitialDelay()
    {
        var seconds = _configuration.GetValue<int?>(
            "Notifications:TimesheetSubmission:SchedulerInitialDelaySeconds");
        return TimeSpan.FromSeconds(
            seconds is > 0 ? seconds.Value : DefaultInitialDelaySeconds);
    }

    private TimeSpan GetInterval()
    {
        var hours = _configuration.GetValue<int?>(
            "Notifications:TimesheetSubmission:SchedulerIntervalHours");
        return TimeSpan.FromHours(
            hours is > 0 ? hours.Value : DefaultIntervalHours);
    }

    private static bool IsEscalationDay(DayOfWeek day)
    {
        return day is DayOfWeek.Monday
            or DayOfWeek.Tuesday
            or DayOfWeek.Wednesday;
    }

    private static async Task<bool> WaitAsync(
        TimeSpan delay,
        CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(delay, cancellationToken);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }
}
