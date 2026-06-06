using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ResourceMindAI.Application.Abstractions.Repositories;
using ResourceMindAI.Domain.Entities;

namespace ResourceMindAI.Infrastructure.Persistence.Repositories;

public class AdminEmployeeRepository : IAdminEmployeeRepository
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<AdminEmployeeRepository> _logger;

    public AdminEmployeeRepository(
        AppDbContext dbContext,
        ILogger<AdminEmployeeRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Employee>> GetAllAsync()
    {
        _logger.LogDebug("Querying all employees with user profiles");

        var employees = await _dbContext.Employees
            .Include(x => x.User)
            .Include(x => x.Allocations)
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

    public async Task<Employee?> GetByEmployeeIdAsync(Guid employeeId)
    {
        _logger.LogDebug("Querying employee by employee {EmployeeId}", employeeId);

        var employee = await _dbContext.Employees
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == employeeId);

        _logger.LogDebug(
            "Employee lookup by employee {EmployeeId} returned {Found}",
            employeeId,
            employee is not null);

        return employee;
    }

    public async Task<IReadOnlyList<Skill>> GetSkillsAsync(Guid employeeId)
    {
        _logger.LogDebug("Querying skills for employee {EmployeeId}", employeeId);

        return await _dbContext.Skills
            .Where(x => x.EmployeeId == employeeId)
            .OrderBy(x => x.SkillName)
            .ToListAsync();
    }

    public async Task<Skill?> GetSkillAsync(Guid employeeId, Guid skillId)
    {
        _logger.LogDebug("Querying skill {SkillId} for employee {EmployeeId}", skillId, employeeId);

        return await _dbContext.Skills
            .FirstOrDefaultAsync(x => x.EmployeeId == employeeId && x.Id == skillId);
    }

    public async Task<Skill?> GetSkillByNameAsync(Guid employeeId, string skillName)
    {
        _logger.LogDebug("Querying skill {SkillName} for employee {EmployeeId}", skillName, employeeId);

        return await _dbContext.Skills
            .FirstOrDefaultAsync(x => x.EmployeeId == employeeId && x.SkillName == skillName);
    }

    public async Task<Skill> AddSkillAsync(Skill skill)
    {
        _dbContext.Skills.Add(skill);
        await _dbContext.SaveChangesAsync();
        return skill;
    }

    public async Task<Skill> UpdateSkillAsync(Skill skill)
    {
        _dbContext.Skills.Update(skill);
        await _dbContext.SaveChangesAsync();
        return skill;
    }
}
