using System.ComponentModel.DataAnnotations;
using ResourceMindAI.Domain.Enums;

namespace ResourceMindAI.Application.DTOs.Project;

public class CreateMilestoneDto
{
    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string Title { get; set; } = null!;

    [Required]
    public DateTime? DueDate { get; set; }

    [Required]
    [EnumDataType(typeof(MilestoneStatus))]
    public MilestoneStatus? Status { get; set; }
}
