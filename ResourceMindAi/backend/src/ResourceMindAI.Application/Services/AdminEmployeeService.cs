using Microsoft.Extensions.Logging;
using ResourceMindAI.Application.Abstractions.Repositories;
using ResourceMindAI.Application.Abstractions.Services;
using ResourceMindAI.Application.DTOs.Auth;
using ResourceMindAI.Application.DTOs.Employee;
using ResourceMindAI.Domain.Entities;
using ResourceMindAI.Domain.Enums;
using ResourceMindAI.Domain.Exceptions;

namespace ResourceMindAI.Application.Services;

public class AdminEmployeeService : IAdminEmployeeService
{
    private readonly IAdminEmployeeRepository _adminEmployeeRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<AdminEmployeeService> _logger;

    public AdminEmployeeService(
        IAdminEmployeeRepository adminEmployeeRepository,
        IUserRepository userRepository,
        ILogger<AdminEmployeeService> logger)
    {
        _adminEmployeeRepository = adminEmployeeRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<EmployeeListDto>> GetAllAsync()
    {
        _logger.LogInformation("Loading employees from repository");
        var employees = await _adminEmployeeRepository.GetAllAsync();
        _logger.LogInformation("Loaded {EmployeeCount} employees from repository", employees.Count);

        return employees.Select(MapEmployee).ToList();
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
            Id = employee.Id,
            FullName = employee.FullName,
            Email = employee.Email,
            Username = employee.Username,
            Role = employee.Role,
            IsActive = employee.IsActive,
            ForcePasswordChange = employee.ForcePasswordChange,
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
        var employee = await GetEmployeeAsync(employeeId);
        await EnsureResourceProfileAsync(employee);

        var skillName = request.SkillName.Trim();
        var existingSkill = await _adminEmployeeRepository.GetSkillByNameAsync(employeeId, skillName);
        if (existingSkill is not null)
        {
            throw new ConflictException($"Skill '{skillName}' already exists for this employee.");
        }

        var skill = new Skill
        {
            Id = Guid.NewGuid(),
            ResourceProfileId = employeeId,
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

    public async Task<EmployeeManagerUpdatePreviewDto> GetManagerUpdatePreviewAsync(
        Guid employeeId,
        Guid newManagerId)
    {
        var employee = await GetValidEmployeeForManagerUpdateAsync(employeeId);
        await GetValidNewManagerAsync(newManagerId, employee.ResourceProfile?.ManagerId);

        return new EmployeeManagerUpdatePreviewDto
        {
            EmployeeId = employee.Id,
            CurrentManagerId = employee.ResourceProfile?.ManagerId,
            NewManagerId = newManagerId,
            ActiveProjects = GetActiveProjectNames(employee)
        };
    }

    public async Task<EmployeeManagerUpdateResultDto> UpdateManagerAsync(
        Guid employeeId,
        UpdateEmployeeManagerDto request)
    {
        var employee = await GetValidEmployeeForManagerUpdateAsync(employeeId);
        var newManager = await GetValidNewManagerAsync(
            request.NewManagerId!.Value,
            employee.ResourceProfile?.ManagerId);
        var activeAllocations = employee.Allocations
            .Where(allocation => allocation.IsActive)
            .ToList();
        var endedProjects = activeAllocations
            .Select(allocation => allocation.Project.Name)
            .Distinct()
            .OrderBy(name => name)
            .ToList();
        var today = DateTime.UtcNow.Date;

        foreach (var allocation in activeAllocations)
        {
            allocation.IsActive = false;
            allocation.ToDate = today;
        }

        var resourceProfile = employee.ResourceProfile ?? new ResourceProfile
        {
            Id = employee.Id,
            User = employee
        };
        resourceProfile.ManagerId = newManager.Id;
        resourceProfile.Manager = newManager;
        employee.ResourceProfile = resourceProfile;

        await _adminEmployeeRepository.SaveResourceProfileAsync(resourceProfile);

        return new EmployeeManagerUpdateResultDto
        {
            Employee = MapEmployee(employee),
            EndedProjects = endedProjects,
            Message = endedProjects.Count > 0
                ? $"Manager updated successfully. {endedProjects.Count} active allocation(s) were ended as of today."
                : "Manager updated successfully."
        };
    }

    private async Task EnsureEmployeeExistsAsync(Guid employeeId)
    {
        await GetEmployeeAsync(employeeId);
    }

    private async Task<User> GetEmployeeAsync(Guid employeeId)
    {
        return await _adminEmployeeRepository.GetByIdAsync(employeeId)
            ?? throw new EntityNotFoundException("Employee", employeeId);
    }

    private async Task<User> GetValidEmployeeForManagerUpdateAsync(Guid employeeId)
    {
        var employee = await _adminEmployeeRepository.GetForManagerUpdateAsync(employeeId);
        if (employee is null)
        {
            throw new EntityNotFoundException("Employee", employeeId);
        }

        if (employee.Role != Role.Resource)
        {
            throw new ValidationException("Manager can be updated only for users with Resource role.");
        }

        if (!employee.IsActive)
        {
            throw new ValidationException("Manager can be updated only for an active employee.");
        }

        return employee;
    }

    private async Task<User> GetValidNewManagerAsync(Guid newManagerId, Guid? currentManagerId)
    {
        if (currentManagerId == newManagerId)
        {
            throw new ValidationException("Select a different manager.");
        }

        var manager = await _userRepository.GetByIdAsync(newManagerId);
        if (manager is null)
        {
            throw new EntityNotFoundException("Manager", newManagerId);
        }

        if (manager.Role != Role.Manager || !manager.IsActive)
        {
            throw new ValidationException("Selected manager must be an active manager.");
        }

        return manager;
    }

    private async Task EnsureResourceProfileAsync(User employee)
    {
        if (employee.ResourceProfile is not null)
        {
            return;
        }

        employee.ResourceProfile = new ResourceProfile
        {
            Id = employee.Id,
            User = employee
        };
        await _adminEmployeeRepository.SaveResourceProfileAsync(employee.ResourceProfile);
    }

    private static IReadOnlyList<string> GetActiveProjectNames(User employee)
    {
        return employee.Allocations
            .Where(allocation => allocation.IsActive)
            .Select(allocation => allocation.Project.Name)
            .Distinct()
            .OrderBy(name => name)
            .ToList();
    }

    private static EmployeeSkillDto MapSkill(Skill skill)
    {
        return new EmployeeSkillDto
        {
            Id = skill.Id,
            EmployeeId = skill.ResourceProfileId,
            SkillName = skill.SkillName,
            Category = skill.Category,
            Proficiency = skill.Proficiency,
            AddedAt = skill.AddedAt,
        };
    }

    private static EmployeeListDto MapEmployee(User employee)
    {
        return new EmployeeListDto
        {
            Id = employee.Id,
            UserId = employee.Id,
            FullName = employee.FullName,
            Email = employee.Email,
            Role = employee.Role,
            AllocationStatus = employee.Allocations.Any(IsCurrentAllocation)
                ? ResourceStatus.Allocated
                : ResourceStatus.Bench,
            Department = employee.Department ?? string.Empty,
            Designation = employee.Designation ?? string.Empty,
            IsActive = employee.IsActive,
            ManagerId = employee.ResourceProfile?.ManagerId,
            ManagerName = employee.ResourceProfile?.Manager?.FullName,
        };
    }

    private static bool IsCurrentAllocation(Allocation allocation)
    {
        var today = DateTime.UtcNow.Date;
        return allocation.IsActive
            && allocation.FromDate.Date <= today
            && allocation.ToDate.Date >= today;
    }
}
