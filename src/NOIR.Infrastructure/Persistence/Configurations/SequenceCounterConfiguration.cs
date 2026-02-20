using NOIR.Domain.Entities.Common;

namespace NOIR.Infrastructure.Persistence.Configurations;

public class SequenceCounterConfiguration : IEntityTypeConfiguration<SequenceCounter>
{
    public void Configure(EntityTypeBuilder<SequenceCounter> builder)
    {
        builder.ToTable("SequenceCounters");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Prefix)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.TenantId)
            .HasMaxLength(64);

        builder.HasIndex(e => new { e.TenantId, e.Prefix })
            .IsUnique()
            .HasDatabaseName("IX_SequenceCounters_TenantId_Prefix");
    }
}
