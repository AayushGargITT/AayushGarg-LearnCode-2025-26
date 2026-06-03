using Microsoft.Extensions.Logging;
using ResourceMindAI.Application.Abstractions.Repositories;
using ResourceMindAI.Application.DTOs.Auth;
using ResourceMindAI.Domain.Entities;
using ResourceMindAI.Domain.Exceptions;

namespace ResourceMindAI.Application.Services;

public class AuthService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IUserRepository userRepository, ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    /// <summary>
    /// Authenticates a user by username and password.
    /// </summary>
    /// <exception cref="ForbiddenException">Thrown when the user's account is inactive.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when credentials are invalid.</exception>
    public async Task<UserProfileDto> LoginAsync(LoginDto request)
    {
        _logger.LogInformation("Authenticating username {Username}", request.Username);

        var user = await _userRepository.GetByUsernameAsync(request.Username);

        if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Authentication failed for username {Username}: invalid credentials", request.Username);
            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Authentication failed for user {UserId}: account is inactive", user.Id);
            throw new ForbiddenException("Your account has been deactivated.", "INACTIVE_ACCOUNT");
        }

        _logger.LogInformation("Authentication succeeded for user {UserId} with role {Role}", user.Id, user.Role);
        return ToProfile(user);
    }

    /// <summary>
    /// Changes the password for an authenticated user.
    /// </summary>
    /// <exception cref="ValidationException">Thrown when input fails business validation rules.</exception>
    /// <exception cref="EntityNotFoundException">Thrown when the user does not exist.</exception>
    /// <exception cref="ForbiddenException">Thrown when the user's account is inactive.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when the current password is incorrect.</exception>
    public async Task<UserProfileDto> ChangePasswordAsync(ChangePasswordDto request)
    {
        _logger.LogInformation("Change password requested for user {UserId}", request.UserId);

        if (request.NewPassword != request.ConfirmPassword)
        {
            _logger.LogWarning("Change password rejected for user {UserId}: confirmation did not match", request.UserId);
            throw new ValidationException(
                nameof(request.ConfirmPassword),
                "New password and confirmation do not match.");
        }

        if (!IsStrongPassword(request.NewPassword))
        {
            _logger.LogWarning("Change password rejected for user {UserId}: password did not meet strength rules", request.UserId);
            throw new ValidationException(
                nameof(request.NewPassword),
                "Password must be at least 8 characters and include an uppercase letter and a number.");
        }

        var user = await _userRepository.GetByIdAsync(request.UserId);
        if (user is null)
        {
            _logger.LogWarning("Change password rejected: user {UserId} was not found", request.UserId);
            throw new EntityNotFoundException("User", request.UserId);
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Change password rejected: user {UserId} is inactive", user.Id);
            throw new ForbiddenException("Your account has been deactivated.", "INACTIVE_ACCOUNT");
        }

        if (!PasswordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            _logger.LogWarning("Change password rejected: current password was invalid for user {UserId}", user.Id);
            throw new UnauthorizedAccessException("Current password is incorrect.");
        }

        if (PasswordHasher.Verify(request.NewPassword, user.PasswordHash))
        {
            _logger.LogWarning("Change password rejected: new password matches current password for user {UserId}", user.Id);
            throw new ValidationException(
                nameof(request.NewPassword),
                "New password must be different from the current password.");
        }

        var updatedUser = await _userRepository.UpdatePasswordAsync(user, PasswordHasher.Hash(request.NewPassword));

        _logger.LogInformation("Password changed successfully for user {UserId}", updatedUser.Id);
        return ToProfile(updatedUser);
    }

    public static UserProfileDto ToProfile(User user)
    {
        return new UserProfileDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Username = user.Username,
            Role = user.Role,
            IsActive = user.IsActive,
            ForcePasswordChange = user.ForcePasswordChange,
            EmployeeId = user.Employee?.Id,
            Department = user.Employee?.Department,
            Designation = user.Employee?.Designation,
        };
    }

    private static bool IsStrongPassword(string password)
    {
        return password.Length >= 8
            && password.Any(char.IsUpper)
            && password.Any(char.IsDigit);
    }
}
