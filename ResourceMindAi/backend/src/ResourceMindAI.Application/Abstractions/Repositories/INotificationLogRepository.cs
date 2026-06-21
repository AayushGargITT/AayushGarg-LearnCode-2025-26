using ResourceMindAI.Domain.Entities;

namespace ResourceMindAI.Application.Abstractions.Repositories;

public interface INotificationLogRepository
{
    Task<NotificationLog?> GetLatestSuccessfulAsync(
        Guid projectId,
        Guid recipientUserId,
        string notificationType,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        NotificationLog notificationLog,
        CancellationToken cancellationToken = default);
}
