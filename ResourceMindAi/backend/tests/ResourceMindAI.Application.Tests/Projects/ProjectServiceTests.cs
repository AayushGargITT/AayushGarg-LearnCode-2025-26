using FluentAssertions;
using Moq;
using ResourceMindAI.Application.Abstractions.Repositories;
using ResourceMindAI.Application.DTOs.Project;
using ResourceMindAI.Application.Exceptions;
using ResourceMindAI.Application.Services;
using ResourceMindAI.Application.Tests.TestData;
using ResourceMindAI.Domain.Entities;
using ResourceMindAI.Domain.Enums;

namespace ResourceMindAI.Application.Tests.Projects;

public class ProjectServiceTests
{
    private readonly Mock<IProjectRepository> _projects = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly ProjectService _sut;

    public ProjectServiceTests()
    {
        _sut = new ProjectService(_projects.Object, _users.Object);
    }

    [Fact]
    public async Task UpdateManagerAsync_WhenEmployeeHasAnotherProjectUnderCurrentManager_ShouldBlockUpdate()
    {
        var currentManager = TestDataBuilder.User(Role.Manager, name: "Current Manager");
        var newManager = TestDataBuilder.User(Role.Manager, name: "New Manager");
        var employee = TestDataBuilder.User();
        var project = TestDataBuilder.Project(currentManager, name: "Project A");
        var otherProject = TestDataBuilder.Project(currentManager, name: "Project B");
        TestDataBuilder.Allocation(employee, project);
        var conflictingAllocation = TestDataBuilder.Allocation(employee, otherProject);
        _projects.Setup(x => x.GetForManagerUpdateAsync(project.Id)).ReturnsAsync(project);
        _users.Setup(x => x.GetByIdAsync(newManager.Id)).ReturnsAsync(newManager);
        _projects.Setup(x => x.GetActiveAllocationsForResourcesUnderManagerAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(),
                currentManager.Id))
            .ReturnsAsync([conflictingAllocation]);

        var act = () => _sut.UpdateManagerAsync(project.Id, new UpdateProjectManagerDto
        {
            NewManagerId = newManager.Id
        });

        var exception = await act.Should().ThrowAsync<ProjectManagerUpdateBlockedException>();
        exception.Which.Details.Conflicts.Should().ContainSingle();
        exception.Which.Details.Conflicts[0].ProjectNames.Should()
            .BeEquivalentTo(["Project A", "Project B"]);
    }

    [Fact]
    public async Task UpdateManagerAsync_WhenNoConflicts_ShouldUpdateProjectAndEmployeeManagers()
    {
        var currentManager = TestDataBuilder.User(Role.Manager, name: "Current Manager");
        var newManager = TestDataBuilder.User(Role.Manager, name: "New Manager");
        var employee = TestDataBuilder.User();
        var project = TestDataBuilder.Project(currentManager);
        TestDataBuilder.Allocation(employee, project);
        _projects.Setup(x => x.GetForManagerUpdateAsync(project.Id)).ReturnsAsync(project);
        _users.Setup(x => x.GetByIdAsync(newManager.Id)).ReturnsAsync(newManager);
        _projects.Setup(x => x.GetActiveAllocationsForResourcesUnderManagerAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(),
                currentManager.Id))
            .ReturnsAsync([]);

        var result = await _sut.UpdateManagerAsync(project.Id, new UpdateProjectManagerDto
        {
            NewManagerId = newManager.Id
        });

        project.ManagerId.Should().Be(newManager.Id);
        employee.ResourceProfile.Should().NotBeNull();
        employee.ResourceProfile!.ManagerId.Should().Be(newManager.Id);
        result.UpdatedResources.Should().ContainSingle(employee.FullName);
        _projects.Verify(x => x.SaveManagerUpdateAsync(
            project,
            It.Is<IReadOnlyCollection<ResourceProfile>>(profiles =>
                profiles.Count == 1 && profiles.Single().ManagerId == newManager.Id)), Times.Once);
    }
}
