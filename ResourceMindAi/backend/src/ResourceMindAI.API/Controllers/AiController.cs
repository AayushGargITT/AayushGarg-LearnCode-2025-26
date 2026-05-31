using Microsoft.AspNetCore.Mvc;

namespace ResourceMindAI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AiController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok("AI Controller Working");
    }

}
