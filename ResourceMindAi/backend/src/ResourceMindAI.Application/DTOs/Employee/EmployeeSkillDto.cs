using ResourceMindAI.Domain.Enums;

namespace ResourceMindAI.Application.DTOs.Employee;

public class EmployeeSkillDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string SkillName { get; set; } = null!;
    public SkillCategory Category { get; set; }
    public ProficiencyLevel Proficiency { get; set; }
    public DateTime AddedAt { get; set; }
}
