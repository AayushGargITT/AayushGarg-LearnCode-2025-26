using ResourceMindAI.Domain.Enums;

namespace ResourceMindAI.Application.DTOs.Manager;

public class ManagerResourceDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string FullName { get; set; } = null!;
    public string Department { get; set; } = null!;
    public string Designation { get; set; } = null!;
    public ResourceStatus CurrentStatus { get; set; }
    public decimal AllocationPercent { get; set; }
    public IReadOnlyList<string> Skills { get; set; } = [];
    public IReadOnlyList<ManagerAllocationDto> ActiveAllocations { get; set; } = [];
    public IReadOnlyList<string> RecentActivityTags { get; set; } = [];
}
