using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ResourceMindAI.Domain.Entities;
using ResourceMindAI.Domain.Enums;
using ResourceMindAI.Infrastructure.Persistence.Repositories;

namespace ResourceMindAI.Infrastructure.Tests.Repositories;

public class ManagerRepositoryTests : RepositoryTestBase
{
    [Fact]
    public async Task GetOrganizationSearchCandidatesAsync_ShouldReturnOnlyActiveResources()
    {
        var activeEmployee = User(Role.Resource, true, "Active Employee");
        var inactiveEmployee = User(Role.Resource, false, "Inactive Employee");
        var activeManager = User(Role.Manager, true, "Active Manager");
        DbContext.Users.AddRange(activeEmployee, inactiveEmployee, activeManager);
        await DbContext.SaveChangesAsync();
        var sut = new ManagerRepository(DbContext, Mock.Of<ILogger<ManagerRepository>>());

        var result = await sut.GetOrganizationSearchCandidatesAsync();

        result.Should().ContainSingle(user => user.Id == activeEmployee.Id);
        result.Should().NotContain(user => user.Id == inactiveEmployee.Id);
        result.Should().NotContain(user => user.Id == activeManager.Id);
    }

    [Fact]
    public async Task GetTeamResourcesAsync_ShouldReturnOnlyResourcesAssignedToManager()
    {
        var manager = User(Role.Manager, true, "Current Manager");
        var otherManager = User(Role.Manager, true, "Other Manager");
        var teamEmployee = User(Role.Resource, true, "Team Employee");
        var otherEmployee = User(Role.Resource, true, "Other Employee");
        DbContext.Users.AddRange(manager, otherManager, teamEmployee, otherEmployee);
        DbContext.ResourceProfiles.AddRange(
            new ResourceProfile
            {
                Id = teamEmployee.Id,
                User = teamEmployee,
                ManagerId = manager.Id,
                Manager = manager
            },
            new ResourceProfile
            {
                Id = otherEmployee.Id,
                User = otherEmployee,
                ManagerId = otherManager.Id,
                Manager = otherManager
            });
        await DbContext.SaveChangesAsync();
        var sut = new ManagerRepository(DbContext, Mock.Of<ILogger<ManagerRepository>>());

        var result = await sut.GetTeamResourcesAsync(manager.Id);

        result.Should().ContainSingle(profile => profile.Id == teamEmployee.Id);
    }

    [Fact]
    public async Task GetOverlappingAllocationPercentAsync_ShouldSumOnlyActiveOverlappingAllocations()
    {
        var manager = User(Role.Manager, true, "Manager");
        var employee = User(Role.Resource, true, "Employee");
        var project = Project(manager);
        var rangeStart = DateTime.UtcNow.Date;
        var rangeEnd = rangeStart.AddDays(10);
        DbContext.Users.AddRange(manager, employee);
        DbContext.Projects.Add(project);
        DbContext.Allocations.AddRange(
            Allocation(employee, project, 40, true, rangeStart, rangeEnd),
            Allocation(employee, project, 30, true, rangeEnd.AddDays(1), rangeEnd.AddDays(5)),
            Allocation(employee, project, 20, false, rangeStart, rangeEnd));
        await DbContext.SaveChangesAsync();
        var sut = new ManagerRepository(DbContext, Mock.Of<ILogger<ManagerRepository>>());

        var result = await sut.GetOverlappingAllocationPercentAsync(
            employee.Id,
            rangeStart,
            rangeEnd);

        result.Should().Be(40);
    }

    private static Allocation Allocation(
        User employee,
        Project project,
        decimal utilisation,
        bool active,
        DateTime from,
        DateTime to)
    {
        return new Allocation
        {
            Id = Guid.NewGuid(),
            UserId = employee.Id,
            User = employee,
            ProjectId = project.Id,
            Project = project,
            UtilisationPercent = utilisation,
            FromDate = from,
            ToDate = to,
            IsActive = active,
            CreatedAt = DateTime.UtcNow
        };
    }
}
