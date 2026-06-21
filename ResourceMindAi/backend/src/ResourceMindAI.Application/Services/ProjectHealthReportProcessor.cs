using Microsoft.Extensions.Logging;
using ResourceMindAI.Application.Abstractions.Repositories;
using ResourceMindAI.Application.Abstractions.Services;

namespace ResourceMindAI.Application.Services;

public sealed class ProjectHealthReportProcessor : IProjectHealthReportProcessor
{
    private readonly ISchedulerRepository _schedulerRepository;
    private readonly IManagerService _managerService;
    private readonly ILogger<ProjectHealthReportProcessor> _logger;

    public ProjectHealthReportProcessor(
        ISchedulerRepository schedulerRepository,
        IManagerService managerService,
        ILogger<ProjectHealthReportProcessor> logger)
    {
        _schedulerRepository = schedulerRepository;
        _managerService = managerService;
        _logger = logger;
    }

    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        var projects = await _schedulerRepository.GetRiskSummaryProjectsAsync(
            cancellationToken);

        foreach (var project in projects)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await _managerService.GenerateScheduledProjectRiskSummaryAsync(
                    project.ManagerId,
                    project.Id);

                _logger.LogInformation(
                    "Scheduled AI risk summary completed for project {ProjectId}",
                    project.Id);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Scheduled AI risk summary failed for project {ProjectId}; existing summary was preserved",
                    project.Id);
            }
        }
    }
}
