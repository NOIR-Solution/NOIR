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
