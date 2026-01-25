namespace NOIR.Infrastructure.Persistence.Configurations;

public class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        builder.ToTable("SubscriptionPlans");

        // Primary key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // Name
        builder.Property(e => e.Name)
            .HasMaxLength(100)
            .IsRequired();

        // Unique name per tenant
        builder.HasIndex(e => new { e.Name, e.TenantId })
            .IsUnique()
            .HasDatabaseName("IX_SubscriptionPlans_Name_TenantId");

        // Description
        builder.Property(e => e.Description)
            .HasMaxLength(500);

        // Price
        builder.Property(e => e.Price)
            .HasPrecision(18, 2)
            .IsRequired();

        // Currency
        builder.Property(e => e.Currency)
            .HasMaxLength(3)
            .IsRequired();

        // Billing interval
        builder.Property(e => e.Interval)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // Trial days
        builder.Property(e => e.TrialDays);

        // Features JSON
        builder.Property(e => e.FeaturesJson)
            .HasColumnType("nvarchar(max)");

        // External plan ID (e.g., Stripe price ID)
        builder.Property(e => e.ExternalPlanId)
            .HasMaxLength(200);

        // IsActive
        builder.Property(e => e.IsActive)
            .HasDefaultValue(true);

        // SortOrder
        builder.Property(e => e.SortOrder);

        // Active plans index
        builder.HasIndex(e => new { e.TenantId, e.IsActive, e.SortOrder })
            .HasDatabaseName("IX_SubscriptionPlans_Tenant_Active_Sort");

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
