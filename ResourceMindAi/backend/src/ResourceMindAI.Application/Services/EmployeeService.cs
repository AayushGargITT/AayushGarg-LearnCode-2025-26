using ResourceMindAI.Application.Abstractions.Repositories;
using ResourceMindAI.Application.DTOs.Auth;

namespace ResourceMindAI.Application.Services;

public class EmployeeService
{
    private readonly IEmployeeRepository _employeeRepository;

    public EmployeeService(IEmployeeRepository employeeRepository)
    {
        _employeeRepository = employeeRepository;
    }

    public async Task<IReadOnlyList<UserProfileDto>> GetAllAsync()
    {
        var employees = await _employeeRepository.GetAllAsync();

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

    public async Task<UserProfileDto?> GetByIdAsync(Guid userId)
    {
        var employee = await _employeeRepository.GetByIdAsync(userId);
        if (employee is null)
        {
            return null;
        }

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
