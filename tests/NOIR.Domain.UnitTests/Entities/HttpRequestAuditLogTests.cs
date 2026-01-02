namespace NOIR.Domain.UnitTests.Entities;

/// <summary>
/// Unit tests for the HttpRequestAuditLog entity.
/// Tests factory methods, Complete method, and property defaults.
/// </summary>
public class HttpRequestAuditLogTests
{
    #region Create Factory Tests

    [Fact]
    public void Create_WithRequiredParameters_ShouldCreateValidLog()
    {
        // Arrange
        var correlationId = "corr-123";
        var httpMethod = "POST";
        var url = "/api/customers";

        // Act
        var log = HttpRequestAuditLog.Create(
            correlationId, httpMethod, url,
            queryString: null, userId: null, userEmail: null,
            tenantId: null, ipAddress: null, userAgent: null);

        // Assert
        log.Should().NotBeNull();
        log.Id.Should().NotBe(Guid.Empty);
        log.CorrelationId.Should().Be(correlationId);
        log.HttpMethod.Should().Be(httpMethod);
        log.Url.Should().Be(url);
    }

    [Fact]
    public void Create_ShouldGenerateUniqueIds()
    {
        // Arrange & Act
        var log1 = HttpRequestAuditLog.Create("corr-1", "GET", "/api/1", null, null, null, null, null, null);
        var log2 = HttpRequestAuditLog.Create("corr-2", "GET", "/api/2", null, null, null, null, null, null);

        // Assert
        log1.Id.Should().NotBe(log2.Id);
    }

    [Fact]
    public void Create_ShouldSetStartTime()
    {
        // Arrange
        var beforeCreate = DateTimeOffset.UtcNow;

        // Act
        var log = HttpRequestAuditLog.Create("corr-123", "GET", "/api/test", null, null, null, null, null, null);

        // Assert
        var afterCreate = DateTimeOffset.UtcNow;
        log.StartTime.Should().BeOnOrAfter(beforeCreate).And.BeOnOrBefore(afterCreate);
    }

    [Fact]
    public void Create_WithAllOptionalParameters_ShouldSetAllProperties()
    {
        // Arrange
        var correlationId = "corr-123";
        var httpMethod = "POST";
        var url = "/api/customers";
        var queryString = "?page=1&size=10";
        var userId = "user-456";
        var userEmail = "user@example.com";
        var tenantId = "tenant-abc";
        var ipAddress = "192.168.1.100";
        var userAgent = "Mozilla/5.0";

        // Act
        var log = HttpRequestAuditLog.Create(
            correlationId, httpMethod, url,
            queryString, userId, userEmail,
            tenantId, ipAddress, userAgent);

        // Assert
        log.QueryString.Should().Be(queryString);
        log.UserId.Should().Be(userId);
        log.UserEmail.Should().Be(userEmail);
        log.TenantId.Should().Be(tenantId);
        log.IpAddress.Should().Be(ipAddress);
        log.UserAgent.Should().Be(userAgent);
    }

