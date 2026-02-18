namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for Promotion entity.
/// </summary>
public class PromotionConfiguration : IEntityTypeConfiguration<Domain.Entities.Promotion.Promotion>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.Promotion.Promotion> builder)
    {
        builder.ToTable("Promotions");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // Name
        builder.Property(e => e.Name)
            .HasMaxLength(200)
            .IsRequired();

        // Description
        builder.Property(e => e.Description)
            .HasMaxLength(1000);

        // Code (unique per tenant)
        builder.Property(e => e.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(e => new { e.Code, e.TenantId })
            .IsUnique()
            .HasDatabaseName("IX_Promotions_Code_TenantId");

        // Enums as strings
        builder.Property(e => e.PromotionType)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(e => e.DiscountType)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(e => e.ApplyLevel)
            .HasConversion<string>()
            .HasMaxLength(30);

        // Financial
        builder.Property(e => e.DiscountValue)
            .HasPrecision(18, 2);

        builder.Property(e => e.MaxDiscountAmount)
            .HasPrecision(18, 2);

        builder.Property(e => e.MinOrderValue)
            .HasPrecision(18, 2);

        // Indexes for querying
        builder.HasIndex(e => new { e.TenantId, e.Status, e.StartDate, e.EndDate })
            .HasDatabaseName("IX_Promotions_TenantId_Status_Dates");

        builder.HasIndex(e => new { e.TenantId, e.IsActive })
            .HasDatabaseName("IX_Promotions_TenantId_IsActive");

        builder.HasIndex(e => new { e.TenantId, e.PromotionType })
            .HasDatabaseName("IX_Promotions_TenantId_PromotionType");

        // Tenant
        builder.Property(e => e.TenantId)
            .HasMaxLength(DatabaseConstants.TenantIdMaxLength);
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
