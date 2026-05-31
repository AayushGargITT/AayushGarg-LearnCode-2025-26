using System;
using System.Threading.Tasks;
using ResourceMindAI.Application.Abstractions.Repositories;
using ResourceMindAI.Domain.Entities;

namespace ResourceMindAI.Infrastructure.Persistence.Repositories;
public class UserRepository : IUserRepository
{
    public Task GetByIdAsync(Guid id)
    {
        throw new NotImplementedException();
    }
}
