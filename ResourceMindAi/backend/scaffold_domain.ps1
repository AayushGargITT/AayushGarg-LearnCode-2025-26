$DomainDir = "src\ResourceMindAI.Domain"

mkdir $DomainDir\Entities -Force
mkdir $DomainDir\Enums -Force
mkdir $DomainDir\Exceptions -Force
mkdir $DomainDir\Common -Force

# Enums
@"
namespace ResourceMindAI.Domain.Enums;
public enum Role { Admin, Manager, Employee }
"@ | Out-File $DomainDir\Enums\Role.cs -Encoding utf8

@"
namespace ResourceMindAI.Domain.Enums;
public enum EmployeeStatus { Active, Inactive, OnLeave }
"@ | Out-File $DomainDir\Enums\EmployeeStatus.cs -Encoding utf8

@"
namespace ResourceMindAI.Domain.Enums;
public enum ProjectStatus { Planned, Active, Completed, OnHold, Cancelled }
"@ | Out-File $DomainDir\Enums\ProjectStatus.cs -Encoding utf8

@"
namespace ResourceMindAI.Domain.Enums;
public enum HealthStatus { Green, Amber, Red }
"@ | Out-File $DomainDir\Enums\HealthStatus.cs -Encoding utf8

@"
namespace ResourceMindAI.Domain.Enums;
public enum MilestoneStatus { Pending, InProgress, Completed, Overdue }
"@ | Out-File $DomainDir\Enums\MilestoneStatus.cs -Encoding utf8

@"
namespace ResourceMindAI.Domain.Enums;
public enum TimesheetStatus { Draft, Submitted, Approved, Rejected }
"@ | Out-File $DomainDir\Enums\TimesheetStatus.cs -Encoding utf8

@"
namespace ResourceMindAI.Domain.Enums;
public enum SkillCategory { Technical, Soft, Management, Domain }
"@ | Out-File $DomainDir\Enums\SkillCategory.cs -Encoding utf8

@"
namespace ResourceMindAI.Domain.Enums;
public enum ProficiencyLevel { Beginner, Intermediate, Advanced, Expert }
"@ | Out-File $DomainDir\Enums\ProficiencyLevel.cs -Encoding utf8

# Exceptions
@"
using System;
namespace ResourceMindAI.Domain.Exceptions;
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
"@ | Out-File $DomainDir\Exceptions\DomainException.cs -Encoding utf8

@"
using System;
namespace ResourceMindAI.Domain.Exceptions;
public class EntityNotFoundException : Exception
{
    public EntityNotFoundException(string message) : base(message) { }
}
"@ | Out-File $DomainDir\Exceptions\EntityNotFoundException.cs -Encoding utf8

@"
using System;
namespace ResourceMindAI.Domain.Exceptions;
public class AllocationOverlapException : Exception
{
    public AllocationOverlapException(string message) : base(message) { }
}
"@ | Out-File $DomainDir\Exceptions\AllocationOverlapException.cs -Encoding utf8

# Entities
@"
using System;
using System.Collections.Generic;
using ResourceMindAI.Domain.Enums;

namespace ResourceMindAI.Domain.Entities;
public class User
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public Role Role { get; set; }
    public bool IsActive { get; set; }
    public bool ForcePasswordChange { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Employee? Employee { get; set; }
    public ICollection<Project> ManagedProjects { get; set; } = new List<Project>();

    public User() { }
}
"@ | Out-File $DomainDir\Entities\User.cs -Encoding utf8

@"
using System;
using System.Collections.Generic;
using ResourceMindAI.Domain.Enums;

namespace ResourceMindAI.Domain.Entities;
public class Employee
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Department { get; set; } = null!;
    public string Designation { get; set; } = null!;
    public EmployeeStatus Status { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
    public ICollection<Skill> Skills { get; set; } = new List<Skill>();
    public ICollection<Allocation> Allocations { get; set; } = new List<Allocation>();
    public ICollection<Timesheet> Timesheets { get; set; } = new List<Timesheet>();

    public Employee() { }
}
"@ | Out-File $DomainDir\Entities\Employee.cs -Encoding utf8

@"
using System;
using ResourceMindAI.Domain.Enums;

namespace ResourceMindAI.Domain.Entities;
public class Skill
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string SkillName { get; set; } = null!;
    public SkillCategory Category { get; set; }
    public ProficiencyLevel Proficiency { get; set; }
    public DateTime AddedAt { get; set; }

    public Employee Employee { get; set; } = null!;

    public Skill() { }
}
"@ | Out-File $DomainDir\Entities\Skill.cs -Encoding utf8

@"
using System;
using System.Collections.Generic;
using ResourceMindAI.Domain.Enums;

namespace ResourceMindAI.Domain.Entities;
public class Project
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public ProjectStatus Status { get; set; }
    public HealthStatus HealthStatus { get; set; }
    public Guid ManagerId { get; set; }
    public string? RiskFlagsJson { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public User Manager { get; set; } = null!;
    public ICollection<Milestone> Milestones { get; set; } = new List<Milestone>();
    public ICollection<Allocation> Allocations { get; set; } = new List<Allocation>();
    public ICollection<Timesheet> Timesheets { get; set; } = new List<Timesheet>();

    public Project() { }
}
"@ | Out-File $DomainDir\Entities\Project.cs -Encoding utf8

@"
using System;
using ResourceMindAI.Domain.Enums;

namespace ResourceMindAI.Domain.Entities;
public class Milestone
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Title { get; set; } = null!;
    public DateTime DueDate { get; set; }
    public MilestoneStatus Status { get; set; }

    public Project Project { get; set; } = null!;

    public Milestone() { }
}
"@ | Out-File $DomainDir\Entities\Milestone.cs -Encoding utf8

@"
using System;

namespace ResourceMindAI.Domain.Entities;
public class Allocation
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid ProjectId { get; set; }
    public decimal UtilisationPercent { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public Employee Employee { get; set; } = null!;
    public Project Project { get; set; } = null!;

    public Allocation() { }
}
"@ | Out-File $DomainDir\Entities\Allocation.cs -Encoding utf8

@"
using System;
using ResourceMindAI.Domain.Enums;

namespace ResourceMindAI.Domain.Entities;
public class Timesheet
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid ProjectId { get; set; }
    public DateTime WeekStartDate { get; set; }
    public decimal HoursLogged { get; set; }
    public TimesheetStatus Status { get; set; }
    public DateTime SubmittedAt { get; set; }

    public Employee Employee { get; set; } = null!;
    public Project Project { get; set; } = null!;

    public Timesheet() { }
}
"@ | Out-File $DomainDir\Entities\Timesheet.cs -Encoding utf8

@"
using System;

namespace ResourceMindAI.Domain.Entities;
public class SystemConfig
{
    public Guid Id { get; set; }
    public string LlmProvider { get; set; } = null!;
    public string? LlmApiKey { get; set; }
    public int SchedulerIntervalHours { get; set; }
    public decimal MaxWeeklyHours { get; set; }

    public SystemConfig() { }
}
"@ | Out-File $DomainDir\Entities\SystemConfig.cs -Encoding utf8

