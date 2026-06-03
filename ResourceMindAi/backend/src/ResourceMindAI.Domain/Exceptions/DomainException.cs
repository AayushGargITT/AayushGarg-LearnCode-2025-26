namespace ResourceMindAI.Domain.Exceptions;

public class DomainException : Exception
{
    /// <summary>Machine-readable error code (e.g. "INACTIVE_ACCOUNT", "DUPLICATE_USER").</summary>
    public string ErrorCode { get; }

    public DomainException(string message, string errorCode = "DOMAIN_ERROR")
        : base(message)
    {
        ErrorCode = errorCode;
    }
}
