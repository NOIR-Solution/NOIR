namespace NOIR.Application.Features.Audit.Queries;

/// <summary>
/// Query to get the change history for a specific entity.
/// Returns all audit entries for the entity ordered by timestamp descending.
/// </summary>
public sealed record GetEntityHistoryQuery(
    string EntityType,
    string EntityId,
    int PageNumber = 1,
    int PageSize = 20);
