namespace ResourceMindAI.Application.Constants;

public static class ActivityTagCatalog
{
    public static readonly IReadOnlySet<string> Allowed = new HashSet<string>(
        new[]
        {
            "Backend API Development",
            "Microservices / Architecture",
            "Database Design & Queries",
            "WebSocket / Real-time Features",
            "Frontend Development",
            "Code Review / Mentoring",
            "Bug Fixing",
            "DevOps / Deployment",
            "Testing & QA",
            "Documentation",
            "Other"
        },
        StringComparer.OrdinalIgnoreCase);
}
