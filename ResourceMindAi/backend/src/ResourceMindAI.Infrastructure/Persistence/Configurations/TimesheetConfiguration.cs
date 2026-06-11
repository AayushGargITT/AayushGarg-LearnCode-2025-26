using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResourceMindAI.Domain.Entities;

namespace ResourceMindAI.Infrastructure.Persistence.Configurations;

public class TimesheetConfiguration : IEntityTypeConfiguration<Timesheet>
{
    public void Configure(EntityTypeBuilder<Timesheet> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.WeekStartDate)
               .IsRequired();

        builder.Property(x => x.HoursLogged)
               .IsRequired();

        builder.Property(x => x.Status)
               .IsRequired()
               .HasConversion<string>();

        builder.HasOne(timesheet => timesheet.ResourceProfile)
            .WithMany(profile => profile.Timesheets)
            .HasForeignKey(timesheet => timesheet.ResourceProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ResourceProfileId, x.ProjectId, x.WeekStartDate }).IsUnique();
    }
}
