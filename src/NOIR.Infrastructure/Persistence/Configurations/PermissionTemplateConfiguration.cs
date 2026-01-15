namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for PermissionTemplate entity.
/// </summary>
public class PermissionTemplateConfiguration : IEntityTypeConfiguration<PermissionTemplate>
{
    public void Configure(EntityTypeBuilder<PermissionTemplate> builder)
    {
        builder.ToTable("PermissionTemplates");

        // Primary key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // Name
        builder.Property(e => e.Name)
            .HasMaxLength(100)
            .IsRequired();

        // Description
        builder.Property(e => e.Description)
            .HasMaxLength(500);

        // Display fields
        builder.Property(e => e.IconName)
            .HasMaxLength(50);
        builder.Property(e => e.Color)
            .HasMaxLength(50);

        // Tenant scoping
        builder.Property(e => e.TenantId);
        builder.HasIndex(e => e.TenantId);

        // Unique name within tenant (or system if TenantId is null)
        builder.HasIndex(e => new { e.Name, e.TenantId })
            .IsUnique()
            .HasDatabaseName("IX_PermissionTemplates_Name_TenantId");

        // Soft delete filter
        builder.HasQueryFilter(e => !e.IsDeleted);
        builder.HasIndex(e => e.IsDeleted);
    }
}

/// <summary>
/// EF Core configuration for PermissionTemplateItem join entity.
/// </summary>
public class PermissionTemplateItemConfiguration : IEntityTypeConfiguration<PermissionTemplateItem>
{
    public void Configure(EntityTypeBuilder<PermissionTemplateItem> builder)
    {
        builder.ToTable("PermissionTemplateItems");

        // Primary key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // Unique constraint on (TemplateId, PermissionId)
        builder.HasIndex(e => new { e.TemplateId, e.PermissionId })
            .IsUnique()
            .HasDatabaseName("IX_PermissionTemplateItems_TemplateId_PermissionId");

        // Relationships
        builder.HasOne(e => e.Template)
            .WithMany(t => t.Items)
            .HasForeignKey(e => e.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Permission)
            .WithMany()
            .HasForeignKey(e => e.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
