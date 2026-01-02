namespace NOIR.Domain.Entities;

/// <summary>
/// Captures HTTP request/response context at the top level of the audit hierarchy.
/// All handler and entity audit logs link back to this via CorrelationId.
/// </summary>
public class HttpRequestAuditLog : Entity<Guid>, ITenantEntity
{
    /// <summary>
    /// Unique correlation ID linking all audit entries for this request.
    /// </summary>
    public string CorrelationId { get; set; } = default!;

    /// <summary>
    /// Tenant ID for multi-tenant filtering.
    /// Protected setter enforces immutability after creation.
    /// </summary>
    public string? TenantId { get; protected set; }

    /// <summary>
    /// User who made the request.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// User's email for display.
    /// </summary>
    public string? UserEmail { get; set; }

    #region Request

    /// <summary>
    /// HTTP method (GET, POST, PUT, DELETE, etc.).
    /// </summary>
    public string HttpMethod { get; set; } = default!;

    /// <summary>
    /// Full request URL path.
    /// </summary>
    public string Url { get; set; } = default!;

    /// <summary>
    /// Query string parameters.
    /// </summary>
    public string? QueryString { get; set; }

    /// <summary>
    /// Request headers as JSON (sensitive headers sanitized).
    /// </summary>
    public string? RequestHeaders { get; set; }

    /// <summary>
    /// Request body as JSON (sensitive data sanitized).
    /// </summary>
    public string? RequestBody { get; set; }

    #endregion

    #region Response

    /// <summary>
    /// HTTP response status code.
    /// </summary>
    public int? ResponseStatusCode { get; set; }

    /// <summary>
    /// Response body as JSON (optional, size-limited).
    /// </summary>
    public string? ResponseBody { get; set; }

    #endregion

    #region Context

    /// <summary>
    /// Client IP address.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent string.
    /// </summary>
    public string? UserAgent { get; set; }

    #endregion

    #region Timing

    /// <summary>
    /// When the request started.
    /// </summary>
    public DateTimeOffset StartTime { get; set; }

    /// <summary>
    /// When the request ended.
    /// </summary>
    public DateTimeOffset? EndTime { get; set; }

    /// <summary>
    /// Total duration in milliseconds.
    /// </summary>
    public long? DurationMs { get; set; }

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
    /// Handler audit logs associated with this request.
    /// </summary>
    public ICollection<HandlerAuditLog> HandlerAuditLogs { get; set; } = new List<HandlerAuditLog>();

    #endregion

    /// <summary>
    /// Creates a new HTTP request audit log entry.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when required parameters are invalid.</exception>
    public static HttpRequestAuditLog Create(
        string correlationId,
        string httpMethod,
        string url,
        string? queryString,
        string? userId,
        string? userEmail,
        string? tenantId,
        string? ipAddress,
        string? userAgent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(httpMethod);
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        return new HttpRequestAuditLog
        {
            Id = Guid.NewGuid(),
            CorrelationId = correlationId,
            HttpMethod = httpMethod,
            Url = url,
            QueryString = queryString,
            UserId = userId,
            UserEmail = userEmail,
            TenantId = tenantId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            StartTime = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Completes the audit log with response information.
    /// </summary>
    public void Complete(int statusCode, string? responseBody = null)
    {
        ResponseStatusCode = statusCode;
        ResponseBody = responseBody;
        EndTime = DateTimeOffset.UtcNow;
        DurationMs = (long)(EndTime.Value - StartTime).TotalMilliseconds;
    }
}
