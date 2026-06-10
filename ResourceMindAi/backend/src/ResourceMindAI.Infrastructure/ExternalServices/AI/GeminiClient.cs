using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ResourceMindAI.Application.Abstractions.Services;
using ResourceMindAI.Application.DTOs.Manager;
using ResourceMindAI.Domain.Exceptions;

namespace ResourceMindAI.Infrastructure.ExternalServices.AI;

public class GeminiClient : ILlmClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GeminiClient> _logger;

    public GeminiClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GeminiClient> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

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
        var apiKey = _configuration["LLMProvider:Gemini"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ExternalServiceException("Gemini API key is not configured.");
        }

        var model = _configuration["LLMProvider:GeminiModel"] ?? "gemini-2.5-flash";
        var endpoint = $"v1beta/models/{model}:generateContent?key={Uri.EscapeDataString(apiKey)}";

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
                    "Gemini {Operation} failed with status {StatusCode}: {ResponseBody}",
                    operation,
                    response.StatusCode,
                    errorBody);
                throw new ExternalServiceException($"AI {operation} is temporarily unavailable.");
            }

            var geminiResponse = await response.Content.ReadFromJsonAsync<GeminiResponse>(
                JsonOptions,
                cancellationToken);
            var json = geminiResponse?.Candidates?
                .FirstOrDefault()?.Content?.Parts?
                .FirstOrDefault()?.Text;

            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ExternalServiceException($"AI {operation} returned an empty response.");
            }

            return JsonSerializer.Deserialize<T>(json, JsonOptions)
                ?? throw new ExternalServiceException($"AI {operation} returned an invalid response.");
        }
        catch (ExternalServiceException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Gemini {Operation} failed", operation);
            throw new ExternalServiceException($"AI {operation} is temporarily unavailable.", exception);
        }
    }

    private sealed class GeminiResponse
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
