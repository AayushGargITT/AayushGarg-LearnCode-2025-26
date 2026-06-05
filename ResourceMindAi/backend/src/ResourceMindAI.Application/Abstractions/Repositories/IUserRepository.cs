using ResourceMindAI.Domain.Entities;

namespace ResourceMindAI.Application.Abstractions.Repositories;

public interface IUserRepository
{
    Task<IReadOnlyList<User>> GetAllAsync();
    Task<IReadOnlyList<User>> GetActiveManagersAsync();
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByUsernameAsync(string username);
    Task<bool> ExistsByUsernameOrEmailAsync(string username, string email);
    Task<User> CreateAsync(User user);
    Task<User> CreateWithEmployeeAsync(User user, Employee employee);
    Task<User> UpdateAsync(User user);
    Task<Employee> AddEmployeeAsync(Employee employee);
    Task<User> UpdatePasswordAsync(User user, string passwordHash);
}
