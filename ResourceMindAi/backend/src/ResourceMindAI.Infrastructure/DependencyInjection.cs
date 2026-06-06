using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using ResourceMindAI.Application.Abstractions.Repositories;
using ResourceMindAI.Application.Abstractions.Services;
using ResourceMindAI.Application.Services;

using ResourceMindAI.Infrastructure.Persistence;
using ResourceMindAI.Infrastructure.Persistence.Repositories;

using ResourceMindAI.Infrastructure.ExternalServices.Jwt;
using ResourceMindAI.Infrastructure.ExternalServices.AI;

namespace ResourceMindAI.Infrastructure;
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection")));

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAdminEmployeeRepository, AdminEmployeeRepository>();
        services.AddScoped<IManagerRepository, ManagerRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IAllocationRepository, AllocationRepository>();
        services.AddScoped<ITimesheetRepository, TimesheetRepository>();
        services.AddScoped<ISystemConfigRepository, SystemConfigRepository>();

        // Services
        services.AddScoped<IJwtService, JwtService>();
        services.AddHttpClient<ILlmClient, GeminiClient>(client =>
        {
            client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddSingleton<LlmClientFactory>();

        return services;
    }
}
