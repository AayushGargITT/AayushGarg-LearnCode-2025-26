using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResourceMindAI.Application.Abstractions.Services;
using ResourceMindAI.Application.DTOs.Auth;
using ResourceMindAI.Application.DTOs.Employee;

namespace ResourceMindAI.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/employees")]
public class AdminEmployeeController : ControllerBase
{
    private readonly IAdminEmployeeService _adminEmployeeService;
    private readonly ILogger<AdminEmployeeController> _logger;

    public AdminEmployeeController(
        IAdminEmployeeService adminEmployeeService,
        ILogger<AdminEmployeeController> logger)
    {
        _adminEmployeeService = adminEmployeeService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EmployeeListDto>>> GetAll()
    {
        _logger.LogInformation("Admin employee list request received");
        var employees = await _adminEmployeeService.GetAllAsync();
        _logger.LogInformation(
            "Admin employee list request completed with {EmployeeCount} employees",
            employees.Count);
        return Ok(employees);
    }

    [HttpGet("{userId:guid}")]
    public async Task<ActionResult<UserProfileDto>> GetById(Guid userId)
    {
        _logger.LogInformation("Admin employee profile request received for user {UserId}", userId);

        var employee = await _adminEmployeeService.GetByIdAsync(userId);

        _logger.LogInformation(
            "Admin employee profile request completed for employee {EmployeeId}",
            employee.EmployeeId);
        return Ok(employee);
    }

    [HttpGet("{employeeId:guid}/skills")]
    public async Task<ActionResult<IEnumerable<EmployeeSkillDto>>> GetSkills(Guid employeeId)
    {
        _logger.LogInformation(
            "Admin employee skills request received for employee {EmployeeId}",
            employeeId);

        var skills = await _adminEmployeeService.GetSkillsAsync(employeeId);

        _logger.LogInformation(
            "Admin employee skills request completed with {SkillCount} skills",
            skills.Count);
        return Ok(skills);
    }

    [HttpPost("{employeeId:guid}/skills")]
    public async Task<ActionResult<EmployeeSkillDto>> AddSkill(
        Guid employeeId,
        CreateEmployeeSkillDto request)
    {
        _logger.LogInformation(
            "Admin add employee skill request received for employee {EmployeeId}",
            employeeId);

        var skill = await _adminEmployeeService.AddSkillAsync(employeeId, request);

        _logger.LogInformation(
            "Admin add employee skill request completed for skill {SkillId}",
            skill.Id);
        return Ok(skill);
    }

    [HttpPatch("{employeeId:guid}/skills/{skillId:guid}/proficiency")]
    public async Task<ActionResult<EmployeeSkillDto>> UpdateSkillProficiency(
        Guid employeeId,
        Guid skillId,
        UpdateEmployeeSkillProficiencyDto request)
    {
        _logger.LogInformation(
            "Admin update skill proficiency request received for skill {SkillId} and employee {EmployeeId}",
            skillId,
            employeeId);

        var skill = await _adminEmployeeService.UpdateSkillProficiencyAsync(
            employeeId,
            skillId,
            request);

        _logger.LogInformation(
            "Admin update skill proficiency request completed for skill {SkillId}",
            skill.Id);
        return Ok(skill);
    }

    [HttpGet("{employeeId:guid}/manager-update-preview")]
    public async Task<ActionResult<EmployeeManagerUpdatePreviewDto>> GetManagerUpdatePreview(
        Guid employeeId,
        [FromQuery] Guid newManagerId)
    {
        return Ok(await _adminEmployeeService.GetManagerUpdatePreviewAsync(
            employeeId,
            newManagerId));
    }

    [HttpPatch("{employeeId:guid}/manager")]
    public async Task<ActionResult<EmployeeManagerUpdateResultDto>> UpdateManager(
        Guid employeeId,
        UpdateEmployeeManagerDto request)
    {
        return Ok(await _adminEmployeeService.UpdateManagerAsync(employeeId, request));
    }
}
