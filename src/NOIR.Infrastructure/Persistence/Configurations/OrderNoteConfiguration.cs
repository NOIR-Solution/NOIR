namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for OrderNote entity.
/// </summary>
public class OrderNoteConfiguration : TenantEntityConfiguration<OrderNote>
{
    public override void Configure(EntityTypeBuilder<OrderNote> builder)
    {
        base.Configure(builder);

        builder.ToTable("OrderNotes");

        // Content
        builder.Property(e => e.Content)
            .HasMaxLength(2000)
            .IsRequired();

        // Author info
        builder.Property(e => e.CreatedByUserId)
            .HasMaxLength(DatabaseConstants.UserIdMaxLength)
            .IsRequired();

        builder.Property(e => e.CreatedByUserName)
            .HasMaxLength(200)
            .IsRequired();

        // Internal flag
        builder.Property(e => e.IsInternal)
            .HasDefaultValue(true);

        // Relationship with Order
        builder.HasOne(e => e.Order)
            .WithMany(o => o.Notes)
            .HasForeignKey(e => e.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index for querying notes by order
        builder.HasIndex(e => new { e.OrderId, e.CreatedAt })
            .HasDatabaseName("IX_OrderNotes_OrderId_CreatedAt");
    }
}
