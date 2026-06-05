using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ResourceMindAI.Application.Abstractions.Repositories;
using ResourceMindAI.Domain.Entities;
using ResourceMindAI.Domain.Enums;

namespace ResourceMindAI.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(AppDbContext dbContext, ILogger<UserRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IReadOnlyList<User>> GetAllAsync()
    {
        _logger.LogDebug("Querying all users with employee profiles");

        var users = await _dbContext.Users
            .Include(x => x.Employee)
            .OrderBy(x => x.FullName)
            .ToListAsync();

        _logger.LogDebug("Queried {UserCount} users", users.Count);
        return users;
    }

    public async Task<IReadOnlyList<User>> GetActiveManagersAsync()
    {
        _logger.LogDebug("Querying active managers with employee profiles");

        var managers = await _dbContext.Users
            .Include(x => x.Employee)
            .Where(x => x.Role == Role.Manager && x.IsActive)
            .OrderBy(x => x.FullName)
            .ToListAsync();

        _logger.LogDebug("Queried {ManagerCount} active managers", managers.Count);
        return managers;
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        var normalized = username.Trim().ToLowerInvariant();
        _logger.LogDebug("Querying user by username {Username}", normalized);

        var user = await _dbContext.Users
            .Include(x => x.Employee)
            .FirstOrDefaultAsync(x => x.Username == normalized);

        _logger.LogDebug(
            "User lookup by username {Username} returned {Found}",
            normalized,
            user is not null);

        return user;
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        _logger.LogDebug("Querying user by id {UserId}", id);

        var user = await _dbContext.Users
            .Include(x => x.Employee)
            .FirstOrDefaultAsync(x => x.Id == id);

        _logger.LogDebug(
            "User lookup by id {UserId} returned {Found}",
            id,
            user is not null);

        return user;
    }

    public async Task<bool> ExistsByUsernameOrEmailAsync(string username, string email)
    {
        var normalizedUsername = username.Trim().ToLowerInvariant();
        var normalizedEmail = email.Trim().ToLowerInvariant();

        _logger.LogDebug(
            "Checking whether user exists for username {Username} or email {Email}",
            normalizedUsername,
            normalizedEmail);

        var exists = await _dbContext.Users.AnyAsync(x =>
            x.Username == normalizedUsername || x.Email == normalizedEmail);

        _logger.LogDebug(
            "User existence check for username {Username} or email {Email} returned {Exists}",
            normalizedUsername,
            normalizedEmail,
            exists);

        return exists;
    }

    public async Task<User> CreateAsync(User user)
    {
        _logger.LogInformation("Adding user {UserId} with role {Role} to database", user.Id, user.Role);

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Saved user {UserId} to database", user.Id);
        return user;
    }

    public async Task<User> CreateWithEmployeeAsync(User user, Employee employee)
    {
        _logger.LogInformation("Adding user {UserId} and employee {EmployeeId} to database", user.Id, employee.Id);

        _dbContext.Users.Add(user);
        _dbContext.Employees.Add(employee);
        await _dbContext.SaveChangesAsync();

        user.Employee = employee;

        _logger.LogInformation("Saved user {UserId} and employee {EmployeeId} to database", user.Id, employee.Id);
        return user;
    }

    public async Task<User> UpdateAsync(User user)
    {
        _logger.LogInformation("Updating user {UserId}", user.Id);

        user.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Updated user {UserId}", user.Id);
        return user;
    }

    public async Task<User> UpdatePasswordAsync(User user, string passwordHash)
    {
        _logger.LogInformation("Updating password for user {UserId}", user.Id);

        user.PasswordHash = passwordHash;
        user.ForcePasswordChange = false;
        user.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Updated password for user {UserId}", user.Id);
        return user;
    }

    public async Task<Employee> AddEmployeeAsync(Employee employee)
    {
        _logger.LogInformation("Adding employee {EmployeeId} for user {UserId}", employee.Id, employee.UserId);

        _dbContext.Employees.Add(employee);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Saved employee {EmployeeId} for user {UserId}", employee.Id, employee.UserId);
        return employee;
    }
}
