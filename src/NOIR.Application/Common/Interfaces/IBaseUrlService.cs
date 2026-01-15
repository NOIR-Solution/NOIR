namespace NOIR.Application.Common.Interfaces;

/// <summary>
/// Service for retrieving the application's base URL.
/// During HTTP requests, auto-detects from HttpContext.
/// For background jobs, falls back to configuration.
/// </summary>
public interface IBaseUrlService
{
    /// <summary>
    /// Gets the application's base URL (e.g., "https://noir.example.com").
    /// Returns URL without trailing slash.
    /// </summary>
    string GetBaseUrl();

    /// <summary>
    /// Builds a full URL by combining the base URL with the given path.
    /// </summary>
    /// <param name="relativePath">Path starting with / (e.g., "/login")</param>
    /// <returns>Full URL (e.g., "https://noir.example.com/login")</returns>
    string BuildUrl(string relativePath);
}
