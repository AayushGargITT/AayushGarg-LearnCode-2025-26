using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResourceMindAI.Domain.Entities;

namespace ResourceMindAI.Infrastructure.Persistence.Configurations;
public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Department)
               .IsRequired()
               .HasMaxLength(100);
        builder.Property(x => x.Designation)
               .IsRequired()
               .HasMaxLength(150);
        builder.Property(x => x.Status)
               .IsRequired()
               .HasConversion<string>();

        builder.HasOne(e => e.User)
               .WithOne(u => u.Employee)
               .HasForeignKey<Employee>(e => e.UserId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Manager)
               .WithMany()
               .HasForeignKey(e => e.ManagerId)
               .OnDelete(DeleteBehavior.NoAction)
               .IsRequired(false);
    }
}
