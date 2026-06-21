using System.ComponentModel.DataAnnotations;

namespace ResourceMindAI.Application.DTOs.Manager;

public class CreateManagerAllocationDto
{
    [Required]
    public Guid? ProjectId { get; set; }

    [Required]
    public Guid? ResourceId { get; set; }

    [Required]
    [Range(1, 100)]
    public decimal? UtilisationPercent { get; set; }

    [Required]
    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }
}
