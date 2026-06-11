using Microsoft.Extensions.Logging;
using ResourceMindAI.Application.Abstractions.Repositories;
using ResourceMindAI.Application.Abstractions.Services;
using ResourceMindAI.Application.DTOs.Auth;
using ResourceMindAI.Domain.Entities;
using ResourceMindAI.Domain.Enums;
using ResourceMindAI.Domain.Exceptions;

namespace ResourceMindAI.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IUserRepository userRepository, ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

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

    public async Task<UserProfileDto> ChangePasswordAsync(ChangePasswordDto request)
    {
        _logger.LogInformation("Change password requested for user {UserId}", request.UserId);

        if (request.NewPassword != request.ConfirmPassword)
        {
            _logger.LogWarning("Change password rejected for user {UserId}: confirmation did not match", request.UserId);
            throw new ValidationException(
                "New password and confirmation do not match.");
        }

        if (!IsStrongPassword(request.NewPassword))
        {
            _logger.LogWarning("Change password rejected for user {UserId}: password did not meet strength rules", request.UserId);
            throw new ValidationException(
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
            EmployeeId = user.Role is Role.Manager or Role.Employee
                ? user.Id
                : null,
            Department = user.Department,
            Designation = user.Designation,
        };
    }

    private static bool IsStrongPassword(string password)
    {
        return password.Length >= 8
            && password.Any(char.IsUpper)
            && password.Any(char.IsDigit);
    }
}
