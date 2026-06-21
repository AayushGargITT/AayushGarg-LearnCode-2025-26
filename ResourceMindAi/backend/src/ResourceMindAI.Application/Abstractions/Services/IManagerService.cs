using ResourceMindAI.Application.DTOs.Manager;

namespace ResourceMindAI.Application.Abstractions.Services;

public interface IManagerService
{
    Task<ManagerResourceDashboardDto> GetResourceDashboardAsync(Guid managerId);
    Task<ManagerResourceDto> GetResourceDetailAsync(Guid managerId, Guid resourceId);
    Task<IReadOnlyList<ManagerProjectDto>> GetProjectsAsync(Guid managerId);
    Task<ManagerProjectDetailDto> GetProjectDetailAsync(Guid managerId, Guid projectId);
    Task<ProjectRiskSummaryDto> GenerateProjectRiskSummaryAsync(Guid managerId, Guid projectId);
    Task<ProjectRiskSummaryDto> GenerateScheduledProjectRiskSummaryAsync(
        Guid managerId,
        Guid projectId);
    Task<IReadOnlyList<ManagerTimesheetDto>> GetSubmittedTimesheetsAsync(Guid managerId);
    Task<ResourceMatchResponseDto> FindResourcesAsync(Guid managerId, FindResourceRequestDto request);
    Task<TeamBuilderResponseDto> BuildTeamAsync(Guid managerId, BuildTeamRequestDto request);
    Task<ManagerAllocationDto> AllocateAsync(Guid managerId, CreateManagerAllocationDto request);
    Task<ManagerAllocationDto> EndAllocationAsync(Guid managerId, Guid allocationId);
}
