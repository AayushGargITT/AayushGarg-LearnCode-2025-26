using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ResourceMindAI.Application.DTOs.Manager;
using ResourceMindAI.Domain.Exceptions;

namespace ResourceMindAI.Infrastructure.ExternalServices.AI;

public abstract class GoogleGenerativeLanguageClient : ILlmProviderClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger _logger;
    private readonly string _defaultModel;

    protected GoogleGenerativeLanguageClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger logger,
        string defaultModel)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _defaultModel = defaultModel;
    }

    public abstract string ProviderName { get; }

    public Task<ResourceIntentDto> ExtractResourceIntentAsync(
        string requirement,
        CancellationToken cancellationToken = default)
    {
        return GenerateAsync<ResourceIntentDto>(
            PromptBuilder.BuildResourceIntentRequest(
                requirement,
                DateOnly.FromDateTime(DateTime.UtcNow)),
            "resource intent extraction",
            cancellationToken);
    }

    public Task<ResourceCandidateExplanationResponseDto> ExplainResourceMatchesAsync(
        ResourceCandidateExplanationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        return GenerateAsync<ResourceCandidateExplanationResponseDto>(
            PromptBuilder.BuildResourceExplanationRequest(request),
            "resource candidate explanation",
            cancellationToken);
    }

    public Task<TeamBuilderAiResponseDto> BuildTeamAsync(
        TeamBuilderAiRequestDto request,
        CancellationToken cancellationToken = default)
    {
        return GenerateAsync<TeamBuilderAiResponseDto>(
            PromptBuilder.BuildTeamRequest(request),
            "team recommendation",
            cancellationToken);
    }

    public Task<ProjectRiskSummaryDto> GenerateProjectRiskSummaryAsync(
        ProjectRiskFactsDto facts,
        CancellationToken cancellationToken = default)
    {
        return GenerateAsync<ProjectRiskSummaryDto>(
            PromptBuilder.BuildProjectRiskRequest(facts),
            "project risk summary",
            cancellationToken);
    }

    private async Task<T> GenerateAsync<T>(
        object request,
        string operation,
        CancellationToken cancellationToken)
    {
        var providerSection = $"LLMProvider:Providers:{ProviderName}";
        var apiKey = _configuration[$"{providerSection}:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey)
            || apiKey.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase))
        {
            throw new ExternalServiceException(
                $"{ProviderName} API key is not configured.");
        }

        var model = _configuration[$"{providerSection}:Model"];
        if (string.IsNullOrWhiteSpace(model))
        {
            model = _defaultModel;
        }

        var endpoint =
            $"v1beta/models/{Uri.EscapeDataString(model)}:generateContent?key={Uri.EscapeDataString(apiKey)}";

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                endpoint,
                request,
                JsonOptions,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "{Provider} {Operation} failed with status {StatusCode}: {ResponseBody}",
                    ProviderName,
                    operation,
                    response.StatusCode,
                    errorBody);
                throw new ExternalServiceException(
                    $"AI {operation} is temporarily unavailable.");
            }

            var providerResponse = await response.Content.ReadFromJsonAsync<ProviderResponse>(
                JsonOptions,
                cancellationToken);
            var json = providerResponse?.Candidates?
                .FirstOrDefault()?.Content?.Parts?
                .FirstOrDefault()?.Text;

            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ExternalServiceException(
                    $"AI {operation} returned an empty response.");
            }

            return JsonSerializer.Deserialize<T>(json, JsonOptions)
                ?? throw new ExternalServiceException(
                    $"AI {operation} returned an invalid response.");
        }
        catch (ExternalServiceException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "{Provider} {Operation} failed",
                ProviderName,
                operation);
            throw new ExternalServiceException(
                $"AI {operation} is temporarily unavailable.",
                exception);
        }
    }

    private sealed class ProviderResponse
    {
        public List<Candidate>? Candidates { get; set; }
    }

    private sealed class Candidate
    {
        public Content? Content { get; set; }
    }

    private sealed class Content
    {
        public List<Part>? Parts { get; set; }
    }

    private sealed class Part
    {
        public string? Text { get; set; }
    }
}
