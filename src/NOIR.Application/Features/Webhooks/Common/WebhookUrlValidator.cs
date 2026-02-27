namespace NOIR.Application.Features.Webhooks.Common;

/// <summary>
/// Validates webhook URLs to prevent SSRF attacks by blocking private/internal addresses.
/// </summary>
public static class WebhookUrlValidator
{
    private static readonly string[] BlockedHostnameSuffixes =
    [
        ".internal",
        ".local",
        ".localhost"
    ];

    private static readonly string[] BlockedHostnames =
    [
        "localhost"
    ];

    /// <summary>
    /// Returns true if the URL targets a blocked (private/internal) address.
    /// Blocks: loopback, RFC 1918, link-local, cloud metadata, and internal hostnames.
    /// </summary>
    public static bool IsBlockedUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return true;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return true;

        var host = uri.Host.ToLowerInvariant();

        // Block known internal hostnames
        if (BlockedHostnames.Contains(host))
            return true;

        // Block internal hostname suffixes
        if (BlockedHostnameSuffixes.Any(suffix => host.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)))
            return true;

        // Resolve and check IP addresses
        if (IPAddress.TryParse(host, out var ipAddress))
        {
            return IsBlockedIpAddress(ipAddress);
        }

        // For hostnames, try DNS resolution to catch SSRF via DNS rebinding
        try
        {
            var addresses = Dns.GetHostAddresses(host);
            return addresses.Any(IsBlockedIpAddress);
        }
        catch
        {
            // DNS resolution failure — block to be safe
            return true;
        }
    }

    private static bool IsBlockedIpAddress(IPAddress address)
    {
        // Normalize IPv6-mapped IPv4 (e.g., ::ffff:127.0.0.1 → 127.0.0.1)
        if (address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();

        // Loopback (127.0.0.0/8, ::1)
        if (IPAddress.IsLoopback(address))
            return true;

        // IPv6 link-local
        if (address.IsIPv6LinkLocal || address.IsIPv6SiteLocal)
            return true;

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = address.GetAddressBytes();

            // 10.0.0.0/8
            if (bytes[0] == 10)
                return true;

            // 172.16.0.0/12
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                return true;

            // 192.168.0.0/16
            if (bytes[0] == 192 && bytes[1] == 168)
                return true;

            // 169.254.0.0/16 (link-local, including cloud metadata 169.254.169.254)
            if (bytes[0] == 169 && bytes[1] == 254)
                return true;

            // 0.0.0.0/8
            if (bytes[0] == 0)
                return true;
        }

        return false;
    }
}
