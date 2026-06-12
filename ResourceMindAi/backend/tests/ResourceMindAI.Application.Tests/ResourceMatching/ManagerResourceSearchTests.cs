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

namespace ResourceMindAI.Application.Tests.ResourceMatching;

public class ManagerResourceSearchTests
{
    private readonly Mock<IManagerRepository> _repository = new();
    private readonly Mock<ISystemConfigRepository> _config = new();
    private readonly Mock<ILlmClient> _llm = new();
    private readonly ManagerService _sut;

    public ManagerResourceSearchTests()
    {
        _sut = new ManagerService(
            _repository.Object,
            _config.Object,
            _llm.Object,
            Mock.Of<ILogger<ManagerService>>());
    }

    [Fact]
    public async Task FindResourcesAsync_ShouldExtractIntentAndSearchOrganizationCandidates()
    {
        var context = SetupSearch();
        var employee = Candidate("Java");
        _repository.Setup(x => x.GetOrganizationSearchCandidatesAsync())
            .ReturnsAsync([employee]);
        SetupSuccessfulExplanation(employee.Id);

        var result = await _sut.FindResourcesAsync(context.Manager.Id, context.Request);

        result.Matches.Should().ContainSingle();
        _llm.Verify(x => x.ExtractResourceIntentAsync(
            context.Request.Requirement,
            It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(x => x.GetOrganizationSearchCandidatesAsync(), Times.Once);
        _repository.Verify(x => x.GetTeamEmployeesAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task FindResourcesAsync_WhenEmployeeAlreadyAllocatedToProject_ShouldExcludeCandidateBeforeAi()
    {
        var context = SetupSearch();
        var employee = Candidate("Java");
        TestDataBuilder.Allocation(employee, context.Project);
        _repository.Setup(x => x.GetOrganizationSearchCandidatesAsync())
            .ReturnsAsync([employee]);

        var result = await _sut.FindResourcesAsync(context.Manager.Id, context.Request);

        result.Matches.Should().BeEmpty();
        _llm.Verify(x => x.ExplainResourceMatchesAsync(
            It.IsAny<ResourceCandidateExplanationRequestDto>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task FindResourcesAsync_ShouldCalculateAvailabilityFromOverlappingAllocations()
    {
        var context = SetupSearch(withDates: true);
        var employee = Candidate("Java");
        _repository.Setup(x => x.GetOrganizationSearchCandidatesAsync())
            .ReturnsAsync([employee]);
        _repository.Setup(x => x.GetOverlappingAllocationPercentAsync(
                employee.Id,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                null))
            .ReturnsAsync(35);
        SetupSuccessfulExplanation(employee.Id);

        var result = await _sut.FindResourcesAsync(context.Manager.Id, context.Request);

        result.Matches.Single().AvailablePercent.Should().Be(65);
    }

    [Fact]
    public async Task FindResourcesAsync_WhenAiReturnsRoleLikeSkill_ShouldMatchNormalizedStoredSkill()
    {
        var manager = TestDataBuilder.User(Role.Manager);
        var project = TestDataBuilder.Project(manager);
        var employee = Candidate("Angular", "Software Engineer");
        var request = new FindResourceRequestDto
        {
            ProjectId = project.Id,
            Requirement = "Need an Angular developer"
        };
        _repository.Setup(x => x.GetProjectAsync(manager.Id, project.Id)).ReturnsAsync(project);
        _repository.Setup(x => x.GetOrganizationSearchCandidatesAsync())
            .ReturnsAsync([employee]);
        _llm.Setup(x => x.ExtractResourceIntentAsync(
                request.Requirement,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestDataBuilder.Intent("Angular Developer"));
        SetupSuccessfulExplanation(employee.Id);

        var result = await _sut.FindResourcesAsync(manager.Id, request);

        result.Intent.RequiredSkills.Should().ContainSingle("angular");
        result.Matches.Should().ContainSingle(match => match.Employee.Id == employee.Id);
    }

    [Fact]
    public async Task FindResourcesAsync_ShouldSendOnlyFilteredCandidatesToExplanationAi()
    {
        var context = SetupSearch();
        var matching = Candidate("Java");
        var nonMatching = Candidate("JavaScript", "Frontend Developer");
        _repository.Setup(x => x.GetOrganizationSearchCandidatesAsync())
            .ReturnsAsync([matching, nonMatching]);
        ResourceCandidateExplanationRequestDto? aiRequest = null;
        _llm.Setup(x => x.ExplainResourceMatchesAsync(
                It.IsAny<ResourceCandidateExplanationRequestDto>(),
                It.IsAny<CancellationToken>()))
            .Callback<ResourceCandidateExplanationRequestDto, CancellationToken>(
                (request, _) => aiRequest = request)
            .ReturnsAsync(Explanation(matching.Id));

        await _sut.FindResourcesAsync(context.Manager.Id, context.Request);

        aiRequest.Should().NotBeNull();
        aiRequest!.Candidates.Should().ContainSingle(candidate =>
            candidate.EmployeeId == matching.Id);
    }

    [Fact]
    public async Task FindResourcesAsync_WhenAiIntroducesUnknownEmployee_ShouldIgnoreAiRanking()
    {
        var context = SetupSearch();
        var employee = Candidate("Java");
        _repository.Setup(x => x.GetOrganizationSearchCandidatesAsync())
            .ReturnsAsync([employee]);
        _llm.Setup(x => x.ExplainResourceMatchesAsync(
                It.IsAny<ResourceCandidateExplanationRequestDto>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Explanation(Guid.NewGuid()));

        var result = await _sut.FindResourcesAsync(context.Manager.Id, context.Request);

        result.Matches.Single().AiRank.Should().BeNull();
        result.Matches.Single().AiReason.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task FindResourcesAsync_WhenExplanationAiFails_ShouldReturnBackendFallbackReasons()
    {
        var context = SetupSearch();
        var employee = Candidate("Java");
        _repository.Setup(x => x.GetOrganizationSearchCandidatesAsync())
            .ReturnsAsync([employee]);
        _llm.Setup(x => x.ExplainResourceMatchesAsync(
                It.IsAny<ResourceCandidateExplanationRequestDto>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ExternalServiceException("AI unavailable"));

        var result = await _sut.FindResourcesAsync(context.Manager.Id, context.Request);

        var match = result.Matches.Single();
        match.AiReason.Should().Be(string.Join(" ", match.Reasons));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task FindResourcesAsync_ShouldReturnCurrentManagerOwnershipFlag(bool belongsToManager)
    {
        var context = SetupSearch();
        var employee = Candidate("Java");
        var owner = belongsToManager
            ? context.Manager
            : TestDataBuilder.User(Role.Manager, name: "Other Manager");
        employee.ResourceProfile!.ManagerId = owner.Id;
        employee.ResourceProfile.Manager = owner;
        _repository.Setup(x => x.GetOrganizationSearchCandidatesAsync())
            .ReturnsAsync([employee]);
        SetupSuccessfulExplanation(employee.Id);

        var result = await _sut.FindResourcesAsync(context.Manager.Id, context.Request);

        result.Matches.Single().IsUnderCurrentManager.Should().Be(belongsToManager);
    }

    private SearchContext SetupSearch(bool withDates = false)
    {
        var manager = TestDataBuilder.User(Role.Manager);
        var project = TestDataBuilder.Project(manager);
        var request = new FindResourceRequestDto
        {
            ProjectId = project.Id,
            Requirement = "Need a Java backend developer"
        };
        var intent = TestDataBuilder.Intent("Java");
        if (withDates)
        {
            intent.FromDate = DateTime.UtcNow.Date.AddDays(1).ToString("yyyy-MM-dd");
            intent.ToDate = DateTime.UtcNow.Date.AddDays(15).ToString("yyyy-MM-dd");
        }

        _repository.Setup(x => x.GetProjectAsync(manager.Id, project.Id)).ReturnsAsync(project);
        _llm.Setup(x => x.ExtractResourceIntentAsync(
                request.Requirement,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(intent);
        return new SearchContext(manager, project, request);
    }

    private static User Candidate(string skill, string designation = "Backend Developer")
    {
        var employee = TestDataBuilder.User();
        employee.Designation = designation;
        TestDataBuilder.Profile(employee);
        TestDataBuilder.Skill(employee, skill);
        return employee;
    }

    private void SetupSuccessfulExplanation(Guid employeeId)
    {
        _llm.Setup(x => x.ExplainResourceMatchesAsync(
                It.IsAny<ResourceCandidateExplanationRequestDto>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Explanation(employeeId));
    }

    private static ResourceCandidateExplanationResponseDto Explanation(Guid employeeId)
    {
        return new ResourceCandidateExplanationResponseDto
        {
            Matches =
            [
                new ResourceCandidateExplanationDto
                {
                    EmployeeId = employeeId,
                    AiRank = 1,
                    AiReason = "Strong Java match.",
                    Strengths = ["Java"],
                    Concerns = []
                }
            ]
        };
    }

    private sealed record SearchContext(
        User Manager,
        Project Project,
        FindResourceRequestDto Request);
}
