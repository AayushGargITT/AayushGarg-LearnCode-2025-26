using ResourceMindAI.Application.DTOs.Manager;
using ResourceMindAI.Domain.Entities;
using ResourceMindAI.Domain.Enums;

namespace ResourceMindAI.Application.Tests.TestData;

internal static class TestDataBuilder
{
    internal static User User(
        Role role = Role.Resource,
        bool isActive = true,
        string name = "Aarav Sharma")
    {
        var username = name.ToLowerInvariant().Replace(" ", ".");
        return new User
        {
            Id = Guid.NewGuid(),
            FullName = name,
            Email = $"{username}@example.com",
            Username = username,
            PasswordHash = ResourceMindAI.Application.Services.PasswordHasher.Hash("Password1"),
            Department = role == Role.Admin ? null : "Engineering",
            Designation = role switch
            {
                Role.Manager => "Engineering Manager",
                Role.Resource => "Backend Developer",
                _ => null
            },
            Role = role,
            IsActive = isActive,
            ForcePasswordChange = false,
            CreatedAt = DateTime.UtcNow.AddMonths(-3)
        };
    }

    internal static ResourceProfile Profile(User employee, User? manager = null)
    {
        var profile = new ResourceProfile
        {
            Id = employee.Id,
            User = employee,
            ManagerId = manager?.Id,
            Manager = manager
        };
        employee.ResourceProfile = profile;
        return profile;
    }

    internal static Project Project(
        User manager,
        ProjectStatus status = ProjectStatus.Active,
        string name = "Apollo")
    {
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = "Delivery project",
            StartDate = DateTime.UtcNow.Date.AddMonths(-1),
            EndDate = DateTime.UtcNow.Date.AddMonths(3),
            Status = status,
            HealthStatus = HealthStatus.Green,
            ManagerId = manager.Id,
            Manager = manager,
            CreatedAt = DateTime.UtcNow.AddMonths(-1)
        };
        manager.ManagedProjects.Add(project);
        return project;
    }

    internal static Allocation Allocation(
        User employee,
        Project project,
        decimal utilisation = 50,
        bool active = true,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        var allocation = new Allocation
        {
            Id = Guid.NewGuid(),
            UserId = employee.Id,
            User = employee,
            ProjectId = project.Id,
            Project = project,
            UtilisationPercent = utilisation,
            FromDate = fromDate ?? DateTime.UtcNow.Date.AddDays(-7),
            ToDate = toDate ?? DateTime.UtcNow.Date.AddDays(30),
            IsActive = active,
            CreatedAt = DateTime.UtcNow
        };
        employee.Allocations.Add(allocation);
        project.Allocations.Add(allocation);
        return allocation;
    }

    internal static Skill Skill(User employee, string name)
    {
        var profile = employee.ResourceProfile ?? Profile(employee);
        var skill = new Skill
        {
            Id = Guid.NewGuid(),
            ResourceProfileId = employee.Id,
            ResourceProfile = profile,
            SkillName = name,
            Category = SkillCategory.Technical,
            Proficiency = ProficiencyLevel.Advanced,
            AddedAt = DateTime.UtcNow
        };
        profile.Skills.Add(skill);
        return skill;
    }

    internal static Timesheet Timesheet(
        User employee,
        Project project,
        DateTime weekStart,
        decimal hours = 20)
    {
        var timesheet = new Timesheet
        {
            Id = Guid.NewGuid(),
            UserId = employee.Id,
            User = employee,
            ProjectId = project.Id,
            Project = project,
            WeekStartDate = weekStart,
            HoursLogged = hours,
            Status = TimesheetStatus.Submitted,
            SubmittedAt = DateTime.UtcNow
        };
        employee.Timesheets.Add(timesheet);
        project.Timesheets.Add(timesheet);
        return timesheet;
    }

    internal static ResourceIntentDto Intent(params string[] skills)
    {
        return new ResourceIntentDto
        {
            RequiredSkills = skills,
            PrioritySignals = [],
            SoftConstraints = [],
            ExclusionConstraints = []
        };
    }

    internal static ProjectRiskSummaryDto RiskSummary(string health = "ON_TRACK")
    {
        return new ProjectRiskSummaryDto
        {
            OverallHealth = health,
            Summary = "Delivery remains stable.",
            RiskPoints = [],
            RecommendedActions = ["Continue weekly reviews."],
            SuggestedSkills = [],
            GeneratedAt = DateTime.UtcNow
        };
    }
}
