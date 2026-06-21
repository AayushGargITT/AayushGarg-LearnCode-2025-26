namespace ResourceMindAI.Domain.Exceptions;

/// <summary>
/// Thrown when input data fails business validation rules.
/// Maps to HTTP 400 Bad Request.
/// Supports both a general <see cref="Exception.Message"/> and
/// a per-field <see cref="Errors"/> dictionary for structured validation output.
/// </summary>
public class ValidationException : DomainException
{
    /// <summary>
    /// Per-field validation errors. Key = field name, Value = array of error messages for that field.
    /// Mirrors the shape used by FluentValidation / ASP.NET ModelState for forward-compatibility.
    /// </summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    /// <summary>Create with a single general-purpose message (no field-level detail).</summary>
    public ValidationException(string message, string errorCode = "VALIDATION_ERROR")
        : base(message, errorCode)
    {
        Errors = new Dictionary<string, string[]>();
    }

    /// <summary>Create with per-field validation errors.</summary>
    public ValidationException(
        string message,
        IDictionary<string, string[]> errors,
        string errorCode = "VALIDATION_ERROR")
        : base(message, errorCode)
    {
        Errors = new Dictionary<string, string[]>(errors);
    }
}
