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
            HtmlContent = BuildHtml(request, displayHealth, impacts),
            Tag = "project-health-alert"
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
            .AppendLine("Project Information:")
            .AppendLine($"- Project: {request.ProjectName}")
            .AppendLine($"- Project Status: {request.ProjectStatus}")
            .AppendLine($"- Start Date: {request.ProjectStartDate:yyyy-MM-dd}")
            .AppendLine($"- End Date: {FormatDate(request.ProjectEndDate)}")
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
            .AppendLine("Matching Team Resources:");
        AppendTextList(
            body,
            request.MatchingResources.Select(resource =>
                FormatMatchingResourceText(resource)));
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
        var badge = GetHealthBadge(summary.OverallHealth);
        return
            $"""
            <html>
            <body style="margin:0;background:#f4f7fb;font-family:Arial,Helvetica,sans-serif;color:#1f2937;line-height:1.55">
              <div style="max-width:720px;margin:0 auto;padding:28px 18px">
                <div style="background:#ffffff;border:1px solid #e5e7eb;border-radius:14px;overflow:hidden;box-shadow:0 14px 36px rgba(15,23,42,0.08)">
                  <div style="background:#111827;color:#ffffff;padding:24px 28px">
                    <div style="font-size:12px;text-transform:uppercase;letter-spacing:0.08em;color:#c7d2fe">Project Health Alert</div>
                    <h1 style="margin:8px 0 0;font-size:24px;line-height:1.25">{Encode(request.ProjectName)}</h1>
                  </div>

                  <div style="padding:24px 28px">
                    <p style="margin:0 0 18px">Hello {Encode(request.ManagerName)},</p>

                    <div style="display:inline-block;border-radius:999px;background:{badge.Background};color:{badge.Color};border:1px solid {badge.Border};padding:6px 12px;font-size:12px;font-weight:700">
                      Health Status: {Encode(displayHealth)}
                    </div>

                    <table style="width:100%;border-collapse:collapse;margin:20px 0;background:#f9fafb;border:1px solid #e5e7eb;border-radius:10px;overflow:hidden">
                      <tr>
                        <td style="padding:12px 14px;color:#6b7280;font-size:12px">Project Status</td>
                        <td style="padding:12px 14px;font-weight:700">{Encode(request.ProjectStatus)}</td>
                      </tr>
                      <tr>
                        <td style="padding:12px 14px;color:#6b7280;font-size:12px;border-top:1px solid #e5e7eb">Timeline</td>
                        <td style="padding:12px 14px;border-top:1px solid #e5e7eb">{request.ProjectStartDate:yyyy-MM-dd} to {Encode(FormatDate(request.ProjectEndDate))}</td>
                      </tr>
                      <tr>
                        <td style="padding:12px 14px;color:#6b7280;font-size:12px;border-top:1px solid #e5e7eb">Generated</td>
                        <td style="padding:12px 14px;border-top:1px solid #e5e7eb">{summary.GeneratedAt:yyyy-MM-dd HH:mm} UTC</td>
                      </tr>
                    </table>

                    {BuildSection("Summary", Encode(summary.Summary))}
                    {BuildSection("Key Risk Reasons", BuildRiskPointList(summary.RiskPoints))}
                    {BuildSection("Potential Impact", BuildHtmlList(impacts))}
                    {BuildSection("Recommended Actions", BuildHtmlList(summary.RecommendedActions))}
                    {BuildSection("Suggested Help and Skills", BuildHtmlList(summary.SuggestedSkills))}
                    {BuildSection("Matching Team Resources", BuildResourceList(request.MatchingResources))}

                    <p style="margin:22px 0 0;color:#6b7280;font-size:12px">
                      This notification was generated from the saved project risk summary. Please review project blockers and resource support before the next status update.
                    </p>
                  </div>
                </div>
              </div>
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

    private static string BuildSection(string title, string body)
    {
        return
            $"""
            <div style="margin-top:20px">
              <h2 style="margin:0 0 8px;font-size:16px;color:#111827">{Encode(title)}</h2>
              <div style="border:1px solid #e5e7eb;border-radius:10px;padding:14px;background:#ffffff">{body}</div>
            </div>
            """;
    }

    private static string BuildRiskPointList(IEnumerable<ProjectRiskPointDto> values)
    {
        var items = values
            .Where(value =>
                !string.IsNullOrWhiteSpace(value.Title)
                || !string.IsNullOrWhiteSpace(value.Description))
            .ToList();
        if (items.Count == 0)
        {
            return "<p style=\"margin:0;color:#6b7280\">No specific items were identified.</p>";
        }

        return string.Join(
            string.Empty,
            items.Select(point =>
                $"""
                <div style="padding:10px 0;border-bottom:1px solid #eef2f7">
                  <span style="display:inline-block;margin-bottom:6px;border-radius:999px;background:{GetSeverityBackground(point.Severity)};color:{GetSeverityColor(point.Severity)};padding:3px 8px;font-size:11px;font-weight:700">{Encode(point.Severity)}</span>
                  <div style="font-weight:700">{Encode(point.Title)}</div>
                  <div style="color:#4b5563;font-size:14px">{Encode(point.Description)}</div>
                </div>
                """));
    }

    private static string BuildResourceList(IEnumerable<ProjectHealthResourceRecommendationDto> values)
    {
        var resources = values.ToList();
        if (resources.Count == 0)
        {
            return "<p style=\"margin:0;color:#6b7280\">No matching active resources were found under this manager for the suggested skills.</p>";
        }

        return string.Join(
            string.Empty,
            resources.Select(resource =>
                $"""
                <div style="padding:10px 0;border-bottom:1px solid #eef2f7">
                  <div style="font-weight:700;color:#111827">{Encode(resource.FullName)}</div>
                  <div style="color:#6b7280;font-size:13px">{Encode(resource.Designation ?? "No designation")} - {Encode(resource.Department ?? "No department")}</div>
                  <div style="margin-top:6px;color:#374151;font-size:13px">Matched: {Encode(FormatMatchedSkills(resource.MatchedSkills))}</div>
                </div>
                """));
    }

    private static string BuildHtmlList(IEnumerable<string> values)
    {
        var items = values.Where(value => !string.IsNullOrWhiteSpace(value)).ToList();
        if (items.Count == 0)
        {
            return "<p style=\"margin:0;color:#6b7280\">No specific items were identified.</p>";
        }

        return
            "<ul style=\"margin:0;padding-left:18px\">"
            + string.Join(
                string.Empty,
                items.Select(item =>
                    $"<li style=\"margin:6px 0;color:#374151\">{Encode(item)}</li>"))
            + "</ul>";
    }

    private static (string Background, string Color, string Border) GetHealthBadge(string health)
    {
        return health == "AT_RISK"
            ? ("#fee2e2", "#991b1b", "#fecaca")
            : ("#fef3c7", "#92400e", "#fde68a");
    }

    private static string GetSeverityBackground(string severity)
    {
        return severity switch
        {
            "HIGH" => "#fee2e2",
            "MEDIUM" => "#fef3c7",
            _ => "#e0f2fe"
        };
    }

    private static string GetSeverityColor(string severity)
    {
        return severity switch
        {
            "HIGH" => "#991b1b",
            "MEDIUM" => "#92400e",
            _ => "#075985"
        };
    }

    private static string FormatDate(DateTime? value)
    {
        return value.HasValue
            ? value.Value.ToString("yyyy-MM-dd")
            : "Not set";
    }

    private static string FormatMatchingResourceText(ProjectHealthResourceRecommendationDto resource)
    {
        var title = string.Join(
            " - ",
            new[]
            {
                resource.FullName,
                resource.Designation,
                resource.Department
            }.Where(value => !string.IsNullOrWhiteSpace(value)));

        return $"{title} (matched: {FormatMatchedSkills(resource.MatchedSkills)})";
    }

    private static string FormatMatchedSkills(IEnumerable<string> skills)
    {
        var values = skills
            .Where(skill => !string.IsNullOrWhiteSpace(skill))
            .ToList();
        return values.Count == 0
            ? "Relevant designation or role match"
            : string.Join(", ", values);
    }

    private static string Encode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
}
