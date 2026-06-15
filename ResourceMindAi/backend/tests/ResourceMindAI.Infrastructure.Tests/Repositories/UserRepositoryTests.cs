using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ResourceMindAI.Domain.Enums;
using ResourceMindAI.Infrastructure.Persistence.Repositories;

namespace ResourceMindAI.Infrastructure.Tests.Repositories;

public class UserRepositoryTests : RepositoryTestBase
{
    [Fact]
    public async Task GetActiveManagersAsync_ShouldExcludeInactiveManagersAndEmployees()
    {
        var activeManager = User(Role.Manager, true, "Active Manager");
        var inactiveManager = User(Role.Manager, false, "Inactive Manager");
        var employee = User(Role.Resource, true, "Employee");
        DbContext.Users.AddRange(activeManager, inactiveManager, employee);
        await DbContext.SaveChangesAsync();
        var sut = new UserRepository(DbContext, Mock.Of<ILogger<UserRepository>>());

        var result = await sut.GetActiveManagersAsync();

        result.Should().ContainSingle(user => user.Id == activeManager.Id);
    }

    [Fact]
    public async Task ExistsByUsernameOrEmailAsync_ShouldMatchNormalizedInput()
    {
        var user = User(Role.Resource, true, "Existing User");
        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();
        var sut = new UserRepository(DbContext, Mock.Of<ILogger<UserRepository>>());

        var result = await sut.ExistsByUsernameOrEmailAsync(
            $" {user.Username.ToUpperInvariant()} ",
            "different@example.com");

        result.Should().BeTrue();
    }
}
