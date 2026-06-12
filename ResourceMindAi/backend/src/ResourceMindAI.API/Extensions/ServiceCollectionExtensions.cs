using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ResourceMindAI.Application.Abstractions.Repositories;
using ResourceMindAI.Application.Abstractions.Services;
using ResourceMindAI.Application.Services;

namespace ResourceMindAI.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAdminEmployeeService, AdminEmployeeService>();
        services.AddScoped<IManagerService, ManagerService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IAllocationService, AllocationService>();
        services.AddScoped<ITimesheetService, TimesheetService>();
        services.AddScoped<ISchedulerComputationService, SchedulerComputationService>();
        services.AddScoped<IProjectHealthReportProcessor, ProjectHealthReportProcessor>();
        services.AddScoped<IProjectHealthEmailProcessor, ProjectHealthEmailProcessor>();

        return services;
    }

    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection("Jwt");
        var secret = jwtSection["Secret"];

        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException("JWT secret is not configured.");
        }

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSection["Issuer"],
                    ValidAudience = jwtSection["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                    ClockSkew = TimeSpan.Zero,
                    NameClaimType = ClaimTypes.NameIdentifier,
                    RoleClaimType = ClaimTypes.Role
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var userIdValue = context.Principal?
                            .FindFirstValue(ClaimTypes.NameIdentifier);

                        if (!Guid.TryParse(userIdValue, out var userId))
                        {
                            context.Fail("Authenticated user id is invalid.");
                            return;
                        }

                        var userRepository = context.HttpContext.RequestServices
                            .GetRequiredService<IUserRepository>();

                        if (!await userRepository.IsActiveAsync(userId))
                        {
                            context.Fail("User account is inactive.");
                        }
                    }
                };
            });

        services.AddAuthorization();

        return services;
    }
}
