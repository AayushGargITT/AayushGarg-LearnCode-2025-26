namespace ResourceMindAI.Domain.Entities;

public class ResourceProfile
{
    public Guid Id { get; set; }
    public string Department { get; set; } = null!;
    public string Designation { get; set; } = null!;
    public Guid? ManagerId { get; set; }

    public User User { get; set; } = null!;
    public User? Manager { get; set; }
    public ICollection<Skill> Skills { get; set; } = new List<Skill>();
    public ICollection<Allocation> Allocations { get; set; } = new List<Allocation>();
    public ICollection<Timesheet> Timesheets { get; set; } = new List<Timesheet>();
}
