namespace ResourceMindAI.Domain.Exceptions;

/// <summary>
/// Thrown when an operation would create a duplicate or conflicting resource.
/// Maps to HTTP 409 Conflict.
/// </summary>
public class ConflictException : DomainException
{
    public ConflictException(string message, string errorCode = "CONFLICT")
        : base(message, errorCode) { }
}
