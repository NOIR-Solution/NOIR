namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for EmployeeTag entity.
/// </summary>
public class EmployeeTagConfiguration : IEntityTypeConfiguration<EmployeeTag>
{
    public void Configure(EntityTypeBuilder<EmployeeTag> builder)
    {
        builder.ToTable("EmployeeTags");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // Name
        builder.Property(e => e.Name)
            .HasMaxLength(100)
            .IsRequired();

        // Category (enum as string)
        builder.Property(e => e.Category)
            .HasConversion<string>()
            .HasMaxLength(30);

        // Color (hex #RRGGBB or #RRGGBBAA)
        builder.Property(e => e.Color)
            .HasMaxLength(9)
            .IsRequired();

        // Description
        builder.Property(e => e.Description)
            .HasMaxLength(500);

        // Sort order
        builder.Property(e => e.SortOrder)
            .HasDefaultValue(0);

        // Unique index: TenantId + Name + Category
        builder.HasIndex(e => new { e.TenantId, e.Name, e.Category })
            .IsUnique()
            .HasDatabaseName("IX_EmployeeTags_TenantId_Name_Category");

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

        // Ignore computed property
        builder.Ignore(e => e.EmployeeCount);
    }
}
