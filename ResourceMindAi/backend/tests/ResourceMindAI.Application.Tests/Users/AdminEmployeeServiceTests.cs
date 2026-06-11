using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ResourceMindAI.Application.Abstractions.Repositories;
using ResourceMindAI.Application.DTOs.Employee;
using ResourceMindAI.Application.Services;
using ResourceMindAI.Application.Tests.TestData;
using ResourceMindAI.Domain.Entities;
using ResourceMindAI.Domain.Enums;
using ResourceMindAI.Domain.Exceptions;

namespace ResourceMindAI.Application.Tests.Users;

public class AdminEmployeeServiceTests
{
    private readonly Mock<IAdminEmployeeRepository> _employees = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly AdminEmployeeService _sut;

    public AdminEmployeeServiceTests()
    {
        _sut = new AdminEmployeeService(
            _employees.Object,
            _users.Object,
            Mock.Of<ILogger<AdminEmployeeService>>());
    }

    [Fact]
    public async Task UpdateManagerAsync_WhenProfileIsMissing_ShouldCreateResourceProfile()
    {
        var employee = TestDataBuilder.User();
        var manager = TestDataBuilder.User(Role.Manager);
        _employees.Setup(x => x.GetForManagerUpdateAsync(employee.Id)).ReturnsAsync(employee);
        _users.Setup(x => x.GetByIdAsync(manager.Id)).ReturnsAsync(manager);
        ResourceProfile? savedProfile = null;
        _employees.Setup(x => x.SaveResourceProfileAsync(It.IsAny<ResourceProfile>()))
            .Callback<ResourceProfile>(profile => savedProfile = profile)
            .Returns(Task.CompletedTask);

        var result = await _sut.UpdateManagerAsync(employee.Id, new UpdateEmployeeManagerDto
        {
            NewManagerId = manager.Id
        });

        savedProfile.Should().NotBeNull();
        savedProfile!.Id.Should().Be(employee.Id);
        savedProfile.ManagerId.Should().Be(manager.Id);
        result.Employee.ManagerId.Should().Be(manager.Id);
    }

    [Fact]
    public async Task UpdateManagerAsync_WhenProfileExists_ShouldUpdateExistingProfile()
    {
        var oldManager = TestDataBuilder.User(Role.Manager, name: "Old Manager");
        var newManager = TestDataBuilder.User(Role.Manager, name: "New Manager");
        var employee = TestDataBuilder.User();
        var profile = TestDataBuilder.Profile(employee, oldManager);
        _employees.Setup(x => x.GetForManagerUpdateAsync(employee.Id)).ReturnsAsync(employee);
        _users.Setup(x => x.GetByIdAsync(newManager.Id)).ReturnsAsync(newManager);
        _employees.Setup(x => x.SaveResourceProfileAsync(profile)).Returns(Task.CompletedTask);

        await _sut.UpdateManagerAsync(employee.Id, new UpdateEmployeeManagerDto
        {
            NewManagerId = newManager.Id
        });

        profile.ManagerId.Should().Be(newManager.Id);
        _employees.Verify(x => x.SaveResourceProfileAsync(profile), Times.Once);
    }

    [Fact]
    public async Task UpdateManagerAsync_WhenManagerIsInactive_ShouldThrowValidationException()
    {
        var employee = TestDataBuilder.User();
        var manager = TestDataBuilder.User(Role.Manager, isActive: false);
        _employees.Setup(x => x.GetForManagerUpdateAsync(employee.Id)).ReturnsAsync(employee);
        _users.Setup(x => x.GetByIdAsync(manager.Id)).ReturnsAsync(manager);

        var act = () => _sut.UpdateManagerAsync(employee.Id, new UpdateEmployeeManagerDto
        {
            NewManagerId = manager.Id
        });

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("Selected manager must be an active manager.");
    }

    [Fact]
    public async Task UpdateManagerAsync_WhenSelectedUserIsNotManager_ShouldThrowValidationException()
    {
        var employee = TestDataBuilder.User();
        var nonManager = TestDataBuilder.User(Role.Employee, name: "Another Employee");
        _employees.Setup(x => x.GetForManagerUpdateAsync(employee.Id)).ReturnsAsync(employee);
        _users.Setup(x => x.GetByIdAsync(nonManager.Id)).ReturnsAsync(nonManager);

        var act = () => _sut.UpdateManagerAsync(employee.Id, new UpdateEmployeeManagerDto
        {
            NewManagerId = nonManager.Id
        });

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task UpdateManagerAsync_WhenTargetUserIsNotEmployee_ShouldThrowValidationException()
    {
        var target = TestDataBuilder.User(Role.Manager);
        var manager = TestDataBuilder.User(Role.Manager, name: "New Manager");
        _employees.Setup(x => x.GetForManagerUpdateAsync(target.Id)).ReturnsAsync(target);

        var act = () => _sut.UpdateManagerAsync(target.Id, new UpdateEmployeeManagerDto
        {
            NewManagerId = manager.Id
        });

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Employee role*");
        _users.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task UpdateManagerAsync_WithActiveAllocations_ShouldEndAllocations()
    {
        var oldManager = TestDataBuilder.User(Role.Manager, name: "Old Manager");
        var newManager = TestDataBuilder.User(Role.Manager, name: "New Manager");
        var employee = TestDataBuilder.User();
        TestDataBuilder.Profile(employee, oldManager);
        var allocation = TestDataBuilder.Allocation(employee, TestDataBuilder.Project(oldManager));
        _employees.Setup(x => x.GetForManagerUpdateAsync(employee.Id)).ReturnsAsync(employee);
        _users.Setup(x => x.GetByIdAsync(newManager.Id)).ReturnsAsync(newManager);

        var result = await _sut.UpdateManagerAsync(employee.Id, new UpdateEmployeeManagerDto
        {
            NewManagerId = newManager.Id
        });

        allocation.IsActive.Should().BeFalse();
        allocation.ToDate.Should().Be(DateTime.UtcNow.Date);
        result.EndedProjects.Should().ContainSingle("Apollo");
    }
}
