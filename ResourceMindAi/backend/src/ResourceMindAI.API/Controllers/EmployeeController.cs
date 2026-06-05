using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ResourceMindAI.Application.Abstractions.Services;
using ResourceMindAI.Application.DTOs.Auth;
using ResourceMindAI.Application.DTOs.Employee;

namespace ResourceMindAI.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
public class EmployeeController : ControllerBase
{
    private readonly IEmployeeService _employeeService;
    private readonly ILogger<EmployeeController> _logger;

    public EmployeeController(IEmployeeService employeeService, ILogger<EmployeeController> logger)
    {
        _employeeService = employeeService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EmployeeListDto>>> GetAll()
    {
        _logger.LogInformation("Employee list request received");
        var employees = await _employeeService.GetAllAsync();
        _logger.LogInformation("Employee list request completed with {EmployeeCount} employees", employees.Count);
        return Ok(employees);
    }

    [HttpGet("{userId:guid}")]
    public async Task<ActionResult<UserProfileDto>> GetById(Guid userId)
    {
        _logger.LogInformation("Employee profile request received for user {UserId}", userId);

        var employee = await _employeeService.GetByIdAsync(userId);

        _logger.LogInformation("Employee profile request completed for employee {EmployeeId}", employee.EmployeeId);
        return Ok(employee);
    }

    [HttpGet("{employeeId:guid}/skills")]
    public async Task<ActionResult<IEnumerable<EmployeeSkillDto>>> GetSkills(Guid employeeId)
    {
        _logger.LogInformation("Employee skills request received for employee {EmployeeId}", employeeId);

        var skills = await _employeeService.GetSkillsAsync(employeeId);

        _logger.LogInformation("Employee skills request completed with {SkillCount} skills", skills.Count);
        return Ok(skills);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{employeeId:guid}/skills")]
    public async Task<ActionResult<EmployeeSkillDto>> AddSkill(Guid employeeId, CreateEmployeeSkillDto request)
    {
        _logger.LogInformation("Add employee skill request received for employee {EmployeeId}", employeeId);

        var skill = await _employeeService.AddSkillAsync(employeeId, request);

        _logger.LogInformation("Add employee skill request completed for skill {SkillId}", skill.Id);
        return Ok(skill);
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("{employeeId:guid}/skills/{skillId:guid}/proficiency")]
    public async Task<ActionResult<EmployeeSkillDto>> UpdateSkillProficiency(
        Guid employeeId,
        Guid skillId,
        UpdateEmployeeSkillProficiencyDto request)
    {
        _logger.LogInformation(
            "Update skill proficiency request received for skill {SkillId} and employee {EmployeeId}",
            skillId,
            employeeId);

        var skill = await _employeeService.UpdateSkillProficiencyAsync(employeeId, skillId, request);

        _logger.LogInformation("Update skill proficiency request completed for skill {SkillId}", skill.Id);
        return Ok(skill);
    }
}
