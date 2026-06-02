using Microsoft.EntityFrameworkCore;
using ResourceMindAI.Application.Abstractions.Repositories;
using ResourceMindAI.Domain.Entities;

namespace ResourceMindAI.Infrastructure.Persistence.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly AppDbContext _dbContext;

    public EmployeeRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Employee>> GetAllAsync()
    {
        return await _dbContext.Employees
            .Include(x => x.User)
            .OrderBy(x => x.User.FullName)
            .ToListAsync();
    }

    public async Task<Employee?> GetByIdAsync(Guid userId)
    {
        return await _dbContext.Employees
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.UserId == userId);
    }
}
