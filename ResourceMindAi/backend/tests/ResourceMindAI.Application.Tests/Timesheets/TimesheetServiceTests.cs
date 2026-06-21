using FluentAssertions;
using Moq;
using ResourceMindAI.Application.Abstractions.Repositories;
using ResourceMindAI.Application.Constants;
using ResourceMindAI.Application.DTOs.Resource;
using ResourceMindAI.Application.Services;
using ResourceMindAI.Application.Tests.TestData;
using ResourceMindAI.Domain.Entities;
using ResourceMindAI.Domain.Exceptions;

namespace ResourceMindAI.Application.Tests.Timesheets;

public class TimesheetServiceTests
{
    private readonly Mock<ITimesheetRepository> _timesheets = new();
    private readonly Mock<ITimesheetSubmissionIssueRepository> _submissionIssues = new();
    private readonly Mock<ISystemConfigRepository> _config = new();
    private readonly TimesheetService _sut;

    public TimesheetServiceTests()
    {
        _config.Setup(x => x.GetMaxWeeklyHoursAsync()).ReturnsAsync(40m);
        _sut = new TimesheetService(
            _timesheets.Object,
            _submissionIssues.Object,
            _config.Object);
    }

    [Fact]
    public async Task SubmitAsync_WithActiveAllocation_ShouldSaveSubmittedTimesheet()
    {
        var resource = TestDataBuilder.User();
        var manager = TestDataBuilder.User(ResourceMindAI.Domain.Enums.Role.Manager);
        var project = TestDataBuilder.Project(manager);
        var week = PreviousMonday();
        var allocation = TestDataBuilder.Allocation(
            resource,
            project,
            utilisation: 50,
            fromDate: week,
            toDate: week.AddDays(6));
        SetupEmployee(resource);
        _timesheets.Setup(x => x.GetAllocationsForWeekAsync(resource.Id, week, week.AddDays(6)))
            .ReturnsAsync([allocation]);

        await _sut.SubmitAsync(resource.Id, Request(week, project.Id, 20));

        _timesheets.Verify(x => x.AddRangeAsync(
            It.Is<IReadOnlyCollection<Timesheet>>(items =>
                items.Count == 1
                && items.Single().Status == ResourceMindAI.Domain.Enums.TimesheetStatus.Submitted
                && items.Single().HoursLogged == 20)), Times.Once);
    }

    [Fact]
    public async Task SubmitAsync_WhenTimesheetAlreadyExists_ShouldThrowConflictException()
    {
        var resource = TestDataBuilder.User();
        var week = PreviousMonday();
        SetupEmployee(resource);
        _timesheets.Setup(x => x.HasTimesheetForWeekAsync(resource.Id, week)).ReturnsAsync(true);

        var act = () => _sut.SubmitAsync(resource.Id, Request(week, Guid.NewGuid(), 8));

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*already been submitted*");
    }

