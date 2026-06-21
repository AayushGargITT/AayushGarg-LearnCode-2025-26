namespace ResourceMindAI.Domain.Exceptions;

public class AllocationOverlapException : DomainException
{
    public AllocationOverlapException(string message)
        : base(message, "ALLOCATION_OVERLAP") { }
}

