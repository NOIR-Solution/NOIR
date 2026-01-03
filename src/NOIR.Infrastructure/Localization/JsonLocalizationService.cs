using System.Globalization;
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
        _logger.LogDebug("Localization: Getting key '{Key}' for culture '{Culture}'", key, culture);

        var value = GetLocalizedValue(key, culture);
        _logger.LogDebug("Localization: Value for '{Key}' in '{Culture}': {Value}", key, culture, value ?? "(null)");

        // Fallback to default culture if enabled and key not found
        if (value == null && _settings.FallbackToDefaultCulture && culture != _settings.DefaultCulture)
        {
            _logger.LogDebug("Localization: Falling back to default culture '{DefaultCulture}'", _settings.DefaultCulture);
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
        {
            _logger.LogDebug("Localization: No HttpContext, using default culture '{Culture}'", _settings.DefaultCulture);
            return _settings.DefaultCulture;
        }

        // Priority 1: Query parameter (for testing/debugging)
        if (httpContext.Request.Query.TryGetValue(LanguageQueryParam, out var queryLang))
        {
            var lang = queryLang.ToString().ToLowerInvariant();
            if (_settings.SupportedCultures.Contains(lang))
            {
                _logger.LogDebug("Localization: Culture from query param: '{Culture}'", lang);
                return lang;
            }
        }

        // Priority 2: Accept-Language header (explicitly set by frontend apiClient)
        // This takes priority over cookie because it's set fresh with each request,
        // while cookies may have stale values from previous sessions
        var acceptLanguage = httpContext.Request.Headers[AcceptLanguageHeader].FirstOrDefault();
        _logger.LogDebug("Localization: Accept-Language header: '{Header}'", acceptLanguage ?? "(not set)");
        if (!string.IsNullOrEmpty(acceptLanguage))
        {
            var preferredLanguages = ParseAcceptLanguageHeader(acceptLanguage);
            _logger.LogDebug("Localization: Parsed languages: [{Languages}]", string.Join(", ", preferredLanguages));
            foreach (var lang in preferredLanguages)
            {
                // Try exact match first (e.g., "en-US")
                if (_settings.SupportedCultures.Contains(lang))
                {
                    _logger.LogDebug("Localization: Culture from Accept-Language (exact): '{Culture}'", lang);
                    return lang;
                }

                // Try language code without region (e.g., "en" from "en-US")
                var langCode = lang.Split('-')[0];
                if (_settings.SupportedCultures.Contains(langCode))
                {
                    _logger.LogDebug("Localization: Culture from Accept-Language (prefix): '{Culture}'", langCode);
                    return langCode;
                }
            }
        }

        // Priority 3: Cookie (fallback for direct page loads without explicit Accept-Language)
        if (httpContext.Request.Cookies.TryGetValue(LanguageCookie, out var cookieLang))
        {
            var lang = cookieLang.ToLowerInvariant();
            if (_settings.SupportedCultures.Contains(lang))
            {
                _logger.LogDebug("Localization: Culture from cookie: '{Culture}'", lang);
                return lang;
            }
        }

        // Priority 4: Default culture
        _logger.LogDebug("Localization: Using default culture '{Culture}'", _settings.DefaultCulture);
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
        var resourcePath = LocalizationResourceHelper.GetCultureResourcePath(
            _environment.ContentRootPath,
            _settings.ResourcesPath,
            culture);

        return LocalizationResourceHelper.LoadResourcesFromDirectory(resourcePath, _logger);
    }

    private static string? NavigateToValue(Dictionary<string, object> resources, string key)
    {
        return LocalizationResourceHelper.NavigateToValue(resources, key);
    }
}
