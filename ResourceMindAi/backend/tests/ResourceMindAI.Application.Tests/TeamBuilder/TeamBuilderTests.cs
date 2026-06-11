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

namespace ResourceMindAI.Application.Tests.TeamBuilder;

public class TeamBuilderTests
{
    private readonly Mock<IManagerRepository> _repository = new();
    private readonly Mock<ISystemConfigRepository> _config = new();
    private readonly Mock<ILlmClient> _llm = new();
    private readonly ManagerService _sut;

    public TeamBuilderTests()
    {
        _sut = new ManagerService(
            _repository.Object,
            _config.Object,
            _llm.Object,
            Mock.Of<ILogger<ManagerService>>());
    }

    [Fact]
    public async Task BuildTeamAsync_ShouldUseOrganizationWideBenchCandidatesOnly()
    {
        var context = Setup();
        var bench = Candidate("Java");
        var allocated = Candidate("Java");
        TestDataBuilder.Allocation(allocated, TestDataBuilder.Project(context.Manager));
        _repository.Setup(x => x.GetOrganizationSearchCandidatesAsync())
            .ReturnsAsync([bench, allocated]);
        TeamBuilderAiRequestDto? aiRequest = null;
        _llm.Setup(x => x.BuildTeamAsync(
                It.IsAny<TeamBuilderAiRequestDto>(),
                It.IsAny<CancellationToken>()))
            .Callback<TeamBuilderAiRequestDto, CancellationToken>((request, _) => aiRequest = request)
            .ReturnsAsync(TeamResponse(bench.Id));

        var result = await _sut.BuildTeamAsync(context.Manager.Id, context.Request);

        aiRequest!.Candidates.Should().ContainSingle(candidate =>
            candidate.EmployeeId == bench.Id && candidate.AvailablePercent == 100);
        result.Members.Should().ContainSingle(member => member.Employee.Id == bench.Id);
        _repository.Verify(x => x.GetOrganizationSearchCandidatesAsync(), Times.Once);
    }

    [Fact]
    public async Task BuildTeamAsync_WhenAiReturnsUnknownEmployee_ShouldRejectResponse()
    {
        var context = Setup();
        var bench = Candidate("Java");
        _repository.Setup(x => x.GetOrganizationSearchCandidatesAsync()).ReturnsAsync([bench]);
        _llm.Setup(x => x.BuildTeamAsync(
                It.IsAny<TeamBuilderAiRequestDto>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(TeamResponse(Guid.NewGuid()));

        var act = () => _sut.BuildTeamAsync(context.Manager.Id, context.Request);

        await act.Should().ThrowAsync<ExternalServiceException>()
            .WithMessage("*invalid members*");
    }

    [Fact]
    public async Task BuildTeamAsync_WhenNoCandidatesMatch_ShouldReturnMissingSkills()
    {
        var context = Setup();
        _repository.Setup(x => x.GetOrganizationSearchCandidatesAsync()).ReturnsAsync([]);

        var result = await _sut.BuildTeamAsync(context.Manager.Id, context.Request);

        result.Members.Should().BeEmpty();
        result.MissingSkills.Should().ContainSingle("java");
        result.TeamSummary.Should().Contain("No bench employees");
        _llm.Verify(x => x.BuildTeamAsync(
            It.IsAny<TeamBuilderAiRequestDto>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    private TeamContext Setup()
    {
        var manager = TestDataBuilder.User(Role.Manager);
        var project = TestDataBuilder.Project(manager);
        var request = new BuildTeamRequestDto
        {
            ProjectId = project.Id,
            Requirement = "Build a Java delivery team"
        };
        _repository.Setup(x => x.GetProjectAsync(manager.Id, project.Id)).ReturnsAsync(project);
        _llm.Setup(x => x.ExtractResourceIntentAsync(
                request.Requirement,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestDataBuilder.Intent("Java"));
        return new TeamContext(manager, request);
    }

    private static User Candidate(string skill)
    {
        var employee = TestDataBuilder.User();
        TestDataBuilder.Profile(employee);
        TestDataBuilder.Skill(employee, skill);
        return employee;
    }

    private static TeamBuilderAiResponseDto TeamResponse(Guid employeeId)
    {
        return new TeamBuilderAiResponseDto
        {
            TeamSummary = "Recommended backend team.",
            Members =
            [
                new TeamBuilderAiMemberDto
                {
                    EmployeeId = employeeId,
                    SuggestedRole = "Backend Developer",
                    Reason = "Strong Java skills.",
                    MatchedSkills = ["Java"]
                }
            ],
            MissingSkills = ["DevOps"]
        };
    }

    private sealed record TeamContext(User Manager, BuildTeamRequestDto Request);
}
