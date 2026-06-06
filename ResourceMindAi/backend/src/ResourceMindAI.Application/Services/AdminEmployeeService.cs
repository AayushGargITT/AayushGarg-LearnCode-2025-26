using Microsoft.Extensions.Logging;
using ResourceMindAI.Application.Abstractions.Repositories;
using ResourceMindAI.Application.Abstractions.Services;
using ResourceMindAI.Application.DTOs.Auth;
using ResourceMindAI.Application.DTOs.Employee;
using ResourceMindAI.Domain.Entities;
using ResourceMindAI.Domain.Exceptions;

namespace ResourceMindAI.Application.Services;

public class AdminEmployeeService : IAdminEmployeeService
{
    private readonly IAdminEmployeeRepository _adminEmployeeRepository;
    private readonly ILogger<AdminEmployeeService> _logger;

    public AdminEmployeeService(
        IAdminEmployeeRepository adminEmployeeRepository,
        ILogger<AdminEmployeeService> logger)
    {
        _adminEmployeeRepository = adminEmployeeRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<EmployeeListDto>> GetAllAsync()
    {
        _logger.LogInformation("Loading employees from repository");
        var employees = await _adminEmployeeRepository.GetAllAsync();
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
        var employee = await _adminEmployeeRepository.GetByIdAsync(userId);

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

        var skills = await _adminEmployeeRepository.GetSkillsAsync(employeeId);
        return skills.Select(MapSkill).ToList();
    }

    public async Task<EmployeeSkillDto> AddSkillAsync(Guid employeeId, CreateEmployeeSkillDto request)
    {
        await EnsureEmployeeExistsAsync(employeeId);

        var skillName = request.SkillName.Trim();
        var existingSkill = await _adminEmployeeRepository.GetSkillByNameAsync(employeeId, skillName);
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

        var createdSkill = await _adminEmployeeRepository.AddSkillAsync(skill);
        return MapSkill(createdSkill);
    }

    public async Task<EmployeeSkillDto> UpdateSkillProficiencyAsync(
        Guid employeeId,
        Guid skillId,
        UpdateEmployeeSkillProficiencyDto request)
    {
        await EnsureEmployeeExistsAsync(employeeId);

        var skill = await _adminEmployeeRepository.GetSkillAsync(employeeId, skillId);
        if (skill is null)
        {
            throw new EntityNotFoundException("Skill", skillId);
        }

        skill.Proficiency = request.Proficiency!.Value;

        var updatedSkill = await _adminEmployeeRepository.UpdateSkillAsync(skill);
        return MapSkill(updatedSkill);
    }

    private async Task EnsureEmployeeExistsAsync(Guid employeeId)
    {
        var employee = await _adminEmployeeRepository.GetByEmployeeIdAsync(employeeId);
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
