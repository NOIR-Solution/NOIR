using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NOIR.Domain.Entities.Analytics;

namespace NOIR.Application.Common.Interfaces;

/// <summary>
/// Interface for the application database context.
/// Defines the contract for data access in the application layer.
/// </summary>
public interface IApplicationDbContext
{
    /// <summary>
    /// Permission templates DbSet for direct access.
    /// </summary>
    DbSet<PermissionTemplate> PermissionTemplates { get; }

    /// <summary>
    /// Email templates DbSet for direct access.
    /// Used by Copy-on-Write pattern to query across tenant boundaries.
    /// </summary>
    DbSet<EmailTemplate> EmailTemplates { get; }

    /// <summary>
    /// Legal pages DbSet for direct access.
    /// Used by Copy-on-Write pattern to query across tenant boundaries.
    /// </summary>
    DbSet<LegalPage> LegalPages { get; }

    /// <summary>
    /// Product categories DbSet for direct access.
    /// Used by ProductFilter for category hierarchy lookups.
    /// </summary>
    DbSet<ProductCategory> ProductCategories { get; }

    /// <summary>
    /// Product attributes DbSet for direct access.
    /// Used by ProductFilter for facet calculations.
    /// </summary>
    DbSet<ProductAttribute> ProductAttributes { get; }

    /// <summary>
    /// Product attribute values DbSet for direct access.
    /// Predefined values for select/multi-select type attributes.
    /// </summary>
    DbSet<ProductAttributeValue> ProductAttributeValues { get; }

    /// <summary>
    /// Category attributes DbSet for direct access.
    /// Junction table linking categories to their assigned product attributes.
    /// </summary>
    DbSet<CategoryAttribute> CategoryAttributes { get; }

    /// <summary>
    /// Product attribute assignments DbSet for direct access.
    /// Stores a product's actual attribute values with polymorphic value storage.
    /// </summary>
    DbSet<ProductAttributeAssignment> ProductAttributeAssignments { get; }

    /// <summary>
    /// Product filter indexes DbSet for direct access.
    /// Denormalized table for high-performance product filtering.
    /// </summary>
    DbSet<ProductFilterIndex> ProductFilterIndexes { get; }

    /// <summary>
    /// Filter analytics events DbSet for direct access.
    /// Used for tracking and analyzing filter usage patterns.
    /// </summary>
    DbSet<FilterAnalyticsEvent> FilterAnalyticsEvents { get; }

    /// <summary>
    /// Order notes DbSet for non-aggregate entity CRUD.
    /// OrderNote is a TenantEntity (not AggregateRoot), so IRepository is not applicable.
    /// </summary>
    DbSet<OrderNote> OrderNotes { get; }

    /// <summary>
    /// Customer group memberships DbSet for junction table CRUD.
    /// CustomerGroupMembership is a TenantEntity (not AggregateRoot), so IRepository is not applicable.
    /// </summary>
    DbSet<CustomerGroupMembership> CustomerGroupMemberships { get; }

    /// <summary>
    /// Tenant module states DbSet for feature management CRUD.
    /// TenantModuleState is a TenantEntity (not AggregateRoot), so IRepository is not applicable.
    /// </summary>
    DbSet<TenantModuleState> TenantModuleStates { get; }

    /// <summary>
    /// Attaches an entity to the context for tracking.
    EntityEntry<TEntity> Attach<TEntity>(TEntity entity) where TEntity : class;

    /// <summary>
    /// Saves all changes to the database.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
