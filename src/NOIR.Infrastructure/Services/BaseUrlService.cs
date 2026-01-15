using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using NOIR.Application.Common.Interfaces;

namespace NOIR.Infrastructure.Services;

/// <summary>
/// Service that provides the application's base URL.
/// Auto-detects from HttpContext when available, falls back to configuration.
/// </summary>
public class BaseUrlService : IBaseUrlService, IScopedService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ApplicationSettings _settings;

    public BaseUrlService(
        IHttpContextAccessor httpContextAccessor,
        IOptions<ApplicationSettings> settings)
    {
        _httpContextAccessor = httpContextAccessor;
        _settings = settings.Value;
    }

    /// <inheritdoc />
    public string GetBaseUrl()
    {
        var httpContext = _httpContextAccessor.HttpContext;

        // If we have an HTTP context, build URL from the request
        if (httpContext?.Request != null)
        {
            var request = httpContext.Request;
            return $"{request.Scheme}://{request.Host}";
        }

        // Fall back to configured URL for background jobs
        if (!string.IsNullOrEmpty(_settings.BaseUrl))
        {
            return _settings.BaseUrl.TrimEnd('/');
        }

        // Last resort fallback
        return "https://localhost";
    }

    /// <inheritdoc />
    public string BuildUrl(string relativePath)
    {
        var baseUrl = GetBaseUrl();
        
        if (string.IsNullOrEmpty(relativePath))
            return baseUrl;

        // Ensure path starts with /
        if (!relativePath.StartsWith('/'))
            relativePath = "/" + relativePath;

        return baseUrl + relativePath;
    }
}

/// <summary>
/// General application settings.
/// </summary>
public class ApplicationSettings
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Application";

    /// <summary>
    /// Base URL of the application (e.g., "https://noir.example.com").
    /// Used for generating links in emails when no HTTP context is available.
    /// </summary>
    public string? BaseUrl { get; set; }

    /// <summary>
    /// Application name for display in emails and UI.
    /// </summary>
    public string ApplicationName { get; set; } = "NOIR";
}
