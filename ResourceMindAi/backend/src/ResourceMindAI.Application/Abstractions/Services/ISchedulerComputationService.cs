namespace ResourceMindAI.Application.Abstractions.Services;

public interface ISchedulerComputationService
{
    Task ExecuteAsync(CancellationToken cancellationToken);
}
