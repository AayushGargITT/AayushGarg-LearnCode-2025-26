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
        var matchingResource = CreateResourceUnderManager(
            project.Manager,
            "Nisha Rao",
            "QA Automation",
            "SQL");
        var unrelatedResource = CreateResourceUnderManager(
            project.Manager,
            "Ravi Kumar",
            "React");
        SetupProjects(project);
        SetupManagerResources(
            project.ManagerId,
            matchingResource,
            unrelatedResource);
        var sut = CreateService();

        await sut.ProcessAsync(CancellationToken.None);

        _notifications.Verify(x => x.NotifyAsync(
            It.Is<ProjectHealthNotificationRequestDto>(request =>
                request.ProjectId == project.Id
                && request.ManagerId == project.ManagerId
                && request.RiskSummary.OverallHealth == "ATTENTION"
                && request.MatchingResources.Count == 1
                && request.MatchingResources[0].FullName == "Nisha Rao"
                && request.MatchingResources[0].MatchedSkills.Contains("QA Automation")),
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
        _repository.Setup(x => x.GetActiveResourcesUnderManagerAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
    }

    private void SetupManagerResources(Guid managerId, params User[] resources)
    {
        _repository.Setup(x => x.GetActiveResourcesUnderManagerAsync(
                managerId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(resources);
    }

    private static Project CreateProject(string health, string name = "Apollo")
    {
        var manager = TestDataBuilder.User(Role.Manager);
        var project = TestDataBuilder.Project(manager, name: name);
        var suggestedSkills = health == "ATTENTION"
            ? new[] { "QA Automation" }
            : [];
        var summary = TestDataBuilder.RiskSummary(health);
        project.RiskFlagsJson = ProjectRiskSummarySerializer.Serialize(
            new ResourceMindAI.Application.DTOs.Manager.ProjectRiskSummaryDto
            {
                OverallHealth = summary.OverallHealth,
                Summary = summary.Summary,
                RiskPoints = summary.RiskPoints,
                RecommendedActions = summary.RecommendedActions,
                SuggestedSkills = suggestedSkills,
                GeneratedAt = summary.GeneratedAt
            });
        return project;
    }

    private static User CreateResourceUnderManager(
        User manager,
        string name,
        params string[] skills)
    {
        var resource = TestDataBuilder.User(Role.Resource, name: name);
        var profile = TestDataBuilder.Profile(resource, manager);
        foreach (var skill in skills)
        {
            profile.Skills.Add(new Skill
            {
                Id = Guid.NewGuid(),
                ResourceProfileId = profile.Id,
                ResourceProfile = profile,
                SkillName = skill,
                Category = SkillCategory.Technical,
                Proficiency = ProficiencyLevel.Intermediate,
                AddedAt = DateTime.UtcNow
            });
        }

        return resource;
    }
}
