using ResourceMindAI.Application.DTOs.Manager;

namespace ResourceMindAI.Application.Abstractions.Services;

public interface ILlmClient
{
    Task<ResourceIntentDto> ExtractResourceIntentAsync(
        string requirement,
        CancellationToken cancellationToken = default);
}
