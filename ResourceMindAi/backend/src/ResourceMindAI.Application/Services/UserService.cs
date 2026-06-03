using Microsoft.Extensions.Logging;
using ResourceMindAI.Application.Abstractions.Repositories;
using ResourceMindAI.Application.DTOs.Auth;
using ResourceMindAI.Application.DTOs.User;
using ResourceMindAI.Domain.Entities;
using ResourceMindAI.Domain.Exceptions;

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

    /// <summary>
    /// Retrieves a single user by ID.
    /// </summary>
    /// <exception cref="EntityNotFoundException">Thrown when the user does not exist.</exception>
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

    /// <summary>
    /// Creates a new user account.
    /// </summary>
    /// <exception cref="ConflictException">Thrown when a user with the same username or email already exists.</exception>
    public async Task<UserProfileDto> CreateAsync(CreateUserDto request)
    {
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

        return AuthService.ToProfile(createdUser);
    }
}
