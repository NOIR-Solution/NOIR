namespace NOIR.Application.Features.Audit.DTOs;

/// <summary>
/// Represents a specific version/snapshot of an entity at a point in time.
/// Used for version comparison.
/// </summary>
public sealed record EntityVersionDto(
    int Version,
    DateTimeOffset Timestamp,
    string Operation,
    string? UserId,
    string? UserEmail,
    Dictionary<string, object?> State);
