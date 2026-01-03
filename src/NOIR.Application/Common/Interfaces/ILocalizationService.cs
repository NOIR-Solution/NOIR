namespace NOIR.Application.Common.Interfaces;

/// <summary>
/// Provides localization services for translating strings based on the current culture.
/// </summary>
public interface ILocalizationService
{
    /// <summary>
    /// Gets a localized string for the specified key.
    /// </summary>
    /// <param name="key">The localization key (e.g., "errors.validation.required").</param>
    /// <returns>The localized string, or the key itself if not found.</returns>
    string this[string key] { get; }

    /// <summary>
    /// Gets a localized string for the specified key.
    /// </summary>
    /// <param name="key">The localization key.</param>
    /// <returns>The localized string, or the key itself if not found.</returns>
    string Get(string key);

    /// <summary>
    /// Gets a localized string for the specified key with format arguments.
    /// </summary>
    /// <param name="key">The localization key.</param>
    /// <param name="args">The format arguments to interpolate into the string.</param>
    /// <returns>The formatted localized string.</returns>
    string Get(string key, params object[] args);

    /// <summary>
    /// Gets the current culture code (e.g., "en", "vi").
    /// </summary>
    string CurrentCulture { get; }

    /// <summary>
    /// Gets all supported culture codes.
    /// </summary>
    IReadOnlyList<string> SupportedCultures { get; }
}
