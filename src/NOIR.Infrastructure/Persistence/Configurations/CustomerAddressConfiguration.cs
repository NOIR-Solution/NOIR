namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for CustomerAddress entity.
/// </summary>
public class CustomerAddressConfiguration : IEntityTypeConfiguration<Domain.Entities.Customer.CustomerAddress>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.Customer.CustomerAddress> builder)
    {
        builder.ToTable("CustomerAddresses");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // CustomerId FK
        builder.Property(e => e.CustomerId)
            .IsRequired();

        builder.HasIndex(e => new { e.TenantId, e.CustomerId })
            .HasDatabaseName("IX_CustomerAddresses_TenantId_CustomerId");

        // AddressType
        builder.Property(e => e.AddressType)
            .HasConversion<string>()
            .HasMaxLength(20);

        // Address fields
        builder.Property(e => e.FullName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Phone)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.AddressLine1)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.AddressLine2)
            .HasMaxLength(200);

        builder.Property(e => e.Ward)
            .HasMaxLength(100);

        builder.Property(e => e.District)
            .HasMaxLength(100);

        builder.Property(e => e.Province)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.PostalCode)
            .HasMaxLength(20);

        builder.Property(e => e.IsDefault)
            .HasDefaultValue(false);

        // Tenant
        builder.Property(e => e.TenantId)
            .HasMaxLength(DatabaseConstants.TenantIdMaxLength);
        builder.HasIndex(e => e.TenantId);
    }
}
