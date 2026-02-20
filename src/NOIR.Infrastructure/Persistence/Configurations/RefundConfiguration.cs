namespace NOIR.Infrastructure.Persistence.Configurations;

public class RefundConfiguration : IEntityTypeConfiguration<Refund>
{
    public void Configure(EntityTypeBuilder<Refund> builder)
    {
        builder.ToTable("Refunds");

        // Primary key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // Payment transaction relationship
        builder.HasOne(e => e.PaymentTransaction)
            .WithMany(e => e.Refunds)
            .HasForeignKey(e => e.PaymentTransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.PaymentTransactionId)
            .HasDatabaseName("IX_Refunds_PaymentTransactionId");

        // Amount
        builder.Property(e => e.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        // Currency
        builder.Property(e => e.Currency)
            .HasMaxLength(3)
            .IsRequired();

        // Status
        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(RefundStatus.Pending);

        // Reason
        builder.Property(e => e.Reason)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        // Reason detail
        builder.Property(e => e.ReasonDetail)
            .HasMaxLength(1000);

        // Refund number
        builder.Property(e => e.RefundNumber)
            .HasMaxLength(50)
            .IsRequired();

        // Requested by
        builder.Property(e => e.RequestedBy)
            .HasMaxLength(200);

        // Approved by
        builder.Property(e => e.ApprovedBy)
            .HasMaxLength(200);

        // Gateway refund ID
        builder.Property(e => e.GatewayRefundId)
            .HasMaxLength(200);

        // Status index
        builder.HasIndex(e => new { e.Status, e.TenantId, e.CreatedAt })
            .HasDatabaseName("IX_Refunds_Status_Tenant_Created");

        // Concurrency token for optimistic concurrency control
        builder.Property(e => e.RowVersion)
            .IsRowVersion();

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
