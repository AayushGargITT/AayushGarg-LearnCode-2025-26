using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ResourceMindAI.Application.Abstractions.Repositories;
using ResourceMindAI.Application.Abstractions.Services;
using ResourceMindAI.Application.Services;
using ResourceMindAI.Application.Tests.TestData;
using ResourceMindAI.Domain.Entities;
using ResourceMindAI.Domain.Enums;

namespace ResourceMindAI.Application.Tests.Managers;

public class ManagerReadModelTests
{
    private readonly Mock<IManagerRepository> _repository = new();
    private readonly ManagerService _sut;

    public ManagerReadModelTests()
    {
        _sut = new ManagerService(
            _repository.Object,
            Mock.Of<ISystemConfigRepository>(),
            Mock.Of<ILlmClient>(),
            Mock.Of<ILogger<ManagerService>>());
    }

    [Fact]
    public async Task GetResourceDashboardAsync_WhenCurrentAllocationExists_ShouldReturnAllocatedStatus()
    {
        var manager = TestDataBuilder.User(Role.Manager);
        var employee = TestDataBuilder.User();
        var profile = TestDataBuilder.Profile(employee, manager);
        TestDataBuilder.Allocation(employee, TestDataBuilder.Project(manager));
        _repository.Setup(x => x.GetTeamResourcesAsync(manager.Id)).ReturnsAsync([profile]);

        var result = await _sut.GetResourceDashboardAsync(manager.Id);

        result.ActiveResources.Should().ContainSingle();
        result.ActiveResources[0].CurrentStatus.Should().Be(ResourceStatus.Allocated);
        result.OnBench.Should().BeEmpty();
    }

    [Fact]
    public async Task GetResourceDashboardAsync_WhenNoCurrentAllocationExists_ShouldReturnBenchStatus()
    {
        var manager = TestDataBuilder.User(Role.Manager);
        var employee = TestDataBuilder.User();
        var profile = TestDataBuilder.Profile(employee, manager);
        _repository.Setup(x => x.GetTeamResourcesAsync(manager.Id)).ReturnsAsync([profile]);

        var result = await _sut.GetResourceDashboardAsync(manager.Id);

        result.OnBench.Should().ContainSingle();
        result.OnBench[0].CurrentStatus.Should().Be(ResourceStatus.Bench);
        result.ActiveResources.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSubmittedTimesheetsAsync_ShouldReturnEmployeeProjectAndActivityDetails()
    {
        var manager = TestDataBuilder.User(Role.Manager);
        var employee = TestDataBuilder.User();
        var project = TestDataBuilder.Project(manager);
        var timesheet = TestDataBuilder.Timesheet(employee, project, DateTime.UtcNow.Date.AddDays(-7));
        timesheet.ActivityTags.Add(new ActivityTag
        {
            Id = Guid.NewGuid(),
            TimesheetId = timesheet.Id,
            Timesheet = timesheet,
            TagName = "Backend API Development"
        });
        _repository.Setup(x => x.GetSubmittedTimesheetsAsync(manager.Id))
            .ReturnsAsync([timesheet]);

        var result = await _sut.GetSubmittedTimesheetsAsync(manager.Id);

        result.Should().ContainSingle();
        result[0].ResourceName.Should().Be(employee.FullName);
        result[0].ProjectName.Should().Be(project.Name);
        result[0].Tags.Should().ContainSingle("Backend API Development");
    }
}
