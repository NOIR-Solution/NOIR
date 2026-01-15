namespace NOIR.Infrastructure.Audit;

/// <summary>
/// Provides ambient context for linking audit logs across the 3-level hierarchy.
/// Uses AsyncLocal to flow context across async calls within a single request.
/// </summary>
public class AuditContext
{
    private static readonly AsyncLocal<AuditContextData?> CurrentData = new();

    /// <summary>
    /// Gets the current audit context for the async flow.
    /// </summary>
    public static AuditContextData? Current
    {
        get => CurrentData.Value;
        private set => CurrentData.Value = value;
    }

    /// <summary>
    /// Starts a new HTTP request audit context.
    /// </summary>
    /// <param name="httpRequestAuditLogId">The ID of the HttpRequestAuditLog.</param>
    /// <param name="correlationId">The correlation ID for the request.</param>
    /// <param name="pageContext">Optional page context from the frontend (e.g., "Users", "Tenants").</param>
    /// <returns>A disposable scope that clears the context on dispose.</returns>
    public static IDisposable BeginRequestScope(Guid httpRequestAuditLogId, string correlationId, string? pageContext = null)
    {
        Current = new AuditContextData
        {
            HttpRequestAuditLogId = httpRequestAuditLogId,
            CorrelationId = correlationId,
            PageContext = pageContext
        };

        return new AuditContextScope();
    }

    /// <summary>
    /// Sets the current handler audit log ID.
    /// </summary>
    /// <param name="handlerAuditLogId">The ID of the HandlerAuditLog.</param>
    public static void SetCurrentHandler(Guid handlerAuditLogId)
    {
        // Capture current context once to avoid TOCTOU race condition
        // where Current could be cleared between the null check and assignment
        var current = Current;
        if (current is not null)
        {
            current.CurrentHandlerAuditLogId = handlerAuditLogId;
        }
    }

    /// <summary>
    /// Clears the current handler audit log ID (when handler completes).
    /// </summary>
    public static void ClearCurrentHandler()
    {
        // Capture current context once to avoid TOCTOU race condition
        var current = Current;
        if (current is not null)
        {
            current.CurrentHandlerAuditLogId = null;
        }
    }

    /// <summary>
    /// Clears the entire audit context.
    /// </summary>
    public static void Clear()
    {
        Current = null;
    }

    private sealed class AuditContextScope : IDisposable
    {
        public void Dispose() => Clear();
    }
}

/// <summary>
/// Data stored in the audit context.
/// </summary>
public class AuditContextData
{
    /// <summary>
    /// The ID of the parent HttpRequestAuditLog for this request.
    /// </summary>
    public Guid HttpRequestAuditLogId { get; set; }

    /// <summary>
    /// The correlation ID linking all audit entries for this request.
    /// </summary>
    public string CorrelationId { get; set; } = default!;

    /// <summary>
    /// The ID of the currently executing HandlerAuditLog (if any).
    /// </summary>
    public Guid? CurrentHandlerAuditLogId { get; set; }

    /// <summary>
    /// The page context from the frontend (e.g., "Users", "Tenants").
    /// Used to display user-friendly context in the activity timeline.
    /// </summary>
    public string? PageContext { get; set; }
}
