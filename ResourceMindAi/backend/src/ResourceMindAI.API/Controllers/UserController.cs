using Microsoft.AspNetCore.Mvc;
using ResourceMindAI.Application.DTOs.Auth;
using ResourceMindAI.Application.DTOs.User;
using ResourceMindAI.Application.Services;

namespace ResourceMindAI.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class UserController : ControllerBase
{
    private readonly UserService _userService;
    private readonly ILogger<UserController> _logger;

    public UserController(UserService userService, ILogger<UserController> logger)
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
}
