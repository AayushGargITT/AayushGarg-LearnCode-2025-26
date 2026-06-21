using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ResourceMindAI.Application.Abstractions.Repositories;
using ResourceMindAI.Domain.Entities;
using ResourceMindAI.Domain.Enums;

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

    public async Task<IReadOnlyList<User>> GetAllAsync()
    {
        _logger.LogDebug("Querying all resource-related users");

        var employees = await _dbContext.Users
            .Include(user => user.ResourceProfile)
                .ThenInclude(profile => profile!.Manager)
            .Include(user => user.Allocations)
            .Where(user => user.Role == Role.Manager || user.Role == Role.Resource)
            .OrderBy(user => user.FullName)
            .ToListAsync();

        _logger.LogDebug("Queried {EmployeeCount} employees", employees.Count);
        return employees;
    }

    public async Task<User?> GetByIdAsync(Guid userId)
    {
        _logger.LogDebug("Querying employee by user {UserId}", userId);

        var employee = await _dbContext.Users
            .Include(user => user.ResourceProfile)
                .ThenInclude(profile => profile!.Manager)
            .Include(user => user.Allocations)
            .FirstOrDefaultAsync(user =>
                user.Id == userId
                && (user.Role == Role.Manager || user.Role == Role.Resource));

        _logger.LogDebug(
            "Employee lookup by user {UserId} returned {Found}",
            userId,
            employee is not null);

        return employee;
    }

    public Task<User?> GetForManagerUpdateAsync(Guid employeeId)
    {
        return _dbContext.Users
            .Include(user => user.ResourceProfile)
                .ThenInclude(profile => profile!.Manager)
            .Include(user => user.Allocations.Where(allocation => allocation.IsActive))
                .ThenInclude(allocation => allocation.Project)
            .FirstOrDefaultAsync(user => user.Id == employeeId);
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

    public async Task SaveResourceProfileAsync(ResourceProfile resourceProfile)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        if (await _dbContext.ResourceProfiles.AnyAsync(profile => profile.Id == resourceProfile.Id))
        {
            _dbContext.ResourceProfiles.Update(resourceProfile);
        }
        else
        {
            _dbContext.ResourceProfiles.Add(resourceProfile);
        }

        await _dbContext.SaveChangesAsync();

        await transaction.CommitAsync();
    }
}
