using System.Security.Cryptography;
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

        if (!VerifyPassword(request.Password, user.PasswordHash))
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

    private static bool VerifyPassword(string password, string storedPassword)
    {
        if (!storedPassword.StartsWith("pbkdf2$", StringComparison.Ordinal))
        {
            return password == storedPassword;
        }

        var parts = storedPassword.Split('$');
        if (parts.Length != 4 || !int.TryParse(parts[1], out var iterations))
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[2]);
        var expectedHash = Convert.FromBase64String(parts[3]);
        var actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expectedHash.Length);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}
