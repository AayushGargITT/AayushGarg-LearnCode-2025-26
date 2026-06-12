using Microsoft.EntityFrameworkCore;
using ResourceMindAI.Application.Abstractions.Repositories;
using ResourceMindAI.Domain.Entities;

namespace ResourceMindAI.Infrastructure.Persistence.Repositories;

public sealed class NotificationLogRepository : INotificationLogRepository
{
    private readonly AppDbContext _dbContext;

    public NotificationLogRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<NotificationLog?> GetLatestSuccessfulAsync(
        Guid projectId,
        Guid recipientUserId,
        string notificationType,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.NotificationLogs
            .AsNoTracking()
            .Where(notification =>
                notification.ProjectId == projectId
                && notification.RecipientUserId == recipientUserId
                && notification.NotificationType == notificationType
                && notification.IsSuccess)
            .OrderByDescending(notification => notification.SentAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(
        NotificationLog notificationLog,
        CancellationToken cancellationToken = default)
    {
        _dbContext.NotificationLogs.Add(notificationLog);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
