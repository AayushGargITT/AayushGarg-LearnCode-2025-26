using System;
using System.Threading.Tasks;
using ResourceMindAI.Application.Abstractions.Repositories;
using ResourceMindAI.Domain.Entities;

namespace ResourceMindAI.Infrastructure.Persistence.Repositories;
public class SystemConfigRepository : ISystemConfigRepository
{
    public Task GetByIdAsync(Guid id)
    {
        throw new NotImplementedException();
    }
}
