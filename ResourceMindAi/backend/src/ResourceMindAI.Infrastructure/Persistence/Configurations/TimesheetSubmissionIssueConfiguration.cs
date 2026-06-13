using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResourceMindAI.Domain.Entities;

namespace ResourceMindAI.Infrastructure.Persistence.Configurations;

public sealed class TimesheetSubmissionIssueConfiguration
    : IEntityTypeConfiguration<TimesheetSubmissionIssue>
{
    public void Configure(EntityTypeBuilder<TimesheetSubmissionIssue> builder)
    {
        builder.HasKey(issue => issue.Id);

        builder.Property(issue => issue.WeekStartDate)
            .IsRequired();

        builder.Property(issue => issue.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(issue => issue.CreatedAtUtc)
            .IsRequired();

        builder.HasOne(issue => issue.EmployeeUser)
            .WithMany()
            .HasForeignKey(issue => issue.EmployeeUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(issue => issue.ManagerUser)
            .WithMany()
            .HasForeignKey(issue => issue.ManagerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(issue => issue.RestoredByManagerUser)
            .WithMany()
            .HasForeignKey(issue => issue.RestoredByManagerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(issue => new
        {
            issue.EmployeeUserId,
            issue.WeekStartDate
        }).IsUnique();
    }
}
