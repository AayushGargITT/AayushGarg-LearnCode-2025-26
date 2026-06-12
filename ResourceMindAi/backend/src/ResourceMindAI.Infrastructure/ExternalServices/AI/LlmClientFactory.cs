using Microsoft.Extensions.Configuration;
using ResourceMindAI.Domain.Exceptions;

namespace ResourceMindAI.Infrastructure.ExternalServices.AI;

public sealed class LlmClientFactory : ILlmClientFactory
{
    private readonly IReadOnlyDictionary<string, ILlmProviderClient> _clients;
    private readonly IConfiguration _configuration;

    public LlmClientFactory(
        IEnumerable<ILlmProviderClient> clients,
        IConfiguration configuration)
    {
        _clients = clients.ToDictionary(
            client => client.ProviderName,
            StringComparer.OrdinalIgnoreCase);
        _configuration = configuration;
    }

    public ILlmProviderClient CreateClient()
    {
        var providerName = _configuration["LLMProvider:ActiveProvider"];
        if (string.IsNullOrWhiteSpace(providerName))
        {
            throw new ExternalServiceException("Active LLM provider is not configured.");
        }

        if (_clients.TryGetValue(providerName.Trim(), out var client))
        {
            return client;
        }

        throw new ExternalServiceException(
            $"LLM provider '{providerName}' is not supported.");
    }
}
