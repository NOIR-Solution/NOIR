using NOIR.Application.Features.TenantSettings.DTOs;
using NOIR.Application.Features.TenantSettings.Queries.GetBrandingSettings;

namespace NOIR.Application.UnitTests.Features.TenantSettings;

/// <summary>
/// Unit tests for GetBrandingSettingsQueryHandler.
/// Tests retrieval of branding settings from tenant settings service.
/// </summary>
public class GetBrandingSettingsQueryHandlerTests
{
    #region Test Setup

    private const string TestTenantId = "test-tenant-id";

    private readonly Mock<ITenantSettingsService> _settingsServiceMock;
    private readonly Mock<IMultiTenantContextAccessor> _tenantAccessorMock;
    private readonly GetBrandingSettingsQueryHandler _handler;

    public GetBrandingSettingsQueryHandlerTests()
    {
        _settingsServiceMock = new Mock<ITenantSettingsService>();
        _tenantAccessorMock = new Mock<IMultiTenantContextAccessor>();

        SetupTenantContext(TestTenantId);

        _handler = new GetBrandingSettingsQueryHandler(
            _settingsServiceMock.Object,
            _tenantAccessorMock.Object);
    }

    private void SetupTenantContext(string? tenantId)
    {
        var mockTenantContext = new Mock<IMultiTenantContext>();
        var mockTenantInfo = tenantId != null
            ? new Tenant(tenantId, "test-tenant", "Test Tenant")
            : null;

        mockTenantContext.Setup(x => x.TenantInfo).Returns(mockTenantInfo);
        _tenantAccessorMock.Setup(x => x.MultiTenantContext).Returns(mockTenantContext.Object);
    }

    private void SetupBrandingSettings(
        string? logoUrl = null,
        string? faviconUrl = null,
        string? primaryColor = null,
        string? secondaryColor = null,
        string? darkModeDefault = null)
    {
        var settings = new Dictionary<string, string>();
        if (logoUrl != null) settings["branding:logo_url"] = logoUrl;
        if (faviconUrl != null) settings["branding:favicon_url"] = faviconUrl;
        if (primaryColor != null) settings["branding:primary_color"] = primaryColor;
        if (secondaryColor != null) settings["branding:secondary_color"] = secondaryColor;
        if (darkModeDefault != null) settings["branding:dark_mode_default"] = darkModeDefault;

        _settingsServiceMock
            .Setup(x => x.GetSettingsAsync(It.IsAny<string?>(), "branding:", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyDictionary<string, string>)settings);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithAllSettings_ShouldReturnFullDto()
    {
        // Arrange
        SetupBrandingSettings(
            logoUrl: "https://example.com/logo.png",
            faviconUrl: "https://example.com/favicon.ico",
            primaryColor: "#3B82F6",
            secondaryColor: "#10B981",
            darkModeDefault: "true");

        var query = new GetBrandingSettingsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.LogoUrl.Should().Be("https://example.com/logo.png");
        result.Value.FaviconUrl.Should().Be("https://example.com/favicon.ico");
        result.Value.PrimaryColor.Should().Be("#3B82F6");
        result.Value.SecondaryColor.Should().Be("#10B981");
        result.Value.DarkModeDefault.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithNoSettings_ShouldReturnEmptyDto()
    {
        // Arrange
        SetupBrandingSettings(); // Empty settings

        var query = new GetBrandingSettingsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.LogoUrl.Should().BeNull();
        result.Value.FaviconUrl.Should().BeNull();
        result.Value.PrimaryColor.Should().BeNull();
        result.Value.SecondaryColor.Should().BeNull();
        result.Value.DarkModeDefault.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithPartialSettings_ShouldReturnPartialDto()
    {
        // Arrange
        SetupBrandingSettings(
            logoUrl: "https://example.com/logo.png",
            primaryColor: "#3B82F6");

        var query = new GetBrandingSettingsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.LogoUrl.Should().Be("https://example.com/logo.png");
        result.Value.FaviconUrl.Should().BeNull();
        result.Value.PrimaryColor.Should().Be("#3B82F6");
        result.Value.SecondaryColor.Should().BeNull();
        result.Value.DarkModeDefault.Should().BeFalse();
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("True", true)]
    [InlineData("TRUE", true)]
    [InlineData("false", false)]
    [InlineData("False", false)]
    [InlineData("invalid", false)]
    [InlineData("", false)]
    public async Task Handle_DarkModeDefault_ShouldParseBoolCorrectly(string value, bool expected)
    {
        // Arrange
        SetupBrandingSettings(darkModeDefault: value);

        var query = new GetBrandingSettingsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.DarkModeDefault.Should().Be(expected);
    }

    [Fact]
    public async Task Handle_ShouldUseCorrectTenantId()
    {
        // Arrange
        SetupBrandingSettings();
        var query = new GetBrandingSettingsQuery();

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _settingsServiceMock.Verify(
            x => x.GetSettingsAsync(TestTenantId, "branding:", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithNullTenant_ShouldUsePlatformLevel()
    {
        // Arrange
        SetupTenantContext(null);
        SetupBrandingSettings();
        var query = new GetBrandingSettingsQuery();

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _settingsServiceMock.Verify(
            x => x.GetSettingsAsync(null, "branding:", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}
