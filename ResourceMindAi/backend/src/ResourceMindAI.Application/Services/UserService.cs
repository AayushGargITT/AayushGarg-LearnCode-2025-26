using Microsoft.Extensions.Logging;
using ResourceMindAI.Application.Abstractions.Repositories;
using ResourceMindAI.Application.Abstractions.Services;
using ResourceMindAI.Application.DTOs.Auth;
using ResourceMindAI.Application.DTOs.User;
using ResourceMindAI.Application.Exceptions;
using ResourceMindAI.Domain.Entities;
using ResourceMindAI.Domain.Enums;
using ResourceMindAI.Domain.Exceptions;

namespace ResourceMindAI.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository userRepository,
        ILogger<UserService> logger)
    {
        _userRepository = userRepository;
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
        var department = NormalizeOptional(request.Department);
        var designation = NormalizeOptional(request.Designation);

        _logger.LogInformation("Validating new user {Username} with role {Role}", username, request.Role);

        if (await _userRepository.ExistsByUsernameOrEmailAsync(username, email))
        {
            _logger.LogWarning("User creation rejected: username/email already exists for {Username}", username);
            throw new ConflictException(
                "A user with this username or email already exists.",
                "DUPLICATE_USER");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            Email = email,
            Username = username,
            PasswordHash = PasswordHasher.Hash(request.TemporaryPassword),
            Department = department,
            Designation = designation,
            Role = request.Role!.Value,
            IsActive = true,
            ForcePasswordChange = true,
            CreatedAt = DateTime.UtcNow
        };

        _logger.LogInformation("Persisting new user {UserId} with role {Role}", user.Id, user.Role);
        var createdUser = await _userRepository.CreateAsync(user);
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

    public async Task<DeactivateUserResultDto> DeactivateAsync(Guid id)
    {
        _logger.LogInformation("Deactivate requested for user {UserId}", id);

        var user = await _userRepository.GetForStatusChangeAsync(id);
        if (user is null)
        {
            _logger.LogWarning("Deactivate rejected: user {UserId} was not found", id);
            throw new EntityNotFoundException("User", id);
        }

        if (!user.IsActive)
        {
            throw new ConflictException("User is already inactive.", "USER_ALREADY_INACTIVE");
        }

        var endedAllocationCount = user.Role switch
        {
            Role.Resource => DeactivateResourceUser(user),
            Role.Manager => await DeactivateManagerUserAsync(user),
            _ => DeactivateAdminUser(user)
        };

        var updatedUser = await _userRepository.UpdateAsync(user);

        _logger.LogInformation(
            "Deactivate completed for user {UserId}; ended allocations={EndedAllocationCount}",
            updatedUser.Id,
            endedAllocationCount);

        return new DeactivateUserResultDto
        {
            User = AuthService.ToProfile(updatedUser),
            EndedAllocationCount = endedAllocationCount,
            Message = endedAllocationCount > 0
                ? $"User deactivated successfully. {endedAllocationCount} active allocation(s) were ended as of today."
                : "User deactivated successfully."
        };
    }

    public async Task<UserProfileDto> ReactivateAsync(Guid id)
    {
        _logger.LogInformation("Reactivate requested for user {UserId}", id);

        var user = await _userRepository.GetForStatusChangeAsync(id);
        if (user is null)
        {
            throw new EntityNotFoundException("User", id);
        }

        if (user.IsActive)
        {
            throw new ConflictException("User is already active.", "USER_ALREADY_ACTIVE");
        }

        user.IsActive = true;

        var updatedUser = await _userRepository.UpdateAsync(user);

        _logger.LogInformation("Reactivate completed for user {UserId}", updatedUser.Id);
        return AuthService.ToProfile(updatedUser);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static int DeactivateAdminUser(User user)
    {
        user.IsActive = false;
        return 0;
    }

    private static int DeactivateResourceUser(User user)
    {
        user.IsActive = false;

        var today = DateTime.UtcNow.Date;
        var activeAllocations = user.Allocations
            .Where(allocation => allocation.IsActive)
            .ToList();

        foreach (var allocation in activeAllocations)
        {
            allocation.IsActive = false;
            allocation.ToDate = today;
        }

        if (user.ResourceProfile is not null)
        {
            user.ResourceProfile.ManagerId = null;
            user.ResourceProfile.Manager = null;
        }

        return activeAllocations.Count;
    }

    private async Task<int> DeactivateManagerUserAsync(User user)
    {
        await CheckManagerDeactivationPossibleAsync(user.Id);

        user.IsActive = false;
        return 0;
    }

    private async Task CheckManagerDeactivationPossibleAsync(Guid managerId)
    {
        var projectNames = await _userRepository.GetActiveOrPlannedProjectNamesAsync(managerId);
        var employeeNames = await _userRepository.GetActiveAssignedEmployeeNamesAsync(managerId);

        if (projectNames.Count == 0 && employeeNames.Count == 0)
        {
            return;
        }

        throw new ManagerDeactivationBlockedException(new ManagerDeactivationValidationDto
        {
            Projects = projectNames,
            Employees = employeeNames
        });
    }
}
