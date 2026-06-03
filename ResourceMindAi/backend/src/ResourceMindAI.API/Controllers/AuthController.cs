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

    [HttpPost("change-password")]
    public async Task<ActionResult<LoginResponseDto>> ChangePassword(ChangePasswordDto request)
    {
        _logger.LogInformation("Change password request received for user {UserId}", request.UserId);

        var result = await _authService.ChangePasswordAsync(request);
        if (result.User is null)
        {
            _logger.LogWarning(
                "Change password request failed for user {UserId} with status {StatusCode}: {Error}",
                request.UserId,
                result.StatusCode,
                result.Error);

            return StatusCode(result.StatusCode, new { message = result.Error });
        }

        _logger.LogInformation("Change password request completed for user {UserId}", result.User.Id);
        return Ok(new LoginResponseDto { User = result.User });
    }
}
