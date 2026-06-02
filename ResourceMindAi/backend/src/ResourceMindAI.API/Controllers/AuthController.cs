using Microsoft.AspNetCore.Mvc;
using ResourceMindAI.Application.DTOs.Auth;
using ResourceMindAI.Application.Services;

namespace ResourceMindAI.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(AuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login(LoginDto request)
    {
        _logger.LogInformation("Login request received for username {Username}", request.Username);

        var profile = await _authService.LoginAsync(request);
        if (profile is not null)
        {
            _logger.LogInformation(
                "Login request completed successfully for user {UserId} with role {Role}",
                profile.Id,
                profile.Role);

            return Ok(new LoginResponseDto { User = profile });
        }

        if (!profile.IsActive)
        {
            _logger.LogWarning(
                "Login request rejected because user {UserId} is inactive",
                profile.Id);

            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Your account has been deactivated." });
        }

        _logger.LogWarning("Login request failed for username {Username}", request.Username);
        return Unauthorized(new { message = "Invalid username or password." });
    }
}
