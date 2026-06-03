using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ResourceMindAI.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class TimesheetController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok("Timesheet Controller Working");
    }
}
