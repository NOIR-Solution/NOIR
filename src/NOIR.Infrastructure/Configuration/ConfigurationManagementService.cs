namespace NOIR.Infrastructure.Configuration;

/// <summary>
/// Service for managing application configuration at runtime with atomic file operations.
/// Implements backup/restore, JSON validation, and change tracking.
/// </summary>
public class ConfigurationManagementService : IConfigurationManagementService, IScopedService
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly IOptionsMonitor<ConfigurationManagementSettings> _settings;
    private readonly ILogger<ConfigurationManagementService> _logger;

    // File paths
    private string AppSettingsPath => Path.Combine(_environment.ContentRootPath, "appsettings.json");
    private string BackupDirectory => Path.Combine(_environment.ContentRootPath, "config-backups");

    public ConfigurationManagementService(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        IOptionsMonitor<ConfigurationManagementSettings> settings,
        ILogger<ConfigurationManagementService> logger)
    {
        _configuration = configuration;
        _environment = environment;
        _settings = settings;
        _logger = logger;

        // Ensure backup directory exists
        Directory.CreateDirectory(BackupDirectory);
    }

    public async Task<Result<IEnumerable<ConfigurationSectionDto>>> GetAvailableSectionsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var settingsValue = _settings.CurrentValue;
            var sections = new List<ConfigurationSectionDto>();

            // Read current appsettings.json
            var json = await File.ReadAllTextAsync(AppSettingsPath, cancellationToken);
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            // Iterate through all top-level sections
            foreach (var property in root.EnumerateObject())
            {
                var sectionName = property.Name;
                var isAllowed = settingsValue.AllowedSections.Contains(sectionName);
                var requiresRestart = ConfigurationManagementSettings.RequiresRestart
                    .GetValueOrDefault(sectionName, false);

                var section = new ConfigurationSectionDto(
                    Name: sectionName,
                    DisplayName: FormatDisplayName(sectionName),
                    IsAllowed: isAllowed,
                    RequiresRestart: requiresRestart,
                    CurrentValueJson: property.Value.GetRawText());

                sections.Add(section);
            }

            return Result<IEnumerable<ConfigurationSectionDto>>.Success(sections.AsEnumerable());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get available configuration sections");
            return Result.Failure<IEnumerable<ConfigurationSectionDto>>(
                Error.Failure("NOIR-CFG-001", "Failed to load configuration sections."));
        }
    }

    public async Task<Result<ConfigurationSectionDto>> GetSectionAsync(
        string sectionName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var settingsValue = _settings.CurrentValue;

            // Check if section is allowed
            if (!settingsValue.AllowedSections.Contains(sectionName))
            {
                return Result.Failure<ConfigurationSectionDto>(
                    Error.Forbidden($"Configuration section '{sectionName}' is not allowed for editing.", "NOIR-CFG-002"));
            }

            // Read current appsettings.json
            var json = await File.ReadAllTextAsync(AppSettingsPath, cancellationToken);
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            // Get section value
            if (!root.TryGetProperty(sectionName, out var sectionValue))
            {
                return Result.Failure<ConfigurationSectionDto>(
                    Error.NotFound($"Configuration section '{sectionName}' not found.", "NOIR-CFG-004"));
            }

            var requiresRestart = ConfigurationManagementSettings.RequiresRestart
                .GetValueOrDefault(sectionName, false);

            var section = new ConfigurationSectionDto(
                Name: sectionName,
                DisplayName: FormatDisplayName(sectionName),
                IsAllowed: true,
                RequiresRestart: requiresRestart,
                CurrentValueJson: sectionValue.GetRawText());

            return Result<ConfigurationSectionDto>.Success(section);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get configuration section {SectionName}", sectionName);
            return Result.Failure<ConfigurationSectionDto>(
                Error.Failure("NOIR-CFG-001", "Failed to load configuration section."));
        }
    }

    public async Task<Result<ConfigurationBackupDto>> UpdateSectionAsync(
        string sectionName,
        string newValueJson,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var settingsValue = _settings.CurrentValue;

        // Check if runtime changes are enabled
        if (!settingsValue.EnableRuntimeChanges)
        {
            return Result.Failure<ConfigurationBackupDto>(
                Error.Forbidden("Runtime configuration changes are disabled.", "NOIR-CFG-001"));
        }

        // Check if section is allowed
        if (!settingsValue.AllowedSections.Contains(sectionName))
        {
            return Result.Failure<ConfigurationBackupDto>(
                Error.Forbidden($"Configuration section '{sectionName}' is not allowed for editing.", "NOIR-CFG-002"));
        }

        try
        {
            // Validate new JSON value
            JsonDocument.Parse(newValueJson);

            // Read current appsettings.json
            var currentJson = await File.ReadAllTextAsync(AppSettingsPath, cancellationToken);

            // Create backup BEFORE making changes
            var backupId = $"{DateTimeOffset.UtcNow:yyyyMMddTHHmmssZ}_{SanitizeUserId(userId)}";
            var backupPath = Path.Combine(BackupDirectory, $"appsettings.{backupId}.json");
            await File.WriteAllTextAsync(backupPath, currentJson, cancellationToken);

            _logger.LogInformation("Created configuration backup: {BackupId}", backupId);

            // Parse and modify the configuration
            using var document = JsonDocument.Parse(currentJson);
            var modifiedJson = MergeSection(document, sectionName, newValueJson);

            // Validate the complete JSON structure
            JsonDocument.Parse(modifiedJson);

            // Atomic write using temp file
            var tempPath = AppSettingsPath + ".tmp";
            await File.WriteAllTextAsync(tempPath, modifiedJson, cancellationToken);

            // Validate temp file before moving
            JsonDocument.Parse(await File.ReadAllTextAsync(tempPath, cancellationToken));

            // Atomic move (overwrites destination)
            File.Move(tempPath, AppSettingsPath, overwrite: true);

            _logger.LogInformation(
                "Configuration section '{SectionName}' updated successfully by {UserId}",
                sectionName, userId);

            // Cleanup old backups
            await CleanupOldBackupsAsync(settingsValue.BackupRetentionCount, cancellationToken);

            // Return backup DTO
            var backupInfo = new FileInfo(backupPath);
            var backup = new ConfigurationBackupDto(
                Id: backupId,
                CreatedAt: backupInfo.CreationTimeUtc,
                CreatedBy: userId,
                FilePath: backupPath,
                SizeBytes: backupInfo.Length);

            // Note: Auto-reload happens automatically via reloadOnChange: true in Program.cs
            // IConfigurationRoot detects file change → IOptionsMonitor<T> notifies subscribers
            // Timeline: 1-2 seconds for changes to propagate

            return Result<ConfigurationBackupDto>.Success(backup);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid JSON in configuration update");
            return Result.Failure<ConfigurationBackupDto>(
                Error.Validation("newValueJson", "Invalid JSON format.", "NOIR-CFG-003"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update configuration section {SectionName}", sectionName);
            return Result.Failure<ConfigurationBackupDto>(
                Error.Failure("NOIR-CFG-005", "Failed to update configuration."));
        }
    }

    public async Task<Result<IEnumerable<ConfigurationBackupDto>>> GetBackupsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var backupFiles = Directory.GetFiles(BackupDirectory, "appsettings.*.json")
                .Select(path => new FileInfo(path))
                .OrderByDescending(f => f.CreationTimeUtc)
                .Select(f =>
                {
                    // Extract backup ID from filename (appsettings.{backupId}.json)
                    var fileName = Path.GetFileNameWithoutExtension(f.Name);
                    var backupId = fileName.Replace("appsettings.", "");

                    // Parse timestamp and user from backup ID (yyyyMMddTHHmmssZ_userId)
                    var parts = backupId.Split('_', 2);
                    var createdBy = parts.Length > 1 ? parts[1] : "unknown";

                    return new ConfigurationBackupDto(
                        Id: backupId,
                        CreatedAt: f.CreationTimeUtc,
                        CreatedBy: createdBy,
                        FilePath: f.FullName,
                        SizeBytes: f.Length);
                })
                .ToList();

            return Result<IEnumerable<ConfigurationBackupDto>>.Success(backupFiles.AsEnumerable());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get configuration backups");
            return Result.Failure<IEnumerable<ConfigurationBackupDto>>(
                Error.Failure("NOIR-CFG-006", "Failed to load backup list."));
        }
    }

    public async Task<Result> RollbackAsync(
        string backupId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var settingsValue = _settings.CurrentValue;

        // Check if runtime changes are enabled
        if (!settingsValue.EnableRuntimeChanges)
        {
            return Result.Failure(
                Error.Forbidden("Runtime configuration changes are disabled.", "NOIR-CFG-001"));
        }

        try
        {
            var backupPath = Path.Combine(BackupDirectory, $"appsettings.{backupId}.json");

            if (!File.Exists(backupPath))
            {
                return Result.Failure(
                    Error.NotFound("Backup file not found.", "NOIR-CFG-007"));
            }

            // Read and validate backup file
            var backupJson = await File.ReadAllTextAsync(backupPath, cancellationToken);
            JsonDocument.Parse(backupJson); // Validate JSON structure

            // Create pre-rollback backup
            var currentJson = await File.ReadAllTextAsync(AppSettingsPath, cancellationToken);
            var preRollbackId = $"{DateTimeOffset.UtcNow:yyyyMMddTHHmmssZ}_pre-rollback_{SanitizeUserId(userId)}";
            var preRollbackPath = Path.Combine(BackupDirectory, $"appsettings.{preRollbackId}.json");
            await File.WriteAllTextAsync(preRollbackPath, currentJson, cancellationToken);

            _logger.LogInformation("Created pre-rollback backup: {BackupId}", preRollbackId);

            // Atomic restore from backup
            var tempPath = AppSettingsPath + ".tmp";
            await File.WriteAllTextAsync(tempPath, backupJson, cancellationToken);

            // Validate temp file
            JsonDocument.Parse(await File.ReadAllTextAsync(tempPath, cancellationToken));

            // Atomic move
            File.Move(tempPath, AppSettingsPath, overwrite: true);

            _logger.LogInformation(
                "Configuration rolled back to {BackupId} by {UserId}",
                backupId, userId);

            return Result.Success();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Corrupted backup file: {BackupId}", backupId);
            return Result.Failure(
                Error.Validation("backupId", "Backup file is corrupted.", "NOIR-CFG-008"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rollback configuration to {BackupId}", backupId);
            return Result.Failure(
                Error.Failure("NOIR-CFG-009", "Failed to rollback configuration."));
        }
    }

    // Helper: Merge a section into the configuration JSON
    private string MergeSection(JsonDocument document, string sectionName, string newValueJson)
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

        writer.WriteStartObject();

        // Copy all sections, replacing the target section
        foreach (var property in document.RootElement.EnumerateObject())
        {
            if (property.Name == sectionName)
            {
                // Write new value
                writer.WritePropertyName(sectionName);
                using var newValueDoc = JsonDocument.Parse(newValueJson);
                newValueDoc.RootElement.WriteTo(writer);
            }
            else
            {
                // Copy existing value
                property.WriteTo(writer);
            }
        }

        writer.WriteEndObject();
        writer.Flush();

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    // Helper: Cleanup old backup files
    private async Task CleanupOldBackupsAsync(int retentionCount, CancellationToken cancellationToken)
    {
        try
        {
            var backupFiles = Directory.GetFiles(BackupDirectory, "appsettings.*.json")
                .Select(path => new FileInfo(path))
                .OrderByDescending(f => f.CreationTimeUtc)
                .Skip(retentionCount)
                .ToList();

            foreach (var file in backupFiles)
            {
                file.Delete();
                _logger.LogDebug("Deleted old backup: {FileName}", file.Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cleanup old backups");
            // Don't fail the operation if cleanup fails
        }
    }

    // Helper: Format section name for display (e.g., "DeveloperLogs" → "Developer Logs")
    private static string FormatDisplayName(string sectionName)
    {
        return System.Text.RegularExpressions.Regex.Replace(
            sectionName,
            "([a-z])([A-Z])",
            "$1 $2");
    }

    // Helper: Sanitize user ID for filename (remove special characters)
    private static string SanitizeUserId(string userId)
    {
        return userId.Replace("@", "-at-").Replace(".", "-");
    }
}
