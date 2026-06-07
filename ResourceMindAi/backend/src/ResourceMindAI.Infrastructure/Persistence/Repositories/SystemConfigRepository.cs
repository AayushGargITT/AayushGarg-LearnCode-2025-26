using Microsoft.EntityFrameworkCore;
using ResourceMindAI.Application.Abstractions.Repositories;

namespace ResourceMindAI.Infrastructure.Persistence.Repositories;

public class SystemConfigRepository : ISystemConfigRepository
{
    private readonly AppDbContext _dbContext;

    public SystemConfigRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<decimal?> GetMaxWeeklyHoursAsync()
    {
        return _dbContext.SystemConfigs
            .AsNoTracking()
            .Select(config => (decimal?)config.MaxWeeklyHours)
            .FirstOrDefaultAsync();
    }
}
