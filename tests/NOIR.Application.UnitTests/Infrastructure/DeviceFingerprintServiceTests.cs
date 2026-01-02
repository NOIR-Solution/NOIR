namespace NOIR.Application.UnitTests.Infrastructure;

/// <summary>
/// Unit tests for DeviceFingerprintService.
/// Tests device fingerprint generation, IP extraction, and user agent parsing.
/// </summary>
public class DeviceFingerprintServiceTests
{
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly DeviceFingerprintService _sut;

    public DeviceFingerprintServiceTests()
    {
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _sut = new DeviceFingerprintService(_httpContextAccessorMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldAcceptHttpContextAccessor()
    {
        // Act
        var service = new DeviceFingerprintService(_httpContextAccessorMock.Object);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void Service_ShouldImplementIDeviceFingerprintService()
    {
        // Assert
        _sut.Should().BeAssignableTo<IDeviceFingerprintService>();
    }

    [Fact]
    public void Service_ShouldImplementIScopedService()
    {
        // Assert
        _sut.Should().BeAssignableTo<IScopedService>();
    }

    #endregion

    #region GenerateFingerprint Tests

    [Fact]
    public void GenerateFingerprint_WithNullHttpContext_ShouldReturnNull()
    {
        // Arrange
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        // Act
        var result = _sut.GenerateFingerprint();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GenerateFingerprint_WithValidContext_ShouldReturnHash()
    {
        // Arrange
        SetupHttpContext("Mozilla/5.0", "en-US", "gzip", "192.168.1.1");

        // Act
        var result = _sut.GenerateFingerprint();

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
    }

    [Fact]
    public void GenerateFingerprint_ShouldReturnBase64String()
    {
        // Arrange
        SetupHttpContext("Mozilla/5.0", "en-US", "gzip", "192.168.1.1");

        // Act
        var result = _sut.GenerateFingerprint();

        // Assert - Valid Base64 can be decoded
        var act = () => Convert.FromBase64String(result!);
        act.Should().NotThrow();
    }

    [Fact]
    public void GenerateFingerprint_SameInputs_ShouldReturnConsistentHash()
    {
        // Arrange
        SetupHttpContext("Mozilla/5.0", "en-US", "gzip", "192.168.1.1");

        // Act
        var result1 = _sut.GenerateFingerprint();
        var result2 = _sut.GenerateFingerprint();

        // Assert
        result1.Should().Be(result2);
    }

    [Fact]
    public void GenerateFingerprint_DifferentUserAgents_ShouldReturnDifferentHashes()
    {
        // Arrange & Act
        SetupHttpContext("Mozilla/5.0", "en-US", "gzip", "192.168.1.1");
        var result1 = _sut.GenerateFingerprint();

        SetupHttpContext("Chrome/100.0", "en-US", "gzip", "192.168.1.1");
        var result2 = _sut.GenerateFingerprint();

        // Assert
        result1.Should().NotBe(result2);
    }

    #endregion

    #region GetClientIpAddress Tests

    [Fact]
    public void GetClientIpAddress_WithNullHttpContext_ShouldReturnNull()
    {
        // Arrange
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        // Act
        var result = _sut.GetClientIpAddress();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetClientIpAddress_WithXForwardedFor_ShouldReturnFirstIp()
    {
        // Arrange
        SetupHttpContextWithForwardedFor("10.0.0.1, 10.0.0.2, 10.0.0.3");

        // Act
        var result = _sut.GetClientIpAddress();

        // Assert
        result.Should().Be("10.0.0.1");
    }

    [Fact]
    public void GetClientIpAddress_WithXRealIp_ShouldReturnRealIp()
    {
        // Arrange
        SetupHttpContextWithRealIp("172.16.0.50");

        // Act
        var result = _sut.GetClientIpAddress();

        // Assert
        result.Should().Be("172.16.0.50");
    }

    [Fact]
    public void GetClientIpAddress_WithRemoteIpAddress_ShouldReturnMappedIp()
    {
        // Arrange
        SetupHttpContext("Mozilla/5.0", "en-US", "gzip", "192.168.1.100");

        // Act
        var result = _sut.GetClientIpAddress();

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void GetClientIpAddress_XForwardedForTakesPrecedence_OverXRealIp()
    {
        // Arrange
        var context = CreateHttpContext();
        context.Request.Headers["X-Forwarded-For"] = "10.0.0.1";
        context.Request.Headers["X-Real-IP"] = "172.16.0.50";
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(context);

        // Act
        var result = _sut.GetClientIpAddress();

        // Assert
        result.Should().Be("10.0.0.1");
    }

    #endregion

    #region GetUserAgent Tests

    [Fact]
    public void GetUserAgent_WithNullHttpContext_ShouldReturnNull()
    {
        // Arrange
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        // Act
        var result = _sut.GetUserAgent();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetUserAgent_WithValidContext_ShouldReturnUserAgent()
    {
        // Arrange
        var expectedUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)";
        SetupHttpContext(expectedUserAgent, "en-US", "gzip", "192.168.1.1");

        // Act
        var result = _sut.GetUserAgent();

        // Assert
        result.Should().Be(expectedUserAgent);
    }

    #endregion

    #region GetDeviceName Tests

    [Fact]
    public void GetDeviceName_WithNullHttpContext_ShouldReturnNull()
    {
        // Arrange
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        // Act
        var result = _sut.GetDeviceName();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetDeviceName_WithEmptyUserAgent_ShouldReturnNull()
    {
        // Arrange
        SetupHttpContext("", "en-US", "gzip", "192.168.1.1");

        // Act
        var result = _sut.GetDeviceName();

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("Mozilla/5.0 (iPhone; CPU iPhone OS 15_0 like Mac OS X)", "iPhone")]
    [InlineData("Mozilla/5.0 (iPad; CPU OS 15_0 like Mac OS X)", "iPad")]
    [InlineData("Mozilla/5.0 (Linux; Android 12; SM-G998B)", "Android Device")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64)", "Windows PC")]
    [InlineData("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7)", "Mac")]
    [InlineData("Mozilla/5.0 (X11; Linux x86_64)", "Linux PC")]
    public void GetDeviceName_WithKnownUserAgent_ShouldReturnCorrectDevice(string userAgent, string expectedDevice)
    {
        // Arrange
        SetupHttpContext(userAgent, "en-US", "gzip", "192.168.1.1");

        // Act
        var result = _sut.GetDeviceName();

        // Assert
        result.Should().Be(expectedDevice);
    }

    [Fact]
    public void GetDeviceName_WithUnknownUserAgent_ShouldReturnUnknownDevice()
    {
        // Arrange
        SetupHttpContext("SomeWeirdBot/1.0", "en-US", "gzip", "192.168.1.1");

        // Act
        var result = _sut.GetDeviceName();

        // Assert
        result.Should().Be("Unknown Device");
    }

    #endregion

    #region Method Existence Tests

    [Fact]
    public void GenerateFingerprint_MethodShouldExist()
    {
        // Assert
        var method = typeof(DeviceFingerprintService).GetMethod("GenerateFingerprint");
        method.Should().NotBeNull();
    }

    [Fact]
    public void GetClientIpAddress_MethodShouldExist()
    {
        // Assert
        var method = typeof(DeviceFingerprintService).GetMethod("GetClientIpAddress");
        method.Should().NotBeNull();
    }

    [Fact]
    public void GetUserAgent_MethodShouldExist()
    {
        // Assert
        var method = typeof(DeviceFingerprintService).GetMethod("GetUserAgent");
        method.Should().NotBeNull();
    }

    [Fact]
    public void GetDeviceName_MethodShouldExist()
    {
        // Assert
        var method = typeof(DeviceFingerprintService).GetMethod("GetDeviceName");
        method.Should().NotBeNull();
    }

    #endregion

    #region Helper Methods

    private DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("192.168.1.1");
        return context;
    }

    private void SetupHttpContext(string userAgent, string acceptLanguage, string acceptEncoding, string remoteIp)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.UserAgent = userAgent;
        context.Request.Headers.AcceptLanguage = acceptLanguage;
        context.Request.Headers.AcceptEncoding = acceptEncoding;
        context.Connection.RemoteIpAddress = IPAddress.Parse(remoteIp);
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(context);
    }

    private void SetupHttpContextWithForwardedFor(string forwardedFor)
    {
        var context = CreateHttpContext();
        context.Request.Headers["X-Forwarded-For"] = forwardedFor;
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(context);
    }

    private void SetupHttpContextWithRealIp(string realIp)
    {
        var context = CreateHttpContext();
        context.Request.Headers["X-Real-IP"] = realIp;
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(context);
    }

    #endregion
}
