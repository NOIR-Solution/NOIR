using NOIR.Application.Features.TenantSettings.Queries.GetTenantSmtpSettings;

namespace NOIR.Application.UnitTests.Features.TenantSettings;

/// <summary>
/// Unit tests for GetTenantSmtpSettingsQueryHandler.
/// Tests tenant SMTP settings retrieval with platform default fallback.
/// </summary>
public class GetTenantSmtpSettingsQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<ITenantSettingsService> _settingsServiceMock;
    private readonly Mock<IMultiTenantContextAccessor> _tenantAccessorMock;
    private readonly GetTenantSmtpSettingsQueryHandler _handler;

    private const string TestTenantId = "tenant-abc";

    public GetTenantSmtpSettingsQueryHandlerTests()
    {
        _settingsServiceMock = new Mock<ITenantSettingsService>();
        _tenantAccessorMock = new Mock<IMultiTenantContextAccessor>();

        SetupTenantContext(TestTenantId);

        _handler = new GetTenantSmtpSettingsQueryHandler(
            _settingsServiceMock.Object,
            _tenantAccessorMock.Object);
    }

    private void SetupTenantContext(string? tenantId)
    {
        var tenantInfo = tenantId != null
            ? new Tenant(tenantId, "test-tenant", "Test Tenant")
            : null;
        var multiTenantContext = new Mock<IMultiTenantContext>();
        multiTenantContext.Setup(x => x.TenantInfo).Returns(tenantInfo);
        _tenantAccessorMock.Setup(x => x.MultiTenantContext).Returns(multiTenantContext.Object);
    }

    #endregion

    #region Tenant Has Custom Settings

    [Fact]
    public async Task Handle_WhenTenantHasCustomSettings_ShouldReturnTenantSettings()
    {
        // Arrange
        _settingsServiceMock
            .Setup(x => x.SettingExistsAsync(TestTenantId, "smtp:host", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var tenantSettings = new Dictionary<string, string>
        {
            { "smtp:host", "tenant-smtp.example.com" },
            { "smtp:port", "587" },
            { "smtp:username", "tenant@example.com" },
            { "smtp:password", "tenant-password" },
            { "smtp:from_email", "noreply@tenant.com" },
            { "smtp:from_name", "Tenant App" },
            { "smtp:use_ssl", "true" }
        };

        _settingsServiceMock
            .Setup(x => x.GetSettingsAsync(TestTenantId, "smtp:", It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantSettings);

        var query = new GetTenantSmtpSettingsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value;
        dto.Host.Should().Be("tenant-smtp.example.com");
        dto.Port.Should().Be(587);
        dto.Username.Should().Be("tenant@example.com");
        dto.HasPassword.Should().BeTrue();
        dto.FromEmail.Should().Be("noreply@tenant.com");
        dto.FromName.Should().Be("Tenant App");
        dto.UseSsl.Should().BeTrue();
        dto.IsConfigured.Should().BeTrue();
        dto.IsInherited.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenTenantHasNoPassword_ShouldReportHasPasswordFalse()
    {
        // Arrange
        _settingsServiceMock
            .Setup(x => x.SettingExistsAsync(TestTenantId, "smtp:host", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var tenantSettings = new Dictionary<string, string>
        {
            { "smtp:host", "smtp.example.com" },
            { "smtp:port", "25" }
        };

        _settingsServiceMock
            .Setup(x => x.GetSettingsAsync(TestTenantId, "smtp:", It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantSettings);

        var query = new GetTenantSmtpSettingsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.HasPassword.Should().BeFalse();
    }

    #endregion

    #region Fallback to Platform Defaults

    [Fact]
    public async Task Handle_WhenTenantHasNoCustomSettings_ShouldFallbackToPlatform()
    {
        // Arrange
        _settingsServiceMock
            .Setup(x => x.SettingExistsAsync(TestTenantId, "smtp:host", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var platformSettings = new Dictionary<string, string>
        {
            { "smtp:host", "platform-smtp.example.com" },
            { "smtp:port", "25" },
            { "smtp:from_email", "noreply@platform.com" },
            { "smtp:from_name", "Platform" },
            { "smtp:use_ssl", "false" }
        };

        _settingsServiceMock
            .Setup(x => x.GetSettingsAsync(null, "smtp:", It.IsAny<CancellationToken>()))
            .ReturnsAsync(platformSettings);

        var query = new GetTenantSmtpSettingsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value;
        dto.Host.Should().Be("platform-smtp.example.com");
        dto.Port.Should().Be(25);
        dto.FromEmail.Should().Be("noreply@platform.com");
        dto.FromName.Should().Be("Platform");
        dto.UseSsl.Should().BeFalse();
        dto.IsInherited.Should().BeTrue();
        dto.IsConfigured.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenNoSettingsExistAnywhere_ShouldReturnDefaults()
    {
        // Arrange
        _settingsServiceMock
            .Setup(x => x.SettingExistsAsync(TestTenantId, "smtp:host", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _settingsServiceMock
            .Setup(x => x.GetSettingsAsync(null, "smtp:", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, string>());

        var query = new GetTenantSmtpSettingsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value;
        dto.Host.Should().Be(string.Empty);
        dto.Port.Should().Be(25);
        dto.Username.Should().BeNull();
        dto.HasPassword.Should().BeFalse();
        dto.FromEmail.Should().Be(string.Empty);
        dto.FromName.Should().Be(string.Empty);
        dto.UseSsl.Should().BeFalse();
        dto.IsConfigured.Should().BeFalse();
        dto.IsInherited.Should().BeTrue();
    }

    #endregion

    #region No Tenant Context

    [Fact]
    public async Task Handle_WithNoTenantContext_ShouldReturnPlatformSettings()
    {
        // Arrange
        SetupTenantContext(null);

        var platformSettings = new Dictionary<string, string>
        {
            { "smtp:host", "platform-smtp.example.com" },
            { "smtp:port", "587" }
        };

        _settingsServiceMock
            .Setup(x => x.GetSettingsAsync(null, "smtp:", It.IsAny<CancellationToken>()))
            .ReturnsAsync(platformSettings);

        var query = new GetTenantSmtpSettingsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Host.Should().Be("platform-smtp.example.com");
        result.Value.IsInherited.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithNullMultiTenantContext_ShouldReturnPlatformSettings()
    {
        // Arrange
        _tenantAccessorMock.Setup(x => x.MultiTenantContext).Returns((IMultiTenantContext?)null!);

        _settingsServiceMock
            .Setup(x => x.GetSettingsAsync(null, "smtp:", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, string>());

        var query = new GetTenantSmtpSettingsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsInherited.Should().BeTrue();
    }

    #endregion

    #region Port Parsing Edge Cases

    [Fact]
    public async Task Handle_WithInvalidPort_ShouldDefaultTo25()
    {
        // Arrange
        _settingsServiceMock
            .Setup(x => x.SettingExistsAsync(TestTenantId, "smtp:host", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var settings = new Dictionary<string, string>
        {
            { "smtp:host", "smtp.example.com" },
            { "smtp:port", "not-a-number" }
        };

        _settingsServiceMock
            .Setup(x => x.GetSettingsAsync(TestTenantId, "smtp:", It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        var query = new GetTenantSmtpSettingsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Port.Should().Be(25);
    }

    [Fact]
    public async Task Handle_WithMissingPort_ShouldDefaultTo25()
    {
        // Arrange
        _settingsServiceMock
            .Setup(x => x.SettingExistsAsync(TestTenantId, "smtp:host", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var settings = new Dictionary<string, string>
        {
            { "smtp:host", "smtp.example.com" }
        };

        _settingsServiceMock
            .Setup(x => x.GetSettingsAsync(TestTenantId, "smtp:", It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        var query = new GetTenantSmtpSettingsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Port.Should().Be(25);
    }

    #endregion

    #region SSL Parsing Edge Cases

    [Fact]
    public async Task Handle_WithInvalidSsl_ShouldDefaultToFalse()
    {
        // Arrange
        _settingsServiceMock
            .Setup(x => x.SettingExistsAsync(TestTenantId, "smtp:host", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var settings = new Dictionary<string, string>
        {
            { "smtp:host", "smtp.example.com" },
            { "smtp:use_ssl", "invalid-bool" }
        };

        _settingsServiceMock
            .Setup(x => x.GetSettingsAsync(TestTenantId, "smtp:", It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        var query = new GetTenantSmtpSettingsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.UseSsl.Should().BeFalse();
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("True", true)]
    [InlineData("false", false)]
    [InlineData("False", false)]
    public async Task Handle_WithVariousSslValues_ShouldParseCorrectly(string sslValue, bool expected)
    {
        // Arrange
        _settingsServiceMock
            .Setup(x => x.SettingExistsAsync(TestTenantId, "smtp:host", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var settings = new Dictionary<string, string>
        {
            { "smtp:host", "smtp.example.com" },
            { "smtp:use_ssl", sslValue }
        };

        _settingsServiceMock
            .Setup(x => x.GetSettingsAsync(TestTenantId, "smtp:", It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        var query = new GetTenantSmtpSettingsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.UseSsl.Should().Be(expected);
    }

    #endregion

    #region CancellationToken Scenarios

    [Fact]
    public async Task Handle_ShouldPassCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        _settingsServiceMock
            .Setup(x => x.SettingExistsAsync(TestTenantId, "smtp:host", token))
            .ReturnsAsync(false);

        _settingsServiceMock
            .Setup(x => x.GetSettingsAsync(null, "smtp:", token))
            .ReturnsAsync(new Dictionary<string, string>());

        var query = new GetTenantSmtpSettingsQuery();

        // Act
        await _handler.Handle(query, token);

        // Assert
        _settingsServiceMock.Verify(
            x => x.SettingExistsAsync(TestTenantId, "smtp:host", token),
            Times.Once);
    }

    #endregion
}
