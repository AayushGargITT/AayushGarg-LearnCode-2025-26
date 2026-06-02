using Microsoft.AspNetCore.Mvc;
using ResourceMindAI.Application.DTOs.Auth;
using ResourceMindAI.Application.Services;

namespace ResourceMindAI.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login(LoginDto request)
    {
        var profile = await _authService.LoginAsync(request);
        if (profile is not null)
        {
            return Ok(new LoginResponseDto { User = profile });
        }

        if (!profile.IsActive)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Your account has been deactivated." });
        }

        return Unauthorized(new { message = "Invalid username or password." });
    }
}
