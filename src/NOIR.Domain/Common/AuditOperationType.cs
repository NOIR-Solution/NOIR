namespace NOIR.Domain.Common;

/// <summary>
/// Represents the type of operation for auditable commands.
/// Used by IAuditableCommand to declare intent.
/// </summary>
public enum AuditOperationType
{
    /// <summary>
    /// Creating a new entity. No before state exists.
    /// </summary>
    Create,

    /// <summary>
    /// Updating an existing entity. Before/after diff tracked.
    /// </summary>
    Update,

    /// <summary>
    /// Deleting an entity. Before state captured.
    /// </summary>
    Delete
}

/// <summary>
/// Entity change operation types for entity-level audit logging.
/// </summary>
public enum EntityAuditOperation
{
    Added,
    Modified,
    Deleted
}
