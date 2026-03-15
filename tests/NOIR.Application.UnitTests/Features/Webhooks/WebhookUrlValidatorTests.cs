using NOIR.Application.Features.Webhooks.Common;

namespace NOIR.Application.UnitTests.Features.Webhooks;

/// <summary>
/// Unit tests for WebhookUrlValidator.
/// Tests SSRF protection by validating that private, internal, and loopback addresses are blocked.
/// </summary>
public class WebhookUrlValidatorTests
{
    #region Loopback Addresses

    [Fact]
    public void IsBlockedUrl_WithLoopbackIp_ShouldReturnTrue()
    {
        // Assert
        WebhookUrlValidator.IsBlockedUrl("https://127.0.0.1/hook").ShouldBe(true);
    }

    [Fact]
    public void IsBlockedUrl_WithLocalhost_ShouldReturnTrue()
    {
        // Assert
        WebhookUrlValidator.IsBlockedUrl("https://localhost/hook").ShouldBe(true);
    }

    [Fact]
    public void IsBlockedUrl_WithIpv6Loopback_ShouldReturnTrue()
    {
        // Assert
        WebhookUrlValidator.IsBlockedUrl("https://[::1]/hook").ShouldBe(true);
    }

    #endregion

    #region Private IP Ranges (RFC 1918)

    [Fact]
    public void IsBlockedUrl_WithPrivate10Network_ShouldReturnTrue()
    {
        // 10.0.0.0/8
        WebhookUrlValidator.IsBlockedUrl("https://10.0.0.1/hook").ShouldBe(true);
    }

    [Fact]
    public void IsBlockedUrl_WithPrivate172Network_ShouldReturnTrue()
    {
        // 172.16.0.0/12
        WebhookUrlValidator.IsBlockedUrl("https://172.16.0.1/hook").ShouldBe(true);
    }

    [Fact]
    public void IsBlockedUrl_WithPrivate192Network_ShouldReturnTrue()
    {
        // 192.168.0.0/16
        WebhookUrlValidator.IsBlockedUrl("https://192.168.1.1/hook").ShouldBe(true);
    }

    #endregion

    #region Link-Local / Cloud Metadata

    [Fact]
    public void IsBlockedUrl_WithLinkLocalCloudMetadata_ShouldReturnTrue()
    {
        // 169.254.169.254 — AWS/GCP/Azure metadata endpoint
        WebhookUrlValidator.IsBlockedUrl("https://169.254.169.254/hook").ShouldBe(true);
    }

    #endregion

    #region Internal Hostnames

    [Fact]
    public void IsBlockedUrl_WithInternalDomain_ShouldReturnTrue()
    {
        // .internal suffix
        WebhookUrlValidator.IsBlockedUrl("https://service.internal/hook").ShouldBe(true);
    }

    [Fact]
    public void IsBlockedUrl_WithLocalDomain_ShouldReturnTrue()
    {
        // .local suffix
        WebhookUrlValidator.IsBlockedUrl("https://service.local/hook").ShouldBe(true);
    }

    [Fact]
    public void IsBlockedUrl_WithLocalhostDomain_ShouldReturnTrue()
    {
        // .localhost suffix
        WebhookUrlValidator.IsBlockedUrl("https://app.localhost/hook").ShouldBe(true);
    }

    #endregion

    #region Public URLs (Should NOT Be Blocked)

    [Fact]
    public void IsBlockedUrl_WithPublicIp_ShouldReturnFalse()
    {
        // Public IP address (Google DNS) should be allowed
        WebhookUrlValidator.IsBlockedUrl("https://8.8.8.8/hook").ShouldBe(false);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void IsBlockedUrl_WithEmptyString_ShouldReturnTrue()
    {
        // Empty string should be blocked (fail closed)
        WebhookUrlValidator.IsBlockedUrl("").ShouldBe(true);
    }

    [Fact]
    public void IsBlockedUrl_WithNull_ShouldReturnTrue()
    {
        // Null should be blocked (fail closed)
        WebhookUrlValidator.IsBlockedUrl(null!).ShouldBe(true);
    }

    [Fact]
    public void IsBlockedUrl_WithInvalidUrl_ShouldReturnTrue()
    {
        // Invalid/malformed URL should be blocked (fail closed)
        WebhookUrlValidator.IsBlockedUrl("not-a-url").ShouldBe(true);
    }

    [Fact]
    public void IsBlockedUrl_WithZeroIp_ShouldReturnTrue()
    {
        // 0.0.0.0/8 should be blocked
        WebhookUrlValidator.IsBlockedUrl("https://0.0.0.0/hook").ShouldBe(true);
    }

    #endregion
}
