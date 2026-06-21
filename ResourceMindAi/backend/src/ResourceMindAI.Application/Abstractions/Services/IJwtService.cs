namespace ResourceMindAI.Application.Abstractions.Services;
public interface IJwtService
{
    string GenerateToken(Guid userId, string username, string role);
}
