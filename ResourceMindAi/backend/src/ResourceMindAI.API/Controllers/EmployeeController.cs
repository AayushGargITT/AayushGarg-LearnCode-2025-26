using Microsoft.AspNetCore.Mvc;
using ResourceMindAI.Application.DTOs.Auth;
using ResourceMindAI.Application.Services;

namespace ResourceMindAI.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class EmployeeController : ControllerBase
{
    private readonly EmployeeService _employeeService;
    private readonly ILogger<EmployeeController> _logger;

    public EmployeeController(EmployeeService employeeService, ILogger<EmployeeController> logger)
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
        if (employee is null)
        {
            _logger.LogWarning("Employee profile was not found for user {UserId}", userId);
            return NotFound();
        }

        _logger.LogInformation("Employee profile request completed for employee {EmployeeId}", employee.EmployeeId);
        return Ok(employee);
    }
}
