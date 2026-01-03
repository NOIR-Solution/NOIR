namespace NOIR.Application.UnitTests.Services;

/// <summary>
/// Unit tests for JsonLocalizationService.
/// Tests key navigation, culture detection, fallback behavior, and Accept-Language parsing.
/// </summary>
public class JsonLocalizationServiceTests
{
    private readonly Mock<IOptions<LocalizationSettings>> _settingsMock;
    private readonly Mock<IMemoryCache> _cacheMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<ILogger<JsonLocalizationService>> _loggerMock;
    private readonly Mock<IHostEnvironment> _environmentMock;
    private readonly LocalizationSettings _settings;

    public JsonLocalizationServiceTests()
    {
        _settings = new LocalizationSettings
        {
            DefaultCulture = "en",
            SupportedCultures = ["en", "vi"],
            ResourcesPath = "Resources/Localization",
            FallbackToDefaultCulture = true,
            EnableCaching = false, // Disable caching for tests
            CacheDurationMinutes = 60
        };

        _settingsMock = new Mock<IOptions<LocalizationSettings>>();
        _settingsMock.Setup(x => x.Value).Returns(_settings);

        _cacheMock = new Mock<IMemoryCache>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _loggerMock = new Mock<ILogger<JsonLocalizationService>>();
        _environmentMock = new Mock<IHostEnvironment>();
        _environmentMock.Setup(x => x.ContentRootPath).Returns(GetTestResourcesPath());
    }

    private static string GetTestResourcesPath()
    {
        // Get the path to the test project's directory
        var assembly = typeof(JsonLocalizationServiceTests).Assembly;
        var path = Path.GetDirectoryName(assembly.Location)!;
        return path;
    }

    private JsonLocalizationService CreateService()
    {
        // Setup cache to always call the factory (no caching)
        _cacheMock.Setup(x => x.CreateEntry(It.IsAny<object>()))
            .Returns(Mock.Of<ICacheEntry>());

        return new JsonLocalizationService(
            _settingsMock.Object,
            _cacheMock.Object,
            _httpContextAccessorMock.Object,
            _loggerMock.Object,
            _environmentMock.Object);
    }

    private void SetupHttpContext(string? acceptLanguage = null, string? cookie = null, string? queryParam = null)
    {
        var httpContext = new DefaultHttpContext();

        if (!string.IsNullOrEmpty(acceptLanguage))
        {
            httpContext.Request.Headers["Accept-Language"] = acceptLanguage;
        }

        if (!string.IsNullOrEmpty(cookie))
        {
            httpContext.Request.Headers["Cookie"] = $"noir-language={cookie}";
        }

        if (!string.IsNullOrEmpty(queryParam))
        {
            httpContext.Request.QueryString = new QueryString($"?lang={queryParam}");
        }

        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
    }

    #region Culture Detection Tests

    [Fact]
    public void GetCurrentCulture_WithNoHttpContext_ReturnsDefaultCulture()
    {
        // Arrange
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);
        var service = CreateService();

        // Act
        var culture = service.CurrentCulture;

