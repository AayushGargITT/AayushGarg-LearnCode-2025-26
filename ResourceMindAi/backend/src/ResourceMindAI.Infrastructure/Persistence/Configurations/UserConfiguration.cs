using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResourceMindAI.Domain.Entities;

namespace ResourceMindAI.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FullName)
               .IsRequired()
               .HasMaxLength(200);

        builder.Property(x => x.Username)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(x => x.Email)
               .IsRequired()
               .HasMaxLength(255);

        builder.Property(x => x.PasswordHash)
               .IsRequired();

        builder.Property(x => x.Department)
               .HasMaxLength(100);

        builder.Property(x => x.Designation)
               .HasMaxLength(150);

        builder.Property(x => x.Role)
               .IsRequired()
               .HasConversion<string>();

        builder.HasIndex(x => x.Username).IsUnique();
        builder.HasIndex(x => x.Email).IsUnique();
    }
}
