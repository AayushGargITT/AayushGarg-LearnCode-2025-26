using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResourceMindAI.Domain.Entities;

namespace ResourceMindAI.Infrastructure.Persistence.Configurations;

public class SystemConfigConfiguration : IEntityTypeConfiguration<SystemConfig>
{
    public void Configure(EntityTypeBuilder<SystemConfig> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.LlmProvider)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(x => x.LlmApiKey)
               .HasMaxLength(500);

        builder.Property(x => x.SchedulerIntervalHours)
               .IsRequired();

        builder.Property(x => x.MaxWeeklyHours)
               .IsRequired();
    }
}
