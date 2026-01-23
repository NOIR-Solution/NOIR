using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

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
    /// Attaches an entity to the context for tracking.
    /// </summary>
    EntityEntry<TEntity> Attach<TEntity>(TEntity entity) where TEntity : class;

    /// <summary>
    /// Saves all changes to the database.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
