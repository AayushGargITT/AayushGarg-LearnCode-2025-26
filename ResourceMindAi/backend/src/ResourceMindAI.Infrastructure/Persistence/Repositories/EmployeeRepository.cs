using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ResourceMindAI.Application.Abstractions.Repositories;
using ResourceMindAI.Domain.Entities;

namespace ResourceMindAI.Infrastructure.Persistence.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<EmployeeRepository> _logger;

    public EmployeeRepository(AppDbContext dbContext, ILogger<EmployeeRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Employee>> GetAllAsync()
    {
        _logger.LogDebug("Querying all employees with user profiles");

        var employees = await _dbContext.Employees
            .Include(x => x.User)
            .OrderBy(x => x.User.FullName)
            .ToListAsync();

        _logger.LogDebug("Queried {EmployeeCount} employees", employees.Count);
        return employees;
    }

    public async Task<Employee?> GetByIdAsync(Guid userId)
    {
        _logger.LogDebug("Querying employee by user {UserId}", userId);

        var employee = await _dbContext.Employees
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.UserId == userId);

        _logger.LogDebug(
            "Employee lookup by user {UserId} returned {Found}",
            userId,
            employee is not null);

        return employee;
    }
}
