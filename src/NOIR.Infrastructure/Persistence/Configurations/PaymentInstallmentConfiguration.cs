namespace NOIR.Infrastructure.Persistence.Configurations;

public class PaymentInstallmentConfiguration : IEntityTypeConfiguration<PaymentInstallment>
{
    public void Configure(EntityTypeBuilder<PaymentInstallment> builder)
    {
        builder.ToTable("PaymentInstallments");

        // Primary key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // Payment transaction relationship
        builder.HasOne(e => e.PaymentTransaction)
            .WithMany(t => t.Installments)
            .HasForeignKey(e => e.PaymentTransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.PaymentTransactionId)
            .HasDatabaseName("IX_PaymentInstallments_TransactionId");

        // Installment number
        builder.Property(e => e.InstallmentNumber)
            .IsRequired();

        // Total installments
        builder.Property(e => e.TotalInstallments)
            .IsRequired();

        // Unique installment number per transaction
        builder.HasIndex(e => new { e.PaymentTransactionId, e.InstallmentNumber })
            .IsUnique()
            .HasDatabaseName("IX_PaymentInstallments_Transaction_Number");

        // Amount
        builder.Property(e => e.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        // Currency
        builder.Property(e => e.Currency)
            .HasMaxLength(3)
            .IsRequired();

        // Due date
        builder.Property(e => e.DueDate)
            .IsRequired();

        // Status
        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(InstallmentStatus.Scheduled);

        // Paid at
        builder.Property(e => e.PaidAt);

        // Gateway reference
        builder.Property(e => e.GatewayReference)
            .HasMaxLength(200);

        // Failure reason
        builder.Property(e => e.FailureReason)
            .HasMaxLength(500);

        // Retry count
        builder.Property(e => e.RetryCount)
            .HasDefaultValue(0);

        // Status and due date index for processing due installments
        builder.HasIndex(e => new { e.Status, e.DueDate })
            .HasDatabaseName("IX_PaymentInstallments_Status_DueDate");

        // Tenant ID
        builder.Property(e => e.TenantId).HasMaxLength(DatabaseConstants.TenantIdMaxLength);
        builder.HasIndex(e => e.TenantId);
    }
}
