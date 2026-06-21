using ResourceMindAI.Application.DTOs.Allocation;

namespace ResourceMindAI.Application.Abstractions.Services;

public interface IAllocationService
{
    Task<IReadOnlyList<AllocationDto>> GetAllAsync();
}
