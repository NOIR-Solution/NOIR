namespace NOIR.Infrastructure.Localization;

/// <summary>
/// Configuration settings for the localization system.
/// </summary>
public class LocalizationSettings
{
    public const string SectionName = "Localization";

    /// <summary>
    /// The default language code to use when no language preference is detected.
    /// </summary>
    public string DefaultCulture { get; set; } = "en";

    /// <summary>
    /// List of supported culture codes (e.g., ["en", "vi"]).
    /// </summary>
    public List<string> SupportedCultures { get; set; } = ["en", "vi"];

    /// <summary>
    /// Path to the localization resources folder, relative to the application root.
    /// </summary>
    public string ResourcesPath { get; set; } = "Resources/Localization";

    /// <summary>
    /// Whether to fall back to the default culture when a key is not found.
    /// </summary>
    public bool FallbackToDefaultCulture { get; set; } = true;

    /// <summary>
    /// Whether to cache localization resources in memory.
    /// </summary>
    public bool EnableCaching { get; set; } = true;

    /// <summary>
    /// Cache duration in minutes when EnableCaching is true.
    /// </summary>
    public int CacheDurationMinutes { get; set; } = 60;
}
