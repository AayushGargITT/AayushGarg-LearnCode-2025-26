$InfraDir = "src\ResourceMindAI.Infrastructure"

mkdir $InfraDir\Persistence\Configurations -Force
mkdir $InfraDir\Persistence\Repositories -Force
mkdir $InfraDir\Persistence\Seed -Force
mkdir $InfraDir\Persistence\Migrations -Force
mkdir $InfraDir\ExternalServices\Jwt -Force
mkdir $InfraDir\ExternalServices\AI -Force
mkdir $InfraDir\BackgroundServices -Force

# AppDbContext
@"
using Microsoft.EntityFrameworkCore;
using ResourceMindAI.Domain.Entities;

namespace ResourceMindAI.Infrastructure.Persistence;
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Employee> Employees { get; set; }
    public DbSet<Skill> Skills { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<Milestone> Milestones { get; set; }
    public DbSet<Allocation> Allocations { get; set; }
    public DbSet<Timesheet> Timesheets { get; set; }
    public DbSet<SystemConfig> SystemConfigs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
"@ | Out-File $InfraDir\Persistence\AppDbContext.cs -Encoding utf8

# EntityConfigurations
$Entities = "User", "Employee", "Skill", "Project", "Milestone", "Allocation", "Timesheet", "SystemConfig"
foreach ($e in $Entities) {
@"
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResourceMindAI.Domain.Entities;

namespace ResourceMindAI.Infrastructure.Persistence.Configurations;
public class $($e)Configuration : IEntityTypeConfiguration<$e>
{
    public void Configure(EntityTypeBuilder<$e> builder)
    {
        builder.HasKey(x => x.Id);
    }
}
"@ | Out-File "$InfraDir\Persistence\Configurations\$($e)Configuration.cs" -Encoding utf8
}

# Repositories
$Repos = "User", "Employee", "Project", "Allocation", "Timesheet", "SystemConfig"
foreach ($r in $Repos) {
@"
using System;
using System.Threading.Tasks;
using ResourceMindAI.Application.Abstractions.Repositories;
using ResourceMindAI.Domain.Entities;

namespace ResourceMindAI.Infrastructure.Persistence.Repositories;
public class $($r)Repository : I$($r)Repository
{
    public Task GetByIdAsync(Guid id)
    {
        throw new NotImplementedException();
    }
}
"@ | Out-File "$InfraDir\Persistence\Repositories\$($r)Repository.cs" -Encoding utf8
}

# External Services
@"
using System;
using ResourceMindAI.Application.Abstractions.Services;

namespace ResourceMindAI.Infrastructure.ExternalServices.Jwt;
public class JwtService : IJwtService
{
    public string GenerateToken()
    {
        throw new NotImplementedException();
    }
}
"@ | Out-File $InfraDir\ExternalServices\Jwt\JwtService.cs -Encoding utf8

@"
using System;
using ResourceMindAI.Application.Abstractions.Services;

namespace ResourceMindAI.Infrastructure.ExternalServices.AI;
public class GeminiClient : ILlmClient
{
    public void Execute()
    {
        throw new NotImplementedException();
    }
}
"@ | Out-File $InfraDir\ExternalServices\AI\GeminiClient.cs -Encoding utf8

@"
using System;

namespace ResourceMindAI.Infrastructure.ExternalServices.AI;
public class LlmClientFactory
{
    public void Create()
    {
        throw new NotImplementedException();
    }
}
"@ | Out-File $InfraDir\ExternalServices\AI\LlmClientFactory.cs -Encoding utf8

@"
using System;

namespace ResourceMindAI.Infrastructure.ExternalServices.AI;
public class PromptBuilder
{
    public void Build()
    {
        throw new NotImplementedException();
    }
}
"@ | Out-File $InfraDir\ExternalServices\AI\PromptBuilder.cs -Encoding utf8

# BackgroundServices
@"
using System;

namespace ResourceMindAI.Infrastructure.BackgroundServices;
public class ResourceSchedulerService
{
    public void Run()
    {
        throw new NotImplementedException();
    }
}
"@ | Out-File $InfraDir\BackgroundServices\ResourceSchedulerService.cs -Encoding utf8

# DependencyInjection
@"
using Microsoft.Extensions.DependencyInjection;
using ResourceMindAI.Application.Abstractions.Repositories;
using ResourceMindAI.Application.Abstractions.Services;
using ResourceMindAI.Infrastructure.Persistence.Repositories;
using ResourceMindAI.Infrastructure.ExternalServices.Jwt;
using ResourceMindAI.Infrastructure.ExternalServices.AI;

namespace ResourceMindAI.Infrastructure;
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
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
"@ | Out-File $InfraDir\DependencyInjection.cs -Encoding utf8
