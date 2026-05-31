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
