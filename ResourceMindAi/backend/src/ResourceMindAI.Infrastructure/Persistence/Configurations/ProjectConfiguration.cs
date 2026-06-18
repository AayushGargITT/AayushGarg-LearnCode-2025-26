using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ResourceMindAI.Domain.Entities;
using ResourceMindAI.Domain.Enums;

namespace ResourceMindAI.Infrastructure.Persistence.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
               .IsRequired()
               .HasMaxLength(200);

        builder.Property(x => x.Description)
               .HasMaxLength(2000);

        builder.Property(x => x.StartDate)
               .IsRequired();

        builder.Property(x => x.ManagerId)
               .IsRequired();

        builder.Property(x => x.Status)
               .IsRequired()
               .HasConversion<string>();

        builder.Property(x => x.HealthStatus)
               .IsRequired()
               .HasConversion(new ValueConverter<HealthStatus, string>(
                   value => ToDatabaseValue(value),
                   value => FromDatabaseValue(value)));

       builder.HasOne(p => p.Manager)
       .WithMany(u => u.ManagedProjects)
       .HasForeignKey(p => p.ManagerId)
       .OnDelete(DeleteBehavior.Restrict);
    }

    private static string ToDatabaseValue(HealthStatus value)
    {
        return value switch
        {
            HealthStatus.Healthy => "Healthy",
            HealthStatus.AtRisk => "At Risk",
            HealthStatus.Critical => "Critical",
            _ => "At Risk"
        };
    }

    private static HealthStatus FromDatabaseValue(string value)
    {
        return value switch
        {
            "Healthy" => HealthStatus.Healthy,
            "At Risk" => HealthStatus.AtRisk,
            "AtRisk" => HealthStatus.AtRisk,
            "Critical" => HealthStatus.Critical,
            "Green" => HealthStatus.Healthy,
            "Amber" => HealthStatus.AtRisk,
            "Red" => HealthStatus.Critical,
            _ => HealthStatus.AtRisk
        };
    }
}
