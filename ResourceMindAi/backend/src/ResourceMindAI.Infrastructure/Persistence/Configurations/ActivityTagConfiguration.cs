using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResourceMindAI.Domain.Entities;

namespace ResourceMindAI.Infrastructure.Persistence.Configurations;

public class ActivityTagConfiguration : IEntityTypeConfiguration<ActivityTag>
{
    public void Configure(EntityTypeBuilder<ActivityTag> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TagName)
               .IsRequired()
               .HasMaxLength(100);

        builder.HasIndex(x => new { x.TimesheetId, x.TagName }).IsUnique();
    }
}
