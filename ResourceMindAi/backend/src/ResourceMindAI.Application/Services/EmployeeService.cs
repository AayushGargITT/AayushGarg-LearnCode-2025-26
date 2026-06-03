using Microsoft.Extensions.Logging;
using ResourceMindAI.Application.Abstractions.Repositories;
using ResourceMindAI.Application.Abstractions.Services;
using ResourceMindAI.Application.DTOs.Auth;
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

    public async Task<IReadOnlyList<UserProfileDto>> GetAllAsync()
    {
        _logger.LogInformation("Loading employees from repository");
        var employees = await _employeeRepository.GetAllAsync();
        _logger.LogInformation("Loaded {EmployeeCount} employees from repository", employees.Count);

        return employees.Select(x => new UserProfileDto
        {
            Id = x.User.Id,
            FullName = x.User.FullName,
            Email = x.User.Email,
            Username = x.User.Username,
            Role = x.User.Role,
            IsActive = x.User.IsActive,
            ForcePasswordChange = x.User.ForcePasswordChange,
            EmployeeId = x.Id,
            Department = x.Department,
            Designation = x.Designation,
        }).ToList();
    }

    /// <summary>
    /// Retrieves a single employee profile by the associated user ID.
    /// </summary>
    /// <exception cref="EntityNotFoundException">Thrown when no employee is linked to the given user ID.</exception>
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
}
