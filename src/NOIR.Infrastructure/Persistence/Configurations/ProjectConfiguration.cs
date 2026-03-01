namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for Project entity.
/// </summary>
public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // ProjectCode
        builder.Property(e => e.ProjectCode)
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(e => new { e.ProjectCode, e.TenantId })
            .IsUnique()
            .HasDatabaseName("IX_Projects_ProjectCode_TenantId");

        // Name
        builder.Property(e => e.Name)
            .HasMaxLength(200)
            .IsRequired();

        // Slug (unique per tenant)
        builder.Property(e => e.Slug)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(e => new { e.Slug, e.TenantId })
            .IsUnique()
            .HasDatabaseName("IX_Projects_Slug_TenantId");

        // Description
        builder.Property(e => e.Description)
            .HasMaxLength(5000);

        // Status
        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        // Visibility
        builder.Property(e => e.Visibility)
            .HasConversion<string>()
            .HasMaxLength(20);

        // Budget
        builder.Property(e => e.Budget)
            .HasPrecision(18, 2);

        // Currency
        builder.Property(e => e.Currency)
            .HasMaxLength(3)
            .HasDefaultValue("VND");

        // Color (hex)
        builder.Property(e => e.Color)
            .HasMaxLength(7);

        // Icon (Lucide icon name)
        builder.Property(e => e.Icon)
            .HasMaxLength(50);

        // Owner FK (Employee, Restrict — cannot delete employee who owns projects)
        builder.HasOne(e => e.Owner)
            .WithMany()
            .HasForeignKey(e => e.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Index on OwnerId + TenantId
        builder.HasIndex(e => new { e.OwnerId, e.TenantId })
            .HasDatabaseName("IX_Projects_OwnerId_TenantId");

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
