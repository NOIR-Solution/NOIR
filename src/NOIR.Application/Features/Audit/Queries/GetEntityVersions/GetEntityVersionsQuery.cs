namespace NOIR.Application.Features.Audit.Queries.GetEntityVersions;

/// <summary>
/// Query to get all versions of an entity for comparison dropdown.
/// </summary>
public sealed record GetEntityVersionsQuery(
    string EntityType,
    string EntityId);
