using Microsoft.Extensions.Logging;
using ResourceMindAI.Application.Abstractions.Repositories;
using ResourceMindAI.Application.Abstractions.Services;
using ResourceMindAI.Application.DTOs.Auth;
using ResourceMindAI.Application.DTOs.Employee;
using ResourceMindAI.Application.DTOs.User;
using ResourceMindAI.Domain.Entities;
using ResourceMindAI.Domain.Enums;
using ResourceMindAI.Domain.Exceptions;

namespace ResourceMindAI.Application.Services;

public class UserService : IUserService
{
    private const string DefaultManagerDepartment = "Management";
    private const string DefaultManagerDesignation = "Manager";

    private readonly IUserRepository _userRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ILogger<UserService> _logger;

    public UserService(IUserRepository userRepository, IEmployeeRepository employeeRepository, ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _employeeRepository=employeeRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<UserProfileDto>> GetAllAsync()
    {
        _logger.LogInformation("Loading users from repository");
        var users = await _userRepository.GetAllAsync();
        _logger.LogInformation("Loaded {UserCount} users from repository", users.Count);

        return users.Select(AuthService.ToProfile).ToList();
    }

    public async Task<IReadOnlyList<UserProfileDto>> GetActiveManagersAsync()
    {
        _logger.LogInformation("Loading active managers from repository");
        var managers = await _userRepository.GetActiveManagersAsync();
        _logger.LogInformation("Loaded {ManagerCount} active managers from repository", managers.Count);

        return managers.Select(AuthService.ToProfile).ToList();
    }

    public async Task<UserProfileDto> GetByIdAsync(Guid id)
    {
        _logger.LogInformation("Loading user {UserId} from repository", id);
        var user = await _userRepository.GetByIdAsync(id);

        if (user is null)
        {
            _logger.LogWarning("User {UserId} was not found", id);
            throw new EntityNotFoundException("User", id);
        }

        _logger.LogInformation("Loaded user {UserId}", user.Id);
        return AuthService.ToProfile(user);
    }

    public async Task<UserProfileDto> CreateAsync(CreateUserDto request)
    {
        var fullName = request.FullName.Trim();
        var username = request.Username.Trim();
        var email = request.Email.Trim();

        _logger.LogInformation("Validating new user {Username} with role {Role}", username, request.Role);

        if (await _userRepository.ExistsByUsernameOrEmailAsync(username, email))
        {
            _logger.LogWarning("User creation rejected: username/email already exists for {Username}", username);
            throw new ConflictException(
                "A user with this username or email already exists.",
                "DUPLICATE_USER");
        }

        var now = DateTime.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            Email = email,
            Username = username,
            PasswordHash = PasswordHasher.Hash(username),
            Role = request.Role!.Value,
            IsActive = true,
            ForcePasswordChange = true,
            CreatedAt = now
        };

        _logger.LogInformation("Persisting new user {UserId} with role {Role}", user.Id, user.Role);
        var createdUser = user.Role == Role.Manager
            ? await CreateManagerWithEmployeeAsync(user, now)
            : await _userRepository.CreateAsync(user);
        _logger.LogInformation("Persisted new user {UserId}", createdUser.Id);

        return AuthService.ToProfile(createdUser);
    }

    public async Task<UserProfileDto> ResetPasswordAsync(Guid id)
    {
        _logger.LogInformation("Reset password requested for user {UserId}", id);

        var user = await _userRepository.GetByIdAsync(id);
        if (user is null)
        {
            _logger.LogWarning("Reset password rejected: user {UserId} was not found", id);
            throw new EntityNotFoundException("User", id);
        }

        user.PasswordHash = PasswordHasher.Hash(user.Username);
        user.ForcePasswordChange = true;
        var updatedUser = await _userRepository.UpdateAsync(user);

        _logger.LogInformation("Password reset completed for user {UserId}", updatedUser.Id);
        return AuthService.ToProfile(updatedUser);
    }

    public async Task<UserProfileDto> ToggleStatusAsync(Guid id)
    {
        _logger.LogInformation("Toggle status requested for user {UserId}", id);

        var user = await _userRepository.GetByIdAsync(id);
        if (user is null)
        {
            _logger.LogWarning("Toggle status rejected: user {UserId} was not found", id);
            throw new EntityNotFoundException("User", id);
        }

        user.IsActive = !user.IsActive;
        var updatedUser = await _userRepository.UpdateAsync(user);

        _logger.LogInformation("Toggle status completed for user {UserId}; active={IsActive}", updatedUser.Id, updatedUser.IsActive);
        return AuthService.ToProfile(updatedUser);
    }

    public async Task<UserProfileDto> AddEmployeeAsync(Guid userId, AddEmployeeDto request)
    {
        _logger.LogInformation("Add employee requested for user {UserId}", userId);

        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null)
        {
            _logger.LogWarning("Add employee rejected: user {UserId} was not found", userId);
            throw new EntityNotFoundException("User", userId);
        }

        if (user.Role == Role.Admin)
        {
            _logger.LogWarning("Add employee rejected: user {UserId} is an Admin", userId);
            throw new ForbiddenException("Admin users cannot be added as employees.", "ADMIN_EMPLOYEE_NOT_ALLOWED");
        }

        var existingEmployee = await _employeeRepository.GetByIdAsync(userId);
        if (existingEmployee is not null)
        {
            _logger.LogWarning("Add employee rejected: user {UserId} is already mapped to employee {EmployeeId}", userId, existingEmployee.Id);
            throw new ConflictException("This user is already added as an employee.", "EMPLOYEE_ALREADY_EXISTS");
        }

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Department = request.Department.Trim(),
            Designation = request.Designation.Trim(),
            Status = EmployeeStatus.Active,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        var createdEmployee = await _userRepository.AddEmployeeAsync(employee);
        user.Employee = createdEmployee;

        _logger.LogInformation("Add employee completed for user {UserId} with employee {EmployeeId}", userId, createdEmployee.Id);
        return AuthService.ToProfile(user);
    }

    private async Task<User> CreateManagerWithEmployeeAsync(User user, DateTime now)
    {
        var existingEmployee = await _employeeRepository.GetByIdAsync(user.Id);
        if (existingEmployee is not null)
        {
            _logger.LogWarning(
                "Manager creation rejected: user {UserId} is already mapped to employee {EmployeeId}",
                user.Id,
                existingEmployee.Id);
            throw new ConflictException("This user is already added as an employee.", "EMPLOYEE_ALREADY_EXISTS");
        }

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Department = DefaultManagerDepartment,
            Designation = DefaultManagerDesignation,
            Status = EmployeeStatus.Active,
            IsActive = true,
            CreatedAt = now,
        };

        return await _userRepository.CreateWithEmployeeAsync(user, employee);
    }
}
