using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ResourceMindAI.Application.Abstractions.Repositories;
using ResourceMindAI.Application.DTOs.User;
using ResourceMindAI.Application.Exceptions;
using ResourceMindAI.Application.Services;
using ResourceMindAI.Application.Tests.TestData;
using ResourceMindAI.Domain.Enums;
using ResourceMindAI.Domain.Exceptions;

namespace ResourceMindAI.Application.Tests.Users;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly UserService _sut;

    public UserServiceTests()
    {
        _sut = new UserService(_users.Object, Mock.Of<ILogger<UserService>>());
    }

    [Fact]
    public async Task CreateAsync_WithValidData_ShouldCreateActiveUserRequiringPasswordChange()
    {
        _users.Setup(x => x.ExistsByUsernameOrEmailAsync("employee.1", "employee@example.com"))
            .ReturnsAsync(false);
        _users.Setup(x => x.CreateAsync(It.IsAny<ResourceMindAI.Domain.Entities.User>()))
            .ReturnsAsync((ResourceMindAI.Domain.Entities.User user) => user);

        var result = await _sut.CreateAsync(new CreateUserDto
        {
            FullName = " Employee One ",
            Email = "employee@example.com",
            Username = "employee.1",
            TemporaryPassword = "Temporary1",
            Role = Role.Employee,
            Department = "Engineering",
            Designation = "Developer"
        });

        result.IsActive.Should().BeTrue();
        result.ForcePasswordChange.Should().BeTrue();
        _users.Verify(x => x.CreateAsync(It.Is<ResourceMindAI.Domain.Entities.User>(user =>
            user.FullName == "Employee One"
            && user.Role == Role.Employee
            && PasswordHasher.Verify("Temporary1", user.PasswordHash))), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenUsernameOrEmailExists_ShouldThrowConflictException()
    {
        _users.Setup(x => x.ExistsByUsernameOrEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var act = () => _sut.CreateAsync(ValidCreateRequest());

        var exception = await act.Should().ThrowAsync<ConflictException>();
        exception.Which.ErrorCode.Should().Be("DUPLICATE_USER");
        _users.Verify(x => x.CreateAsync(It.IsAny<ResourceMindAI.Domain.Entities.User>()), Times.Never);
    }

    [Fact]
    public async Task ResetPasswordAsync_WhenUserExists_ShouldUseUsernameAndRequirePasswordChange()
    {
        var user = TestDataBuilder.User();
        _users.Setup(x => x.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _users.Setup(x => x.UpdateAsync(user)).ReturnsAsync(user);

        var result = await _sut.ResetPasswordAsync(user.Id);

        result.ForcePasswordChange.Should().BeTrue();
        PasswordHasher.Verify(user.Username, user.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task DeactivateAsync_WhenUserIsAdmin_ShouldOnlyDeactivateUser()
    {
        var admin = TestDataBuilder.User(Role.Admin);
        _users.Setup(x => x.GetForStatusChangeAsync(admin.Id)).ReturnsAsync(admin);
        _users.Setup(x => x.UpdateAsync(admin)).ReturnsAsync(admin);

        var result = await _sut.DeactivateAsync(admin.Id);

        result.User.IsActive.Should().BeFalse();
        result.EndedAllocationCount.Should().Be(0);
    }

    [Fact]
    public async Task DeactivateAsync_WhenUserIsEmployee_ShouldEndAllocationsAndClearManager()
    {
        var manager = TestDataBuilder.User(Role.Manager);
        var employee = TestDataBuilder.User();
        var profile = TestDataBuilder.Profile(employee, manager);
        var project = TestDataBuilder.Project(manager);
        var allocation = TestDataBuilder.Allocation(employee, project);
        _users.Setup(x => x.GetForStatusChangeAsync(employee.Id)).ReturnsAsync(employee);
        _users.Setup(x => x.UpdateAsync(employee)).ReturnsAsync(employee);

        var result = await _sut.DeactivateAsync(employee.Id);

        result.EndedAllocationCount.Should().Be(1);
        employee.IsActive.Should().BeFalse();
        allocation.IsActive.Should().BeFalse();
        allocation.ToDate.Should().Be(DateTime.UtcNow.Date);
        profile.ManagerId.Should().BeNull();
    }

    [Fact]
    public async Task DeactivateAsync_WhenManagerHasDependencies_ShouldReturnDependencyDetails()
    {
        var manager = TestDataBuilder.User(Role.Manager);
        _users.Setup(x => x.GetForStatusChangeAsync(manager.Id)).ReturnsAsync(manager);
        _users.Setup(x => x.GetActiveOrPlannedProjectNamesAsync(manager.Id))
            .ReturnsAsync(["Apollo"]);
        _users.Setup(x => x.GetActiveAssignedEmployeeNamesAsync(manager.Id))
            .ReturnsAsync(["Aarav Sharma"]);

        var act = () => _sut.DeactivateAsync(manager.Id);

        var exception = await act.Should().ThrowAsync<ManagerDeactivationBlockedException>();
        exception.Which.Details.Projects.Should().ContainSingle("Apollo");
        exception.Which.Details.Employees.Should().ContainSingle("Aarav Sharma");
        manager.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task DeactivateAsync_WhenManagerHasNoDependencies_ShouldDeactivateManager()
    {
        var manager = TestDataBuilder.User(Role.Manager);
        _users.Setup(x => x.GetForStatusChangeAsync(manager.Id)).ReturnsAsync(manager);
        _users.Setup(x => x.GetActiveOrPlannedProjectNamesAsync(manager.Id))
            .ReturnsAsync([]);
        _users.Setup(x => x.GetActiveAssignedEmployeeNamesAsync(manager.Id))
            .ReturnsAsync([]);
        _users.Setup(x => x.UpdateAsync(manager)).ReturnsAsync(manager);

        var result = await _sut.DeactivateAsync(manager.Id);

        result.User.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task ReactivateAsync_WhenUserIsInactive_ShouldNotRestorePreviousAllocations()
    {
        var manager = TestDataBuilder.User(Role.Manager);
        var employee = TestDataBuilder.User(isActive: false);
        var allocation = TestDataBuilder.Allocation(
            employee,
            TestDataBuilder.Project(manager),
            active: false,
            toDate: DateTime.UtcNow.Date.AddDays(-1));
        _users.Setup(x => x.GetForStatusChangeAsync(employee.Id)).ReturnsAsync(employee);
        _users.Setup(x => x.UpdateAsync(employee)).ReturnsAsync(employee);

        var result = await _sut.ReactivateAsync(employee.Id);

        result.IsActive.Should().BeTrue();
        allocation.IsActive.Should().BeFalse();
    }

    private static CreateUserDto ValidCreateRequest()
    {
        return new CreateUserDto
        {
            FullName = "Employee One",
            Email = "employee@example.com",
            Username = "employee.1",
            TemporaryPassword = "Temporary1",
            Role = Role.Employee,
            Department = "Engineering",
            Designation = "Developer"
        };
    }
}
