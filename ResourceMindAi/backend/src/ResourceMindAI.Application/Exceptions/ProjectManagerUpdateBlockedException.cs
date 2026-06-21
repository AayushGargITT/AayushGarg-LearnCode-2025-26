using ResourceMindAI.Application.DTOs.Project;
using ResourceMindAI.Domain.Exceptions;

namespace ResourceMindAI.Application.Exceptions;

public sealed class ProjectManagerUpdateBlockedException : ValidationException
{
    public ProjectManagerUpdateValidationDto Details { get; }

    public ProjectManagerUpdateBlockedException(ProjectManagerUpdateValidationDto details)
        : base(
            "Project manager cannot be updated because employees have conflicting active project allocations.",
            "PROJECT_MANAGER_UPDATE_BLOCKED")
    {
        Details = details;
    }
}
