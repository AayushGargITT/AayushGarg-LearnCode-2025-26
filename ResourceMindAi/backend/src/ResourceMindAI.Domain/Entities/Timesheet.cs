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
