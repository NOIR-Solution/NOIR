namespace NOIR.Application.Features.Audit.DTOs;

/// <summary>
/// Represents an entity found in the audit history search.
/// </summary>
public sealed record EntitySearchResultDto(
    string EntityType,
    string EntityId,
    string DisplayName,
    string? Description,
    DateTimeOffset? LastModified,
    string? LastModifiedBy,
    int TotalChanges);
