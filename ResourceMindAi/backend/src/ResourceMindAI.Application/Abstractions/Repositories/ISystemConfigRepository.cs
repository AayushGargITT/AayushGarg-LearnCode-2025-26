namespace ResourceMindAI.Application.Abstractions.Repositories;

public interface ISystemConfigRepository
{
    Task<decimal?> GetMaxWeeklyHoursAsync();
}
