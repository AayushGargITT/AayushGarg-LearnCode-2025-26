using Microsoft.AspNetCore.Mvc;

namespace ResourceMindAI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TimesheetController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok("Timesheet Controller Working");
    }

}
