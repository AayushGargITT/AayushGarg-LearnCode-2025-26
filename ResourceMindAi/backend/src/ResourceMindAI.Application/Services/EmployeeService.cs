using Microsoft.Extensions.Logging;
using ResourceMindAI.Application.Abstractions.Repositories;
using ResourceMindAI.Application.Abstractions.Services;
using ResourceMindAI.Application.DTOs.Auth;
using ResourceMindAI.Application.DTOs.Employee;
using ResourceMindAI.Domain.Entities;
using ResourceMindAI.Domain.Exceptions;

namespace ResourceMindAI.Application.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ILogger<EmployeeService> _logger;

    public EmployeeService(IEmployeeRepository employeeRepository, ILogger<EmployeeService> logger)
    {
        _employeeRepository = employeeRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<EmployeeListDto>> GetAllAsync()
    {
        _logger.LogInformation("Loading employees from repository");
        var employees = await _employeeRepository.GetAllAsync();
        _logger.LogInformation("Loaded {EmployeeCount} employees from repository", employees.Count);

        return employees.Select(x => new EmployeeListDto
        {
            Id = x.Id,
            UserId = x.User.Id,
            FullName = x.User.FullName,
            Email = x.User.Email,
            Role = x.User.Role,
            AllocationStatus = x.Allocations.Any(allocation => allocation.IsActive) ? "Allocated" : "Bench",
            Department = x.Department,
            Designation = x.Designation,
            IsActive = x.IsActive,
        }).ToList();
    }

    public async Task<UserProfileDto> GetByIdAsync(Guid userId)
    {
        _logger.LogInformation("Loading employee profile for user {UserId}", userId);
        var employee = await _employeeRepository.GetByIdAsync(userId);

        if (employee is null)
        {
            _logger.LogWarning("Employee profile was not found for user {UserId}", userId);
            throw new EntityNotFoundException("Employee", userId);
        }

        _logger.LogInformation("Loaded employee profile {EmployeeId} for user {UserId}", employee.Id, userId);
        return new UserProfileDto
        {
            Id = employee.User.Id,
            FullName = employee.User.FullName,
            Email = employee.User.Email,
            Username = employee.User.Username,
            Role = employee.User.Role,
            IsActive = employee.User.IsActive,
            ForcePasswordChange = employee.User.ForcePasswordChange,
            EmployeeId = employee.Id,
            Department = employee.Department,
            Designation = employee.Designation,
        };
    }

    public async Task<IReadOnlyList<EmployeeSkillDto>> GetSkillsAsync(Guid employeeId)
    {
        await EnsureEmployeeExistsAsync(employeeId);

        var skills = await _employeeRepository.GetSkillsAsync(employeeId);
        return skills.Select(MapSkill).ToList();
    }

    public async Task<EmployeeSkillDto> AddSkillAsync(Guid employeeId, CreateEmployeeSkillDto request)
    {
        await EnsureEmployeeExistsAsync(employeeId);

        var skillName = request.SkillName.Trim();
        var existingSkill = await _employeeRepository.GetSkillByNameAsync(employeeId, skillName);
        if (existingSkill is not null)
        {
            throw new ConflictException($"Skill '{skillName}' already exists for this employee.");
        }

        var skill = new Skill
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            SkillName = skillName,
            Category = request.Category!.Value,
            Proficiency = request.Proficiency!.Value,
            AddedAt = DateTime.UtcNow,
        };

        var createdSkill = await _employeeRepository.AddSkillAsync(skill);
        return MapSkill(createdSkill);
    }

    public async Task<EmployeeSkillDto> UpdateSkillProficiencyAsync(
        Guid employeeId,
        Guid skillId,
        UpdateEmployeeSkillProficiencyDto request)
    {
        await EnsureEmployeeExistsAsync(employeeId);

        var skill = await _employeeRepository.GetSkillAsync(employeeId, skillId);
        if (skill is null)
        {
            throw new EntityNotFoundException("Skill", skillId);
        }

        skill.Proficiency = request.Proficiency!.Value;

        var updatedSkill = await _employeeRepository.UpdateSkillAsync(skill);
        return MapSkill(updatedSkill);
    }

    private async Task EnsureEmployeeExistsAsync(Guid employeeId)
    {
        var employee = await _employeeRepository.GetByEmployeeIdAsync(employeeId);
        if (employee is null)
        {
            throw new EntityNotFoundException("Employee", employeeId);
        }
    }

    private static EmployeeSkillDto MapSkill(Skill skill)
    {
        return new EmployeeSkillDto
        {
            Id = skill.Id,
            EmployeeId = skill.EmployeeId,
            SkillName = skill.SkillName,
            Category = skill.Category,
            Proficiency = skill.Proficiency,
            AddedAt = skill.AddedAt,
        };
    }
}
