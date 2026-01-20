namespace NOIR.Domain.Common;

/// <summary>
/// Marker interface for entities that support smart seed updates.
/// Entities implement version tracking to distinguish system defaults from user customizations.
/// </summary>
public interface ISeedableEntity
{
    /// <summary>
    /// The version number of the entity.
    /// Version 1 = never modified by user, safe to update during seeding.
    /// Version > 1 = user customized, skip during seed updates.
    /// </summary>
    int Version { get; }

    /// <summary>
    /// Resets the version to 1 after a seed update.
    /// FOR SEEDING USE ONLY - prevents version increments from affecting user-customization detection.
    /// </summary>
    void ResetVersionForSeeding();
}
