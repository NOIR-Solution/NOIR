namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for ProjectTask entity.
/// </summary>
public class ProjectTaskConfiguration : IEntityTypeConfiguration<ProjectTask>
{
    public void Configure(EntityTypeBuilder<ProjectTask> builder)
    {
        builder.ToTable("ProjectTasks");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // TaskNumber (unique per tenant)
        builder.Property(e => e.TaskNumber)
            .HasMaxLength(30)
            .IsRequired();

        builder.HasIndex(e => new { e.TaskNumber, e.TenantId })
            .IsUnique()
            .HasDatabaseName("IX_ProjectTasks_TaskNumber_TenantId");

        // Title
        builder.Property(e => e.Title)
            .HasMaxLength(500)
            .IsRequired();

        // Description (rich text)
        builder.Property(e => e.Description)
            .HasMaxLength(10000);

        // Status
        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        // Priority
        builder.Property(e => e.Priority)
            .HasConversion<string>()
            .HasMaxLength(20);

        // EstimatedHours
        builder.Property(e => e.EstimatedHours)
            .HasPrecision(8, 2);

        // ActualHours
        builder.Property(e => e.ActualHours)
            .HasPrecision(8, 2);

        // SortOrder (for Kanban drag)
        builder.Property(e => e.SortOrder)
            .HasDefaultValue(0d);

        // Composite index for Kanban board queries
        builder.HasIndex(e => new { e.ProjectId, e.ColumnId, e.Status, e.TenantId })
            .HasDatabaseName("IX_ProjectTasks_ProjectId_ColumnId_Status_TenantId");

        // Project FK (Cascade — delete tasks when project is deleted)
        builder.HasOne(e => e.Project)
            .WithMany(p => p.Tasks)
            .HasForeignKey(e => e.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        // Column FK (Restrict — cannot delete column with tasks)
        builder.HasOne(e => e.Column)
            .WithMany(c => c.Tasks)
            .HasForeignKey(e => e.ColumnId)
            .OnDelete(DeleteBehavior.Restrict);

        // Assignee FK (Employee, Restrict — unassign before deleting employee)
        builder.HasOne(e => e.Assignee)
            .WithMany()
            .HasForeignKey(e => e.AssigneeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Reporter FK (Employee, Restrict — unassign before deleting employee)
        builder.HasOne(e => e.Reporter)
            .WithMany()
            .HasForeignKey(e => e.ReporterId)
            .OnDelete(DeleteBehavior.Restrict);

        // ParentTask FK (self-referencing, Restrict)
        builder.HasOne(e => e.ParentTask)
            .WithMany(e => e.SubTasks)
            .HasForeignKey(e => e.ParentTaskId)
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

        // Archive
        builder.Property(e => e.IsArchived).HasDefaultValue(false);

        // Soft delete query filter
        builder.HasQueryFilter("SoftDelete", e => !e.IsDeleted);
    }
}
