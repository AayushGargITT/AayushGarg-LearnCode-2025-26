using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ResourceMindAI.Application.Abstractions.Repositories;
using ResourceMindAI.Application.Abstractions.Services;
using ResourceMindAI.Application.DTOs.Notifications;
using ResourceMindAI.Application.Services;
using ResourceMindAI.Application.Tests.TestData;
using ResourceMindAI.Domain.Entities;
using ResourceMindAI.Domain.Enums;

namespace ResourceMindAI.Application.Tests.Scheduler;

public class ProjectHealthEmailProcessorTests
{
    private readonly Mock<ISchedulerRepository> _repository = new();
    private readonly Mock<IProjectHealthNotificationService> _notifications = new();

    [Fact]
    public async Task ProcessAsync_WhenSavedHealthIsAttention_ShouldRequestNotification()
    {
        var project = CreateProject("ATTENTION");
        SetupProjects(project);
        var sut = CreateService();

        await sut.ProcessAsync(CancellationToken.None);

        _notifications.Verify(x => x.NotifyAsync(
            It.Is<ProjectHealthNotificationRequestDto>(request =>
                request.ProjectId == project.Id
                && request.ManagerId == project.ManagerId
                && request.RiskSummary.OverallHealth == "ATTENTION"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_WhenSavedHealthIsOnTrack_ShouldNotRequestNotification()
    {
        SetupProjects(CreateProject("ON_TRACK"));
        var sut = CreateService();

        await sut.ProcessAsync(CancellationToken.None);

        _notifications.Verify(x => x.NotifyAsync(
            It.IsAny<ProjectHealthNotificationRequestDto>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_WhenSavedJsonIsInvalid_ShouldSkipProject()
    {
        var manager = TestDataBuilder.User(Role.Manager);
        var project = TestDataBuilder.Project(manager);
        project.RiskFlagsJson = "{not-json}";
        SetupProjects(project);
        var sut = CreateService();

        var act = () => sut.ProcessAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
        _notifications.Verify(x => x.NotifyAsync(
            It.IsAny<ProjectHealthNotificationRequestDto>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_WhenOneNotificationFails_ShouldContinueWithRemainingProjects()
    {
        var first = CreateProject("AT_RISK", "First");
        var second = CreateProject("ATTENTION", "Second");
        SetupProjects(first, second);
        _notifications.Setup(x => x.NotifyAsync(
                It.Is<ProjectHealthNotificationRequestDto>(request =>
                    request.ProjectId == first.Id),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Delivery failure"));
        var sut = CreateService();

        var act = () => sut.ProcessAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
        _notifications.Verify(x => x.NotifyAsync(
            It.Is<ProjectHealthNotificationRequestDto>(request =>
                request.ProjectId == second.Id),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private ProjectHealthEmailProcessor CreateService()
    {
        return new ProjectHealthEmailProcessor(
            _repository.Object,
            _notifications.Object,
            Mock.Of<ILogger<ProjectHealthEmailProcessor>>());
    }

    private void SetupProjects(params Project[] projects)
    {
        _repository.Setup(x => x.GetProjectHealthNotificationCandidatesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(projects);
    }

    private static Project CreateProject(string health, string name = "Apollo")
    {
        var manager = TestDataBuilder.User(Role.Manager);
        var project = TestDataBuilder.Project(manager, name: name);
        project.RiskFlagsJson = ProjectRiskSummarySerializer.Serialize(
            TestDataBuilder.RiskSummary(health));
        return project;
    }
}
