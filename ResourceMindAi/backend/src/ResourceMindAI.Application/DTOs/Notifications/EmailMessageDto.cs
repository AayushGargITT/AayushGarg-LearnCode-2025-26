namespace ResourceMindAI.Application.DTOs.Notifications;

public sealed class EmailMessageDto
{
    public string RecipientEmail { get; init; } = null!;
    public string RecipientName { get; init; } = null!;
    public string Subject { get; init; } = null!;
    public string TextContent { get; init; } = null!;
    public string HtmlContent { get; init; } = null!;
}
