namespace NOIR.Application.Features.Audit.Queries;

/// <summary>
/// Query to get paginated HTTP request audit logs.
/// Supports filtering by user, method, status code, and date range.
/// </summary>
public sealed record GetHttpRequestAuditLogsQuery(
    int PageNumber = 1,
    int PageSize = 20,
    string? UserId = null,
    string? HttpMethod = null,
    int? StatusCode = null,
    DateTimeOffset? FromDate = null,
    DateTimeOffset? ToDate = null);
