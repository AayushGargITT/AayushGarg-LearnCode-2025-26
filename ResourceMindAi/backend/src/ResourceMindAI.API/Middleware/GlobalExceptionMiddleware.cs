using System.Diagnostics;
using System.Net;
using System.Text.Json;
using ResourceMindAI.Application.Exceptions;
using ResourceMindAI.Domain.Exceptions;

namespace ResourceMindAI.API.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title, errorCode) = MapException(exception);

        if (statusCode >= 500)
        {
            _logger.LogError(
                exception,
                "Unhandled exception on {Method} {Path} — {ErrorCode}: {Message}",
                context.Request.Method,
                context.Request.Path,
                errorCode,
                exception.Message);
        }
        else
        {
            _logger.LogWarning(
                "Handled domain exception on {Method} {Path} — {ErrorCode}: {Message}",
                context.Request.Method,
                context.Request.Path,
                errorCode,
                exception.Message);
        }

        // Build the Problem Details response body.
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

        var responseBody = new
        {
            Status = statusCode,
            Message = exception.Message,
            Errors = (exception as ValidationException)?.Errors,
            Details = exception switch
            {
                ManagerDeactivationBlockedException managerException
                    => (object)managerException.Details,
                ProjectManagerUpdateBlockedException projectException
                    => (object)projectException.Details,
                _ => null
            },
            StackTrace = _env.IsDevelopment() ? exception.StackTrace : null,
            TraceId = Activity.Current?.Id ?? context.TraceIdentifier
        };

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        await context.Response.WriteAsync(JsonSerializer.Serialize(responseBody, JsonOptions));
    }

    private static (int StatusCode, string Title, string ErrorCode) MapException(Exception exception)
    {
        return exception switch
        {
            ValidationException ex
                => (StatusCodes.Status400BadRequest, "Bad Request", ex.ErrorCode),

            UnauthorizedAccessException
                => (StatusCodes.Status401Unauthorized, "Unauthorized", "UNAUTHORIZED"),

            ForbiddenException ex
                => (StatusCodes.Status403Forbidden, "Forbidden", ex.ErrorCode),

            EntityNotFoundException ex
                => (StatusCodes.Status404NotFound, "Not Found", ex.ErrorCode),

            ConflictException ex
                => (StatusCodes.Status409Conflict, "Conflict", ex.ErrorCode),

            ExternalServiceException
                => (StatusCodes.Status502BadGateway, "Bad Gateway", "EXTERNAL_SERVICE_ERROR"),

            DomainException ex
                => (StatusCodes.Status422UnprocessableEntity, "Unprocessable Entity", ex.ErrorCode),

            _
                => (StatusCodes.Status500InternalServerError, "Internal Server Error", "INTERNAL_ERROR")
        };
    }
}
