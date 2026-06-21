using System.ComponentModel.DataAnnotations;

namespace ResourceMindAI.Application.DTOs.Project;

public sealed class UpdateProjectManagerDto
{
    [Required(ErrorMessage = "Manager is required.")]
    public Guid? NewManagerId { get; set; }
}

public sealed class ProjectManagerConflictDto
{
    public string ResourceName { get; init; } = null!;
    public IReadOnlyList<string> ProjectNames { get; init; } = Array.Empty<string>();
}

public sealed class ProjectManagerUpdateValidationDto
{
    public IReadOnlyList<ProjectManagerConflictDto> Conflicts { get; init; }
        = Array.Empty<ProjectManagerConflictDto>();
}

public sealed class ProjectManagerUpdateResultDto
{
    public ProjectDto Project { get; init; } = null!;
    public IReadOnlyList<string> UpdatedResources { get; init; } = Array.Empty<string>();
    public string Message { get; init; } = null!;
}
