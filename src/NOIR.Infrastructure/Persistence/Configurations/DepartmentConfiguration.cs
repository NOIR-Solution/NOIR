namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for Department entity.
/// </summary>
public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("Departments");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // Name
        builder.Property(e => e.Name)
            .HasMaxLength(200)
            .IsRequired();

        // Code (unique per tenant)
        builder.Property(e => e.Code)
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(e => new { e.TenantId, e.Code })
            .IsUnique()
            .HasDatabaseName("IX_Departments_TenantId_Code");

        // Description
        builder.Property(e => e.Description)
            .HasMaxLength(500);

        // Sort order
        builder.Property(e => e.SortOrder)
            .HasDefaultValue(0);

        // Active status
        builder.Property(e => e.IsActive)
            .HasDefaultValue(true);

        // Manager FK (employee who manages this department — Restrict, application handles cascade)
        builder.HasOne(e => e.Manager)
            .WithMany()
            .HasForeignKey(e => e.ManagerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Self-referencing hierarchy
        builder.HasOne(e => e.ParentDepartment)
            .WithMany(e => e.SubDepartments)
            .HasForeignKey(e => e.ParentDepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique name per parent per tenant
        builder.HasIndex(e => new { e.Name, e.ParentDepartmentId, e.TenantId })
            .IsUnique()
            .HasFilter(null)
            .HasDatabaseName("IX_Departments_Name_Parent_Tenant");

        // Indexes
        builder.HasIndex(e => new { e.TenantId, e.ParentDepartmentId, e.SortOrder })
            .HasDatabaseName("IX_Departments_TenantId_Parent_Sort");

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
