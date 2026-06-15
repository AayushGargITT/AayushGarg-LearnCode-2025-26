using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResourceMindAI.Application.Abstractions.Services;
using ResourceMindAI.Application.DTOs.Employee;

namespace ResourceMindAI.API.Controllers;

[ApiController]
[Authorize(Roles = "Resource")]
[Route("api/v1/resource")]
public class EmployeeController : ControllerBase
{
    private readonly ITimesheetService _timesheetService;

    public EmployeeController(ITimesheetService timesheetService)
    {
        _timesheetService = timesheetService;
    }

    [HttpGet("allocations")]
    public async Task<ActionResult<EmployeeAllocationsDto>> GetAllocations()
    {
        return Ok(await _timesheetService.GetAllocationsAsync(GetCurrentUserId()));
    }

    [HttpGet("timesheets/week")]
    public async Task<ActionResult<TimesheetWeekDto>> GetTimesheetWeek(
        [FromQuery] DateTime? weekStartDate)
    {
        return Ok(await _timesheetService.GetWeekAsync(GetCurrentUserId(), weekStartDate));
    }

    [HttpPost("timesheets")]
    public async Task<IActionResult> SubmitTimesheet(SubmitEmployeeTimesheetDto request)
    {
        await _timesheetService.SubmitAsync(GetCurrentUserId(), request);
        return NoContent();
    }

    [HttpGet("timesheets")]
    public async Task<ActionResult<IReadOnlyList<EmployeeTimesheetSummaryDto>>> GetTimesheets()
    {
        return Ok(await _timesheetService.GetHistoryAsync(GetCurrentUserId()));
    }

    [HttpGet("timesheets/{weekStartDate:datetime}")]
    public async Task<ActionResult<EmployeeTimesheetDetailDto>> GetTimesheetDetail(
        DateTime weekStartDate)
    {
        return Ok(await _timesheetService.GetWeekDetailAsync(
            GetCurrentUserId(),
            weekStartDate));
    }

    private Guid GetCurrentUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var id))
        {
            throw new UnauthorizedAccessException("Authenticated user id is invalid.");
        }

        return id;
    }
}
