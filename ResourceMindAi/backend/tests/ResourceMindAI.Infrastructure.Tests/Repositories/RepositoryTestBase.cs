using Microsoft.EntityFrameworkCore;
using ResourceMindAI.Domain.Entities;
using ResourceMindAI.Domain.Enums;
using ResourceMindAI.Infrastructure.Persistence;

namespace ResourceMindAI.Infrastructure.Tests.Repositories;

public abstract class RepositoryTestBase : IAsyncLifetime
{
    protected AppDbContext DbContext { get; private set; } = null!;

    public Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"ResourceMindAI-{Guid.NewGuid()}")
            .Options;
        DbContext = new AppDbContext(options);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await DbContext.DisposeAsync();
    }

    protected static User User(Role role, bool active, string name)
    {
        var username = name.ToLowerInvariant().Replace(" ", ".");
        return new User
        {
            Id = Guid.NewGuid(),
            FullName = name,
            Email = $"{username}@example.com",
            Username = username,
            PasswordHash = "hash",
            Department = "Engineering",
            Designation = "Developer",
            Role = role,
            IsActive = active,
            CreatedAt = DateTime.UtcNow
        };
    }

    protected static Project Project(User manager, string name = "Apollo")
    {
        return new Project
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = "Project",
            StartDate = DateTime.UtcNow.Date.AddMonths(-1),
            EndDate = DateTime.UtcNow.Date.AddMonths(2),
            Status = ProjectStatus.Active,
            HealthStatus = HealthStatus.Green,
            ManagerId = manager.Id,
            Manager = manager,
            CreatedAt = DateTime.UtcNow
        };
    }
}
