namespace NOIR.Application.Features.Audit.Queries;

/// <summary>
/// Query to get paginated handler audit logs.
/// Supports filtering by handler name, operation type, and success status.
/// </summary>
public sealed record GetHandlerAuditLogsQuery(
    int PageNumber = 1,
    int PageSize = 20,
    string? HandlerName = null,
    string? OperationType = null,
    bool? IsSuccess = null,
    DateTimeOffset? FromDate = null,
    DateTimeOffset? ToDate = null);
