using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ResourceMindAI.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class AllocationController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok("Allocation Controller Working");
    }
}
