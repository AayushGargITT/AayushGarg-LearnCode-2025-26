namespace ResourceMindAI.Domain.Exceptions;

public class EntityNotFoundException : DomainException
{
    public EntityNotFoundException(string entityName, object id)
        : base($"{entityName} with id '{id}' was not found.", "ENTITY_NOT_FOUND") { }

    public EntityNotFoundException(string message)
        : base(message, "ENTITY_NOT_FOUND") { }
}

