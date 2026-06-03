using Microsoft.Extensions.Logging;
using ResourceMindAI.Application.Abstractions.Repositories;
using ResourceMindAI.Application.DTOs.Auth;
using ResourceMindAI.Application.DTOs.User;
using ResourceMindAI.Domain.Entities;

namespace ResourceMindAI.Application.Services;

public class UserService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserService> _logger;

    public UserService(IUserRepository userRepository, ILogger<UserService> logger)
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

    public async Task<(UserProfileDto? User, string? Error, int StatusCode)> CreateAsync(CreateUserDto request)
    {
        var username = request.Username.Trim();
        var email = request.Email.Trim();

        _logger.LogInformation("Validating new user {Username} with role {Role}", username, request.Role);

        if (await _userRepository.ExistsByUsernameOrEmailAsync(username, email))
        {
            _logger.LogWarning("User creation rejected because username/email already exists for {Username}", username);
            return (null, "A user with this username or email already exists.", 409);
        }

        var now = DateTime.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName.Trim(),
            Email = email,
            Username = username,
            PasswordHash = PasswordHasher.Hash(request.Password),
            Role = request.Role,
            IsActive = true,
            ForcePasswordChange = true,
            CreatedAt = now
        };

        _logger.LogInformation("Persisting new user {UserId} with role {Role}", user.Id, user.Role);
        var createdUser = await _userRepository.CreateAsync(user);
        _logger.LogInformation("Persisted new user {UserId}", createdUser.Id);

        return (AuthService.ToProfile(createdUser), null, 201);
    }
}
