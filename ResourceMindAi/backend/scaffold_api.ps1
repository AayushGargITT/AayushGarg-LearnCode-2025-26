$ApiDir = "src\ResourceMindAI.API"

mkdir $ApiDir\Controllers -Force
mkdir $ApiDir\Middleware -Force
mkdir $ApiDir\Extensions -Force

# Controllers
$Controllers = "Auth", "User", "Employee", "Project", "Allocation", "Timesheet", "Ai", "SystemConfig"
foreach ($c in $Controllers) {
@"
using Microsoft.AspNetCore.Mvc;

namespace ResourceMindAI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class $($c)Controller : ControllerBase
{
}
"@ | Out-File "$ApiDir\Controllers\$($c)Controller.cs" -Encoding utf8
}

# Middleware
@"
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
"@ | Out-File $ApiDir\Middleware\ExceptionMiddleware.cs -Encoding utf8

@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ResourceMindAI.API.Middleware;
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    public RequestLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);
    }
}
"@ | Out-File $ApiDir\Middleware\RequestLoggingMiddleware.cs -Encoding utf8

# Extensions
@"
using Microsoft.Extensions.DependencyInjection;

namespace ResourceMindAI.API.Extensions;
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        return services;
    }
}
"@ | Out-File $ApiDir\Extensions\ServiceCollectionExtensions.cs -Encoding utf8
