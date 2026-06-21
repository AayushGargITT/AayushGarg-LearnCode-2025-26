namespace ResourceMindAI.Domain.Exceptions;

public class ForbiddenException : DomainException
{
    public ForbiddenException(string message, string errorCode = "FORBIDDEN")
        : base(message, errorCode) { }
}
