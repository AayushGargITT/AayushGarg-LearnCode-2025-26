namespace ResourceMindAI.Infrastructure.ExternalServices.AI;

public interface ILlmClientFactory
{
    ILlmProviderClient CreateClient();
}
