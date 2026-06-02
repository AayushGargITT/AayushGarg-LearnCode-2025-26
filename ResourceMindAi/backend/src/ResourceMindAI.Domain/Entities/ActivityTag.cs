namespace ResourceMindAI.Domain.Entities;
public class ActivityTag
{
    public Guid Id { get; set; }

    public Guid TimesheetId { get; set; }

    public string TagName { get; set; } = null!;

    public Timesheet Timesheet { get; set; } = null!;
}