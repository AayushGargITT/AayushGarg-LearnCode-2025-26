using System.Net;
using System.Text;
using ResourceMindAI.Application.DTOs.Manager;
using ResourceMindAI.Application.DTOs.Notifications;

namespace ResourceMindAI.Infrastructure.ExternalServices.Email;

internal static class ProjectHealthEmailBuilder
{
    internal static EmailMessageDto Build(ProjectHealthNotificationRequestDto request)
    {
        var summary = request.RiskSummary;
        var displayHealth = summary.OverallHealth.Replace('_', ' ');
        var subject = $"Project Health Alert - {request.ProjectName}";
        var impacts = BuildPotentialImpacts(summary.OverallHealth);

        return new EmailMessageDto
        {
            RecipientEmail = request.ManagerEmail,
            RecipientName = request.ManagerName,
            Subject = subject,
            TextContent = BuildText(request, displayHealth, impacts),
            HtmlContent = BuildHtml(request, displayHealth, impacts)
        };
    }

    private static string BuildText(
        ProjectHealthNotificationRequestDto request,
        string displayHealth,
        IReadOnlyList<string> impacts)
    {
        var summary = request.RiskSummary;
        var body = new StringBuilder()
            .AppendLine($"Hello {request.ManagerName},")
            .AppendLine()
            .AppendLine($"Project Health Status: {displayHealth}")
            .AppendLine()
            .AppendLine("Summary:")
            .AppendLine(summary.Summary)
            .AppendLine()
            .AppendLine("Key Issues:");
        AppendTextList(
            body,
            summary.RiskPoints.Select(point => $"{point.Title}: {point.Description}"));
        body.AppendLine()
            .AppendLine("Potential Impact:");
        AppendTextList(body, impacts);
        body.AppendLine()
            .AppendLine("Recommended Actions:");
        AppendTextList(body, summary.RecommendedActions);
        body.AppendLine()
            .AppendLine("Suggested Skills or Resources:");
        AppendTextList(body, summary.SuggestedSkills);
        body.AppendLine()
            .AppendLine($"Generated On: {summary.GeneratedAt:yyyy-MM-dd HH:mm} UTC");

        return body.ToString();
    }

    private static string BuildHtml(
        ProjectHealthNotificationRequestDto request,
        string displayHealth,
        IReadOnlyList<string> impacts)
    {
        var summary = request.RiskSummary;
        return
            $"""
            <html>
            <body style="font-family:Arial,sans-serif;color:#1f2937;line-height:1.5">
              <p>Hello {Encode(request.ManagerName)},</p>
              <h2 style="margin-bottom:4px">Project Health Alert</h2>
              <p><strong>Project:</strong> {Encode(request.ProjectName)}<br>
                 <strong>Status:</strong> {Encode(displayHealth)}</p>
              <h3>Summary</h3>
              <p>{Encode(summary.Summary)}</p>
              <h3>Key Issues</h3>
              {BuildHtmlList(summary.RiskPoints.Select(point => $"{point.Title}: {point.Description}"))}
              <h3>Potential Impact</h3>
              {BuildHtmlList(impacts)}
              <h3>Recommended Actions</h3>
              {BuildHtmlList(summary.RecommendedActions)}
              <h3>Suggested Skills or Resources</h3>
              {BuildHtmlList(summary.SuggestedSkills)}
              <p style="color:#6b7280;font-size:12px">
                Generated on {summary.GeneratedAt:yyyy-MM-dd HH:mm} UTC
              </p>
            </body>
            </html>
            """;
    }

    private static IReadOnlyList<string> BuildPotentialImpacts(string health)
    {
        return health == "AT_RISK"
            ?
            [
                "The delivery timeline or committed scope may be affected.",
                "Quality risk and pressure on the current team may increase.",
                "Unresolved blockers may require immediate management attention."
            ]
            :
            [
                "Upcoming milestones may become harder to meet if issues remain unresolved.",
                "Delivery confidence may decrease without timely corrective action."
            ];
    }

    private static void AppendTextList(StringBuilder builder, IEnumerable<string> values)
    {
        var items = values.Where(value => !string.IsNullOrWhiteSpace(value)).ToList();
        if (items.Count == 0)
        {
            builder.AppendLine("- No specific items were identified.");
            return;
        }

        foreach (var item in items)
        {
            builder.AppendLine($"- {item}");
        }
    }

    private static string BuildHtmlList(IEnumerable<string> values)
    {
        var items = values.Where(value => !string.IsNullOrWhiteSpace(value)).ToList();
        if (items.Count == 0)
        {
            return "<p>No specific items were identified.</p>";
        }

        return $"<ul>{string.Join(string.Empty, items.Select(item => $"<li>{Encode(item)}</li>"))}</ul>";
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}
