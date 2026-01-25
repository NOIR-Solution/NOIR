using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NOIR.Domain.Entities.Payment;

namespace NOIR.Infrastructure.Persistence.Configurations;

public class PaymentOperationLogConfiguration : IEntityTypeConfiguration<PaymentOperationLog>
{
    public void Configure(EntityTypeBuilder<PaymentOperationLog> builder)
    {
        builder.ToTable("PaymentOperationLogs");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.OperationType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(e => e.Provider)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.TransactionNumber)
            .HasMaxLength(50);

        builder.Property(e => e.CorrelationId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.RequestData)
            .HasMaxLength(10500); // 10KB + truncation message

        builder.Property(e => e.ResponseData)
            .HasMaxLength(10500);

        builder.Property(e => e.ErrorCode)
            .HasMaxLength(100);

        builder.Property(e => e.ErrorMessage)
            .HasMaxLength(2100);

        builder.Property(e => e.StackTrace)
            .HasMaxLength(4100);

        builder.Property(e => e.AdditionalContext)
            .HasMaxLength(4100);

        builder.Property(e => e.UserId)
            .HasMaxLength(450);

        builder.Property(e => e.IpAddress)
            .HasMaxLength(45); // IPv6 max length

        // Indexes for common queries
        builder.HasIndex(e => new { e.TenantId, e.CreatedAt })
            .HasDatabaseName("IX_PaymentOperationLogs_TenantId_CreatedAt");

        builder.HasIndex(e => new { e.TenantId, e.PaymentTransactionId })
            .HasDatabaseName("IX_PaymentOperationLogs_TenantId_TransactionId");

        builder.HasIndex(e => new { e.TenantId, e.CorrelationId })
            .HasDatabaseName("IX_PaymentOperationLogs_TenantId_CorrelationId");

        builder.HasIndex(e => new { e.TenantId, e.Provider, e.OperationType })
            .HasDatabaseName("IX_PaymentOperationLogs_TenantId_Provider_OperationType");

        builder.HasIndex(e => new { e.TenantId, e.Success, e.CreatedAt })
            .HasDatabaseName("IX_PaymentOperationLogs_TenantId_Success_CreatedAt");
    }
}
