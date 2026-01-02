namespace NOIR.Infrastructure.Services;

/// <summary>
/// Service for generating device fingerprints for token binding.
/// </summary>
public class DeviceFingerprintService : IDeviceFingerprintService, IScopedService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DeviceFingerprintService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? GenerateFingerprint()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context is null)
        {
            return null;
        }

        var components = new List<string>
        {
            context.Request.Headers.UserAgent.ToString(),
            context.Request.Headers.AcceptLanguage.ToString(),
            context.Request.Headers.AcceptEncoding.ToString(),
            GetClientIpAddress() ?? "unknown"
        };

        var combined = string.Join("|", components);

        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
        return Convert.ToBase64String(hash);
    }

    public string? GetClientIpAddress()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context is null)
        {
            return null;
        }

        // Check for forwarded headers (reverse proxy)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            // Take the first IP (client IP)
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return context.Connection.RemoteIpAddress?.MapToIPv4().ToString();
    }

    public string? GetUserAgent()
    {
        return _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString();
    }

    public string? GetDeviceName()
    {
        var userAgent = GetUserAgent();
        if (string.IsNullOrEmpty(userAgent))
        {
            return null;
        }

        // Simple device name extraction from user agent
        if (userAgent.Contains("iPhone"))
            return "iPhone";
        if (userAgent.Contains("iPad"))
            return "iPad";
        if (userAgent.Contains("Android"))
            return "Android Device";
        if (userAgent.Contains("Windows"))
            return "Windows PC";
        if (userAgent.Contains("Mac"))
            return "Mac";
        if (userAgent.Contains("Linux"))
            return "Linux PC";

        return "Unknown Device";
    }
}
