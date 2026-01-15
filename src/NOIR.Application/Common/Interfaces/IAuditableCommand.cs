namespace NOIR.Application.Common.Interfaces;

/// <summary>
/// Marker interface for commands that support DTO-level diff auditing.
/// Implement this interface to enable automatic before/after state tracking.
/// </summary>
public interface IAuditableCommand
{
    /// <summary>
    /// Gets the ID of the target entity/DTO being modified.
    /// Returns null for create operations (no existing entity).
    /// </summary>
    object? GetTargetId();

    /// <summary>
    /// Gets the type of operation being performed.
    /// </summary>
    AuditOperationType OperationType { get; }

    /// <summary>
    /// Gets a human-readable description of the action being performed.
    /// Example: "Create User", "Update Role Permissions", "Delete Tenant".
    /// Returns null to use a default description based on handler name.
    /// </summary>
    string? GetActionDescription() => null;

    /// <summary>
    /// Gets a display-friendly name for the target entity.
    /// Example: User's email, Role name, Tenant name.
    /// Returns null if no meaningful display name is available.
    /// </summary>
    string? GetTargetDisplayName() => null;
}

/// <summary>
/// Typed version of IAuditableCommand that specifies the DTO type.
/// This enables automatic "before state" fetching for update operations.
/// </summary>
/// <typeparam name="TDto">The DTO type that represents the entity state.</typeparam>
public interface IAuditableCommand<TDto> : IAuditableCommand
    where TDto : class
{
}
