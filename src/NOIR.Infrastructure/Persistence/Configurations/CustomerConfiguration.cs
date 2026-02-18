namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for Customer entity.
/// </summary>
public class CustomerConfiguration : IEntityTypeConfiguration<Domain.Entities.Customer.Customer>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.Customer.Customer> builder)
    {
        builder.ToTable("Customers");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // UserId (FK to ApplicationUser, optional)
        builder.Property(e => e.UserId)
            .HasMaxLength(DatabaseConstants.UserIdMaxLength);

        builder.HasIndex(e => new { e.TenantId, e.UserId })
            .IsUnique()
            .HasFilter("[UserId] IS NOT NULL")
            .HasDatabaseName("IX_Customers_TenantId_UserId");

        // Email
        builder.Property(e => e.Email)
            .HasMaxLength(256)
            .IsRequired();

        builder.HasIndex(e => new { e.TenantId, e.Email })
            .HasDatabaseName("IX_Customers_TenantId_Email");

        // Name
        builder.Property(e => e.FirstName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.LastName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Phone)
            .HasMaxLength(20);

        // Segment & Tier (stored as strings via global convention)
        builder.Property(e => e.Segment)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.HasIndex(e => new { e.TenantId, e.Segment })
            .HasDatabaseName("IX_Customers_TenantId_Segment");

        builder.Property(e => e.Tier)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.HasIndex(e => new { e.TenantId, e.Tier })
            .HasDatabaseName("IX_Customers_TenantId_Tier");

        // RFM metrics
        builder.Property(e => e.TotalOrders)
            .HasDefaultValue(0);

        builder.Property(e => e.TotalSpent)
            .HasPrecision(18, 2)
            .HasDefaultValue(0m);

        builder.Property(e => e.AverageOrderValue)
            .HasPrecision(18, 2)
            .HasDefaultValue(0m);

        // Loyalty
        builder.Property(e => e.LoyaltyPoints)
            .HasDefaultValue(0);

        builder.Property(e => e.LifetimeLoyaltyPoints)
            .HasDefaultValue(0);

        // Tags & Notes
        builder.Property(e => e.Tags)
            .HasMaxLength(1000);

        builder.Property(e => e.Notes)
            .HasColumnType("nvarchar(max)");

        // IsActive
        builder.Property(e => e.IsActive)
            .HasDefaultValue(true);

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

        // Navigation to addresses
        builder.HasMany(e => e.Addresses)
            .WithOne(a => a.Customer)
            .HasForeignKey(a => a.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