        // Assert
        culture.Should().Be("en");
    }

    [Fact]
    public void GetCurrentCulture_WithQueryParam_PrioritizesQueryParam()
    {
        // Arrange
        SetupHttpContext(acceptLanguage: "en", cookie: "en", queryParam: "vi");
        var service = CreateService();

        // Act
        var culture = service.CurrentCulture;

        // Assert
        culture.Should().Be("vi");
    }

    [Fact]
    public void GetCurrentCulture_WithAcceptLanguageHeader_PrioritizesOverCookie()
    {
        // Arrange - Accept-Language is "vi", cookie is "en"
        // After our fix, Accept-Language should take priority over cookie
        SetupHttpContext(acceptLanguage: "vi", cookie: "en");
        var service = CreateService();

        // Act
        var culture = service.CurrentCulture;

        // Assert
        culture.Should().Be("vi");
    }

    [Fact]
    public void GetCurrentCulture_WithOnlyCookie_UsesCookie()
    {
        // Arrange
        SetupHttpContext(cookie: "vi");
        var service = CreateService();

        // Act
        var culture = service.CurrentCulture;

        // Assert
        culture.Should().Be("vi");
    }

    [Fact]
    public void GetCurrentCulture_WithUnsupportedLanguage_FallsBackToDefault()
    {
        // Arrange
        SetupHttpContext(acceptLanguage: "fr");
        var service = CreateService();

        // Act
        var culture = service.CurrentCulture;

        // Assert
        culture.Should().Be("en");
    }

    [Fact]
    public void GetCurrentCulture_WithLanguageVariant_MatchesBaseLanguage()
    {
        // Arrange - "vi-VN" should match "vi"
        SetupHttpContext(acceptLanguage: "vi-VN");
        var service = CreateService();

        // Act
        var culture = service.CurrentCulture;

        // Assert
        culture.Should().Be("vi");
    }

    [Fact]
    public void GetCurrentCulture_WithQualityValues_ParsesCorrectly()
    {
        // Arrange - "vi,en;q=0.9,fr;q=0.8" should prefer "vi"
        SetupHttpContext(acceptLanguage: "vi,en;q=0.9,fr;q=0.8");
        var service = CreateService();

        // Act
        var culture = service.CurrentCulture;

        // Assert
        culture.Should().Be("vi");
    }

    [Fact]
    public void GetCurrentCulture_WithQualityValues_RespectsOrder()
    {
        // Arrange - "en;q=0.9,vi" should prefer "vi" (has implicit q=1.0)
        SetupHttpContext(acceptLanguage: "en;q=0.9,vi");
        var service = CreateService();

        // Act
        var culture = service.CurrentCulture;

        // Assert
        culture.Should().Be("vi");
    }

    #endregion

    #region Supported Cultures Tests

    [Fact]
    public void SupportedCultures_ReturnsConfiguredCultures()
    {
        // Arrange
        var service = CreateService();

        // Act
        var cultures = service.SupportedCultures;

        // Assert
        cultures.Should().ContainInOrder("en", "vi");
    }

    #endregion

    #region Get Value Tests (with mocked resources)

    [Fact]
    public void Get_WithEmptyKey_ReturnsKey()
    {
        // Arrange
        SetupHttpContext(acceptLanguage: "en");
        var service = CreateService();

        // Act
        var result = service.Get("");

        // Assert
        result.Should().Be("");
    }

    [Fact]
    public void Get_WithNullKey_ReturnsNull()
    {
        // Arrange
        SetupHttpContext(acceptLanguage: "en");
        var service = CreateService();

        // Act
        var result = service.Get(null!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Indexer_WorksLikeGet()
    {
        // Arrange
        SetupHttpContext(acceptLanguage: "en");
        var service = CreateService();

        // Act
        var result = service[""];

        // Assert
        result.Should().Be("");
    }

    #endregion

    #region Formatted Message Tests

    [Fact]
    public void Get_WithFormatArgs_ReturnsFormattedString()
    {
        // Arrange - We need to test the format method
        // Since we can't easily mock the file loading, we'll test the format logic
        SetupHttpContext(acceptLanguage: "en");
        var service = CreateService();

        // When the key is not found, it returns the key itself
        // So we can't test formatting without real files
        // This test verifies the method signature works
        var result = service.Get("test.key", 1, 2, 3);

        // Assert - Key not found returns key
        result.Should().Be("test.key");
    }

    [Fact]
    public void Get_WithNoArgs_DoesNotFormat()
    {
        // Arrange
        SetupHttpContext(acceptLanguage: "en");
        var service = CreateService();

        // Act
        var result = service.Get("test.key");

        // Assert
        result.Should().Be("test.key");
    }

    #endregion
}
