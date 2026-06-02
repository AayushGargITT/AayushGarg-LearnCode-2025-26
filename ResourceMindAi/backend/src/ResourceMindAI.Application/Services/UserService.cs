using System.Security.Cryptography;
using ResourceMindAI.Application.Abstractions.Repositories;
using ResourceMindAI.Application.DTOs.Auth;
using ResourceMindAI.Application.DTOs.User;
using ResourceMindAI.Domain.Entities;
using ResourceMindAI.Domain.Enums;

namespace ResourceMindAI.Application.Services;

public class UserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<IReadOnlyList<UserProfileDto>> GetAllAsync()
    {
        var users = await _userRepository.GetAllAsync();
        return users.Select(AuthService.ToProfile).ToList();
    }

    public async Task<(UserProfileDto? User, string? Error, int StatusCode)> CreateAsync(CreateUserDto request)
    {
        var username = request.Username.Trim();
        var email = request.Email.Trim();

        if (await _userRepository.ExistsByUsernameOrEmailAsync(username, email))
        {
            return (null, "A user with this username or email already exists.", 409);
        }

        var now = DateTime.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName.Trim(),
            Email = email,
            Username = username,
            PasswordHash = HashPassword(request.Password),
            Role = request.Role,
            IsActive = true,
            ForcePasswordChange = true,
            CreatedAt = now
        };

        var createdUser = await _userRepository.CreateAsync(user);
        return (AuthService.ToProfile(createdUser), null, 201);
    }

    private static string HashPassword(string password)
    {
        const int iterations = 100_000;
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, 32);

        return $"pbkdf2${iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }
}
