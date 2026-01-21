namespace NOIR.Application.Common.Interfaces;

/// <summary>
/// Service for managing application configuration at runtime.
/// Provides atomic file operations with backup/restore capabilities.
/// </summary>
public interface IConfigurationManagementService
{
    /// <summary>
    /// Gets all available configuration sections with their allowed status and restart requirements.
    /// </summary>
    Task<Result<IEnumerable<ConfigurationSectionDto>>> GetAvailableSectionsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current JSON value for a specific configuration section.
    /// </summary>
    Task<Result<ConfigurationSectionDto>> GetSectionAsync(
        string sectionName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a configuration section with atomic file operations.
    /// Creates a backup before applying changes.
    /// </summary>
    Task<Result<ConfigurationBackupDto>> UpdateSectionAsync(
        string sectionName,
        string newValueJson,
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a list of all configuration backups, sorted by creation date (newest first).
    /// </summary>
    Task<Result<IEnumerable<ConfigurationBackupDto>>> GetBackupsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores configuration from a backup file.
    /// Creates a pre-rollback backup before applying the restore.
    /// </summary>
    Task<Result> RollbackAsync(
        string backupId,
        string userId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a configuration section with its metadata.
/// </summary>
public sealed record ConfigurationSectionDto(
    string Name,
    string DisplayName,
    bool IsAllowed,
    bool RequiresRestart,
    string CurrentValueJson);

/// <summary>
/// DTO for configuration backup information.
/// </summary>
public sealed record ConfigurationBackupDto(
    string Id,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    string FilePath,
    long SizeBytes);
