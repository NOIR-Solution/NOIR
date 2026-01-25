namespace NOIR.Infrastructure.Persistence.Configurations;

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("Subscriptions");

        // Primary key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // Customer ID
        builder.Property(e => e.CustomerId)
            .IsRequired();

        builder.HasIndex(e => new { e.CustomerId, e.TenantId })
            .HasDatabaseName("IX_Subscriptions_Customer_Tenant");

        // Plan relationship
        builder.HasOne(e => e.Plan)
            .WithMany(p => p.Subscriptions)
            .HasForeignKey(e => e.PlanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.PlanId)
            .HasDatabaseName("IX_Subscriptions_PlanId");

        // Status
        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // Period dates
        builder.Property(e => e.CurrentPeriodStart)
            .IsRequired();

        builder.Property(e => e.CurrentPeriodEnd)
            .IsRequired();

        builder.Property(e => e.CancelledAt);
        builder.Property(e => e.TrialEnd);

        // Billing interval
        builder.Property(e => e.Interval)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // Amount
        builder.Property(e => e.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        // Currency
        builder.Property(e => e.Currency)
            .HasMaxLength(3)
            .IsRequired();

        // External subscription ID (e.g., Stripe subscription ID)
        builder.Property(e => e.ExternalSubscriptionId)
            .HasMaxLength(200);

        builder.HasIndex(e => e.ExternalSubscriptionId)
            .HasDatabaseName("IX_Subscriptions_ExternalId");

        // Cancel at period end
        builder.Property(e => e.CancelAtPeriodEnd)
            .HasDefaultValue(false);

        // Metadata JSON
        builder.Property(e => e.MetadataJson)
            .HasColumnType("nvarchar(max)");

        // Status indexes for common queries
        builder.HasIndex(e => new { e.TenantId, e.Status })
            .HasDatabaseName("IX_Subscriptions_Tenant_Status");

        builder.HasIndex(e => new { e.Status, e.CurrentPeriodEnd })
            .HasDatabaseName("IX_Subscriptions_Status_PeriodEnd");

        // Tenant ID
        builder.Property(e => e.TenantId).HasMaxLength(DatabaseConstants.TenantIdMaxLength);
        builder.HasIndex(e => e.TenantId);

        // Audit fields
        builder.Property(e => e.CreatedBy).HasMaxLength(DatabaseConstants.UserIdMaxLength);
        builder.Property(e => e.ModifiedBy).HasMaxLength(DatabaseConstants.UserIdMaxLength);
        builder.Property(e => e.DeletedBy).HasMaxLength(DatabaseConstants.UserIdMaxLength);
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);

        // Soft delete query filter
        builder.HasQueryFilter("SoftDelete", e => !e.IsDeleted);
    }
}
