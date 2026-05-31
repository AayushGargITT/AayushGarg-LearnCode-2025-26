using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ResourceMindAI.API.Middleware;
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    public ExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception)
        {
            throw; // Real implementation would handle this
        }
    }
}
