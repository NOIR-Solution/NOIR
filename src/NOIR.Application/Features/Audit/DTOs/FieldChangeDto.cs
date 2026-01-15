namespace NOIR.Application.Features.Audit.DTOs;

/// <summary>
/// Represents a single field change in an entity audit log.
/// </summary>
public sealed record FieldChangeDto(
    string FieldName,
    object? OldValue,
    object? NewValue,
    ChangeOperation Operation);

/// <summary>
/// The type of change operation.
/// </summary>
public enum ChangeOperation
{
    Added,
    Modified,
    Removed
}
