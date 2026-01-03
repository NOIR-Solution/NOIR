using System.Globalization;
using System.Text.Json;
using NOIR.Application.Common.Interfaces;

namespace NOIR.Infrastructure.Localization;

/// <summary>
/// JSON-based localization service that loads translations from JSON files.
/// Supports nested keys (e.g., "common.buttons.save") and culture fallback.
/// </summary>
public class JsonLocalizationService : ILocalizationService, IScopedService
{
    private readonly LocalizationSettings _settings;
    private readonly IMemoryCache _cache;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<JsonLocalizationService> _logger;
    private readonly IHostEnvironment _environment;

    private const string CacheKeyPrefix = "loc_";
    private const string AcceptLanguageHeader = "Accept-Language";
    private const string LanguageQueryParam = "lang";
    private const string LanguageCookie = "noir-language";

    public JsonLocalizationService(
        IOptions<LocalizationSettings> settings,
        IMemoryCache cache,
        IHttpContextAccessor httpContextAccessor,
        ILogger<JsonLocalizationService> logger,
        IHostEnvironment environment)
    {
        _settings = settings.Value;
        _cache = cache;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _environment = environment;
    }

    public string this[string key] => Get(key);

    public string CurrentCulture => GetCurrentCulture();

    public IReadOnlyList<string> SupportedCultures => _settings.SupportedCultures.AsReadOnly();

    public string Get(string key)
    {
        if (string.IsNullOrEmpty(key))
            return key;

        var culture = GetCurrentCulture();
        var value = GetLocalizedValue(key, culture);

        // Fallback to default culture if enabled and key not found
        if (value == null && _settings.FallbackToDefaultCulture && culture != _settings.DefaultCulture)
        {
            value = GetLocalizedValue(key, _settings.DefaultCulture);
        }

        // Return key if not found (standard i18n behavior)
        return value ?? key;
    }

    public string Get(string key, params object[] args)
    {
        var value = Get(key);

        if (args.Length == 0 || value == key)
            return value;

        try
        {
            return string.Format(value, args);
        }
        catch (FormatException ex)
        {
            _logger.LogWarning(ex, "Failed to format localized string for key: {Key}", key);
            return value;
        }
    }

    private string GetCurrentCulture()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
            return _settings.DefaultCulture;

        // Priority 1: Query parameter (for testing/debugging)
        if (httpContext.Request.Query.TryGetValue(LanguageQueryParam, out var queryLang))
        {
            var lang = queryLang.ToString().ToLowerInvariant();
            if (_settings.SupportedCultures.Contains(lang))
                return lang;
        }

        // Priority 2: Cookie (user's saved preference)
        if (httpContext.Request.Cookies.TryGetValue(LanguageCookie, out var cookieLang))
        {
            var lang = cookieLang.ToLowerInvariant();
            if (_settings.SupportedCultures.Contains(lang))
                return lang;
        }

        // Priority 3: Accept-Language header
        var acceptLanguage = httpContext.Request.Headers[AcceptLanguageHeader].FirstOrDefault();
        if (!string.IsNullOrEmpty(acceptLanguage))
        {
            var preferredLanguages = ParseAcceptLanguageHeader(acceptLanguage);
            foreach (var lang in preferredLanguages)
            {
                // Try exact match first (e.g., "en-US")
                if (_settings.SupportedCultures.Contains(lang))
                    return lang;

                // Try language code without region (e.g., "en" from "en-US")
                var langCode = lang.Split('-')[0];
                if (_settings.SupportedCultures.Contains(langCode))
                    return langCode;
            }
        }

        // Priority 4: Default culture
        return _settings.DefaultCulture;
    }

    private static List<string> ParseAcceptLanguageHeader(string acceptLanguage)
    {
        return acceptLanguage
            .Split(',')
            .Select(lang =>
            {
                var parts = lang.Trim().Split(';');
                var langCode = parts[0].Trim().ToLowerInvariant();
                var quality = 1.0;

                if (parts.Length > 1)
                {
                    var qPart = parts[1].Trim();
                    if (qPart.StartsWith("q=", StringComparison.OrdinalIgnoreCase))
                    {
                        double.TryParse(qPart[2..], NumberStyles.Float, CultureInfo.InvariantCulture, out quality);
                    }
                }

                return (Lang: langCode, Quality: quality);
            })
            .OrderByDescending(x => x.Quality)
            .Select(x => x.Lang)
            .ToList();
    }

    private string? GetLocalizedValue(string key, string culture)
    {
        var resources = GetResourcesForCulture(culture);
        if (resources == null)
            return null;

        return NavigateToValue(resources, key);
    }

    private Dictionary<string, object>? GetResourcesForCulture(string culture)
    {
        var cacheKey = $"{CacheKeyPrefix}{culture}";

        return _cache.GetOrCreate(cacheKey, entry =>
        {
            // When caching is disabled, use minimal expiration to effectively bypass cache
            // while still benefiting from IMemoryCache's thread-safety
            if (_settings.EnableCaching)
            {
                entry.SlidingExpiration = TimeSpan.FromMinutes(_settings.CacheDurationMinutes);
            }
            else
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMilliseconds(1);
            }
            return LoadResourcesFromFiles(culture);
        });
    }

    private Dictionary<string, object> LoadResourcesFromFiles(string culture)
    {
        var resources = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        var resourcePath = Path.Combine(_environment.ContentRootPath, _settings.ResourcesPath, culture);

        if (!Directory.Exists(resourcePath))
        {
            _logger.LogWarning("Localization resource path not found: {Path}", resourcePath);
            return resources;
        }

        foreach (var file in Directory.GetFiles(resourcePath, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var fileResources = JsonSerializer.Deserialize<Dictionary<string, object>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                });

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
                _logger.LogError(ex, "Failed to load localization file: {File}", file);
            }
        }

        return resources;
    }

    private static string? NavigateToValue(Dictionary<string, object> resources, string key)
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
}
