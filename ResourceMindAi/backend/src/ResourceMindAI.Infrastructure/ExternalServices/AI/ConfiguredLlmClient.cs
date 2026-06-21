using ResourceMindAI.Application.Abstractions.Services;
using ResourceMindAI.Application.DTOs.Manager;

namespace ResourceMindAI.Infrastructure.ExternalServices.AI;

public sealed class ConfiguredLlmClient : ILlmClient
{
    private readonly ILlmClientFactory _factory;

    public ConfiguredLlmClient(ILlmClientFactory factory)
    {
        _factory = factory;
    }

    public Task<ResourceIntentDto> ExtractResourceIntentAsync(
        string requirement,
        CancellationToken cancellationToken = default)
    {
        return _factory.CreateClient()
            .ExtractResourceIntentAsync(requirement, cancellationToken);
    }

    public Task<ResourceCandidateExplanationResponseDto> ExplainResourceMatchesAsync(
        ResourceCandidateExplanationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        return _factory.CreateClient()
            .ExplainResourceMatchesAsync(request, cancellationToken);
    }

    public Task<TeamBuilderAiResponseDto> BuildTeamAsync(
        TeamBuilderAiRequestDto request,
        CancellationToken cancellationToken = default)
    {
        return _factory.CreateClient()
            .BuildTeamAsync(request, cancellationToken);
    }

    public Task<ProjectRiskSummaryDto> GenerateProjectRiskSummaryAsync(
        ProjectRiskFactsDto facts,
        CancellationToken cancellationToken = default)
    {
        return _factory.CreateClient()
            .GenerateProjectRiskSummaryAsync(facts, cancellationToken);
    }
}
