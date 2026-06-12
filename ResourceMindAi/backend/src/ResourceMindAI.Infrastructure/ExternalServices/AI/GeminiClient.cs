using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ResourceMindAI.Infrastructure.ExternalServices.AI;

public sealed class GeminiClient : GoogleGenerativeLanguageClient
{
    public GeminiClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GeminiClient> logger)
        : base(
            httpClient,
            configuration,
            logger,
            "gemini-2.5-flash")
    {
    }

    public override string ProviderName => LlmProviderNames.Gemini;
}
