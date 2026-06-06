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
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
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

    public async Task<ResourceIntentDto> ExtractResourceIntentAsync(
        string requirement,
        CancellationToken cancellationToken = default)
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
                BuildRequest(requirement, DateOnly.FromDateTime(DateTime.UtcNow)),
                JsonOptions,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "Gemini intent extraction failed with status {StatusCode}: {ResponseBody}",
                    response.StatusCode,
                    errorBody);
                throw new ExternalServiceException("AI intent detection is temporarily unavailable.");
            }

            var geminiResponse = await response.Content.ReadFromJsonAsync<GeminiResponse>(
                JsonOptions,
                cancellationToken);
            var json = geminiResponse?.Candidates?
                .FirstOrDefault()?.Content?.Parts?
                .FirstOrDefault()?.Text;

            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ExternalServiceException("AI intent detection returned an empty response.");
            }

            return JsonSerializer.Deserialize<ResourceIntentDto>(json, JsonOptions)
                ?? throw new ExternalServiceException("AI intent detection returned an invalid response.");
        }
        catch (ExternalServiceException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Gemini intent extraction failed");
            throw new ExternalServiceException("AI intent detection is temporarily unavailable.", exception);
        }
    }

    private static object BuildRequest(string requirement, DateOnly currentDate)
    {
        var instruction =
            $"""
            Analyze the manager's resource requirement and extract only structured intent.
            Today's date is {currentDate:yyyy-MM-dd}. Resolve relative dates using this date.
            Do not recommend employees and do not add facts that are not present.
            Always return only the intent JSON matching the supplied schema.
            Do not return a dateRange property.
            Split date ranges into fromDate and toDate in YYYY-MM-DD format.
            availabilityRequirement must be an integer from 1 to 100 or null.
            For phrases such as "50% utilization remaining", "50% available", or "50% utilization", return 50.
            Never include words or a percent symbol in availabilityRequirement.
            Use concise normalized values. Return empty arrays or null when information is absent.
            """;

        return new
        {
            systemInstruction = new
            {
                parts = new[] { new { text = instruction } }
            },
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[] { new { text = requirement } }
                }
            },
            generationConfig = new
            {
                temperature = 0.1,
                responseMimeType = "application/json",
                responseJsonSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        requiredRole = new { type = new[] { "string", "null" } },
                        requiredSkills = new { type = "array", items = new { type = "string" } },
                        experienceHint = new { type = new[] { "string", "null" } },
                        availabilityRequirement = new
                        {
                            type = new[] { "integer", "null" },
                            minimum = 1,
                            maximum = 100
                        },
                        fromDate = new { type = new[] { "string", "null" }, format = "date" },
                        toDate = new { type = new[] { "string", "null" }, format = "date" },
                        prioritySignals = new { type = "array", items = new { type = "string" } },
                        softConstraints = new { type = "array", items = new { type = "string" } },
                        exclusionConstraints = new { type = "array", items = new { type = "string" } },
                    },
                    required = new[]
                    {
                        "requiredRole",
                        "requiredSkills",
                        "experienceHint",
                        "availabilityRequirement",
                        "fromDate",
                        "toDate",
                        "prioritySignals",
                        "softConstraints",
                        "exclusionConstraints",
                    }
                }
            }
        };
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
