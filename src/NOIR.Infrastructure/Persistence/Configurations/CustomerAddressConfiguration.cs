namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for CustomerAddress entity.
/// </summary>
public class CustomerAddressConfiguration : TenantEntityConfiguration<Domain.Entities.Customer.CustomerAddress>
{
    public override void Configure(EntityTypeBuilder<Domain.Entities.Customer.CustomerAddress> builder)
    {
        base.Configure(builder);

        builder.ToTable("CustomerAddresses");

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
    }
}
