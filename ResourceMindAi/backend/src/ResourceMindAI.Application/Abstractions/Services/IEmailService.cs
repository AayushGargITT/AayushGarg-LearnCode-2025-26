using ResourceMindAI.Application.DTOs.Notifications;

namespace ResourceMindAI.Application.Abstractions.Services;

public interface IEmailService
{
    Task SendAsync(
        EmailMessageDto message,
        CancellationToken cancellationToken = default);
}
