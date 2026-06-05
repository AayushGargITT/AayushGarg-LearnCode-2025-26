using System.ComponentModel.DataAnnotations;
using ResourceMindAI.Domain.Enums;

namespace ResourceMindAI.Application.DTOs.Employee;

public class UpdateEmployeeSkillProficiencyDto
{
    [Required]
    [EnumDataType(typeof(ProficiencyLevel))]
    public ProficiencyLevel? Proficiency { get; set; }
}
