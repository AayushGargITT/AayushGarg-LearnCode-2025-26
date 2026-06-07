using ResourceMindAI.Application.DTOs.Employee;

namespace ResourceMindAI.Application.Abstractions.Services;

public interface ITimesheetService
{
    Task<EmployeeAllocationsDto> GetAllocationsAsync(Guid userId);
    Task<TimesheetWeekDto> GetWeekAsync(Guid userId, DateTime? weekStartDate);
    Task SubmitAsync(Guid userId, SubmitEmployeeTimesheetDto request);
    Task<IReadOnlyList<EmployeeTimesheetSummaryDto>> GetHistoryAsync(Guid userId);
    Task<EmployeeTimesheetDetailDto> GetWeekDetailAsync(Guid userId, DateTime weekStartDate);
}
