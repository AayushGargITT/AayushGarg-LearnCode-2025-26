using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResourceMindAI.Application.Abstractions.Services;
using ResourceMindAI.Application.DTOs.Allocation;

namespace ResourceMindAI.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
public class AllocationController : ControllerBase
{
    private readonly IAllocationService _allocationService;
    private readonly ILogger<AllocationController> _logger;

    public AllocationController(IAllocationService allocationService, ILogger<AllocationController> logger)
    {
        _allocationService = allocationService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AllocationDto>>> GetAll()
    {
        _logger.LogInformation("Allocation list request received");

        var allocations = await _allocationService.GetAllAsync();

        _logger.LogInformation("Allocation list request completed with {AllocationCount} allocations", allocations.Count);
        return Ok(allocations);
    }
}
