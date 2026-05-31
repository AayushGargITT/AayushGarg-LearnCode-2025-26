using Microsoft.AspNetCore.Mvc;

namespace ResourceMindAI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AllocationController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok("Allocation Controller Working");
    }

}
