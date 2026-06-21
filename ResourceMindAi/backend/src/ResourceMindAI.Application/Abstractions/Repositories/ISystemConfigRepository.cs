namespace ResourceMindAI.Application.Abstractions.Repositories;

public interface ISystemConfigRepository
{
    Task<decimal?> GetMaxWeeklyHoursAsync();
    Task<int?> GetSchedulerIntervalHoursAsync(CancellationToken cancellationToken = default);
}