    [Fact]
    public async Task SubmitAsync_WhenWeekIsFrozen_ShouldRejectSubmission()
    {
        var resource = TestDataBuilder.User();
        var week = PreviousMonday();
        SetupEmployee(resource);
        _submissionIssues.Setup(x => x.IsFrozenAsync(
                resource.Id,
                week,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var act = () => _sut.SubmitAsync(
            resource.Id,
            Request(week, Guid.NewGuid(), 8));

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*frozen*contact your manager*");
        _timesheets.Verify(x => x.AddRangeAsync(
            It.IsAny<IReadOnlyCollection<Timesheet>>()), Times.Never);
    }

    [Fact]
    public async Task SubmitAsync_ForFutureWeek_ShouldThrowValidationException()
    {
        var resource = TestDataBuilder.User();
        SetupEmployee(resource);
        var futureMonday = CurrentMonday().AddDays(7);

        var act = () => _sut.SubmitAsync(
            resource.Id,
            Request(futureMonday, Guid.NewGuid(), 8));

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*future week*");
    }

    [Fact]
    public async Task SubmitAsync_WhenProjectHoursExceedAllocationCapacity_ShouldThrowValidationException()
    {
        var resource = TestDataBuilder.User();
        var manager = TestDataBuilder.User(ResourceMindAI.Domain.Enums.Role.Manager);
        var project = TestDataBuilder.Project(manager);
        var week = PreviousMonday();
        var allocation = TestDataBuilder.Allocation(
            resource,
            project,
            utilisation: 50,
            fromDate: week,
            toDate: week.AddDays(6));
        SetupEmployee(resource);
        _timesheets.Setup(x => x.GetAllocationsForWeekAsync(resource.Id, week, week.AddDays(6)))
            .ReturnsAsync([allocation]);

        var act = () => _sut.SubmitAsync(resource.Id, Request(week, project.Id, 21));

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*cannot exceed 20*");
    }

    [Fact]
    public async Task SubmitAsync_WhenTotalHoursExceedConfiguredMaximum_ShouldThrowValidationException()
    {
        var resource = TestDataBuilder.User();
        var week = PreviousMonday();
        SetupEmployee(resource);

        var request = new SubmitResourceTimesheetDto
        {
            WeekStartDate = week,
            Entries =
            [
                Entry(Guid.NewGuid(), 25),
                Entry(Guid.NewGuid(), 20)
            ]
        };

        var act = () => _sut.SubmitAsync(resource.Id, request);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*weekly maximum of 40*");
        _timesheets.Verify(x => x.GetAllocationsForWeekAsync(
            It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Never);
    }

    [Fact]
    public async Task GetHistoryAsync_WithSubmittedAndUnsubmittedAllocatedWeeks_ShouldReturnSubmittedAndMissed()
    {
        var resource = TestDataBuilder.User();
        var manager = TestDataBuilder.User(ResourceMindAI.Domain.Enums.Role.Manager);
        var project = TestDataBuilder.Project(manager);
        var submittedWeek = CurrentMonday().AddDays(-14);
        var missedWeek = CurrentMonday().AddDays(-7);
        var allocation = TestDataBuilder.Allocation(
            resource,
            project,
            fromDate: submittedWeek,
            toDate: missedWeek.AddDays(6));
        var timesheet = TestDataBuilder.Timesheet(resource, project, submittedWeek);
        SetupEmployee(resource);
        _timesheets.Setup(x => x.GetTimesheetsAsync(resource.Id)).ReturnsAsync([timesheet]);
        _timesheets.Setup(x => x.GetAllocationsAsync(resource.Id)).ReturnsAsync([allocation]);

        var result = await _sut.GetHistoryAsync(resource.Id);

        result.Should().Contain(item => item.WeekStartDate == submittedWeek && item.Status == "Submitted");
        result.Should().Contain(item => item.WeekStartDate == missedWeek && item.Status == "Missed");
    }

    [Fact]
    public async Task GetWeekDetailAsync_WhenSubmitted_ShouldReturnProjectTags()
    {
        var resource = TestDataBuilder.User();
        var manager = TestDataBuilder.User(ResourceMindAI.Domain.Enums.Role.Manager);
        var project = TestDataBuilder.Project(manager);
        var week = PreviousMonday();
        var timesheet = TestDataBuilder.Timesheet(resource, project, week);
        timesheet.ActivityTags.Add(new ActivityTag
        {
            Id = Guid.NewGuid(),
            TimesheetId = timesheet.Id,
            Timesheet = timesheet,
            TagName = ActivityTagCatalog.Allowed.First()
        });
        SetupEmployee(resource);
        _timesheets.Setup(x => x.GetTimesheetsAsync(resource.Id)).ReturnsAsync([timesheet]);

        var result = await _sut.GetWeekDetailAsync(resource.Id, week);

        result.Status.Should().Be("Submitted");
        result.Entries.Should().ContainSingle();
        result.Entries[0].ActivityTags.Should().ContainSingle();
    }

    private void SetupEmployee(User resource)
    {
        _timesheets.Setup(x => x.GetResourceUserAsync(resource.Id)).ReturnsAsync(resource);
        _timesheets.Setup(x => x.HasTimesheetForWeekAsync(resource.Id, It.IsAny<DateTime>()))
            .ReturnsAsync(false);
    }

    private static SubmitResourceTimesheetDto Request(
        DateTime week,
        Guid projectId,
        decimal hours)
    {
        return new SubmitResourceTimesheetDto
        {
            WeekStartDate = week,
            Entries = [Entry(projectId, hours)]
        };
    }

    private static SubmitResourceTimesheetEntryDto Entry(Guid projectId, decimal hours)
    {
        return new SubmitResourceTimesheetEntryDto
        {
            ProjectId = projectId,
            Hours = hours,
            ActivityTags = [ActivityTagCatalog.Allowed.First()]
        };
    }

    private static DateTime CurrentMonday()
    {
        var today = DateTime.UtcNow.Date;
        return today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
    }

    private static DateTime PreviousMonday() => CurrentMonday().AddDays(-7);
}
