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
        var employees = await _schedulerRepository.GetActiveEmployeesAsync(
            evaluationDate,
            cancellationToken);
        var allocatedCount = 0;
        var benchCount = 0;

        foreach (var employee in employees)
        {
            try
            {
                var status = employee.Allocations.Count > 0
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
                    employee.Id,
                    evaluationDate);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Resource status computation failed for user {UserId}",
                    employee.Id);
            }
        }

        _logger.LogInformation(
            "Resource status computation completed for {EvaluationDate}: {AllocatedCount} allocated, {BenchCount} bench",
            evaluationDate,
            allocatedCount,
            benchCount);
    }

}
