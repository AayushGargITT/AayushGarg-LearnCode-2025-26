using Microsoft.Extensions.Logging;
using ResourceMindAI.Application.Abstractions.Repositories;
using ResourceMindAI.Application.DTOs.Auth;
using ResourceMindAI.Domain.Entities;

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

    public async Task<UserProfileDto?> LoginAsync(LoginDto request)
    {
        _logger.LogInformation("Authenticating username {Username}", request.Username);

        var user = await _userRepository.GetByUsernameAsync(request.Username);

        if (user is null)
        {
            _logger.LogWarning("Authentication failed because username {Username} was not found", request.Username);
            return null;
        }

        if (!PasswordHasher.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Authentication failed because password was invalid for user {UserId}", user.Id);
            return null;
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Authentication failed because user {UserId} is inactive", user.Id);
            return null;
        }

        _logger.LogInformation("Authentication succeeded for user {UserId} with role {Role}", user.Id, user.Role);
        return ToProfile(user);
    }

    public async Task<(UserProfileDto? User, string? Error, int StatusCode)> ChangePasswordAsync(ChangePasswordDto request)
    {
        _logger.LogInformation("Change password requested for user {UserId}", request.UserId);

        if (request.NewPassword != request.ConfirmPassword)
        {
            _logger.LogWarning("Change password rejected for user {UserId} because confirmation did not match", request.UserId);
            return (null, "New password and confirmation do not match.", 400);
        }

        if (!IsStrongPassword(request.NewPassword))
        {
            _logger.LogWarning("Change password rejected for user {UserId} because password did not meet strength rules", request.UserId);
            return (null, "Password must be at least 8 characters and include an uppercase letter and a number.", 400);
        }

        var user = await _userRepository.GetByIdAsync(request.UserId);
        if (user is null)
        {
            _logger.LogWarning("Change password rejected because user {UserId} was not found", request.UserId);
            return (null, "User was not found.", 404);
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Change password rejected because user {UserId} is inactive", user.Id);
            return (null, "Your account has been deactivated.", 403);
        }

        if (!PasswordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            _logger.LogWarning("Change password rejected because current password was invalid for user {UserId}", user.Id);
            return (null, "Current password is incorrect.", 400);
        }

        if (PasswordHasher.Verify(request.NewPassword, user.PasswordHash))
        {
            _logger.LogWarning("Change password rejected because new password matches current password for user {UserId}", user.Id);
            return (null, "New password must be different from the current password.", 400);
        }

        var updatedUser = await _userRepository.UpdatePasswordAsync(user, PasswordHasher.Hash(request.NewPassword));

        _logger.LogInformation("Password changed successfully for user {UserId}", updatedUser.Id);
        return (ToProfile(updatedUser), null, 200);
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
