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
    /// Saves all changes to the database.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
