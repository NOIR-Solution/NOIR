namespace NOIR.Application.Common.Interfaces;

/// <summary>
/// Interface for logging payment operations to database.
/// Provides queryable audit trail for debugging payment issues.
/// </summary>
public interface IPaymentOperationLogger
{
    /// <summary>
    /// Starts a new operation log entry.
    /// </summary>
    Task<Guid> StartOperationAsync(
        PaymentOperationType operationType,
        string provider,
        string? transactionNumber = null,
        Guid? paymentTransactionId = null,
        Guid? refundId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records the request data for an operation.
    /// </summary>
    Task SetRequestDataAsync(
        Guid operationLogId,
        object? requestData,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes an operation as successful.
    /// </summary>
    Task CompleteSuccessAsync(
        Guid operationLogId,
        object? responseData = null,
        int? httpStatusCode = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes an operation as failed.
    /// </summary>
    Task CompleteFailedAsync(
        Guid operationLogId,
        string? errorCode,
        string? errorMessage,
        object? responseData = null,
        int? httpStatusCode = null,
        Exception? exception = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds additional context to an operation.
    /// </summary>
    Task AddContextAsync(
        Guid operationLogId,
        object context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a complete operation log in one call (for simple operations).
    /// </summary>
    Task LogOperationAsync(
        PaymentOperationType operationType,
        string provider,
        bool success,
        string? transactionNumber = null,
        Guid? paymentTransactionId = null,
        object? requestData = null,
        object? responseData = null,
        int? httpStatusCode = null,
        long? durationMs = null,
        string? errorCode = null,
        string? errorMessage = null,
        CancellationToken cancellationToken = default);
}
