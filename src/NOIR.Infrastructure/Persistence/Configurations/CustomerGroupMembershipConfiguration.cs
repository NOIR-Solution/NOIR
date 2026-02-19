namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for CustomerGroupMembership entity.
/// </summary>
public class CustomerGroupMembershipConfiguration : TenantEntityConfiguration<CustomerGroupMembership>
{
    public override void Configure(EntityTypeBuilder<CustomerGroupMembership> builder)
    {
        base.Configure(builder);

        builder.ToTable("CustomerGroupMemberships");

        // Foreign keys
        builder.Property(e => e.CustomerGroupId).IsRequired();
        builder.Property(e => e.CustomerId).IsRequired();

        // Composite unique index (CLAUDE.md Rule 18 - include TenantId)
        builder.HasIndex(e => new { e.CustomerGroupId, e.CustomerId, e.TenantId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_CustomerGroupMemberships_GroupId_CustomerId_TenantId");

        // Lookup index for customer's groups
        builder.HasIndex(e => new { e.TenantId, e.CustomerId })
            .HasDatabaseName("IX_CustomerGroupMemberships_TenantId_CustomerId");

        // Navigation to Customer
        builder.HasOne(e => e.Customer)
            .WithMany(c => c.GroupMemberships)
            .HasForeignKey(e => e.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Navigation to CustomerGroup (configured in CustomerGroupConfiguration)
    }
}
