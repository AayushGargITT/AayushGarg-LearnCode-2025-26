using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ResourceMindAI.Application.Abstractions.Repositories;
using ResourceMindAI.Application.Abstractions.Services;
using ResourceMindAI.Application.DTOs.Manager;
using ResourceMindAI.Application.Services;
using ResourceMindAI.Application.Tests.TestData;
using ResourceMindAI.Domain.Entities;
using ResourceMindAI.Domain.Enums;
using ResourceMindAI.Domain.Exceptions;

namespace ResourceMindAI.Application.Tests.Allocations;

public class ManagerAllocationTests
{
    private readonly Mock<IManagerRepository> _repository = new();
    private readonly Mock<ISystemConfigRepository> _config = new();
    private readonly Mock<ILlmClient> _llm = new();
    private readonly ManagerService _sut;

    public ManagerAllocationTests()
    {
        _sut = new ManagerService(
            _repository.Object,
            _config.Object,
            _llm.Object,
            Mock.Of<ILogger<ManagerService>>());
    }

    [Fact]
    public async Task AllocateAsync_WithValidRequest_ShouldCreateAllocation()
    {
        var context = SetupAllocationContext();
        _repository.Setup(x => x.GetOverlappingAllocationPercentAsync(
                context.Employee.Id,
                context.From,
                context.To,
                null))
            .ReturnsAsync(40);
        _repository.Setup(x => x.AddAllocationAsync(It.IsAny<Allocation>()))
            .ReturnsAsync((Allocation allocation) => allocation);

        var result = await _sut.AllocateAsync(
            context.Manager.Id,
            Request(context, utilisation: 60));

        result.UtilisationPercent.Should().Be(60);
        _repository.Verify(x => x.AddAllocationAsync(
            It.Is<Allocation>(allocation =>
                allocation.UserId == context.Employee.Id
                && allocation.ProjectId == context.Project.Id)), Times.Once);
    }

    [Theory]
    [InlineData(ProjectStatus.Completed)]
    [InlineData(ProjectStatus.Cancelled)]
    [InlineData(ProjectStatus.OnHold)]
    public async Task AllocateAsync_WhenProjectCannotAcceptAllocations_ShouldThrowValidationException(
        ProjectStatus status)
    {
        var context = SetupAllocationContext(status);

        var act = () => _sut.AllocateAsync(context.Manager.Id, Request(context));

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Active or Planned*");
    }

    [Fact]
    public async Task AllocateAsync_WhenFromDateIsNotBeforeToDate_ShouldThrowValidationException()
    {
        var context = SetupAllocationContext();
        var request = Request(context);
        request.FromDate = context.To;
        request.ToDate = context.From;

        var act = () => _sut.AllocateAsync(context.Manager.Id, request);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("From date must be before to date.");
    }

    [Fact]
    public async Task AllocateAsync_WhenEndDateExceedsProjectEndDate_ShouldThrowValidationException()
    {
        var context = SetupAllocationContext();
        var request = Request(context);
        request.ToDate = context.Project.EndDate!.Value.AddDays(1);

        var act = () => _sut.AllocateAsync(context.Manager.Id, request);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*cannot be after the project end date*");
    }

    [Fact]
    public async Task AllocateAsync_WhenOverlappingUtilisationExceedsHundred_ShouldThrowValidationException()
    {
        var context = SetupAllocationContext();
        _repository.Setup(x => x.GetOverlappingAllocationPercentAsync(
                context.Employee.Id,
                context.From,
                context.To,
                null))
            .ReturnsAsync(60);

        var act = () => _sut.AllocateAsync(
            context.Manager.Id,
            Request(context, utilisation: 50));

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*cannot exceed 100%*");
    }

    [Fact]
    public async Task AllocateAsync_WhenOverlappingUtilisationTotalsHundred_ShouldAllowAllocation()
    {
        var context = SetupAllocationContext();
        _repository.Setup(x => x.GetOverlappingAllocationPercentAsync(
                context.Employee.Id,
                context.From,
                context.To,
                null))
            .ReturnsAsync(50);
        _repository.Setup(x => x.AddAllocationAsync(It.IsAny<Allocation>()))
            .ReturnsAsync((Allocation allocation) => allocation);

        var result = await _sut.AllocateAsync(
            context.Manager.Id,
            Request(context, utilisation: 50));

        result.UtilisationPercent.Should().Be(50);
    }

    [Fact]
    public async Task EndAllocationAsync_WhenAllocationExists_ShouldEndItToday()
    {
        var context = SetupAllocationContext();
        var allocation = TestDataBuilder.Allocation(context.Employee, context.Project);
        _repository.Setup(x => x.GetAllocationAsync(context.Manager.Id, allocation.Id))
            .ReturnsAsync(allocation);

        var result = await _sut.EndAllocationAsync(context.Manager.Id, allocation.Id);

        result.IsActive.Should().BeFalse();
        allocation.ToDate.Should().Be(DateTime.UtcNow.Date);
        _repository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    private AllocationContext SetupAllocationContext(
        ProjectStatus status = ProjectStatus.Active)
    {
        var manager = TestDataBuilder.User(Role.Manager);
        var employee = TestDataBuilder.User();
        var profile = TestDataBuilder.Profile(employee, manager);
        var project = TestDataBuilder.Project(manager, status);
        var from = DateTime.UtcNow.Date.AddDays(1);
        var to = from.AddDays(10);
        _repository.Setup(x => x.GetProjectAsync(manager.Id, project.Id)).ReturnsAsync(project);
        _repository.Setup(x => x.GetTeamEmployeeAsync(manager.Id, employee.Id))
            .ReturnsAsync(profile);
        return new AllocationContext(manager, employee, project, from, to);
    }

    private static CreateManagerAllocationDto Request(
        AllocationContext context,
        decimal utilisation = 50)
    {
        return new CreateManagerAllocationDto
        {
            ProjectId = context.Project.Id,
            EmployeeId = context.Employee.Id,
            UtilisationPercent = utilisation,
            FromDate = context.From,
            ToDate = context.To
        };
    }

    private sealed record AllocationContext(
        User Manager,
        User Employee,
        Project Project,
        DateTime From,
        DateTime To);
}
