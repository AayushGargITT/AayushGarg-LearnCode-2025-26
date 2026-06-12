using ResourceMindAI.Application.Abstractions.Services;

namespace ResourceMindAI.Infrastructure.ExternalServices.AI;

public interface ILlmProviderClient : ILlmClient
{
    string ProviderName { get; }
}
