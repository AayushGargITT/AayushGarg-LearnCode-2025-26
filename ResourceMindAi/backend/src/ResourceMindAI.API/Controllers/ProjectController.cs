using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResourceMindAI.Application.Abstractions.Services;
using ResourceMindAI.Application.DTOs.Project;

namespace ResourceMindAI.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
public class ProjectController : ControllerBase
{
    private readonly IProjectService _projectService;
    private readonly ILogger<ProjectController> _logger;

    public ProjectController(IProjectService projectService, ILogger<ProjectController> logger)
    {
        _projectService = projectService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProjectDto>>> GetAll()
    {
        _logger.LogInformation("Project list request received");

        var projects = await _projectService.GetAllAsync();

        _logger.LogInformation("Project list request completed with {ProjectCount} projects", projects.Count);
        return Ok(projects);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<ProjectDto>> Create(CreateProjectDto request)
    {
        _logger.LogInformation("Create project request received for {ProjectName}", request.Name);

        var project = await _projectService.CreateAsync(request);

        _logger.LogInformation("Create project request completed for project {ProjectId}", project.Id);
        return Ok(project);
    }

    [HttpGet("{projectId:guid}/milestones")]
    public async Task<ActionResult<IEnumerable<MilestoneDto>>> GetMilestones(Guid projectId)
    {
        _logger.LogInformation("Project milestones request received for project {ProjectId}", projectId);

        var milestones = await _projectService.GetMilestonesAsync(projectId);

        _logger.LogInformation("Project milestones request completed with {MilestoneCount} milestones", milestones.Count);
        return Ok(milestones);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{projectId:guid}/milestones")]
    public async Task<ActionResult<MilestoneDto>> AddMilestone(Guid projectId, CreateMilestoneDto request)
    {
        _logger.LogInformation("Add milestone request received for project {ProjectId}", projectId);

        var milestone = await _projectService.AddMilestoneAsync(projectId, request);

        _logger.LogInformation("Add milestone request completed for milestone {MilestoneId}", milestone.Id);
        return Ok(milestone);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{projectId:guid}/milestones/{milestoneId:guid}")]
    public async Task<ActionResult<MilestoneDto>> UpdateMilestone(
        Guid projectId,
        Guid milestoneId,
        UpdateMilestoneDto request)
    {
        _logger.LogInformation(
            "Update milestone request received for milestone {MilestoneId} and project {ProjectId}",
            milestoneId,
            projectId);

        var milestone = await _projectService.UpdateMilestoneAsync(projectId, milestoneId, request);

        _logger.LogInformation("Update milestone request completed for milestone {MilestoneId}", milestone.Id);
        return Ok(milestone);
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("{projectId:guid}/manager")]
    public async Task<ActionResult<ProjectManagerUpdateResultDto>> UpdateManager(
        Guid projectId,
        UpdateProjectManagerDto request)
    {
        return Ok(await _projectService.UpdateManagerAsync(projectId, request));
    }
}
