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
    public async Task<ActionResult<IEnumerable<UserProfileDto>>> Get(Guid id)
    {
        _logger.LogInformation("User lookup requested for user {UserId}", id);
        return Ok(await _userService.GetAllAsync());
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

        var result = await _userService.CreateAsync(request);
        if (result.User is null)
        {
            _logger.LogWarning(
                "Create user request failed for username {Username} with status {StatusCode}: {Error}",
                request.Username,
                result.StatusCode,
                result.Error);

            return StatusCode(result.StatusCode, new { message = result.Error });
        }

        _logger.LogInformation(
            "Create user request completed for user {UserId} with role {Role}",
            result.User.Id,
            result.User.Role);

        return CreatedAtAction(nameof(Get), new { id = result.User.Id }, result.User);
    }
}
