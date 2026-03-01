namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for EmployeeTagAssignment junction entity.
/// </summary>
public class EmployeeTagAssignmentConfiguration : IEntityTypeConfiguration<EmployeeTagAssignment>
{
    public void Configure(EntityTypeBuilder<EmployeeTagAssignment> builder)
    {
        builder.ToTable("EmployeeTagAssignments");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // Composite unique index: TenantId + EmployeeId + EmployeeTagId
        builder.HasIndex(e => new { e.TenantId, e.EmployeeId, e.EmployeeTagId })
            .IsUnique()
            .HasDatabaseName("IX_EmployeeTagAssignments_TenantId_Employee_Tag");

        // FK to Employee (Restrict — no cascade on delete)
        builder.HasOne(e => e.Employee)
            .WithMany(emp => emp.TagAssignments)
            .HasForeignKey(e => e.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        // FK to EmployeeTag (Restrict)
        builder.HasOne(e => e.EmployeeTag)
            .WithMany(tag => tag.TagAssignments)
            .HasForeignKey(e => e.EmployeeTagId)
            .OnDelete(DeleteBehavior.Restrict);

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
