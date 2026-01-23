using NOIR.Application.Features.TenantSettings.DTOs;
using NOIR.Application.Features.TenantSettings.Commands.UpdateBrandingSettings;

namespace NOIR.Application.UnitTests.Features.TenantSettings;

/// <summary>
/// Unit tests for UpdateBrandingSettingsCommandHandler.
/// Tests updating branding settings via tenant settings service.
/// </summary>
public class UpdateBrandingSettingsCommandHandlerTests
{
    #region Test Setup

    private const string TestTenantId = "test-tenant-id";

    private readonly Mock<ITenantSettingsService> _settingsServiceMock;
    private readonly Mock<IMultiTenantContextAccessor> _tenantAccessorMock;
    private readonly Mock<ILogger<UpdateBrandingSettingsCommandHandler>> _loggerMock;
    private readonly UpdateBrandingSettingsCommandHandler _handler;

    public UpdateBrandingSettingsCommandHandlerTests()
    {
        _settingsServiceMock = new Mock<ITenantSettingsService>();
        _tenantAccessorMock = new Mock<IMultiTenantContextAccessor>();
        _loggerMock = new Mock<ILogger<UpdateBrandingSettingsCommandHandler>>();

        SetupTenantContext(TestTenantId);

        _handler = new UpdateBrandingSettingsCommandHandler(
            _settingsServiceMock.Object,
            _tenantAccessorMock.Object,
            _loggerMock.Object);
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

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithAllFields_ShouldUpdateAllSettings()
    {
        // Arrange
        var command = new UpdateBrandingSettingsCommand(
            LogoUrl: "https://example.com/logo.png",
            FaviconUrl: "https://example.com/favicon.ico",
            PrimaryColor: "#3B82F6",
            SecondaryColor: "#10B981",
            DarkModeDefault: true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.LogoUrl.Should().Be("https://example.com/logo.png");
        result.Value.FaviconUrl.Should().Be("https://example.com/favicon.ico");
        result.Value.PrimaryColor.Should().Be("#3B82F6");
        result.Value.SecondaryColor.Should().Be("#10B981");
        result.Value.DarkModeDefault.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldCallSetSettingForEachField()
    {
        // Arrange
        var command = new UpdateBrandingSettingsCommand(
            LogoUrl: "https://example.com/logo.png",
            FaviconUrl: "https://example.com/favicon.ico",
            PrimaryColor: "#3B82F6",
            SecondaryColor: "#10B981",
            DarkModeDefault: true);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _settingsServiceMock.Verify(x => x.SetSettingAsync(TestTenantId, "branding:logo_url", "https://example.com/logo.png", "string", It.IsAny<CancellationToken>()), Times.Once);
        _settingsServiceMock.Verify(x => x.SetSettingAsync(TestTenantId, "branding:favicon_url", "https://example.com/favicon.ico", "string", It.IsAny<CancellationToken>()), Times.Once);
        _settingsServiceMock.Verify(x => x.SetSettingAsync(TestTenantId, "branding:primary_color", "#3B82F6", "string", It.IsAny<CancellationToken>()), Times.Once);
        _settingsServiceMock.Verify(x => x.SetSettingAsync(TestTenantId, "branding:secondary_color", "#10B981", "string", It.IsAny<CancellationToken>()), Times.Once);
        _settingsServiceMock.Verify(x => x.SetSettingAsync(TestTenantId, "branding:dark_mode_default", "true", "bool", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNullValues_ShouldStoreEmptyStrings()
    {
        // Arrange
        var command = new UpdateBrandingSettingsCommand(
            LogoUrl: null,
            FaviconUrl: null,
            PrimaryColor: null,
            SecondaryColor: null,
            DarkModeDefault: false);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _settingsServiceMock.Verify(x => x.SetSettingAsync(TestTenantId, "branding:logo_url", string.Empty, "string", It.IsAny<CancellationToken>()), Times.Once);
        _settingsServiceMock.Verify(x => x.SetSettingAsync(TestTenantId, "branding:favicon_url", string.Empty, "string", It.IsAny<CancellationToken>()), Times.Once);
        _settingsServiceMock.Verify(x => x.SetSettingAsync(TestTenantId, "branding:dark_mode_default", "false", "bool", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DarkModeDefault_ShouldStoreLowercaseString()
    {
        // Arrange
        var command = new UpdateBrandingSettingsCommand(
            LogoUrl: null,
            FaviconUrl: null,
            PrimaryColor: null,
            SecondaryColor: null,
            DarkModeDefault: true);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _settingsServiceMock.Verify(x => x.SetSettingAsync(TestTenantId, "branding:dark_mode_default", "true", "bool", It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
