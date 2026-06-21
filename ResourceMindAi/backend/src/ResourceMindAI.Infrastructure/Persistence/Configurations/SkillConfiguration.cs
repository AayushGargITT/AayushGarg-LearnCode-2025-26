using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResourceMindAI.Domain.Entities;

namespace ResourceMindAI.Infrastructure.Persistence.Configurations;

public class SkillConfiguration : IEntityTypeConfiguration<Skill>
{
    public void Configure(EntityTypeBuilder<Skill> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SkillName)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(x => x.Category)
               .IsRequired()
               .HasConversion<string>();

        builder.Property(x => x.Proficiency)
               .IsRequired()
               .HasConversion<string>();

        builder.HasOne(skill => skill.ResourceProfile)
            .WithMany(profile => profile.Skills)
            .HasForeignKey(skill => skill.ResourceProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.ResourceProfileId, x.SkillName }).IsUnique();
    }
}
