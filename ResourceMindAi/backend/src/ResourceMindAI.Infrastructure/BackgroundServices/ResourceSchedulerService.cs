using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ResourceMindAI.Application.Abstractions.Repositories;
using ResourceMindAI.Application.Abstractions.Services;

namespace ResourceMindAI.Infrastructure.BackgroundServices;

public class ResourceSchedulerService : BackgroundService
{
    private const int DefaultIntervalHours = 24;
    private static readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(15);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ResourceSchedulerService> _logger;

    public ResourceSchedulerService(
        IServiceScopeFactory scopeFactory,
        ILogger<ResourceSchedulerService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Resource and project scheduler started");

        try
        {
            await Task.Delay(InitialDelay, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var interval = TimeSpan.FromHours(DefaultIntervalHours);

            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var computationService = scope.ServiceProvider
                    .GetRequiredService<ISchedulerComputationService>();
                var systemConfigRepository = scope.ServiceProvider
                    .GetRequiredService<ISystemConfigRepository>();

                _logger.LogInformation("Resource and project scheduler execution started");
                await computationService.ExecuteAsync(stoppingToken);
                _logger.LogInformation("Resource and project scheduler execution completed");

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
                    "Resource and project scheduler execution failed; retrying after {IntervalHours} hours",
                    interval.TotalHours);
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
        }
    }
}
