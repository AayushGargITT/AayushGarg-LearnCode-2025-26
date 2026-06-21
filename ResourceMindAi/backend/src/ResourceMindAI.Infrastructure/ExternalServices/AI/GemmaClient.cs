using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ResourceMindAI.Application.DTOs.Manager;
using ResourceMindAI.Domain.Exceptions;

namespace ResourceMindAI.Infrastructure.ExternalServices.AI;

public sealed class GemmaClient : ILlmProviderClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GemmaClient> _logger;

    public GemmaClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GemmaClient> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public string ProviderName => LlmProviderNames.Gemma;

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
        object promptRequest,
        string operation,
        CancellationToken cancellationToken)
    {
        const string providerSection = "LLMProvider:Providers:Gemma";
        var endpoint = _configuration[$"{providerSection}:Endpoint"];
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var endpointUri))
        {
            throw new ExternalServiceException("Gemma endpoint is not configured.");
        }

        var model = _configuration[$"{providerSection}:Model"];
        if (string.IsNullOrWhiteSpace(model))
        {
            throw new ExternalServiceException("Gemma model is not configured.");
        }

        var requestPayload = new
        {
            model,
            prompt = BuildPrompt(promptRequest),
            stream = false,
            format = "json",
            options = new { temperature = 0.1 }
        };
        var requestBody = JsonSerializer.Serialize(requestPayload, JsonOptions);
        var contentType = _configuration[$"{providerSection}:ContentType"]
            ?? "application/json";

        using var request = new HttpRequestMessage(HttpMethod.Post, endpointUri)
        {
            Content = new StringContent(requestBody, Encoding.UTF8, contentType)
        };
        AddCurlCompatibleHeaders(request, providerSection);
        AddApiKey(request, providerSection);

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Gemma {Operation} failed with status {StatusCode}: {ResponseBody}",
                    operation,
                    response.StatusCode,
                    responseBody);
                throw new ExternalServiceException(
                    $"AI {operation} is temporarily unavailable.");
            }

            var json = ExtractGeneratedJson(responseBody);
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
            _logger.LogError(exception, "Gemma {Operation} failed", operation);
            throw new ExternalServiceException(
                $"AI {operation} is temporarily unavailable.",
                exception);
        }
    }

    private void AddApiKey(HttpRequestMessage request, string providerSection)
    {
        var configuredApiKey = _configuration[$"{providerSection}:ApiKey"];
        var apiKey = configuredApiKey?.Trim();
        var headerName = _configuration[$"{providerSection}:ApiKeyHeader"]
            ?? "ApiKey";

        if (string.IsNullOrWhiteSpace(apiKey)
            || apiKey.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (headerName.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
        {
            var scheme = _configuration[$"{providerSection}:ApiKeyScheme"] ?? "Bearer";
            request.Headers.Authorization = new AuthenticationHeaderValue(scheme, apiKey);
            return;
        }

        request.Headers.TryAddWithoutValidation(headerName, apiKey);
    }

    private void AddCurlCompatibleHeaders(
        HttpRequestMessage request,
        string providerSection)
    {
        var acceptHeader = _configuration[$"{providerSection}:Accept"] ?? "*/*";
        if (!string.IsNullOrWhiteSpace(acceptHeader))
        {
            request.Headers.Accept.Clear();
            request.Headers.Accept.ParseAdd(acceptHeader);
        }

        var userAgent = _configuration[$"{providerSection}:UserAgent"] ?? "curl/8.0";
        if (!string.IsNullOrWhiteSpace(userAgent))
        {
            request.Headers.UserAgent.Clear();
            request.Headers.UserAgent.ParseAdd(userAgent);
        }
    }

    private static string BuildPrompt(object promptRequest)
    {
        return
            """
            Follow the system instruction, user content, and JSON response schema in the request below.
            Return only the final JSON object. Do not include markdown fences or explanatory text.

            """
            + JsonSerializer.Serialize(promptRequest, JsonOptions);
    }

    private static string ExtractGeneratedJson(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            throw new ExternalServiceException("AI returned an empty response.");
        }

        using var document = JsonDocument.Parse(responseBody);
        var root = document.RootElement;
        if (root.ValueKind == JsonValueKind.Object)
        {
            foreach (var propertyName in new[] { "response", "generated_text", "text" })
            {
                if (root.TryGetProperty(propertyName, out var value)
                    && value.ValueKind == JsonValueKind.String)
                {
                    return RemoveMarkdownFences(value.GetString()!);
                }
            }
        }

        return responseBody;
    }

    private static string RemoveMarkdownFences(string value)
    {
        var trimmed = value.Trim();
        if (!trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            return trimmed;
        }

        var firstLineEnd = trimmed.IndexOf('\n');
        var closingFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
        return firstLineEnd >= 0 && closingFence > firstLineEnd
            ? trimmed[(firstLineEnd + 1)..closingFence].Trim()
            : trimmed;
    }

}
