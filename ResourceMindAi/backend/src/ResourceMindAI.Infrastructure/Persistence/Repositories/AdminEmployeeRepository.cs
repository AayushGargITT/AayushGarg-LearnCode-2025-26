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

    public async Task<IReadOnlyList<ResourceProfile>> GetAllAsync()
    {
        _logger.LogDebug("Querying all employees with user profiles");

        var employees = await _dbContext.ResourceProfiles
            .Include(x => x.User)
            .Include(x => x.Manager)
            .Include(x => x.Allocations)
            .OrderBy(x => x.User.FullName)
            .ToListAsync();

        _logger.LogDebug("Queried {EmployeeCount} employees", employees.Count);
        return employees;
    }

    public async Task<ResourceProfile?> GetByIdAsync(Guid userId)
    {
        _logger.LogDebug("Querying employee by user {UserId}", userId);

        var employee = await _dbContext.ResourceProfiles
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == userId);

        _logger.LogDebug(
            "Employee lookup by user {UserId} returned {Found}",
            userId,
            employee is not null);

        return employee;
    }

    public Task<ResourceProfile?> GetForManagerUpdateAsync(Guid employeeId)
    {
        return _dbContext.ResourceProfiles
            .Include(employee => employee.User)
            .Include(employee => employee.Manager)
            .Include(employee => employee.Allocations.Where(allocation => allocation.IsActive))
                .ThenInclude(allocation => allocation.Project)
            .FirstOrDefaultAsync(employee => employee.Id == employeeId);
    }

    public async Task<IReadOnlyList<Skill>> GetSkillsAsync(Guid employeeId)
    {
        _logger.LogDebug("Querying skills for employee {EmployeeId}", employeeId);

        return await _dbContext.Skills
            .Where(x => x.ResourceProfileId == employeeId)
            .OrderBy(x => x.SkillName)
            .ToListAsync();
    }

    public async Task<Skill?> GetSkillAsync(Guid employeeId, Guid skillId)
    {
        _logger.LogDebug("Querying skill {SkillId} for employee {EmployeeId}", skillId, employeeId);

        return await _dbContext.Skills
            .FirstOrDefaultAsync(x => x.ResourceProfileId == employeeId && x.Id == skillId);
    }

    public async Task<Skill?> GetSkillByNameAsync(Guid employeeId, string skillName)
    {
        _logger.LogDebug("Querying skill {SkillName} for employee {EmployeeId}", skillName, employeeId);

        return await _dbContext.Skills
            .FirstOrDefaultAsync(x => x.ResourceProfileId == employeeId && x.SkillName == skillName);
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

    public async Task SaveManagerUpdateAsync(ResourceProfile resourceProfile)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        _dbContext.ResourceProfiles.Update(resourceProfile);
        await _dbContext.SaveChangesAsync();

        await transaction.CommitAsync();
    }
}
