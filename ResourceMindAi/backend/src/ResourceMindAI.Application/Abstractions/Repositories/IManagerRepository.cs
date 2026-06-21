using ResourceMindAI.Domain.Entities;

namespace ResourceMindAI.Application.Abstractions.Repositories;

public interface IManagerRepository
{
    Task<IReadOnlyList<ResourceProfile>> GetTeamResourcesAsync(Guid managerId);
    Task<ResourceProfile?> GetTeamResourceAsync(Guid managerId, Guid resourceId);
    Task<IReadOnlyList<User>> GetOrganizationSearchCandidatesAsync();
    Task<IReadOnlyList<Project>> GetProjectsAsync(Guid managerId);
    Task<Project?> GetProjectAsync(Guid managerId, Guid projectId);
    Task<Project?> GetProjectForRiskSummaryAsync(Guid managerId, Guid projectId);
    Task<IReadOnlyList<Timesheet>> GetSubmittedTimesheetsAsync(Guid managerId);
    Task<Allocation?> GetAllocationAsync(Guid managerId, Guid allocationId);
    Task<decimal> GetOverlappingAllocationPercentAsync(Guid resourceId, DateTime fromDate, DateTime toDate, Guid? excludedAllocationId = null);
    Task<Allocation> AddAllocationAsync(Allocation allocation);
    Task SaveChangesAsync();
}
