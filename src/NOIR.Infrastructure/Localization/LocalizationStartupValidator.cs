using System.Text.Json;

namespace NOIR.Infrastructure.Localization;

/// <summary>
/// Hosted service that validates localization resource files at application startup.
/// Ensures resource directories exist, JSON files are parseable, and critical keys are present.
/// </summary>
public class LocalizationStartupValidator : IHostedService
{
    private readonly LocalizationSettings _settings;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<LocalizationStartupValidator> _logger;

    /// <summary>
    /// Critical keys that must exist in the default culture.
    /// These are essential for the application to function correctly.
    /// </summary>
    private static readonly string[] CriticalKeys =
    [
        "validation.email.required",
        "validation.password.required",
        "auth.login.invalidCredentials",
        "auth.user.notFound",
        "auth.role.notFound"
    ];

    public LocalizationStartupValidator(
        IOptions<LocalizationSettings> settings,
        IHostEnvironment environment,
        ILogger<LocalizationStartupValidator> logger)
    {
        _settings = settings.Value;
        _environment = environment;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var basePath = Path.Combine(_environment.ContentRootPath, _settings.ResourcesPath);
        var errors = new List<string>();

        _logger.LogInformation("Validating localization resources at {Path}", basePath);

        // Validate base resources directory exists
        if (!Directory.Exists(basePath))
        {
            var error = $"Localization resources directory not found: {basePath}";
            errors.Add(error);
            _logger.LogError("Localization validation failed: {Error}", error);

            // Critical failure - can't continue without resources directory
            if (_environment.IsProduction())
            {
                throw new InvalidOperationException(
                    $"Localization startup validation failed: {error}");
            }

            _logger.LogWarning("Continuing despite missing resources directory (non-production environment)");
            return Task.CompletedTask;
        }

        // Validate each supported culture
        foreach (var culture in _settings.SupportedCultures)
        {
            var culturePath = Path.Combine(basePath, culture);

            if (!Directory.Exists(culturePath))
            {
                errors.Add($"Resource directory not found for culture '{culture}': {culturePath}");
                _logger.LogWarning("Resource directory not found for culture '{Culture}': {Path}", culture, culturePath);
                continue;
            }

            // Validate JSON files in the culture directory
            var jsonFiles = Directory.GetFiles(culturePath, "*.json");
            if (jsonFiles.Length == 0)
            {
                errors.Add($"No JSON resource files found for culture '{culture}'");
                _logger.LogWarning("No JSON resource files found for culture '{Culture}' at {Path}", culture, culturePath);
                continue;
            }

            foreach (var file in jsonFiles)
            {
                ValidateJsonFile(file, culture, errors);
            }
        }

        // Validate critical keys exist in default culture
        ValidateCriticalKeys(basePath, errors);

        // Log summary
        if (errors.Count > 0)
        {
            _logger.LogWarning(
                "Localization validation completed with {ErrorCount} warning(s). " +
                "Application will continue but some translations may be missing.",
                errors.Count);

            // In production, fail fast on critical errors
            if (_environment.IsProduction() && HasCriticalErrors(errors))
            {
                throw new InvalidOperationException(
                    $"Localization startup validation failed with critical errors: {string.Join("; ", errors.Take(5))}");
            }
        }
        else
        {
            _logger.LogInformation(
                "Localization validation completed successfully. " +
                "Validated {CultureCount} culture(s): {Cultures}",
                _settings.SupportedCultures.Count,
                string.Join(", ", _settings.SupportedCultures));
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private void ValidateJsonFile(string filePath, string culture, List<string> errors)
    {
        var fileName = Path.GetFileName(filePath);

        try
        {
            var content = File.ReadAllText(filePath);

            if (string.IsNullOrWhiteSpace(content))
            {
                errors.Add($"Empty JSON file: {fileName} ({culture})");
                _logger.LogWarning("Empty JSON file detected: {File} for culture '{Culture}'", filePath, culture);
                return;
            }

            // Try to parse the JSON using shared options
            using var document = JsonDocument.Parse(content, LocalizationResourceHelper.JsonDocumentOptions);

            // Validate it's an object (not an array or primitive)
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                errors.Add($"Invalid JSON structure in {fileName} ({culture}): root must be an object");
                _logger.LogWarning(
                    "Invalid JSON structure in {File} for culture '{Culture}': expected object, got {Kind}",
                    filePath, culture, document.RootElement.ValueKind);
                return;
            }

            // Count keys for logging
            var keyCount = CountKeys(document.RootElement);
            _logger.LogDebug(
                "Validated {File} for culture '{Culture}': {KeyCount} translation key(s)",
                fileName, culture, keyCount);
        }
        catch (JsonException ex)
        {
            errors.Add($"Invalid JSON in {fileName} ({culture}): {ex.Message}");
            _logger.LogError(ex, "Failed to parse JSON file {File} for culture '{Culture}'", filePath, culture);
        }
        catch (IOException ex)
        {
            errors.Add($"Failed to read {fileName} ({culture}): {ex.Message}");
            _logger.LogError(ex, "Failed to read JSON file {File} for culture '{Culture}'", filePath, culture);
        }
    }

    private void ValidateCriticalKeys(string basePath, List<string> errors)
    {
        var defaultCulturePath = Path.Combine(basePath, _settings.DefaultCulture);

        if (!Directory.Exists(defaultCulturePath))
        {
            errors.Add($"Default culture directory not found: {_settings.DefaultCulture}");
            return;
        }

        // Load all resources for the default culture using shared helper
        var resources = LocalizationResourceHelper.LoadResourcesFromDirectory(defaultCulturePath);

        foreach (var key in CriticalKeys)
        {
            if (!LocalizationResourceHelper.KeyExists(resources, key))
            {
                errors.Add($"Critical translation key missing in default culture: {key}");
                _logger.LogWarning(
                    "Critical translation key '{Key}' is missing in default culture '{Culture}'",
                    key, _settings.DefaultCulture);
            }
        }
    }

    private static int CountKeys(JsonElement element, int depth = 0)
    {
        if (depth > 10) return 0; // Prevent infinite recursion

        var count = 0;

        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in element.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.String)
                {
                    count++;
                }
                else if (prop.Value.ValueKind == JsonValueKind.Object)
                {
                    count += CountKeys(prop.Value, depth + 1);
                }
            }
        }

        return count;
    }

    private static bool HasCriticalErrors(List<string> errors)
    {
        // Critical errors are those that would prevent basic functionality
        return errors.Any(e =>
            e.Contains("Default culture directory not found") ||
            e.Contains("Critical translation key missing") ||
            e.Contains("Resource directory not found for culture") && e.Contains("en"));
    }
}
