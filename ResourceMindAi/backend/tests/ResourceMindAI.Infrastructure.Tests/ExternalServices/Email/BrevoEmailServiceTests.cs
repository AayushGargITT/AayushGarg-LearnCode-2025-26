using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using ResourceMindAI.Application.DTOs.Notifications;
using ResourceMindAI.Infrastructure.ExternalServices.Email;

namespace ResourceMindAI.Infrastructure.Tests.ExternalServices.Email;

public class BrevoEmailServiceTests
{
    [Fact]
    public async Task SendAsync_ShouldUseBrevoTransactionalEmailContract()
    {
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;
        var handler = new StubHttpMessageHandler(async request =>
        {
            capturedRequest = request;
            capturedBody = await request.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent("""{"messageId":"message-1"}""")
            };
        });
        var sut = CreateService(handler);

        await sut.SendAsync(new EmailMessageDto
        {
            RecipientEmail = "manager@example.com",
            RecipientName = "Manager One",
            Subject = "Project Health Alert - Apollo",
            TextContent = "Plain text",
            HtmlContent = "<p>Plain text</p>"
        });

        capturedRequest!.RequestUri.Should()
            .Be("https://api.brevo.com/v3/smtp/email");
        capturedRequest.Headers.GetValues("api-key").Should().ContainSingle("brevo-key");
        using var payload = JsonDocument.Parse(capturedBody!);
        payload.RootElement.GetProperty("sender").GetProperty("email").GetString()
            .Should().Be("alerts@example.com");
        payload.RootElement.GetProperty("to")[0].GetProperty("email").GetString()
            .Should().Be("manager@example.com");
        payload.RootElement.GetProperty("textContent").GetString()
            .Should().Be("Plain text");
        payload.RootElement.GetProperty("htmlContent").GetString()
            .Should().Be("<p>Plain text</p>");
    }

    private static BrevoEmailService CreateService(HttpMessageHandler handler)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Notifications:Brevo:ApiKey"] = "brevo-key",
                ["Notifications:Brevo:SenderEmail"] = "alerts@example.com",
                ["Notifications:Brevo:SenderName"] = "ResourceMindAI"
            })
            .Build();
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.brevo.com/")
        };

        return new BrevoEmailService(
            client,
            configuration,
            Mock.Of<ILogger<BrevoEmailService>>());
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;

        public StubHttpMessageHandler(
            Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return _handler(request);
        }
    }
}
