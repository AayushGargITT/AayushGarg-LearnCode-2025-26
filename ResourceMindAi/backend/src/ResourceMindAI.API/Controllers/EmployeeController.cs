using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ResourceMindAI.Application.Abstractions.Services;
using ResourceMindAI.Application.DTOs.Auth;

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
    public async Task<ActionResult<IEnumerable<UserProfileDto>>> GetAll()
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
}
