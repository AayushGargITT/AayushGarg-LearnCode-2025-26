using FluentAssertions;
using Microsoft.Extensions.Configuration;
using ResourceMindAI.Application.DTOs.Manager;
using ResourceMindAI.Domain.Exceptions;
using ResourceMindAI.Infrastructure.ExternalServices.AI;

namespace ResourceMindAI.Infrastructure.Tests.ExternalServices.AI;

public class LlmClientFactoryTests
{
    [Theory]
    [InlineData("Gemini", LlmProviderNames.Gemini)]
    [InlineData("gemma", LlmProviderNames.Gemma)]
    public void CreateClient_WhenProviderIsConfigured_ShouldReturnMatchingClient(
        string configuredProvider,
        string expectedProvider)
    {
        var factory = CreateFactory(
            configuredProvider,
            new FakeProviderClient(LlmProviderNames.Gemini),
            new FakeProviderClient(LlmProviderNames.Gemma));

        var result = factory.CreateClient();

        result.ProviderName.Should().Be(expectedProvider);
    }

    [Fact]
    public void CreateClient_WhenProviderIsUnsupported_ShouldThrowExternalServiceException()
    {
        var factory = CreateFactory(
            "FutureProvider",
            new FakeProviderClient(LlmProviderNames.Gemini));

        var act = factory.CreateClient;

        act.Should().Throw<ExternalServiceException>()
            .WithMessage("LLM provider 'FutureProvider' is not supported.");
    }

    [Fact]
    public void CreateClient_WhenActiveProviderIsMissing_ShouldThrowExternalServiceException()
    {
        var factory = CreateFactory(
            null,
            new FakeProviderClient(LlmProviderNames.Gemini));

        var act = factory.CreateClient;

        act.Should().Throw<ExternalServiceException>()
            .WithMessage("Active LLM provider is not configured.");
    }

    private static LlmClientFactory CreateFactory(
        string? providerName,
        params ILlmProviderClient[] clients)
    {
        var values = new Dictionary<string, string?>();
        if (providerName is not null)
        {
            values["LLMProvider:ActiveProvider"] = providerName;
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        return new LlmClientFactory(clients, configuration);
    }

    private sealed class FakeProviderClient : ILlmProviderClient
    {
        public FakeProviderClient(string providerName)
        {
            ProviderName = providerName;
        }

        public string ProviderName { get; }

        public Task<ResourceIntentDto> ExtractResourceIntentAsync(
            string requirement,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<ResourceCandidateExplanationResponseDto> ExplainResourceMatchesAsync(
            ResourceCandidateExplanationRequestDto request,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<TeamBuilderAiResponseDto> BuildTeamAsync(
            TeamBuilderAiRequestDto request,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<ProjectRiskSummaryDto> GenerateProjectRiskSummaryAsync(
            ProjectRiskFactsDto facts,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
