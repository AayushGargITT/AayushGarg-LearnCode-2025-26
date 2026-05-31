using System;
namespace ResourceMindAI.Domain.Exceptions;
public class AllocationOverlapException : Exception
{
    public AllocationOverlapException(string message) : base(message) { }
}
