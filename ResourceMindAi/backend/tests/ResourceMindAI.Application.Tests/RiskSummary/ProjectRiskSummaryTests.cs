using System.Text.Json;
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

namespace ResourceMindAI.Application.Tests.RiskSummary;

public class ProjectRiskSummaryTests
{
    private readonly Mock<IManagerRepository> _repository = new();
    private readonly Mock<ISystemConfigRepository> _config = new();
    private readonly Mock<ILlmClient> _llm = new();
    private readonly ManagerService _sut;

    public ProjectRiskSummaryTests()
    {
        _config.Setup(x => x.GetMaxWeeklyHoursAsync()).ReturnsAsync(40m);
        _sut = new ManagerService(
            _repository.Object,
            _config.Object,
            _llm.Object,
            Mock.Of<ILogger<ManagerService>>());
    }

    [Fact]
    public async Task GenerateProjectRiskSummaryAsync_WhenManagerDoesNotOwnProject_ShouldRejectRequest()
    {
        var managerId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        _repository.Setup(x => x.GetProjectForRiskSummaryAsync(managerId, projectId))
            .ReturnsAsync((Project?)null);

        var act = () => _sut.GenerateProjectRiskSummaryAsync(managerId, projectId);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*managed by you*");
        _llm.Verify(x => x.GenerateProjectRiskSummaryAsync(
            It.IsAny<ProjectRiskFactsDto>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GenerateProjectRiskSummaryAsync_ShouldSendEssentialFactsAndSaveValidSummary()
    {
        var context = SetupProject();
        ProjectRiskFactsDto? sentFacts = null;
        _llm.Setup(x => x.GenerateProjectRiskSummaryAsync(
                It.IsAny<ProjectRiskFactsDto>(),
                It.IsAny<CancellationToken>()))
            .Callback<ProjectRiskFactsDto, CancellationToken>((facts, _) => sentFacts = facts)
            .ReturnsAsync(TestDataBuilder.RiskSummary("ATTENTION"));

        var result = await _sut.GenerateProjectRiskSummaryAsync(
            context.Manager.Id,
            context.Project.Id);

        result.OverallHealth.Should().Be("ATTENTION");
        sentFacts.Should().NotBeNull();
        sentFacts!.ProjectName.Should().Be(context.Project.Name);
        sentFacts.ActiveAllocations.Should().ContainSingle();
        sentFacts.ActiveAllocations[0].ExpectedWeeklyHours.Should().Be(20);
        context.Project.RiskFlagsJson.Should().NotBeNullOrWhiteSpace();
        _repository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task GenerateProjectRiskSummaryAsync_WhenAiReturnsInvalidSummary_ShouldPreserveAndReturnSavedSummary()
    {
        var context = SetupProject();
        const string existingJson = """{"overallHealth":"ON_TRACK","summary":"Saved","riskPoints":[],"recommendedActions":[],"generatedAt":"2026-06-10T12:00:00Z"}""";
        context.Project.RiskFlagsJson = existingJson;
        _llm.Setup(x => x.GenerateProjectRiskSummaryAsync(
                It.IsAny<ProjectRiskFactsDto>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestDataBuilder.RiskSummary("UNKNOWN"));

        var result = await _sut.GenerateProjectRiskSummaryAsync(
            context.Manager.Id,
            context.Project.Id);

        result.OverallHealth.Should().Be("ON_TRACK");
        result.Summary.Should().Be("Saved");
        context.Project.RiskFlagsJson.Should().Be(existingJson);
        _repository.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task GenerateProjectRiskSummaryAsync_WhenAiFailsAndSavedSummaryExists_ShouldReturnSavedSummary()
    {
        var context = SetupProject();
        var saved = TestDataBuilder.RiskSummary();
        context.Project.RiskFlagsJson = JsonSerializer.Serialize(
            saved,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        _llm.Setup(x => x.GenerateProjectRiskSummaryAsync(
                It.IsAny<ProjectRiskFactsDto>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ExternalServiceException("AI unavailable"));

        var result = await _sut.GenerateProjectRiskSummaryAsync(
            context.Manager.Id,
            context.Project.Id);

        result.Summary.Should().Be(saved.Summary);
        context.Project.RiskFlagsJson.Should().NotBeNullOrWhiteSpace();
    }

    private RiskContext SetupProject()
    {
        var manager = TestDataBuilder.User(Role.Manager);
        var employee = TestDataBuilder.User();
        var project = TestDataBuilder.Project(manager);
        TestDataBuilder.Allocation(employee, project, 50);
        _repository.Setup(x => x.GetProjectForRiskSummaryAsync(manager.Id, project.Id))
            .ReturnsAsync(project);
        return new RiskContext(manager, project);
    }

    private sealed record RiskContext(User Manager, Project Project);
}
