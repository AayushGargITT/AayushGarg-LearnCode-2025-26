using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using ResourceMindAI.Infrastructure.ExternalServices.AI;

namespace ResourceMindAI.Infrastructure.Tests.ExternalServices.AI;

public class GemmaClientTests
{
    [Fact]
    public async Task ExtractResourceIntentAsync_ShouldCallConfiguredGenerateEndpoint()
    {
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;
        var handler = new StubHttpMessageHandler(async request =>
        {
            capturedRequest = request;
            capturedBody = await request.Content!.ReadAsStringAsync();
            return JsonResponse(new
            {
                response =
                    """
                    {
                      "requiredSkills": ["c#", "backend"],
                      "experienceHint": null,
                      "availabilityRequirement": 50,
                      "fromDate": null,
                      "toDate": null,
                      "prioritySignals": [],
                      "softConstraints": [],
                      "exclusionConstraints": []
                    }
                    """
            });
        });
        var sut = CreateClient(handler);

        var result = await sut.ExtractResourceIntentAsync(
            "Need a C# backend developer with 50% availability");

        capturedRequest!.RequestUri.Should()
            .Be("http://164.52.211.238/api/generate");
        capturedRequest.Method.Should().Be(HttpMethod.Post);
        capturedRequest.Content!.Headers.ContentType!.MediaType.Should()
            .Be("application/x-www-form-urlencoded");
        capturedRequest.Headers.Accept.Select(header => header.MediaType)
            .Should()
            .ContainSingle("*/*");
        capturedRequest.Headers.UserAgent.ToString().Should().Be("curl/8.0");
        capturedRequest.Headers.TryGetValues("ApiKey", out var apiKeyValues)
            .Should()
            .BeTrue();
        apiKeyValues.Should().ContainSingle("test-gemma-key");
        capturedBody.Should().Contain("\"model\":\"gemma3:12b-it-q8_0\"");
        capturedBody.Should().Contain("\"stream\":false");
        capturedBody.Should().NotContain("\"format\"");
        capturedBody.Should().NotContain("\"options\"");
        result.RequiredSkills.Should().BeEquivalentTo("c#", "backend");
        result.AvailabilityRequirement.Should().Be(50);
    }

    [Fact]
    public async Task ExtractResourceIntentAsync_WhenEndpointReturnsDirectJson_ShouldParseResponse()
    {
        var handler = new StubHttpMessageHandler(_ => Task.FromResult(JsonResponse(new
        {
            requiredSkills = Array.Empty<string>(),
            experienceHint = (string?)null,
            availabilityRequirement = (int?)null,
            fromDate = (string?)null,
            toDate = (string?)null,
            prioritySignals = Array.Empty<string>(),
            softConstraints = Array.Empty<string>(),
            exclusionConstraints = Array.Empty<string>()
        })));
        var sut = CreateClient(handler);

        var result = await sut.ExtractResourceIntentAsync("Find an available resource");

        result.RequiredSkills.Should().BeEmpty();
    }

    private static GemmaClient CreateClient(HttpMessageHandler handler)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["LLMProvider:Providers:Gemma:Endpoint"] =
                    "http://164.52.211.238/api/generate",
                ["LLMProvider:Providers:Gemma:Model"] = "gemma3:12b-it-q8_0",
                ["LLMProvider:Providers:Gemma:ApiKey"] = " test-gemma-key ",
                ["LLMProvider:Providers:Gemma:ApiKeyHeader"] = "ApiKey",
                ["LLMProvider:Providers:Gemma:ContentType"] =
                    "application/x-www-form-urlencoded",
                ["LLMProvider:Providers:Gemma:Accept"] = "*/*",
                ["LLMProvider:Providers:Gemma:UserAgent"] = "curl/8.0"
            })
            .Build();

        return new GemmaClient(
            new HttpClient(handler),
            configuration,
            Mock.Of<ILogger<GemmaClient>>());
    }

    private static HttpResponseMessage JsonResponse(object body)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json")
        };
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
