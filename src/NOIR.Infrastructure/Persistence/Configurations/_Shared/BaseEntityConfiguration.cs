namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// Base configuration for all Entity<Guid> entities.
/// Configures primary key and creation timestamp.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "EF Core configuration - tested via integration tests")]
public abstract class BaseEntityConfiguration<TEntity> : IEntityTypeConfiguration<TEntity>
    where TEntity : Entity<Guid>
{
    public virtual void Configure(EntityTypeBuilder<TEntity> builder)
    {
        // Primary key
        builder.HasKey(e => e.Id);

        // Id generation strategy
        builder.Property(e => e.Id)
            .ValueGeneratedOnAdd();

        // Creation timestamp
        builder.Property(e => e.CreatedAt)
            .IsRequired();

        // Modification timestamp
        builder.Property(e => e.ModifiedAt);
    }
}

/// <summary>
/// Configuration for entities that implement IAuditableEntity.
/// Adds audit fields and soft delete query filter.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "EF Core configuration - tested via integration tests")]
public abstract class AuditableEntityConfiguration<TEntity> : BaseEntityConfiguration<TEntity>
    where TEntity : Entity<Guid>, IAuditableEntity
{
    public override void Configure(EntityTypeBuilder<TEntity> builder)
    {
        base.Configure(builder);

        // Creation audit
        builder.Property(e => e.CreatedBy)
            .HasMaxLength(450);

        // Modification audit
        builder.Property(e => e.ModifiedBy)
            .HasMaxLength(450);

        // Soft delete
        builder.Property(e => e.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.DeletedAt);

        builder.Property(e => e.DeletedBy)
            .HasMaxLength(450);

        // Index for soft delete queries
        builder.HasIndex(e => e.IsDeleted);

        // Global query filter for soft delete (named filter for EF Core 10 compatibility with multi-tenancy)
        builder.HasQueryFilter("SoftDelete", e => !e.IsDeleted);
    }
}

/// <summary>
/// Configuration for entities that implement ITenantEntity.
/// Adds tenant ID and index.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "EF Core configuration - tested via integration tests")]
public abstract class TenantEntityConfiguration<TEntity> : AuditableEntityConfiguration<TEntity>
    where TEntity : Entity<Guid>, IAuditableEntity, ITenantEntity
{
    public override void Configure(EntityTypeBuilder<TEntity> builder)
    {
        base.Configure(builder);

        // Tenant ID
        builder.Property(e => e.TenantId)
            .HasMaxLength(64);

        // Index for tenant filtering (critical for performance)
        builder.HasIndex(e => e.TenantId);
    }
}
