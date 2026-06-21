using FluentAssertions;
using ResourceMindAI.Application.Constants;
using ResourceMindAI.Domain.Entities;
using ResourceMindAI.Domain.Enums;
using ResourceMindAI.Infrastructure.Persistence.Repositories;

namespace ResourceMindAI.Infrastructure.Tests.Repositories;

public class NotificationLogRepositoryTests : RepositoryTestBase
{
    [Fact]
    public async Task GetLatestSuccessfulAsync_ShouldReturnLatestSuccessfulMatchingNotification()
    {
        var manager = User(Role.Manager, true, "Manager One");
        var project = Project(manager);
        var olderSuccess = Notification(project, manager, true, DateTime.UtcNow.AddHours(-30));
        var recentFailure = Notification(project, manager, false, DateTime.UtcNow.AddHours(-2));
        var latestSuccess = Notification(project, manager, true, DateTime.UtcNow.AddHours(-4));
        DbContext.Users.Add(manager);
        DbContext.Projects.Add(project);
        DbContext.NotificationLogs.AddRange(olderSuccess, recentFailure, latestSuccess);
        await DbContext.SaveChangesAsync();
        var sut = new NotificationLogRepository(DbContext);

        var result = await sut.GetLatestSuccessfulAsync(
            project.Id,
            manager.Id,
            NotificationTypes.ProjectHealthAlert);

        result.Should().NotBeNull();
        result!.Id.Should().Be(latestSuccess.Id);
    }

    [Fact]
    public async Task AddAsync_ShouldPersistNotificationAttempt()
    {
        var manager = User(Role.Manager, true, "Manager One");
        var project = Project(manager);
        DbContext.Users.Add(manager);
        DbContext.Projects.Add(project);
        await DbContext.SaveChangesAsync();
        var notification = Notification(project, manager, false, DateTime.UtcNow);
        notification.FailureReason = "Email delivery failed.";
        var sut = new NotificationLogRepository(DbContext);

        await sut.AddAsync(notification);

        DbContext.NotificationLogs.Should().ContainSingle(log =>
            log.Id == notification.Id
            && !log.IsSuccess
            && log.FailureReason == "Email delivery failed.");
    }

    private static NotificationLog Notification(
        Project project,
        User recipient,
        bool isSuccess,
        DateTime sentAtUtc)
    {
        return new NotificationLog
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Project = project,
            RecipientUserId = recipient.Id,
            RecipientUser = recipient,
            RecipientEmail = recipient.Email,
            NotificationType = NotificationTypes.ProjectHealthAlert,
            SentAtUtc = sentAtUtc,
            IsSuccess = isSuccess
        };
    }
}
