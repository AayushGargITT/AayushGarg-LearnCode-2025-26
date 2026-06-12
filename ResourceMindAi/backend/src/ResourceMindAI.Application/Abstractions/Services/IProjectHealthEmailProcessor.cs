namespace ResourceMindAI.Application.Abstractions.Services;

public interface IProjectHealthEmailProcessor
{
    Task ProcessAsync(CancellationToken cancellationToken);
}
