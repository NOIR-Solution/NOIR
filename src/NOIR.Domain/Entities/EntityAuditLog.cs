namespace NOIR.Domain.Entities;

/// <summary>
/// Captures entity-level changes with property diff tracking.
/// Bottom level of the audit hierarchy, links to handler execution.
/// </summary>
public class EntityAuditLog : Entity<Guid>, ITenantEntity
{
    /// <summary>
    /// Reference to the parent handler audit log.
    /// </summary>
    public Guid? HandlerAuditLogId { get; set; }

    /// <summary>
    /// Correlation ID linking all audit entries.
    /// </summary>
    public string CorrelationId { get; set; } = default!;

    /// <summary>
    /// Tenant ID for multi-tenant filtering.
    /// Protected setter enforces immutability after creation.
    /// </summary>
    public string? TenantId { get; protected set; }

    #region Entity Info

    /// <summary>
    /// Type of entity (e.g., "Customer", "Order").
    /// </summary>
    public string EntityType { get; set; } = default!;

    /// <summary>
    /// Primary key of the entity.
    /// </summary>
    public string EntityId { get; set; } = default!;

    /// <summary>
    /// Operation type: Added, Modified, Deleted.
    /// </summary>
    public string Operation { get; set; } = default!;

    #endregion

    #region Diff

    /// <summary>
    /// RFC 6902 JSON Patch diff of entity changes.
    /// Extended with oldValue for UI display.
    /// </summary>
    public string? EntityDiff { get; set; }

    #endregion

    #region Ordering

    /// <summary>
    /// When this change occurred.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Entity version number for ordering.
    /// </summary>
    public int Version { get; set; } = 1;

    #endregion

    #region Archiving

    /// <summary>
    /// Whether this log has been archived (for retention policy).
    /// </summary>
    public bool IsArchived { get; set; }

    /// <summary>
    /// When this log was archived.
    /// </summary>
    public DateTimeOffset? ArchivedAt { get; set; }

    #endregion

    #region Navigation

    /// <summary>
    /// Parent handler audit log.
    /// </summary>
    public HandlerAuditLog? HandlerAuditLog { get; set; }

    #endregion

    /// <summary>
    /// Creates a new entity audit log entry.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when required parameters are invalid.</exception>
    public static EntityAuditLog Create(
        string correlationId,
        string entityType,
        string entityId,
        EntityAuditOperation operation,
        string? entityDiff,
        string? tenantId,
        Guid? handlerAuditLogId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityType);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);

        if (!Enum.IsDefined(operation))
            throw new ArgumentException($"Invalid operation type: {operation}", nameof(operation));

        return new EntityAuditLog
        {
            Id = Guid.NewGuid(),
            CorrelationId = correlationId,
            EntityType = entityType,
            EntityId = entityId,
            Operation = operation.ToString(),
            EntityDiff = entityDiff,
            TenantId = tenantId,
            HandlerAuditLogId = handlerAuditLogId,
            Timestamp = DateTimeOffset.UtcNow
        };
    }
}
