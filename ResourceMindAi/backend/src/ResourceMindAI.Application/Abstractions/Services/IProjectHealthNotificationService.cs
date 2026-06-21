using ResourceMindAI.Application.DTOs.Notifications;

namespace ResourceMindAI.Application.Abstractions.Services;

public interface IProjectHealthNotificationService
{
    Task<ProjectHealthNotificationResultDto> NotifyAsync(
        ProjectHealthNotificationRequestDto request,
        CancellationToken cancellationToken = default);
}
