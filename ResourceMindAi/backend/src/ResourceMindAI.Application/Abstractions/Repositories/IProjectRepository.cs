using System;
using System.Threading.Tasks;
using ResourceMindAI.Domain.Entities;

namespace ResourceMindAI.Application.Abstractions.Repositories;
public interface IProjectRepository
{
    // Minimal method signature for compilation
    Task GetByIdAsync(Guid id);
}
