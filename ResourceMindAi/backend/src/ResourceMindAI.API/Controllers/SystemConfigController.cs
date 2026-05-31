using Microsoft.AspNetCore.Mvc;

namespace ResourceMindAI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SystemConfigController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok("SystemConfig Controller Working");
    }

}
