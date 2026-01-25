namespace NOIR.Application.Features.Payments.Queries.GetOperationLogs;

/// <summary>
/// Query to get payment operation logs with filtering and pagination.
/// </summary>
public sealed record GetOperationLogsQuery(
    string? Provider = null,
    PaymentOperationType? OperationType = null,
    bool? Success = null,
    string? TransactionNumber = null,
    string? CorrelationId = null,
    DateTimeOffset? FromDate = null,
    DateTimeOffset? ToDate = null,
    int Page = 1,
    int PageSize = 20);
