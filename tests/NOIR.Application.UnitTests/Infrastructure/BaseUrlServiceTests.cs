namespace NOIR.Application.UnitTests.Infrastructure;

/// <summary>
/// Unit tests for BaseUrlService.
/// Tests URL generation from HttpContext and configuration fallback.
/// </summary>
public class BaseUrlServiceTests
{
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<IOptions<ApplicationSettings>> _settingsMock;
    private readonly BaseUrlService _sut;

    public BaseUrlServiceTests()
    {
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _settingsMock = new Mock<IOptions<ApplicationSettings>>();

        // Setup default settings
        var settings = new ApplicationSettings { BaseUrl = "https://configured.example.com" };
        _settingsMock.Setup(x => x.Value).Returns(settings);

        _sut = new BaseUrlService(_httpContextAccessorMock.Object, _settingsMock.Object);
    }

    #region GetBaseUrl Tests

    [Fact]
    public void GetBaseUrl_WithHttpContext_ShouldReturnUrlFromRequest()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("myapp.example.com", 443);
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        var result = _sut.GetBaseUrl();

        // Assert
        result.Should().Be("https://myapp.example.com:443");
    }

    [Fact]
    public void GetBaseUrl_WithHttpContextDefaultPort_ShouldNotIncludePort()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("myapp.example.com");
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        var result = _sut.GetBaseUrl();

        // Assert
        result.Should().Be("https://myapp.example.com");
    }

    [Fact]
    public void GetBaseUrl_WithHttpScheme_ShouldReturnHttpUrl()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "http";
        httpContext.Request.Host = new HostString("localhost", 5000);
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        var result = _sut.GetBaseUrl();

        // Assert
        result.Should().Be("http://localhost:5000");
    }

    [Fact]
    public void GetBaseUrl_WithoutHttpContext_ShouldFallbackToConfiguration()
    {
        // Arrange
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        // Act
        var result = _sut.GetBaseUrl();

        // Assert
        result.Should().Be("https://configured.example.com");
    }

    [Fact]
    public void GetBaseUrl_WithConfiguredUrlWithTrailingSlash_ShouldTrimSlash()
    {
        // Arrange
        var settings = new ApplicationSettings { BaseUrl = "https://configured.example.com/" };
        var settingsMock = new Mock<IOptions<ApplicationSettings>>();
        settingsMock.Setup(x => x.Value).Returns(settings);

        var service = new BaseUrlService(_httpContextAccessorMock.Object, settingsMock.Object);
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        // Act
        var result = service.GetBaseUrl();

        // Assert
        result.Should().Be("https://configured.example.com");
    }

    [Fact]
    public void GetBaseUrl_WithNoHttpContextAndNoConfiguration_ShouldReturnLocalhost()
    {
        // Arrange
        var settings = new ApplicationSettings { BaseUrl = null };
        var settingsMock = new Mock<IOptions<ApplicationSettings>>();
        settingsMock.Setup(x => x.Value).Returns(settings);

        var service = new BaseUrlService(_httpContextAccessorMock.Object, settingsMock.Object);
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        // Act
        var result = service.GetBaseUrl();

        // Assert
        result.Should().Be("https://localhost");
    }

    [Fact]
    public void GetBaseUrl_WithEmptyConfiguration_ShouldReturnLocalhost()
    {
        // Arrange
        var settings = new ApplicationSettings { BaseUrl = "" };
        var settingsMock = new Mock<IOptions<ApplicationSettings>>();
        settingsMock.Setup(x => x.Value).Returns(settings);

        var service = new BaseUrlService(_httpContextAccessorMock.Object, settingsMock.Object);
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        // Act
        var result = service.GetBaseUrl();

        // Assert
        result.Should().Be("https://localhost");
    }

    #endregion

    #region BuildUrl Tests

    [Fact]
    public void BuildUrl_WithRelativePath_ShouldCombineWithBaseUrl()
    {
        // Arrange
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        // Act
        var result = _sut.BuildUrl("/login");

        // Assert
        result.Should().Be("https://configured.example.com/login");
    }

    [Fact]
    public void BuildUrl_WithPathWithoutLeadingSlash_ShouldAddSlash()
    {
        // Arrange
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        // Act
        var result = _sut.BuildUrl("dashboard/settings");

        // Assert
        result.Should().Be("https://configured.example.com/dashboard/settings");
    }

    [Fact]
    public void BuildUrl_WithEmptyPath_ShouldReturnBaseUrl()
    {
        // Arrange
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        // Act
        var result = _sut.BuildUrl("");

        // Assert
        result.Should().Be("https://configured.example.com");
    }

    [Fact]
    public void BuildUrl_WithNullPath_ShouldReturnBaseUrl()
    {
        // Arrange
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        // Act
        var result = _sut.BuildUrl(null!);

        // Assert
        result.Should().Be("https://configured.example.com");
    }

    [Fact]
    public void BuildUrl_WithQueryString_ShouldPreserveQueryString()
    {
        // Arrange
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        // Act
        var result = _sut.BuildUrl("/search?q=test&page=1");

        // Assert
        result.Should().Be("https://configured.example.com/search?q=test&page=1");
    }

    [Fact]
    public void BuildUrl_WithHttpContext_ShouldUseContextBaseUrl()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("dynamic.example.com");
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        var result = _sut.BuildUrl("/api/users");

        // Assert
        result.Should().Be("https://dynamic.example.com/api/users");
    }

    #endregion

    #region Service Registration Tests

    [Fact]
    public void Service_ShouldImplementIBaseUrlService()
    {
        // Assert
        _sut.Should().BeAssignableTo<IBaseUrlService>();
    }

    [Fact]
    public void Service_ShouldImplementIScopedService()
    {
        // Assert
        _sut.Should().BeAssignableTo<IScopedService>();
    }

    [Fact]
    public void Constructor_ShouldAcceptDependencies()
    {
        // Act
        var service = new BaseUrlService(_httpContextAccessorMock.Object, _settingsMock.Object);

        // Assert
        service.Should().NotBeNull();
    }

    #endregion
}
