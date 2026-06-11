using ResourceMindAI.Application.Abstractions.Repositories;
using ResourceMindAI.Application.Abstractions.Services;
using ResourceMindAI.Application.DTOs.Allocation;

namespace ResourceMindAI.Application.Services;

public class AllocationService : IAllocationService
{
    private readonly IAllocationRepository _allocationRepository;

    public AllocationService(IAllocationRepository allocationRepository)
    {
        _allocationRepository = allocationRepository;
    }

    public async Task<IReadOnlyList<AllocationDto>> GetAllAsync()
    {
        var allocations = await _allocationRepository.GetAllAsync();

        return allocations.Select(allocation => new AllocationDto
        {
            Id = allocation.Id,
            EmployeeId = allocation.UserId,
            EmployeeName = allocation.User.FullName,
            EmployeeDesignation = allocation.User.Designation ?? string.Empty,
            ProjectId = allocation.ProjectId,
            ProjectName = allocation.Project.Name,
            ProjectManager = allocation.Project.Manager.FullName,
            UtilisationPercent = allocation.UtilisationPercent,
            FromDate = allocation.FromDate,
            ToDate = allocation.ToDate,
            IsActive = allocation.IsActive,
            CreatedAt = allocation.CreatedAt,
        }).ToList();
    }
}
