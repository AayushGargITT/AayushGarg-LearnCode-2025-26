using System.ComponentModel.DataAnnotations;

namespace ResourceMindAI.Application.DTOs.Manager;

public class FindResourceRequestDto
{
    [Required]
    public Guid? ProjectId { get; set; }

    [Required]
    [StringLength(2000, MinimumLength = 5)]
    public string Requirement { get; set; } = null!;
}
