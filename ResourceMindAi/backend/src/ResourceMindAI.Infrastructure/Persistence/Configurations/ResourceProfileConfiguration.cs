using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResourceMindAI.Domain.Entities;

namespace ResourceMindAI.Infrastructure.Persistence.Configurations;

public class ResourceProfileConfiguration : IEntityTypeConfiguration<ResourceProfile>
{
    public void Configure(EntityTypeBuilder<ResourceProfile> builder)
    {
        builder.HasKey(profile => profile.Id);

        builder.Property(profile => profile.Id)
            .ValueGeneratedNever();

        builder.Property(profile => profile.Department)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(profile => profile.Designation)
            .IsRequired()
            .HasMaxLength(150);

        builder.HasOne(profile => profile.User)
            .WithOne(user => user.ResourceProfile)
            .HasForeignKey<ResourceProfile>(profile => profile.Id)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(profile => profile.Manager)
            .WithMany()
            .HasForeignKey(profile => profile.ManagerId)
            .OnDelete(DeleteBehavior.NoAction)
            .IsRequired(false);
    }
}
