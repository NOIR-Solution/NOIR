namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for Cart entity.
/// </summary>
public class CartConfiguration : IEntityTypeConfiguration<Cart>
{
    public void Configure(EntityTypeBuilder<Cart> builder)
    {
        builder.ToTable("Carts");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // User association
        builder.Property(e => e.UserId)
            .HasMaxLength(DatabaseConstants.UserIdMaxLength);

        builder.Property(e => e.SessionId)
            .HasMaxLength(128);

        // Status
        builder.Property(e => e.Status)
            .HasConversion<int>();

        // Timestamps
        builder.Property(e => e.LastActivityAt)
            .IsRequired();

        builder.Property(e => e.ExpiresAt);

        // Currency
        builder.Property(e => e.Currency)
            .HasMaxLength(10)
            .HasDefaultValue("VND");

        // Indexes for user and session lookups
        builder.HasIndex(e => new { e.TenantId, e.UserId })
            .HasFilter("[UserId] IS NOT NULL")
            .HasDatabaseName("IX_Carts_TenantId_UserId");

        builder.HasIndex(e => new { e.TenantId, e.SessionId })
            .HasFilter("[SessionId] IS NOT NULL")
            .HasDatabaseName("IX_Carts_TenantId_SessionId");

        // Index for finding active carts
        builder.HasIndex(e => new { e.TenantId, e.Status })
            .HasDatabaseName("IX_Carts_TenantId_Status");

        // Index for abandonment detection (active carts not updated recently)
        builder.HasIndex(e => new { e.TenantId, e.Status, e.LastActivityAt })
            .HasDatabaseName("IX_Carts_TenantId_Status_LastActivity");

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

        // Ignore computed properties
        builder.Ignore(e => e.ItemCount);
        builder.Ignore(e => e.Subtotal);
        builder.Ignore(e => e.IsEmpty);
        builder.Ignore(e => e.IsGuest);
    }
}
