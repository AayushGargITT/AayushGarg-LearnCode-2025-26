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
using ResourceMindAI.Infrastructure.ExternalServices.Email;
using ResourceMindAI.Infrastructure.BackgroundServices;

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
        services.AddScoped<ISchedulerRepository, SchedulerRepository>();
        services.AddScoped<INotificationLogRepository, NotificationLogRepository>();

        // Services
        services.AddScoped<IJwtService, JwtService>();
        services.AddHttpClient<GeminiClient>(ConfigureGoogleAiClient);
        services.AddHttpClient<GemmaClient>(client =>
            client.Timeout = TimeSpan.FromSeconds(60));
        services.AddTransient<ILlmProviderClient>(
            provider => provider.GetRequiredService<GeminiClient>());
        services.AddTransient<ILlmProviderClient>(
            provider => provider.GetRequiredService<GemmaClient>());
        services.AddScoped<ILlmClientFactory, LlmClientFactory>();
        services.AddScoped<ILlmClient, ConfiguredLlmClient>();
        services.AddHttpClient<IEmailService, BrevoEmailService>(client =>
        {
            client.BaseAddress = new Uri("https://api.brevo.com/");
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddScoped<IProjectHealthNotificationService, ProjectHealthNotificationService>();
        services.AddHostedService<ResourceSchedulerService>();
        services.AddHostedService<ProjectHealthSchedulerService>();
        services.AddHostedService<ProjectHealthEmailSchedulerService>();
        return services;
    }

    private static void ConfigureGoogleAiClient(HttpClient client)
    {
        client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
        client.Timeout = TimeSpan.FromSeconds(30);
    }
}
