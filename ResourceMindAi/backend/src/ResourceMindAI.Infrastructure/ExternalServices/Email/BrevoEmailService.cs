using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ResourceMindAI.Application.Abstractions.Services;
using ResourceMindAI.Application.DTOs.Notifications;
using ResourceMindAI.Domain.Exceptions;

namespace ResourceMindAI.Infrastructure.ExternalServices.Email;

public sealed class BrevoEmailService : IEmailService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BrevoEmailService> _logger;

    public BrevoEmailService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<BrevoEmailService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendAsync(
        EmailMessageDto message,
        CancellationToken cancellationToken = default)
    {
        var apiKey = GetRequiredSetting("Notifications:Brevo:ApiKey");
        var senderEmail = GetRequiredSetting("Notifications:Brevo:SenderEmail");
        var senderName = GetRequiredSetting("Notifications:Brevo:SenderName");

        using var request = new HttpRequestMessage(HttpMethod.Post, "v3/smtp/email")
        {
            Content = JsonContent.Create(
                new
                {
                    sender = new
                    {
                        email = senderEmail,
                        name = senderName
                    },
                    to = new[]
                    {
                        new
                        {
                            email = message.RecipientEmail,
                            name = message.RecipientName
                        }
                    },
                    subject = message.Subject,
                    textContent = message.TextContent,
                    htmlContent = message.HtmlContent,
                    tags = new[] { "project-health-alert" }
                },
                options: JsonOptions)
        };
        request.Headers.TryAddWithoutValidation("api-key", apiKey);

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            _logger.LogError(
                "Brevo email delivery failed with status {StatusCode}",
                response.StatusCode);
            throw new ExternalServiceException(
                "Project health notification email could not be delivered.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (ExternalServiceException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Brevo email delivery request failed");
            throw new ExternalServiceException(
                "Project health notification email could not be delivered.",
                exception);
        }
    }

    private string GetRequiredSetting(string key)
    {
        var value = _configuration[key];
        if (string.IsNullOrWhiteSpace(value)
            || value.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase))
        {
            throw new ExternalServiceException(
                $"Email setting '{key}' is not configured.");
        }

        return value;
    }
}
