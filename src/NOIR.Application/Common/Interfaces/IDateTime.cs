namespace NOIR.Application.Common.Interfaces;

/// <summary>
/// Interface for abstracting date/time operations for testability.
/// Uses DateTimeOffset for timezone-aware timestamps.
/// </summary>
public interface IDateTime
{
    DateTimeOffset UtcNow { get; }
    DateOnly Today { get; }
}
