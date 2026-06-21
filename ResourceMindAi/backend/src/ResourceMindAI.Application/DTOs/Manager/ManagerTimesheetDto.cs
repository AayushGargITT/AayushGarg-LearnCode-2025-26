using ResourceMindAI.Domain.Enums;

namespace ResourceMindAI.Application.DTOs.Manager;

public class ManagerTimesheetDto
{
    public Guid Id { get; set; }
    public Guid ResourceId { get; set; }
    public string ResourceName { get; set; } = null!;
    public string ProjectName { get; set; } = null!;
    public DateTime WeekStartDate { get; set; }
    public decimal HoursLogged { get; set; }
    public TimesheetStatus Status { get; set; }
    public IReadOnlyList<string> Tags { get; set; } = [];
}
