using System.Text.Json;
using ResourceMindAI.Application.DTOs.Manager;

namespace ResourceMindAI.Application.Services;

public static class ProjectRiskSummarySerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string Serialize(ProjectRiskSummaryDto summary)
    {
        return JsonSerializer.Serialize(summary, JsonOptions);
    }

    public static ProjectRiskSummaryDto? Deserialize(string? json)
    {
        return TryDeserialize(json, out var summary)
            ? summary
            : null;
    }

    public static bool TryDeserialize(
        string? json,
        out ProjectRiskSummaryDto? summary)
    {
        summary = null;
        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<ProjectRiskSummaryDto>(
                json,
                JsonOptions);
            if (!IsValid(parsed))
            {
                return false;
            }

            summary = parsed;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool IsValid(ProjectRiskSummaryDto? summary)
    {
        if (summary is null
            || summary.GeneratedAt == default
            || string.IsNullOrWhiteSpace(summary.Summary)
            || summary.OverallHealth is not ("ON_TRACK" or "ATTENTION" or "AT_RISK"))
        {
            return false;
        }

        return summary.RiskPoints.All(point =>
            point.Severity is "LOW" or "MEDIUM" or "HIGH"
            && !string.IsNullOrWhiteSpace(point.Title)
            && !string.IsNullOrWhiteSpace(point.Description));
    }
}
