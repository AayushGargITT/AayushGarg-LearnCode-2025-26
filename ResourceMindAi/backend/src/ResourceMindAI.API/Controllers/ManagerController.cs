using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResourceMindAI.Application.Abstractions.Services;
using ResourceMindAI.Application.DTOs.Manager;

namespace ResourceMindAI.API.Controllers;

[ApiController]
[Authorize(Roles = "Manager")]
[Route("api/v1/[controller]")]
public class ManagerController : ControllerBase
{
    private readonly IManagerService _managerService;
    private readonly ILogger<ManagerController> _logger;

    public ManagerController(IManagerService managerService, ILogger<ManagerController> logger)
    {
        _managerService = managerService;
        _logger = logger;
    }

    [HttpGet("resources")]
    public async Task<ActionResult<ManagerResourceDashboardDto>> GetResourceDashboard()
    {
        var managerId = GetCurrentUserId();
        _logger.LogInformation("Manager resource dashboard requested by manager {ManagerId}", managerId);

        var dashboard = await _managerService.GetResourceDashboardAsync(managerId);
        return Ok(dashboard);
    }

    [HttpGet("resources/{employeeId:guid}")]
    public async Task<ActionResult<ManagerResourceDto>> GetResourceDetail(Guid employeeId)
    {
        var managerId = GetCurrentUserId();
        var resource = await _managerService.GetResourceDetailAsync(managerId, employeeId);
        return Ok(resource);
    }

    [HttpGet("projects")]
    public async Task<ActionResult<IEnumerable<ManagerProjectDto>>> GetProjects()
    {
        var managerId = GetCurrentUserId();
        var projects = await _managerService.GetProjectsAsync(managerId);
        return Ok(projects);
    }

    [HttpGet("projects/{projectId:guid}")]
    public async Task<ActionResult<ManagerProjectDetailDto>> GetProjectDetail(Guid projectId)
    {
        var managerId = GetCurrentUserId();
        var project = await _managerService.GetProjectDetailAsync(managerId, projectId);
        return Ok(project);
    }

    [HttpPost("projects/{projectId:guid}/risk-summary")]
    public async Task<ActionResult<ProjectRiskSummaryDto>> GenerateProjectRiskSummary(Guid projectId)
    {
        var managerId = GetCurrentUserId();
        var summary = await _managerService.GenerateProjectRiskSummaryAsync(managerId, projectId);
        return Ok(summary);
    }

    [HttpGet("timesheets")]
    public async Task<ActionResult<IEnumerable<ManagerTimesheetDto>>> GetSubmittedTimesheets()
    {
        var managerId = GetCurrentUserId();
        var timesheets = await _managerService.GetSubmittedTimesheetsAsync(managerId);
        return Ok(timesheets);
    }

    [HttpPost("resources/find")]
    public async Task<ActionResult<ResourceMatchResponseDto>> FindResources(FindResourceRequestDto request)
    {
        var managerId = GetCurrentUserId();
        var response = await _managerService.FindResourcesAsync(managerId, request);
        return Ok(response);
    }

    [HttpPost("allocations")]
    public async Task<ActionResult<ManagerAllocationDto>> Allocate(CreateManagerAllocationDto request)
    {
        var managerId = GetCurrentUserId();
        var allocation = await _managerService.AllocateAsync(managerId, request);
        return Ok(allocation);
    }

    [HttpPatch("allocations/{allocationId:guid}/end")]
    public async Task<ActionResult<ManagerAllocationDto>> EndAllocation(Guid allocationId)
    {
        var managerId = GetCurrentUserId();
        var allocation = await _managerService.EndAllocationAsync(managerId, allocationId);
        return Ok(allocation);
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
