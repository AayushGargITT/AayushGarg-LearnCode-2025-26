using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResourceMindAI.Domain.Entities;

namespace ResourceMindAI.Infrastructure.Persistence.Configurations;

public class AllocationConfiguration : IEntityTypeConfiguration<Allocation>
{
    public void Configure(EntityTypeBuilder<Allocation> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.UtilisationPercent)
               .IsRequired();

        builder.Property(x => x.FromDate)
               .IsRequired();

        builder.Property(x => x.ToDate)
               .IsRequired();

        builder.ToTable(t =>
            t.HasCheckConstraint(
                "CK_Allocation_UtilisationPercent",
                "\"UtilisationPercent\" BETWEEN 0 AND 100"));
    }
}
