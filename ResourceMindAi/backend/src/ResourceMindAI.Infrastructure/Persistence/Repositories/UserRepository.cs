using Microsoft.EntityFrameworkCore;
using ResourceMindAI.Application.Abstractions.Repositories;
using ResourceMindAI.Domain.Entities;

namespace ResourceMindAI.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _dbContext;

    public UserRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<User>> GetAllAsync()
    {
        return await _dbContext.Users
            .Include(x => x.Employee)
            .OrderBy(x => x.FullName)
            .ToListAsync();
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        var normalized = username.Trim().ToLowerInvariant();

        return await _dbContext.Users
            .Include(x => x.Employee)
            .FirstOrDefaultAsync(x => x.Username==normalized);
    }

    public async Task<bool> ExistsByUsernameOrEmailAsync(string username, string email)
    {
        var normalizedUsername = username.Trim().ToLowerInvariant();
        var normalizedEmail = email.Trim().ToLowerInvariant();

        return await _dbContext.Users.AnyAsync(x =>
            x.Username == normalizedUsername && x.Email == normalizedEmail);
    }

    public async Task<User> CreateAsync(User user)
    {
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();
        
        return user;
    }
}
