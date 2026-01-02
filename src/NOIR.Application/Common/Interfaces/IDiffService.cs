namespace NOIR.Application.Common.Interfaces;

/// <summary>
/// Service for creating RFC 6902 JSON Patch diffs between objects.
/// Used by the audit logging system to track changes.
/// </summary>
public interface IDiffService : IScopedService
{
    /// <summary>
    /// Creates a JSON Patch diff between two objects.
    /// Returns null if the objects are equal.
    /// </summary>
    /// <typeparam name="T">Type of objects to compare.</typeparam>
    /// <param name="before">The original state (null for creates).</param>
    /// <param name="after">The new state (null for deletes).</param>
    /// <returns>RFC 6902 JSON Patch as a string, extended with oldValue for UI display.</returns>
    string? CreateDiff<T>(T? before, T? after) where T : class;

    /// <summary>
    /// Creates a JSON Patch diff from JSON strings.
    /// </summary>
    /// <param name="beforeJson">The original JSON (null for creates).</param>
    /// <param name="afterJson">The new JSON (null for deletes).</param>
    /// <returns>RFC 6902 JSON Patch as a string, extended with oldValue for UI display.</returns>
    string? CreateDiffFromJson(string? beforeJson, string? afterJson);

    /// <summary>
    /// Creates a JSON Patch diff from property dictionaries (for entity changes).
    /// </summary>
    /// <param name="before">Dictionary of original property values.</param>
    /// <param name="after">Dictionary of new property values.</param>
    /// <returns>RFC 6902 JSON Patch as a string, extended with oldValue for UI display.</returns>
    string? CreateDiffFromDictionaries(
        IReadOnlyDictionary<string, object?>? before,
        IReadOnlyDictionary<string, object?>? after);
}
