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
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        IJwtService jwtService,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _jwtService = jwtService;
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login(LoginDto request)
    {
        _logger.LogInformation("Login request received for username {Username}", request.Username);

        var profile = await _authService.LoginAsync(request);
        var token = _jwtService.GenerateToken(profile.Id, profile.Username, profile.Role.ToString());
        AppendAuthCookie(token);

        _logger.LogInformation(
            "Login request completed successfully for user {UserId} with role {Role}",
            profile.Id,
            profile.Role);

        return Ok(new LoginResponseDto { User = profile });
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

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(GetCookieName(), GetAuthCookieOptions());
        return NoContent();
    }

    private void AppendAuthCookie(string token)
    {
        Response.Cookies.Append(GetCookieName(), token, GetAuthCookieOptions());
    }

    private CookieOptions GetAuthCookieOptions()
    {
        var jwtSection = _configuration.GetSection("Jwt");
        var expirationMinutes = int.TryParse(jwtSection["ExpirationMinutes"], out var configuredMinutes)
            ? configuredMinutes
            : 60;

        return new CookieOptions
        {
            HttpOnly = true,
            Secure = !_environment.IsDevelopment() || Request.IsHttps,
            SameSite = SameSiteMode.None,
            Expires = DateTimeOffset.UtcNow.AddMinutes(expirationMinutes),
            Path = "/"
        };
    }

    private string GetCookieName()
    {
        return _configuration["Jwt:CookieName"] ?? "ResourceMindAuth";
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
