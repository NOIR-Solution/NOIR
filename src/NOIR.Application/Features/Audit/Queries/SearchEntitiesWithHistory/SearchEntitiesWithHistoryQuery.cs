namespace NOIR.Application.Features.Audit.Queries.SearchEntitiesWithHistory;

/// <summary>
/// Query to search for entities that have audit history.
/// </summary>
public sealed record SearchEntitiesWithHistoryQuery(
    string? EntityType,
    string? SearchTerm,
    int Page = 1,
    int PageSize = 20);
