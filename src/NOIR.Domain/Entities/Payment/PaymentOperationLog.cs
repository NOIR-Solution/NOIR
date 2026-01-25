namespace NOIR.Domain.Entities.Payment;

/// <summary>
/// Database log entry for payment operations.
/// Provides queryable audit trail for debugging payment issues.
/// </summary>
public class PaymentOperationLog : TenantAggregateRoot<Guid>
{
    private PaymentOperationLog() : base() { }
    private PaymentOperationLog(Guid id, string? tenantId) : base(id, tenantId) { }

    /// <summary>
    /// Type of operation performed.
    /// </summary>
    public PaymentOperationType OperationType { get; private set; }

    /// <summary>
    /// Payment provider (vnpay, momo, zalopay, sepay, cod).
    /// </summary>
    public string Provider { get; private set; } = string.Empty;

    /// <summary>
    /// Associated payment transaction ID (if applicable).
    /// </summary>
    public Guid? PaymentTransactionId { get; private set; }

    /// <summary>
    /// Transaction number for easy lookup.
    /// </summary>
    public string? TransactionNumber { get; private set; }

    /// <summary>
    /// Associated refund ID (if applicable).
    /// </summary>
    public Guid? RefundId { get; private set; }

    /// <summary>
    /// Correlation ID for tracing across operations.
    /// </summary>
    public string CorrelationId { get; private set; } = string.Empty;

    /// <summary>
    /// Request data sent to gateway (sanitized, no secrets).
    /// </summary>
    public string? RequestData { get; private set; }

    /// <summary>
    /// Response data received from gateway.
    /// </summary>
    public string? ResponseData { get; private set; }

    /// <summary>
    /// HTTP status code from gateway (if applicable).
    /// </summary>
    public int? HttpStatusCode { get; private set; }

    /// <summary>
    /// Operation duration in milliseconds.
    /// </summary>
    public long DurationMs { get; private set; }

    /// <summary>
    /// Whether the operation succeeded.
    /// </summary>
    public bool Success { get; private set; }

    /// <summary>
    /// Error code (if failed).
    /// </summary>
    public string? ErrorCode { get; private set; }

    /// <summary>
    /// Error message (if failed).
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Stack trace for exceptions (truncated).
    /// </summary>
    public string? StackTrace { get; private set; }

    /// <summary>
    /// Additional context as JSON.
    /// </summary>
    public string? AdditionalContext { get; private set; }

    /// <summary>
    /// User who initiated the operation (if applicable).
    /// </summary>
    public string? UserId { get; private set; }

    /// <summary>
    /// IP address of the request origin.
    /// </summary>
    public string? IpAddress { get; private set; }

    public static PaymentOperationLog Create(
        PaymentOperationType operationType,
        string provider,
        string correlationId,
        string? tenantId = null)
    {
        return new PaymentOperationLog(Guid.NewGuid(), tenantId)
        {
            OperationType = operationType,
            Provider = provider,
            CorrelationId = correlationId
        };
    }

    public void SetTransactionInfo(Guid? transactionId, string? transactionNumber)
    {
        PaymentTransactionId = transactionId;
        TransactionNumber = transactionNumber;
    }

    public void SetRefundInfo(Guid refundId)
    {
        RefundId = refundId;
    }

    public void SetRequestData(string? requestData)
    {
        // Truncate if too long (max 10KB)
        RequestData = requestData?.Length > 10240
            ? requestData[..10240] + "...[TRUNCATED]"
            : requestData;
    }

    public void SetResponseData(string? responseData, int? httpStatusCode = null)
    {
        // Truncate if too long (max 10KB)
        ResponseData = responseData?.Length > 10240
            ? responseData[..10240] + "...[TRUNCATED]"
            : responseData;
        HttpStatusCode = httpStatusCode;
    }

    public void SetDuration(long durationMs)
    {
        DurationMs = durationMs;
    }

    public void MarkAsSuccess()
    {
        Success = true;
    }

    public void MarkAsFailed(string? errorCode, string? errorMessage, string? stackTrace = null)
    {
        Success = false;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage?.Length > 2000
            ? errorMessage[..2000] + "...[TRUNCATED]"
            : errorMessage;
        StackTrace = stackTrace?.Length > 4000
            ? stackTrace[..4000] + "...[TRUNCATED]"
            : stackTrace;
    }

    public void SetAdditionalContext(string? context)
    {
        AdditionalContext = context?.Length > 4000
            ? context[..4000] + "...[TRUNCATED]"
            : context;
    }

    public void SetUserInfo(string? userId, string? ipAddress)
    {
        UserId = userId;
        IpAddress = ipAddress;
    }
}
