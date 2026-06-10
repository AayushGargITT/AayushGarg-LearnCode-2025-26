namespace ResourceMindAI.Application.DTOs.Manager;

public sealed class ProjectRiskFactsDto
{
    public string ProjectName { get; init; } = null!;
    public string ProjectStatus { get; init; } = null!;
    public DateTime StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public IReadOnlyList<ProjectRiskMilestoneFactDto> Milestones { get; init; } = [];
    public IReadOnlyList<ProjectRiskAllocationFactDto> ActiveAllocations { get; init; } = [];
    public IReadOnlyList<ProjectRiskTimesheetFactDto> RecentTimesheets { get; init; } = [];
    public IReadOnlyList<string> SystemRiskFlags { get; init; } = [];
}

public sealed class ProjectRiskMilestoneFactDto
{
    public string Title { get; init; } = null!;
    public DateTime DueDate { get; init; }
    public string Status { get; init; } = null!;
}

public sealed class ProjectRiskAllocationFactDto
{
    public string EmployeeName { get; init; } = null!;
    public decimal AllocationPercent { get; init; }
    public DateTime FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public decimal ExpectedWeeklyHours { get; init; }
}

public sealed class ProjectRiskTimesheetFactDto
{
    public string EmployeeName { get; init; } = null!;
    public DateTime WeekStart { get; init; }
    public decimal LoggedHours { get; init; }
    public decimal ExpectedHours { get; init; }
    public string Status { get; init; } = null!;
}

public sealed class ProjectRiskSummaryDto
{
    public string OverallHealth { get; init; } = null!;
    public string Summary { get; init; } = null!;
    public IReadOnlyList<ProjectRiskPointDto> RiskPoints { get; init; } = [];
    public IReadOnlyList<string> RecommendedActions { get; init; } = [];
    public DateTime GeneratedAt { get; init; }
}

public sealed class ProjectRiskPointDto
{
    public string Severity { get; init; } = null!;
    public string Title { get; init; } = null!;
    public string Description { get; init; } = null!;
}
