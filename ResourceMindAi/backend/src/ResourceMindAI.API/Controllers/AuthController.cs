using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResourceMindAI.Application.Abstractions.Services;
using ResourceMindAI.Application.DTOs.Auth;

namespace ResourceMindAI.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IJwtService _jwtService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        IJwtService jwtService,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _jwtService = jwtService;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login(LoginDto request)
    {
        _logger.LogInformation("Login request received for username {Username}", request.Username);

        var profile = await _authService.LoginAsync(request);
        var token = _jwtService.GenerateToken(profile.Id, profile.Username, profile.Role.ToString());

        _logger.LogInformation(
            "Login request completed successfully for user {UserId} with role {Role}",
            profile.Id,
            profile.Role);

        return Ok(new LoginResponseDto
        {
            User = profile,
            Token = token
        });
    }

    [HttpPost("change-password")]
    public async Task<ActionResult<LoginResponseDto>> ChangePassword(ChangePasswordDto request)
    {
        request.UserId = GetCurrentUserId();

        _logger.LogInformation("Change password request received for user {UserId}", request.UserId);

        var profile = await _authService.ChangePasswordAsync(request);

        _logger.LogInformation("Change password request completed for user {UserId}", profile.Id);
        return Ok(new LoginResponseDto { User = profile });
    }

    private Guid GetCurrentUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userId, out var id))
        {
            throw new UnauthorizedAccessException("Authenticated user id is invalid.");
        }

        return id;
    }
}
