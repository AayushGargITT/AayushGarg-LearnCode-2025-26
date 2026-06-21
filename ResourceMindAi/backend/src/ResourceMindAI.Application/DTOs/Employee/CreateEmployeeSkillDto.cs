using System.ComponentModel.DataAnnotations;
using ResourceMindAI.Domain.Enums;

namespace ResourceMindAI.Application.DTOs.Employee;

public class CreateEmployeeSkillDto
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string SkillName { get; set; } = null!;

    [Required]
    [EnumDataType(typeof(SkillCategory))]
    public SkillCategory? Category { get; set; }

    [Required]
    [EnumDataType(typeof(ProficiencyLevel))]
    public ProficiencyLevel? Proficiency { get; set; }
}
