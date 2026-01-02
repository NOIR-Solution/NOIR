namespace NOIR.Application.Common.Interfaces;

/// <summary>
/// Service for generating device fingerprints for token binding.
/// </summary>
public interface IDeviceFingerprintService
{
    /// <summary>
    /// Generates a fingerprint based on HTTP context.
    /// </summary>
    string? GenerateFingerprint();

    /// <summary>
    /// Gets the client IP address.
    /// </summary>
    string? GetClientIpAddress();

    /// <summary>
    /// Gets the user agent string.
    /// </summary>
    string? GetUserAgent();

    /// <summary>
    /// Generates a device name from the user agent.
    /// </summary>
    string? GetDeviceName();
}
