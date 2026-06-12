namespace ResourceMindAI.Application.Abstractions.Services;

public interface IProjectHealthReportProcessor
{
    Task ProcessAsync(CancellationToken cancellationToken);
}
