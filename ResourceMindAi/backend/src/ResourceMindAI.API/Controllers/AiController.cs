using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ResourceMindAI.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class AiController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok("AI Controller Working");
    }
}
