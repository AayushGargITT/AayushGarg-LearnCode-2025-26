using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using ResourceMindAI.Application.Abstractions.Repositories;
using ResourceMindAI.Application.Abstractions.Services;

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
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IAllocationRepository, AllocationRepository>();
        services.AddScoped<ITimesheetRepository, TimesheetRepository>();
        services.AddScoped<ISystemConfigRepository, SystemConfigRepository>();

        // Services
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<ILlmClient, GeminiClient>();
        services.AddSingleton<LlmClientFactory>();

        return services;
    }
}
