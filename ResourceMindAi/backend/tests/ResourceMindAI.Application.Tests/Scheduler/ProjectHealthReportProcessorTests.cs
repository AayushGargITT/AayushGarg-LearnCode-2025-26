using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ResourceMindAI.Application.Abstractions.Repositories;
using ResourceMindAI.Application.Abstractions.Services;
using ResourceMindAI.Application.Services;
using ResourceMindAI.Application.Tests.TestData;
using ResourceMindAI.Domain.Enums;

namespace ResourceMindAI.Application.Tests.Scheduler;

public class ProjectHealthReportProcessorTests
{
    [Fact]
    public async Task ProcessAsync_WhenOneProjectFails_ShouldContinueWithRemainingProjects()
    {
        var repository = new Mock<ISchedulerRepository>();
        var managerService = new Mock<IManagerService>();
        var manager = TestDataBuilder.User(Role.Manager);
        var failedProject = TestDataBuilder.Project(manager, name: "Failed Project");
        var successfulProject = TestDataBuilder.Project(manager, name: "Successful Project");
        repository.Setup(x => x.GetRiskSummaryProjectsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([failedProject, successfulProject]);
        managerService.Setup(x => x.GenerateScheduledProjectRiskSummaryAsync(
                manager.Id,
                failedProject.Id))
            .ThrowsAsync(new InvalidOperationException("Failure"));
        managerService.Setup(x => x.GenerateScheduledProjectRiskSummaryAsync(
                manager.Id,
                successfulProject.Id))
            .ReturnsAsync(TestDataBuilder.RiskSummary());
        var sut = new ProjectHealthReportProcessor(
            repository.Object,
            managerService.Object,
            Mock.Of<ILogger<ProjectHealthReportProcessor>>());

        var act = () => sut.ProcessAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
        managerService.Verify(x => x.GenerateScheduledProjectRiskSummaryAsync(
            manager.Id,
            successfulProject.Id), Times.Once);
    }
}
