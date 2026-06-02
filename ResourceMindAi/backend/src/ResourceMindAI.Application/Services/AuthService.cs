using System.Security.Cryptography;
using ResourceMindAI.Application.Abstractions.Repositories;
using ResourceMindAI.Application.DTOs.Auth;
using ResourceMindAI.Domain.Entities;

namespace ResourceMindAI.Application.Services;

public class AuthService
{
    private readonly IUserRepository _userRepository;

    public AuthService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserProfileDto?> LoginAsync(LoginDto request)
    {
        var user = await _userRepository.GetByUsernameAsync(request.Username);

        if (user is null || !VerifyPassword(request.Password, user.PasswordHash))
        {
            return null;
        }

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
