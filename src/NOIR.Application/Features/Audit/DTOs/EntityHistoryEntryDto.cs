namespace NOIR.Application.Features.Audit.DTOs;

/// <summary>
/// Represents a single history entry in an entity's audit trail.
/// </summary>
public sealed record EntityHistoryEntryDto(
    Guid Id,
    DateTimeOffset Timestamp,
    string Operation,
    string? UserId,
    string? UserEmail,
    string? HandlerName,
    string? CorrelationId,
    IReadOnlyList<FieldChangeDto> Changes,
    int Version);
