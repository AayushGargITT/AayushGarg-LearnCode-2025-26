using System;
using ResourceMindAI.Domain.Enums;

namespace ResourceMindAI.Domain.Entities;
public class Timesheet
{
    public Guid Id { get; set; }
    public Guid ResourceProfileId { get; set; }
    public Guid ProjectId { get; set; }
    public DateTime WeekStartDate { get; set; }
    public decimal HoursLogged { get; set; }
    public TimesheetStatus Status { get; set; }
    public DateTime SubmittedAt { get; set; }

    public ResourceProfile ResourceProfile { get; set; } = null!;
    public Project Project { get; set; } = null!;
    public ICollection<ActivityTag> ActivityTags { get; set; }
        = new List<ActivityTag>();
}
