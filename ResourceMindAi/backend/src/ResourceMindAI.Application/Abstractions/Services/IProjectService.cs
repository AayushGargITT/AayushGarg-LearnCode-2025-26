using ResourceMindAI.Application.DTOs.Project;

namespace ResourceMindAI.Application.Abstractions.Services;

public interface IProjectService
{
    Task<IReadOnlyList<ProjectDto>> GetAllAsync();
    Task<ProjectDto> CreateAsync(CreateProjectDto request);
    Task<IReadOnlyList<MilestoneDto>> GetMilestonesAsync(Guid projectId);
    Task<MilestoneDto> AddMilestoneAsync(Guid projectId, CreateMilestoneDto request);
    Task<MilestoneDto> UpdateMilestoneAsync(Guid projectId, Guid milestoneId, UpdateMilestoneDto request);
    Task<ProjectManagerUpdateResultDto> UpdateManagerAsync(
        Guid projectId,
        UpdateProjectManagerDto request);
}
