using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ResourceMindAI.Application.Abstractions.Repositories;
using ResourceMindAI.Application.Abstractions.Services;
using ResourceMindAI.Application.Services;
using ResourceMindAI.Application.Tests.TestData;
using ResourceMindAI.Domain.Entities;
using ResourceMindAI.Domain.Enums;
using ResourceMindAI.Domain.Exceptions;

namespace ResourceMindAI.Application.Tests.Timesheets;

public class TimesheetSubmissionEscalationServiceTests
{
    private readonly Mock<ITimesheetSubmissionIssueRepository> _repository = new();
    private readonly Mock<ITimesheetSubmissionNotificationService> _notifications = new();

    [Fact]
    public async Task ProcessAsync_OnMonday_ShouldCreateIssueAndSendFirstReminderOnce()
    {
        var Resource = ResourceWithManager();
        var monday = new DateTime(2026, 6, 15, 9, 0, 0, DateTimeKind.Utc);
        SetupResources(Resource);
        var sut = CreateService();

        await sut.ProcessAsync(monday, CancellationToken.None);
        var createdIssue = GetAddedIssue();
        await sut.ProcessAsync(monday.AddHours(2), CancellationToken.None);

        createdIssue.Status.Should().Be(TimesheetSubmissionIssueStatus.FirstReminderSent);
        createdIssue.WeekStartDate.Should().Be(new DateTime(2026, 6, 8));
        _notifications.Verify(x => x.SendFirstReminderAsync(
            Resource,
            createdIssue.WeekStartDate,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_OnTuesday_ShouldSendSecondReminderOnlyOnce()
    {
        var Resource = ResourceWithManager();
        var issue = Issue(Resource, TimesheetSubmissionIssueStatus.FirstReminderSent);
        var tuesday = new DateTime(2026, 6, 16, 9, 0, 0, DateTimeKind.Utc);
        SetupResources(Resource);
        SetupIssue(issue);
        var sut = CreateService();

        await sut.ProcessAsync(tuesday, CancellationToken.None);
        await sut.ProcessAsync(tuesday.AddHours(2), CancellationToken.None);

        issue.Status.Should().Be(TimesheetSubmissionIssueStatus.SecondReminderSent);
        _notifications.Verify(x => x.SendSecondReminderAsync(
            Resource,
            issue.WeekStartDate,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_OnWednesday_ShouldFreezeAndEscalate()
    {
        var Resource = ResourceWithManager();
        var issue = Issue(Resource, TimesheetSubmissionIssueStatus.SecondReminderSent);
        var wednesday = new DateTime(2026, 6, 17, 9, 0, 0, DateTimeKind.Utc);
        SetupResources(Resource);
        SetupIssue(issue);
        var sut = CreateService();

        await sut.ProcessAsync(wednesday, CancellationToken.None);

        issue.Status.Should().Be(TimesheetSubmissionIssueStatus.Frozen);
        issue.FrozenAtUtc.Should().Be(wednesday);
        _notifications.Verify(x => x.SendFrozenEscalationAsync(
            Resource,
            Resource.ResourceProfile!.Manager,
            issue.WeekStartDate,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_OnThursday_ShouldNotLoadresources()
    {
        var sut = CreateService();

        await sut.ProcessAsync(
            new DateTime(2026, 6, 18, 9, 0, 0, DateTimeKind.Utc),
            CancellationToken.None);

        _repository.Verify(x => x.GetActiveResourcesWithManagersAsync(
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RestoreAccessAsync_WhenManagerOwnsEmployee_ShouldRestoreFrozenIssue()
    {
        var Resource = ResourceWithManager();
        var manager = Resource.ResourceProfile!.Manager!;
        var issue = Issue(Resource, TimesheetSubmissionIssueStatus.Frozen);
        _repository.Setup(x => x.GetResourceWithManagerAsync(
                Resource.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Resource);
        SetupIssue(issue);
        var sut = CreateService();

        await sut.RestoreAccessAsync(
            manager.Id,
            Resource.Id,
            issue.WeekStartDate,
            CancellationToken.None);

        issue.Status.Should().Be(TimesheetSubmissionIssueStatus.Restored);
        issue.RestoredByManagerUserId.Should().Be(manager.Id);
        issue.RestoredAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task RestoreAccessAsync_WhenEmployeeBelongsToAnotherManager_ShouldReject()
    {
        var Resource = ResourceWithManager();
        _repository.Setup(x => x.GetResourceWithManagerAsync(
                Resource.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Resource);
        var sut = CreateService();

        var act = () => sut.RestoreAccessAsync(
            Guid.NewGuid(),
            Resource.Id,
            PreviousMonday(),
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*current manager*");
    }

    private TimesheetSubmissionEscalationService CreateService()
    {
        return new TimesheetSubmissionEscalationService(
            _repository.Object,
            _notifications.Object,
            Mock.Of<ILogger<TimesheetSubmissionEscalationService>>());
    }

    private void SetupResources(User Resource)
    {
        _repository.Setup(x => x.GetActiveResourcesWithManagersAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([Resource]);
        _repository.Setup(x => x.HasSubmittedTimesheetAsync(
                Resource.Id,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
    }

    private void SetupIssue(TimesheetSubmissionIssue issue)
    {
        _repository.Setup(x => x.GetAsync(
                issue.ResourceUserId,
                issue.WeekStartDate,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(issue);
    }

    private TimesheetSubmissionIssue GetAddedIssue()
    {
        var invocation = _repository.Invocations.Single(invocation =>
            invocation.Method.Name == nameof(ITimesheetSubmissionIssueRepository.AddAsync));
        var issue = (TimesheetSubmissionIssue)invocation.Arguments[0];
        SetupIssue(issue);
        return issue;
    }

    private static User ResourceWithManager()
    {
        var Resource = TestDataBuilder.User();
        var manager = TestDataBuilder.User(Role.Manager);
        TestDataBuilder.Profile(Resource, manager);
        return Resource;
    }

    private static TimesheetSubmissionIssue Issue(
        User Resource,
        TimesheetSubmissionIssueStatus status)
    {
        return new TimesheetSubmissionIssue
        {
            Id = Guid.NewGuid(),
            ResourceUserId = Resource.Id,
            ManagerUserId = Resource.ResourceProfile?.ManagerId,
            WeekStartDate = PreviousMonday(),
            Status = status,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-2)
        };
    }

    private static DateTime PreviousMonday()
    {
        return new DateTime(2026, 6, 8);
    }
}
