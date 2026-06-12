using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ResourceMindAI.Application.Abstractions.Services;

namespace ResourceMindAI.Infrastructure.BackgroundServices;

public sealed class ProjectHealthEmailSchedulerService : BackgroundService
{
    private const int DefaultIntervalMinutes = 60;
    private const int DefaultInitialDelaySeconds = 30;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ProjectHealthEmailSchedulerService> _logger;

    public ProjectHealthEmailSchedulerService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<ProjectHealthEmailSchedulerService> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Project health email scheduler started");

        if (!await WaitAsync(GetInitialDelay(), stoppingToken))
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var processor = scope.ServiceProvider
                    .GetRequiredService<IProjectHealthEmailProcessor>();

                _logger.LogInformation("Project health email scheduler execution started");
                await processor.ProcessAsync(stoppingToken);
                _logger.LogInformation("Project health email scheduler execution completed");
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Project health email scheduler execution failed");
            }

            if (!await WaitAsync(GetInterval(), stoppingToken))
            {
                return;
            }
        }
    }

    private TimeSpan GetInitialDelay()
    {
        var seconds = _configuration.GetValue<int?>(
            "Notifications:ProjectHealth:EmailSchedulerInitialDelaySeconds");
        return TimeSpan.FromSeconds(
            seconds is > 0 ? seconds.Value : DefaultInitialDelaySeconds);
    }

    private TimeSpan GetInterval()
    {
        var minutes = _configuration.GetValue<int?>(
            "Notifications:ProjectHealth:EmailSchedulerIntervalMinutes");
        return TimeSpan.FromMinutes(
            minutes is > 0 ? minutes.Value : DefaultIntervalMinutes);
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
