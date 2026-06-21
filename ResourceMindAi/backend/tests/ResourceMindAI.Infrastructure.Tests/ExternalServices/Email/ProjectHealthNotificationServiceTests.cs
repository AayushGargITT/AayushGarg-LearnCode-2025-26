using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using ResourceMindAI.Application.Abstractions.Repositories;
using ResourceMindAI.Application.Abstractions.Services;
using ResourceMindAI.Application.Constants;
using ResourceMindAI.Application.DTOs.Manager;
using ResourceMindAI.Application.DTOs.Notifications;
using ResourceMindAI.Domain.Entities;
using ResourceMindAI.Infrastructure.ExternalServices.Email;

namespace ResourceMindAI.Infrastructure.Tests.ExternalServices.Email;

public class ProjectHealthNotificationServiceTests
{
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<INotificationLogRepository> _notificationLogs = new();

    [Fact]
    public async Task NotifyAsync_WhenProjectIsOnTrack_ShouldNotSendEmail()
    {
        var sut = CreateService();

        var result = await sut.NotifyAsync(Request("ON_TRACK"));

        result.Sent.Should().BeFalse();
        _emailService.Verify(
            x => x.SendAsync(It.IsAny<EmailMessageDto>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData("ATTENTION")]
    [InlineData("AT_RISK")]
    public async Task NotifyAsync_WhenProjectIsUnhealthy_ShouldSendPlainEnglishEmail(
        string health)
    {
        EmailMessageDto? sentMessage = null;
        _emailService.Setup(x => x.SendAsync(
                It.IsAny<EmailMessageDto>(),
                It.IsAny<CancellationToken>()))
            .Callback<EmailMessageDto, CancellationToken>((message, _) => sentMessage = message)
            .Returns(Task.CompletedTask);
        var sut = CreateService();
        var request = Request(health);

        var result = await sut.NotifyAsync(request);

        result.Sent.Should().BeTrue();
        result.SentAt.Should().NotBeNull();
        sentMessage.Should().NotBeNull();
        sentMessage!.Subject.Should().Be("Project Health Alert - Apollo");
        sentMessage.TextContent.Should().Contain("Key Issues:");
        sentMessage.TextContent.Should().Contain("Recommended Actions:");
        sentMessage.TextContent.Should().Contain("Suggested Skills or Resources:");
        sentMessage.TextContent.Should().Contain("QA Automation");
        sentMessage.TextContent.Should().NotContain("{\"overallHealth\"");
        _notificationLogs.Verify(x => x.AddAsync(
            It.Is<NotificationLog>(log =>
                log.IsSuccess
                && log.ProjectId == request.ProjectId
                && log.RecipientUserId == request.ManagerId
                && log.NotificationType == NotificationTypes.ProjectHealthAlert
                && log.FailureReason == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NotifyAsync_WhenSameHealthWasRecentlyNotified_ShouldSkipEmail()
    {
        var request = Request("ATTENTION");
        _notificationLogs.Setup(x => x.GetLatestSuccessfulAsync(
                request.ProjectId,
                request.ManagerId,
                NotificationTypes.ProjectHealthAlert,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Notification(request, true, DateTime.UtcNow.AddHours(-2)));
        var sut = CreateService(throttleHours: 24);

        var result = await sut.NotifyAsync(request);

        result.Sent.Should().BeFalse();
        _emailService.Verify(
            x => x.SendAsync(It.IsAny<EmailMessageDto>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task NotifyAsync_WhenLatestSuccessfulNotificationIsOlderThanThrottleWindow_ShouldSend()
    {
        var request = Request("AT_RISK");
        _notificationLogs.Setup(x => x.GetLatestSuccessfulAsync(
                request.ProjectId,
                request.ManagerId,
                NotificationTypes.ProjectHealthAlert,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Notification(request, true, DateTime.UtcNow.AddHours(-25)));
        _emailService.Setup(x => x.SendAsync(
                It.IsAny<EmailMessageDto>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var sut = CreateService(throttleHours: 24);

        var result = await sut.NotifyAsync(request);

        result.Sent.Should().BeTrue();
    }

    [Fact]
    public async Task NotifyAsync_WhenLatestAttemptFailed_ShouldSendAgain()
    {
        var request = Request("AT_RISK");
        _notificationLogs.Setup(x => x.GetLatestSuccessfulAsync(
                request.ProjectId,
                request.ManagerId,
                NotificationTypes.ProjectHealthAlert,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotificationLog?)null);
        _emailService.Setup(x => x.SendAsync(
                It.IsAny<EmailMessageDto>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var sut = CreateService();

        var result = await sut.NotifyAsync(request);

        result.Sent.Should().BeTrue();
    }

    [Fact]
    public async Task NotifyAsync_WhenEmailProviderFails_ShouldNotThrow()
    {
        _emailService.Setup(x => x.SendAsync(
                It.IsAny<EmailMessageDto>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Provider unavailable"));
        var sut = CreateService();

        var result = await sut.NotifyAsync(Request("AT_RISK"));

        result.Sent.Should().BeFalse();
        _notificationLogs.Verify(x => x.AddAsync(
            It.Is<NotificationLog>(log =>
                !log.IsSuccess
                && log.NotificationType == NotificationTypes.ProjectHealthAlert
                && log.FailureReason == "Unexpected email delivery failure."),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private ProjectHealthNotificationService CreateService(int throttleHours = 24)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Notifications:ProjectHealth:ThrottleHours"] = throttleHours.ToString()
            })
            .Build();

        return new ProjectHealthNotificationService(
            _emailService.Object,
            _notificationLogs.Object,
            configuration,
            Mock.Of<ILogger<ProjectHealthNotificationService>>());
    }

    private static ProjectHealthNotificationRequestDto Request(
        string health)
    {
        return new ProjectHealthNotificationRequestDto
        {
            ProjectId = Guid.NewGuid(),
            ProjectName = "Apollo",
            ManagerId = Guid.NewGuid(),
            ManagerName = "Manager One",
            ManagerEmail = "manager@example.com",
            RiskSummary = new ProjectRiskSummaryDto
            {
                OverallHealth = health,
                Summary = "The project has milestone and testing concerns.",
                RiskPoints =
                [
                    new ProjectRiskPointDto
                    {
                        Severity = "HIGH",
                        Title = "Testing delay",
                        Description = "Testing has not started."
                    }
                ],
                RecommendedActions = ["Start a focused testing plan."],
                SuggestedSkills = ["QA Automation"],
                GeneratedAt = DateTime.UtcNow
            }
        };
    }

    private static NotificationLog Notification(
        ProjectHealthNotificationRequestDto request,
        bool isSuccess,
        DateTime sentAtUtc)
    {
        return new NotificationLog
        {
            Id = Guid.NewGuid(),
            ProjectId = request.ProjectId,
            RecipientUserId = request.ManagerId,
            RecipientEmail = request.ManagerEmail,
            NotificationType = NotificationTypes.ProjectHealthAlert,
            SentAtUtc = sentAtUtc,
            IsSuccess = isSuccess
        };
    }
}
