using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ResourceMindAI.Application.Abstractions.Repositories;
using ResourceMindAI.Domain.Entities;

namespace ResourceMindAI.Infrastructure.Persistence.Repositories;

public class AllocationRepository : IAllocationRepository
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<AllocationRepository> _logger;

    public AllocationRepository(AppDbContext dbContext, ILogger<AllocationRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Allocation>> GetAllAsync()
    {
        _logger.LogDebug("Querying all allocations");

        return await _dbContext.Allocations
            .Include(x => x.User)
            .Include(x => x.Project)
                .ThenInclude(x => x.Manager)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }
}
