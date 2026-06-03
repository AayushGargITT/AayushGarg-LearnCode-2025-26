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

        _logger.LogInformation(
            "Login request completed successfully for user {UserId} with role {Role}",
            profile.Id,
            profile.Role);

        return Ok(new LoginResponseDto { User = profile });
    }

    [HttpPost("change-password")]
    public async Task<ActionResult<LoginResponseDto>> ChangePassword(ChangePasswordDto request)
    {
        _logger.LogInformation("Change password request received for user {UserId}", request.UserId);

        var profile = await _authService.ChangePasswordAsync(request);

        _logger.LogInformation("Change password request completed for user {UserId}", profile.Id);
        return Ok(new LoginResponseDto { User = profile });
    }
}
