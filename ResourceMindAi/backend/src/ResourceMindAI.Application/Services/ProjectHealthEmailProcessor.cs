using Microsoft.Extensions.Logging;
using ResourceMindAI.Application.Abstractions.Repositories;
using ResourceMindAI.Application.Abstractions.Services;
using ResourceMindAI.Application.DTOs.Notifications;

namespace ResourceMindAI.Application.Services;

public sealed class ProjectHealthEmailProcessor : IProjectHealthEmailProcessor
{
    private readonly ISchedulerRepository _schedulerRepository;
    private readonly IProjectHealthNotificationService _notificationService;
    private readonly ILogger<ProjectHealthEmailProcessor> _logger;

    public ProjectHealthEmailProcessor(
        ISchedulerRepository schedulerRepository,
        IProjectHealthNotificationService notificationService,
        ILogger<ProjectHealthEmailProcessor> logger)
    {
        _schedulerRepository = schedulerRepository;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        var projects = await _schedulerRepository
            .GetProjectHealthNotificationCandidatesAsync(cancellationToken);

        foreach (var project in projects)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!ProjectRiskSummarySerializer.TryDeserialize(
                    project.RiskFlagsJson,
                    out var summary))
            {
                _logger.LogWarning(
                    "Project health email skipped because saved risk JSON is invalid for project {ProjectId}",
                    project.Id);
                continue;
            }

            if (summary!.OverallHealth is not ("ATTENTION" or "AT_RISK"))
            {
                continue;
            }

            try
            {
                await _notificationService.NotifyAsync(
                    new ProjectHealthNotificationRequestDto
                    {
                        ProjectId = project.Id,
                        ProjectName = project.Name,
                        ManagerId = project.ManagerId,
                        ManagerName = project.Manager.FullName,
                        ManagerEmail = project.Manager.Email,
                        RiskSummary = summary
                    },
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
                    "Project health email processing failed for project {ProjectId}; continuing",
                    project.Id);
            }
        }
    }
}
