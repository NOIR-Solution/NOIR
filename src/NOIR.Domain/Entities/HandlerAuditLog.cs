namespace NOIR.Domain.Entities;

/// <summary>
/// Captures handler/command execution with DTO-level diff tracking.
/// Middle level of the audit hierarchy, links HTTP request to entity changes.
/// </summary>
public class HandlerAuditLog : Entity<Guid>, ITenantEntity
{
    /// <summary>
    /// Reference to the parent HTTP request audit log.
    /// </summary>
    public Guid? HttpRequestAuditLogId { get; set; }

    /// <summary>
    /// Correlation ID linking all audit entries.
    /// </summary>
    public string CorrelationId { get; set; } = default!;

    /// <summary>
    /// Tenant ID for multi-tenant filtering.
    /// Protected setter enforces immutability after creation.
    /// </summary>
    public string? TenantId { get; protected set; }

    #region Handler Info

    /// <summary>
    /// Name of the handler/command (e.g., "UpdateCustomerCommand").
    /// </summary>
    public string HandlerName { get; set; } = default!;

    /// <summary>
    /// Type of operation performed.
    /// </summary>
    public string OperationType { get; set; } = default!;

    #endregion

    #region Activity Context

    /// <summary>
    /// UI page context that triggered this action (e.g., "Users", "Tenants", "Roles").
    /// Null for API calls without UI context. Falls back to HandlerName for display.
    /// </summary>
    public string? PageContext { get; set; }

    /// <summary>
    /// Human-readable description of the action (e.g., "edited User John Doe").
    /// </summary>
    public string? ActionDescription { get; set; }

    /// <summary>
    /// Display name of the target entity (e.g., "John Doe", "Test Corp").
    /// Stored at write time for historical accuracy.
    /// </summary>
    public string? TargetDisplayName { get; set; }

    #endregion

    #region Target DTO

    /// <summary>
    /// Type of the target DTO (e.g., "CustomerDto").
    /// </summary>
    public string? TargetDtoType { get; set; }

    /// <summary>
    /// ID of the target DTO entity.
    /// </summary>
    public string? TargetDtoId { get; set; }

    /// <summary>
    /// RFC 6902 JSON Patch diff of the DTO changes.
    /// </summary>
    public string? DtoDiff { get; set; }

    #endregion

    #region Input/Output

    /// <summary>
    /// Input parameters as JSON (sanitized).
    /// </summary>
    public string? InputParameters { get; set; }

    /// <summary>
    /// Output result as JSON (sanitized).
    /// </summary>
    public string? OutputResult { get; set; }

    #endregion

    #region Timing & Status

    /// <summary>
    /// When the handler started.
    /// </summary>
    public DateTimeOffset StartTime { get; set; }

    /// <summary>
    /// When the handler ended.
    /// </summary>
    public DateTimeOffset? EndTime { get; set; }

    /// <summary>
    /// Duration in milliseconds.
    /// </summary>
    public long? DurationMs { get; set; }

    /// <summary>
    /// Whether the handler succeeded.
    /// </summary>
    public bool IsSuccess { get; set; } = true;

    /// <summary>
    /// Error message if the handler failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

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
    /// Parent HTTP request audit log.
    /// </summary>
    public HttpRequestAuditLog? HttpRequestAuditLog { get; set; }

    /// <summary>
    /// Entity audit logs created during this handler execution.
    /// </summary>
    public ICollection<EntityAuditLog> EntityAuditLogs { get; set; } = new List<EntityAuditLog>();

    #endregion

    /// <summary>
    /// Creates a new handler audit log entry.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when required parameters are invalid.</exception>
    public static HandlerAuditLog Create(
        string correlationId,
        string handlerName,
        AuditOperationType operationType,
        string? tenantId,
        Guid? httpRequestAuditLogId = null,
        string? pageContext = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(handlerName);

        if (!Enum.IsDefined(operationType))
            throw new ArgumentException($"Invalid operation type: {operationType}", nameof(operationType));

        return new HandlerAuditLog
        {
            Id = Guid.NewGuid(),
            CorrelationId = correlationId,
            HandlerName = handlerName,
            OperationType = operationType.ToString(),
            TenantId = tenantId,
            HttpRequestAuditLogId = httpRequestAuditLogId,
            PageContext = pageContext,
            StartTime = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Completes the audit log with result information.
    /// </summary>
    public void Complete(
        bool isSuccess,
        string? outputResult = null,
        string? dtoDiff = null,
        string? errorMessage = null)
    {
        IsSuccess = isSuccess;
        OutputResult = outputResult;
        DtoDiff = dtoDiff;
        ErrorMessage = errorMessage;
        EndTime = DateTimeOffset.UtcNow;
        DurationMs = (long)(EndTime.Value - StartTime).TotalMilliseconds;
    }

    /// <summary>
    /// Marks the audit log as failed (e.g., when HTTP response indicates failure).
    /// </summary>
    public void MarkAsFailed(string? errorMessage = null)
    {
        IsSuccess = false;
        if (!string.IsNullOrEmpty(errorMessage))
        {
            ErrorMessage = string.IsNullOrEmpty(ErrorMessage)
                ? errorMessage
                : $"{ErrorMessage}; {errorMessage}";
        }
    }

    /// <summary>
    /// Sets the target DTO information.
    /// </summary>
    public void SetTargetDto(string dtoType, string? dtoId)
    {
        TargetDtoType = dtoType;
        TargetDtoId = dtoId;
    }

    /// <summary>
    /// Sets the activity context for timeline display.
    /// </summary>
    /// <param name="displayName">Display name of the target entity (e.g., "John Doe").</param>
    /// <param name="actionDescription">Human-readable description (e.g., "edited User John Doe").</param>
    public void SetActivityContext(string? displayName, string? actionDescription)
    {
        TargetDisplayName = displayName;
        ActionDescription = actionDescription;
    }

    /// <summary>
    /// Gets the display context for the timeline.
    /// Returns PageContext if set, otherwise falls back to HandlerName.
    /// </summary>
    public string GetDisplayContext() => PageContext ?? HandlerName;
}
