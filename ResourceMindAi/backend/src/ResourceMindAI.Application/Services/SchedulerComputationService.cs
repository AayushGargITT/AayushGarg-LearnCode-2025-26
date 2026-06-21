using Microsoft.Extensions.Logging;
using ResourceMindAI.Application.Abstractions.Repositories;
using ResourceMindAI.Application.Abstractions.Services;
using ResourceMindAI.Domain.Enums;

namespace ResourceMindAI.Application.Services;

public class SchedulerComputationService : ISchedulerComputationService
{
    private readonly ISchedulerRepository _schedulerRepository;
    private readonly ILogger<SchedulerComputationService> _logger;

    public SchedulerComputationService(
        ISchedulerRepository schedulerRepository,
        ILogger<SchedulerComputationService> logger)
    {
        _schedulerRepository = schedulerRepository;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var evaluationDate = DateTime.UtcNow.Date;
        await ComputeResourceStatusesAsync(evaluationDate, cancellationToken);
    }

    private async Task ComputeResourceStatusesAsync(
        DateTime evaluationDate,
        CancellationToken cancellationToken)
    {
        var resources = await _schedulerRepository.GetActiveResourcesAsync(
            evaluationDate,
            cancellationToken);
        var allocatedCount = 0;
        var benchCount = 0;

        foreach (var resource in resources)
        {
            try
            {
                var status = resource.Allocations.Count > 0
                    ? ResourceStatus.Allocated
                    : ResourceStatus.Bench;

                if (status == ResourceStatus.Allocated)
                {
                    allocatedCount++;
                }
                else
                {
                    benchCount++;
                }

                _logger.LogDebug(
                    "Computed resource status {ResourceStatus} for user {UserId} on {EvaluationDate}",
                    status,
                    resource.Id,
                    evaluationDate);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Resource status computation failed for user {UserId}",
                    resource.Id);
            }
        }

        _logger.LogInformation(
            "Resource status computation completed for {EvaluationDate}: {AllocatedCount} allocated, {BenchCount} bench",
            evaluationDate,
            allocatedCount,
            benchCount);
    }

}
