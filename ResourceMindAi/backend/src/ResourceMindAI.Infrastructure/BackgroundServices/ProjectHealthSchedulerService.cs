using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ResourceMindAI.Application.Abstractions.Repositories;
using ResourceMindAI.Application.Abstractions.Services;

namespace ResourceMindAI.Infrastructure.BackgroundServices;

public sealed class ProjectHealthSchedulerService : BackgroundService
{
    private const int DefaultIntervalHours = 24;
    private static readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(15);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ProjectHealthSchedulerService> _logger;

    public ProjectHealthSchedulerService(
        IServiceScopeFactory scopeFactory,
        ILogger<ProjectHealthSchedulerService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Project health scheduler started");

        if (!await WaitAsync(InitialDelay, stoppingToken))
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var interval = TimeSpan.FromHours(DefaultIntervalHours);

            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var processor = scope.ServiceProvider
                    .GetRequiredService<IProjectHealthReportProcessor>();
                var systemConfigRepository = scope.ServiceProvider
                    .GetRequiredService<ISystemConfigRepository>();

                _logger.LogInformation("Project health scheduler execution started");
                await processor.ProcessAsync(stoppingToken);
                _logger.LogInformation("Project health scheduler execution completed");

                var configuredInterval = await systemConfigRepository
                    .GetSchedulerIntervalHoursAsync(stoppingToken);
                interval = TimeSpan.FromHours(
                    configuredInterval is > 0
                        ? configuredInterval.Value
                        : DefaultIntervalHours);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Project health scheduler execution failed; retrying after {IntervalHours} hours",
                    interval.TotalHours);
            }

            if (!await WaitAsync(interval, stoppingToken))
            {
                return;
            }
        }
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
