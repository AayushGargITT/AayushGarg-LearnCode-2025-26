using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ResourceMindAI.Application.Abstractions.Repositories;
using ResourceMindAI.Application.Abstractions.Services;
using ResourceMindAI.Application.Services;
using ResourceMindAI.Application.Tests.TestData;
using ResourceMindAI.Domain.Entities;
using ResourceMindAI.Domain.Enums;

namespace ResourceMindAI.Application.Tests.Scheduler;

public class SchedulerComputationServiceTests
{
    private readonly Mock<ISchedulerRepository> _repository = new();
    private readonly Mock<IManagerService> _managerService = new();
    private readonly SchedulerComputationService _sut;

    public SchedulerComputationServiceTests()
    {
        _sut = new SchedulerComputationService(
            _repository.Object,
            _managerService.Object,
            Mock.Of<ILogger<SchedulerComputationService>>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenOneProjectFails_ShouldContinueWithRemainingProjects()
    {
        var manager = TestDataBuilder.User(Role.Manager);
        var failedProject = TestDataBuilder.Project(manager, name: "Failed Project");
        var successfulProject = TestDataBuilder.Project(manager, name: "Successful Project");
        _repository.Setup(x => x.GetActiveEmployeesAsync(
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _repository.Setup(x => x.GetRiskSummaryProjectsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([failedProject, successfulProject]);
        _managerService.Setup(x => x.GenerateScheduledProjectRiskSummaryAsync(
                manager.Id,
                failedProject.Id))
            .ThrowsAsync(new InvalidOperationException("Failure"));
        _managerService.Setup(x => x.GenerateScheduledProjectRiskSummaryAsync(
                manager.Id,
                successfulProject.Id))
            .ReturnsAsync(TestDataBuilder.RiskSummary());

        var act = () => _sut.ExecuteAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
        _managerService.Verify(x => x.GenerateScheduledProjectRiskSummaryAsync(
            manager.Id,
            successfulProject.Id), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithAllocatedAndBenchEmployees_ShouldProcessBothWithoutPersistingNewStatus()
    {
        var manager = TestDataBuilder.User(Role.Manager);
        var allocated = TestDataBuilder.User(name: "Allocated Employee");
        var bench = TestDataBuilder.User(name: "Bench Employee");
        TestDataBuilder.Allocation(allocated, TestDataBuilder.Project(manager));
        _repository.Setup(x => x.GetActiveEmployeesAsync(
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([allocated, bench]);
        _repository.Setup(x => x.GetRiskSummaryProjectsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var act = () => _sut.ExecuteAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
        Enum.GetValues<ResourceStatus>().Should()
            .BeEquivalentTo([ResourceStatus.Bench, ResourceStatus.Allocated]);
    }
}
