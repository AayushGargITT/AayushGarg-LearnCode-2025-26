using ResourceMindAI.Domain.Entities;

namespace ResourceMindAI.Application.Abstractions.Repositories;

public interface IAllocationRepository
{
    Task<IReadOnlyList<Allocation>> GetAllAsync();
}
