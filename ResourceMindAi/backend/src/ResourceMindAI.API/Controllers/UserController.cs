using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ResourceMindAI.Application.Abstractions.Services;
using ResourceMindAI.Application.DTOs.Auth;
using ResourceMindAI.Application.DTOs.User;
using ResourceMindAI.Application.DTOs.Employee;

namespace ResourceMindAI.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/[controller]")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UserController> _logger;

    public UserController(
        IUserService userService,
        ILogger<UserController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserProfileDto>> Get(Guid id)
    {
        _logger.LogInformation("User lookup requested for user {UserId}", id);
        var user = await _userService.GetByIdAsync(id);
        return Ok(user);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserProfileDto>>> GetAll()
    {
        _logger.LogInformation("User list request received");
        var users = await _userService.GetAllAsync();
        _logger.LogInformation("User list request completed with {UserCount} users", users.Count);
        return Ok(users);
    }

    [HttpGet("active-managers")]
    public async Task<ActionResult<IEnumerable<UserProfileDto>>> GetActiveManagers()
    {
        _logger.LogInformation("Active manager list request received");
        var managers = await _userService.GetActiveManagersAsync();
        _logger.LogInformation("Active manager list request completed with {ManagerCount} managers", managers.Count);
        return Ok(managers);
    }

    [HttpPost]
    public async Task<ActionResult<UserProfileDto>> Create(CreateUserDto request)
    {
        _logger.LogInformation(
            "Create user request received for username {Username} and role {Role}",
            request.Username,
            request.Role);

        var user = await _userService.CreateAsync(request);

        _logger.LogInformation(
            "Create user request completed for user {UserId} with role {Role}",
            user.Id,
            user.Role);

        return CreatedAtAction(nameof(Get), new { id = user.Id }, user);
    }

    [HttpPost("reset-password/{id:guid}")]
    public async Task<ActionResult<UserProfileDto>> ResetPassword(Guid id)
    {
        _logger.LogInformation("Reset password request received for user {UserId}", id);
        var user = await _userService.ResetPasswordAsync(id);
        return Ok(user);
    }

    [HttpPatch("{id:guid}/deactivate")]
    public async Task<ActionResult<DeactivateUserResultDto>> Deactivate(Guid id)
    {
        _logger.LogInformation("Deactivate user request received for user {UserId}", id);
        return Ok(await _userService.DeactivateAsync(id));
    }

    [HttpPatch("{id:guid}/reactivate")]
    public async Task<ActionResult<UserProfileDto>> Reactivate(Guid id)
    {
        _logger.LogInformation("Reactivate user request received for user {UserId}", id);
        return Ok(await _userService.ReactivateAsync(id));
    }

    [HttpPost("add-employee/{id:guid}")]
    public async Task<ActionResult<UserProfileDto>> AddEmployee(Guid id, AddEmployeeDto request)
    {
        _logger.LogInformation("Add employee request received for user {UserId}", id);
        var user = await _userService.AddEmployeeAsync(id, request);
        return Ok(user);
    }
}
