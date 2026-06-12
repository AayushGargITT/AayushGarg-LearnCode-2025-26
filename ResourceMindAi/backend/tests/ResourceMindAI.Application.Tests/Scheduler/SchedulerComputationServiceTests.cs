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
    private readonly SchedulerComputationService _sut;

    public SchedulerComputationServiceTests()
    {
        _sut = new SchedulerComputationService(
            _repository.Object,
            Mock.Of<ILogger<SchedulerComputationService>>());
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
        var act = () => _sut.ExecuteAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
        Enum.GetValues<ResourceStatus>().Should()
            .BeEquivalentTo([ResourceStatus.Bench, ResourceStatus.Allocated]);
    }
}
