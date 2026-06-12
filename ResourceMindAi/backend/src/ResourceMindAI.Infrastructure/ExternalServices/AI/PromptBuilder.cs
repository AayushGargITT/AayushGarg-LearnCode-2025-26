using System.Text.Json;
using System.Text.Json.Serialization;
using ResourceMindAI.Application.DTOs.Manager;

namespace ResourceMindAI.Infrastructure.ExternalServices.AI;

public static class PromptBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    static PromptBuilder()
    {
        JsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    public static object BuildResourceIntentRequest(string requirement, DateOnly currentDate)
    {
        var instruction =
            $"""
            Analyze the manager's resource requirement and extract only structured intent.
            Today's date is {currentDate:yyyy-MM-dd}. Resolve relative dates using this date.
            Do not recommend employees and do not add facts that are not present.
            Always return only the intent JSON matching the supplied schema.
            There is no requiredRole field. Convert role-like phrases into requiredSkills.
            Remove generic job words such as developer, engineer, specialist, resource, and person.
            Examples:
            "Angular developer" becomes requiredSkills ["angular"].
            "backend developer" becomes requiredSkills ["backend"].
            "frontend engineer with React" becomes requiredSkills ["frontend", "react"].
            "QA engineer" becomes requiredSkills ["qa"].
            "DevOps engineer with Docker" becomes requiredSkills ["devops", "docker"].
            Keep named technologies and capability areas as separate normalized skills.
            Do not return a dateRange property.
            Split date ranges into fromDate and toDate in YYYY-MM-DD format.
            availabilityRequirement must be an integer from 1 to 100 or null.
            If hours per week are provided, convert them to a percentage using a 40-hour work week.
            Never include words or a percent symbol in availabilityRequirement.
            Use concise normalized values. Return empty arrays or null when information is absent.
            """;

        return BuildRequest(
            instruction,
            requirement,
            new
            {
                type = "object",
                properties = new
                {
                    requiredSkills = StringArray(),
                    experienceHint = NullableString(),
                    availabilityRequirement = new
                    {
                        type = new[] { "integer", "null" },
                        minimum = 1,
                        maximum = 100
                    },
                    fromDate = new { type = new[] { "string", "null" }, format = "date" },
                    toDate = new { type = new[] { "string", "null" }, format = "date" },
                    prioritySignals = StringArray(),
                    softConstraints = StringArray(),
                    exclusionConstraints = StringArray()
                },
                required = new[]
                {
                    "requiredSkills",
                    "experienceHint",
                    "availabilityRequirement",
                    "fromDate",
                    "toDate",
                    "prioritySignals",
                    "softConstraints",
                    "exclusionConstraints"
                }
            });
    }

    public static object BuildResourceExplanationRequest(ResourceCandidateExplanationRequestDto request)
    {
        const string instruction =
            """
            Rank and explain only the supplied resource candidates for the manager's requirement.
            Candidate eligibility and backend scores are authoritative.
            Do not add, remove, or invent employees. Return each supplied employeeId at most once.
            Use backend facts only. Keep reasons brief, factual, and useful to a manager.
            Return strict JSON matching the supplied schema.
            """;

        return BuildRequest(
            instruction,
            JsonSerializer.Serialize(request, JsonOptions),
            new
            {
                type = "object",
                properties = new
                {
                    matches = new
                    {
                        type = "array",
                        items = new
                        {
                            type = "object",
                            properties = new
                            {
                                employeeId = new { type = "string" },
                                aiRank = new { type = "integer", minimum = 1 },
                                aiReason = new { type = "string" },
                                strengths = StringArray(),
                                concerns = StringArray()
                            },
                            required = new[] { "employeeId", "aiRank", "aiReason", "strengths", "concerns" }
                        }
                    }
                },
                required = new[] { "matches" }
            });
    }

    public static object BuildTeamRequest(TeamBuilderAiRequestDto request)
    {
        const string instruction =
            """
            Analyze all supplied active employees for the requested project team.
            Use the manager requirement, extracted intent, and candidate facts.
            Do not invent employees and do not return an employeeId outside the supplied list.
            Return each employeeId at most once.
            Recommend members only when isEligible is true and status is Bench.
            Never recommend an Allocated employee as a team member.
            If an Allocated employee matches a requested role or skill, return that employee in
            unavailableMatches and clearly explain that the match exists but is not eligible
            because Team Builder allows only bench employees.
            Do not place the same employee in members and unavailableMatches.
            Select only useful bench members; do not force a candidate for every requirement.
            Clearly report uncovered capabilities in missingSkills.
            If matching employees exist but all are allocated, return an empty members array,
            include those matches in unavailableMatches, and explain this in teamSummary.
            If no employee matches, return empty members and unavailableMatches arrays and
            explain that no matching employees were found.
            Keep suggested roles and reasons concise and factual.
            Return strict JSON matching the supplied schema.
            """;

        return BuildRequest(
            instruction,
            JsonSerializer.Serialize(request, JsonOptions),
            new
            {
                type = "object",
                properties = new
                {
                    teamSummary = new { type = "string" },
                    members = new
                    {
                        type = "array",
                        items = new
                        {
                            type = "object",
                            properties = new
                            {
                                employeeId = new { type = "string" },
                                suggestedRole = new { type = "string" },
                                reason = new { type = "string" },
                                matchedSkills = StringArray()
                            },
                            required = new[]
                            {
                                "employeeId",
                                "suggestedRole",
                                "reason",
                                "matchedSkills"
                            }
                        }
                    },
                    unavailableMatches = new
                    {
                        type = "array",
                        items = new
                        {
                            type = "object",
                            properties = new
                            {
                                employeeId = new { type = "string" },
                                matchedRole = new { type = "string" },
                                reason = new { type = "string" },
                                matchedSkills = StringArray()
                            },
                            required = new[]
                            {
                                "employeeId",
                                "matchedRole",
                                "reason",
                                "matchedSkills"
                            }
                        }
                    },
                    missingSkills = StringArray()
                },
                required = new[]
                {
                    "teamSummary",
                    "members",
                    "unavailableMatches",
                    "missingSkills"
                }
            });
    }

    public static object BuildProjectRiskRequest(ProjectRiskFactsDto facts)
    {
        const string instruction =
            """
            Generate a factual project risk summary from only the supplied project facts.
            Do not invent blockers, dates, employees, milestones, or delivery claims.
            overallHealth must be ON_TRACK, ATTENTION, or AT_RISK.
            Risk severity must be LOW, MEDIUM, or HIGH.
            Keep the summary concise and recommended actions practical.
            Recommend concise skills or resource capabilities that could address the identified risks.
            suggestedSkills must contain only relevant skills or resource types supported by the facts.
            Examples include Senior Backend Developer, QA Automation, SQL Performance Tuning,
            DevOps, CI/CD, Azure, React, or Technical Lead when justified by the supplied risks.
            Set generatedAt to the current UTC timestamp.
            Return strict JSON matching the supplied schema.
            """;

        return BuildRequest(
            instruction,
            JsonSerializer.Serialize(facts, JsonOptions),
            new
            {
                type = "object",
                properties = new
                {
                    overallHealth = new
                    {
                        type = "string",
                        @enum = new[] { "ON_TRACK", "ATTENTION", "AT_RISK" }
                    },
                    summary = new { type = "string" },
                    riskPoints = new
                    {
                        type = "array",
                        items = new
                        {
                            type = "object",
                            properties = new
                            {
                                severity = new
                                {
                                    type = "string",
                                    @enum = new[] { "LOW", "MEDIUM", "HIGH" }
                                },
                                title = new { type = "string" },
                                description = new { type = "string" }
                            },
                            required = new[] { "severity", "title", "description" }
                        }
                    },
                    recommendedActions = StringArray(),
                    suggestedSkills = StringArray(),
                    generatedAt = new { type = "string", format = "date-time" }
                },
                required = new[]
                {
                    "overallHealth",
                    "summary",
                    "riskPoints",
                    "recommendedActions",
                    "suggestedSkills",
                    "generatedAt"
                }
            });
    }

    private static object BuildRequest(string instruction, string content, object schema)
    {
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
                    parts = new[] { new { text = content } }
                }
            },
            generationConfig = new
            {
                temperature = 0.1,
                responseMimeType = "application/json",
                responseJsonSchema = schema
            }
        };
    }

    private static object NullableString()
    {
        return new { type = new[] { "string", "null" } };
    }

    private static object StringArray()
    {
        return new { type = "array", items = new { type = "string" } };
    }
}