    [Fact]
    public void Create_ShouldNotBeArchived()
    {
        // Act
        var log = HttpRequestAuditLog.Create("corr-123", "GET", "/api/test", null, null, null, null, null, null);

        // Assert
        log.IsArchived.Should().BeFalse();
        log.ArchivedAt.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldHaveNullResponseFields()
    {
        // Act
        var log = HttpRequestAuditLog.Create("corr-123", "GET", "/api/test", null, null, null, null, null, null);

        // Assert
        log.ResponseStatusCode.Should().BeNull();
        log.ResponseBody.Should().BeNull();
        log.EndTime.Should().BeNull();
        log.DurationMs.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldInitializeHandlerAuditLogsCollection()
    {
        // Act
        var log = HttpRequestAuditLog.Create("corr-123", "GET", "/api/test", null, null, null, null, null, null);

        // Assert
        log.HandlerAuditLogs.Should().NotBeNull();
        log.HandlerAuditLogs.Should().BeEmpty();
    }

    #endregion

    #region HTTP Method Tests

    [Theory]
    [InlineData("GET")]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    [InlineData("OPTIONS")]
    [InlineData("HEAD")]
    public void Create_AllHttpMethods_ShouldWork(string httpMethod)
    {
        // Act
        var log = HttpRequestAuditLog.Create("corr-123", httpMethod, "/api/test", null, null, null, null, null, null);

        // Assert
        log.HttpMethod.Should().Be(httpMethod);
    }

    #endregion

    #region Complete Method Tests

    [Fact]
    public void Complete_ShouldSetStatusCode()
    {
        // Arrange
        var log = HttpRequestAuditLog.Create("corr-123", "GET", "/api/test", null, null, null, null, null, null);

        // Act
        log.Complete(statusCode: 200);

        // Assert
        log.ResponseStatusCode.Should().Be(200);
    }

    [Fact]
    public void Complete_ShouldSetEndTime()
    {
        // Arrange
        var log = HttpRequestAuditLog.Create("corr-123", "GET", "/api/test", null, null, null, null, null, null);
        var afterCreate = DateTimeOffset.UtcNow;

        // Act
        log.Complete(statusCode: 200);

        // Assert
        log.EndTime.Should().NotBeNull();
        log.EndTime.Should().BeOnOrAfter(afterCreate);
    }

    [Fact]
    public void Complete_ShouldCalculateDurationMs()
    {
        // Arrange
        var log = HttpRequestAuditLog.Create("corr-123", "GET", "/api/test", null, null, null, null, null, null);
        Thread.Sleep(50); // Wait 50ms

        // Act
        log.Complete(statusCode: 200);

        // Assert
        log.DurationMs.Should().NotBeNull();
        log.DurationMs!.Value.Should().BeGreaterThanOrEqualTo(40); // Allow some tolerance
        log.DurationMs.Should().BeLessThan(500); // Should not be too long
    }

    [Fact]
    public void Complete_WithResponseBody_ShouldSetResponseBody()
    {
        // Arrange
        var log = HttpRequestAuditLog.Create("corr-123", "GET", "/api/test", null, null, null, null, null, null);
        var responseBody = "{\"id\":\"123\",\"name\":\"Test\"}";

        // Act
        log.Complete(statusCode: 200, responseBody: responseBody);

        // Assert
        log.ResponseBody.Should().Be(responseBody);
    }

    [Theory]
    [InlineData(200)]
    [InlineData(201)]
    [InlineData(204)]
    [InlineData(400)]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(404)]
    [InlineData(500)]
    public void Complete_VariousStatusCodes_ShouldSetCorrectCode(int statusCode)
    {
        // Arrange
        var log = HttpRequestAuditLog.Create("corr-123", "GET", "/api/test", null, null, null, null, null, null);

        // Act
        log.Complete(statusCode: statusCode);

        // Assert
        log.ResponseStatusCode.Should().Be(statusCode);
    }

    [Fact]
    public void Complete_MultipleTimes_ShouldUpdateValues()
    {
        // Arrange
        var log = HttpRequestAuditLog.Create("corr-123", "GET", "/api/test", null, null, null, null, null, null);

        // Act
        log.Complete(statusCode: 200, responseBody: "first");
        var firstEndTime = log.EndTime;

        Thread.Sleep(10);
        log.Complete(statusCode: 500, responseBody: "second");

        // Assert
        log.ResponseStatusCode.Should().Be(500);
        log.ResponseBody.Should().Be("second");
        log.EndTime.Should().BeOnOrAfter(firstEndTime!.Value);
    }

    #endregion

    #region Validation Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidCorrelationId_ShouldThrow(string? correlationId)
    {
        // Act
        var act = () => HttpRequestAuditLog.Create(correlationId!, "GET", "/api/test", null, null, null, null, null, null);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidHttpMethod_ShouldThrow(string? httpMethod)
    {
        // Act
        var act = () => HttpRequestAuditLog.Create("corr-123", httpMethod!, "/api/test", null, null, null, null, null, null);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidUrl_ShouldThrow(string? url)
    {
        // Act
        var act = () => HttpRequestAuditLog.Create("corr-123", "GET", url!, null, null, null, null, null, null);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region URL Variations

    [Theory]
    [InlineData("/api/customers")]
    [InlineData("/api/customers/123")]
    [InlineData("/api/orders/456/items")]
    [InlineData("/health")]
    [InlineData("/api/auth/login")]
    public void Create_VariousUrls_ShouldWork(string url)
    {
        // Act
        var log = HttpRequestAuditLog.Create("corr-123", "GET", url, null, null, null, null, null, null);

        // Assert
        log.Url.Should().Be(url);
    }

    #endregion

    #region IP Address Variations

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("192.168.1.100")]
    [InlineData("10.0.0.1")]
    [InlineData("::1")]
    [InlineData("2001:0db8:85a3:0000:0000:8a2e:0370:7334")]
    public void Create_VariousIpAddresses_ShouldWork(string ipAddress)
    {
        // Act
        var log = HttpRequestAuditLog.Create("corr-123", "GET", "/api/test", null, null, null, null, ipAddress, null);

        // Assert
        log.IpAddress.Should().Be(ipAddress);
    }

    #endregion

    #region User Agent Variations

    [Theory]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36")]
    [InlineData("PostmanRuntime/7.29.0")]
    [InlineData("curl/7.68.0")]
    [InlineData("NOIR-Client/1.0")]
    public void Create_VariousUserAgents_ShouldWork(string userAgent)
    {
        // Act
        var log = HttpRequestAuditLog.Create("corr-123", "GET", "/api/test", null, null, null, null, null, userAgent);

        // Assert
        log.UserAgent.Should().Be(userAgent);
    }

    #endregion

    #region Multi-Tenant Tests

    [Fact]
    public void Create_WithTenantId_ShouldSetTenantId()
    {
        // Act
        var log = HttpRequestAuditLog.Create("corr-123", "GET", "/api/test", null, null, null, "tenant-abc", null, null);

        // Assert
        log.TenantId.Should().Be("tenant-abc");
    }

    [Fact]
    public void Create_WithoutTenantId_ShouldHaveNullTenantId()
    {
        // Act
        var log = HttpRequestAuditLog.Create("corr-123", "GET", "/api/test", null, null, null, null, null, null);

        // Assert
        log.TenantId.Should().BeNull();
    }

    #endregion
}
