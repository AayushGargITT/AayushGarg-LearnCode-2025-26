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
        var employee = EmployeeWithManager();
        var monday = new DateTime(2026, 6, 15, 9, 0, 0, DateTimeKind.Utc);
        SetupEmployees(employee);
        var sut = CreateService();

        await sut.ProcessAsync(monday, CancellationToken.None);
        var createdIssue = GetAddedIssue();
        await sut.ProcessAsync(monday.AddHours(2), CancellationToken.None);

        createdIssue.Status.Should().Be(TimesheetSubmissionIssueStatus.FirstReminderSent);
        createdIssue.WeekStartDate.Should().Be(new DateTime(2026, 6, 8));
        _notifications.Verify(x => x.SendFirstReminderAsync(
            employee,
            createdIssue.WeekStartDate,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_OnTuesday_ShouldSendSecondReminderOnlyOnce()
    {
        var employee = EmployeeWithManager();
        var issue = Issue(employee, TimesheetSubmissionIssueStatus.FirstReminderSent);
        var tuesday = new DateTime(2026, 6, 16, 9, 0, 0, DateTimeKind.Utc);
        SetupEmployees(employee);
        SetupIssue(issue);
        var sut = CreateService();

        await sut.ProcessAsync(tuesday, CancellationToken.None);
        await sut.ProcessAsync(tuesday.AddHours(2), CancellationToken.None);

        issue.Status.Should().Be(TimesheetSubmissionIssueStatus.SecondReminderSent);
        _notifications.Verify(x => x.SendSecondReminderAsync(
            employee,
            issue.WeekStartDate,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_OnWednesday_ShouldFreezeAndEscalate()
    {
        var employee = EmployeeWithManager();
        var issue = Issue(employee, TimesheetSubmissionIssueStatus.SecondReminderSent);
        var wednesday = new DateTime(2026, 6, 17, 9, 0, 0, DateTimeKind.Utc);
        SetupEmployees(employee);
        SetupIssue(issue);
        var sut = CreateService();

        await sut.ProcessAsync(wednesday, CancellationToken.None);

        issue.Status.Should().Be(TimesheetSubmissionIssueStatus.Frozen);
        issue.FrozenAtUtc.Should().Be(wednesday);
        _notifications.Verify(x => x.SendFrozenEscalationAsync(
            employee,
            employee.ResourceProfile!.Manager,
            issue.WeekStartDate,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_OnThursday_ShouldNotLoadEmployees()
    {
        var sut = CreateService();

        await sut.ProcessAsync(
            new DateTime(2026, 6, 18, 9, 0, 0, DateTimeKind.Utc),
            CancellationToken.None);

        _repository.Verify(x => x.GetActiveEmployeesWithManagersAsync(
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RestoreAccessAsync_WhenManagerOwnsEmployee_ShouldRestoreFrozenIssue()
    {
        var employee = EmployeeWithManager();
        var manager = employee.ResourceProfile!.Manager!;
        var issue = Issue(employee, TimesheetSubmissionIssueStatus.Frozen);
        _repository.Setup(x => x.GetEmployeeWithManagerAsync(
                employee.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);
        SetupIssue(issue);
        var sut = CreateService();

        await sut.RestoreAccessAsync(
            manager.Id,
            employee.Id,
            issue.WeekStartDate,
            CancellationToken.None);

        issue.Status.Should().Be(TimesheetSubmissionIssueStatus.Restored);
        issue.RestoredByManagerUserId.Should().Be(manager.Id);
        issue.RestoredAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task RestoreAccessAsync_WhenEmployeeBelongsToAnotherManager_ShouldReject()
    {
        var employee = EmployeeWithManager();
        _repository.Setup(x => x.GetEmployeeWithManagerAsync(
                employee.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);
        var sut = CreateService();

        var act = () => sut.RestoreAccessAsync(
            Guid.NewGuid(),
            employee.Id,
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

    private void SetupEmployees(User employee)
    {
        _repository.Setup(x => x.GetActiveEmployeesWithManagersAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([employee]);
        _repository.Setup(x => x.HasSubmittedTimesheetAsync(
                employee.Id,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
    }

    private void SetupIssue(TimesheetSubmissionIssue issue)
    {
        _repository.Setup(x => x.GetAsync(
                issue.EmployeeUserId,
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

    private static User EmployeeWithManager()
    {
        var employee = TestDataBuilder.User();
        var manager = TestDataBuilder.User(Role.Manager);
        TestDataBuilder.Profile(employee, manager);
        return employee;
    }

    private static TimesheetSubmissionIssue Issue(
        User employee,
        TimesheetSubmissionIssueStatus status)
    {
        return new TimesheetSubmissionIssue
        {
            Id = Guid.NewGuid(),
            EmployeeUserId = employee.Id,
            ManagerUserId = employee.ResourceProfile?.ManagerId,
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
