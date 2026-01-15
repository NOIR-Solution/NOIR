namespace NOIR.Application.Features.Audit.Queries.GetEntityHistory;

/// <summary>
/// Query to get the full history timeline for a specific entity.
/// </summary>
public sealed record GetEntityHistoryQuery(
    string EntityType,
    string EntityId,
    DateTimeOffset? FromDate = null,
    DateTimeOffset? ToDate = null,
    string? UserId = null,
    int Page = 1,
    int PageSize = 20);
