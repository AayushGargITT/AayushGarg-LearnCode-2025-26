using ResourceMindAI.Application.DTOs.Resource;

namespace ResourceMindAI.Application.Abstractions.Services;

public interface ITimesheetService
{
    Task<ResourceAllocationsDto> GetAllocationsAsync(Guid userId);
    Task<TimesheetWeekDto> GetWeekAsync(Guid userId, DateTime? weekStartDate);
    Task SubmitAsync(Guid userId, SubmitResourceTimesheetDto request);
    Task<IReadOnlyList<ResourceTimesheetSummaryDto>> GetHistoryAsync(Guid userId);
    Task<ResourceTimesheetDetailDto> GetWeekDetailAsync(Guid userId, DateTime weekStartDate);
}
