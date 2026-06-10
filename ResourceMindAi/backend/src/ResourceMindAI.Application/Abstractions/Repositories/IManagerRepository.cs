using ResourceMindAI.Domain.Entities;

namespace ResourceMindAI.Application.Abstractions.Repositories;

public interface IManagerRepository
{
    Task<IReadOnlyList<Employee>> GetTeamEmployeesAsync(Guid managerId);
    Task<Employee?> GetTeamEmployeeAsync(Guid managerId, Guid employeeId);
    Task<IReadOnlyList<Project>> GetProjectsAsync(Guid managerId);
    Task<Project?> GetProjectAsync(Guid managerId, Guid projectId);
    Task<Project?> GetProjectForRiskSummaryAsync(Guid managerId, Guid projectId);
    Task<IReadOnlyList<Timesheet>> GetSubmittedTimesheetsAsync(Guid managerId);
    Task<Allocation?> GetAllocationAsync(Guid managerId, Guid allocationId);
    Task<decimal> GetOverlappingAllocationPercentAsync(Guid employeeId, DateTime fromDate, DateTime toDate, Guid? excludedAllocationId = null);
    Task<Allocation> AddAllocationAsync(Allocation allocation);
    Task SaveChangesAsync();
}
