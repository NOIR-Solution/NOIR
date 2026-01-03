using System.Text.Json;

namespace NOIR.Infrastructure.Localization;

/// <summary>
/// Shared utilities for loading and navigating JSON localization resources.
/// Used by both JsonLocalizationService and LocalizationStartupValidator.
/// </summary>
internal static class LocalizationResourceHelper
{
    /// <summary>
    /// Standard JSON serializer options for localization files.
    /// Supports comments and trailing commas for developer-friendly JSON.
    /// </summary>
    public static JsonSerializerOptions JsonSerializerOptions { get; } = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <summary>
    /// Standard JSON document options for validation parsing.
    /// </summary>
    public static JsonDocumentOptions JsonDocumentOptions { get; } = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <summary>
    /// Constructs the full path to a culture's resource directory.
    /// </summary>
    public static string GetCultureResourcePath(
        string contentRootPath,
        string resourcesPath,
        string culture)
    {
        return Path.Combine(contentRootPath, resourcesPath, culture);
    }

    /// <summary>
    /// Navigates through a nested dictionary/JsonElement structure to find a value.
    /// Supports dot-notation keys (e.g., "validation.email.required").
    /// </summary>
    /// <param name="resources">Root resource dictionary</param>
    /// <param name="key">Dot-notation key to navigate</param>
    /// <returns>The string value if found, null otherwise</returns>
    public static string? NavigateToValue(Dictionary<string, object> resources, string key)
    {
        var parts = key.Split('.');
        object? current = resources;

        foreach (var part in parts)
        {
            if (current is Dictionary<string, object> dict)
            {
                if (!dict.TryGetValue(part, out current))
                    return null;
            }
            else if (current is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.Object)
                {
                    if (!jsonElement.TryGetProperty(part, out var nextElement))
                    {
                        // Try case-insensitive search
                        var found = false;
                        foreach (var prop in jsonElement.EnumerateObject())
                        {
                            if (string.Equals(prop.Name, part, StringComparison.OrdinalIgnoreCase))
                            {
                                current = prop.Value;
                                found = true;
                                break;
                            }
                        }
                        if (!found)
                            return null;
                    }
                    else
                    {
                        current = nextElement;
                    }
                }
                else if (jsonElement.ValueKind == JsonValueKind.String)
                {
                    return jsonElement.GetString();
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        // Final value should be a string
        if (current is string strValue)
            return strValue;

        if (current is JsonElement finalElement && finalElement.ValueKind == JsonValueKind.String)
            return finalElement.GetString();

        return null;
    }

    /// <summary>
    /// Checks if a key exists in the resource dictionary and points to a string value.
    /// </summary>
    public static bool KeyExists(Dictionary<string, object> resources, string key)
    {
        return NavigateToValue(resources, key) != null;
    }

    /// <summary>
    /// Loads all JSON resource files from a culture directory into a dictionary.
    /// File names become the top-level keys (namespaces).
    /// </summary>
    public static Dictionary<string, object> LoadResourcesFromDirectory(
        string culturePath,
        ILogger? logger = null)
    {
        var resources = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        if (!Directory.Exists(culturePath))
        {
            logger?.LogWarning("Localization resource path not found: {Path}", culturePath);
            return resources;
        }

        foreach (var file in Directory.GetFiles(culturePath, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var fileResources = JsonSerializer.Deserialize<Dictionary<string, object>>(
                    json,
                    JsonSerializerOptions);

                if (fileResources != null)
                {
                    // Use the file name (without extension) as the namespace prefix
                    var fileName = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();

                    // Create or get the namespace dictionary for this file
                    if (!resources.TryGetValue(fileName, out var existingNamespace))
                    {
                        existingNamespace = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                        resources[fileName] = existingNamespace;
                    }

                    // Add all entries (except _metadata) to the namespace
                    if (existingNamespace is Dictionary<string, object> namespaceDict)
                    {
                        foreach (var kvp in fileResources.Where(kvp => kvp.Key != "_metadata"))
                        {
                            namespaceDict[kvp.Key] = kvp.Value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to load localization file: {File}", file);
            }
        }

        return resources;
    }
}
