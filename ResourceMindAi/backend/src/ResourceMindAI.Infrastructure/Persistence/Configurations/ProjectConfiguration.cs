using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResourceMindAI.Domain.Entities;

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
               .HasConversion<string>();

       builder.HasOne(p => p.Manager)
       .WithMany(u => u.ManagedProjects)
       .HasForeignKey(p => p.ManagerId)
       .OnDelete(DeleteBehavior.Restrict);
    }
}
