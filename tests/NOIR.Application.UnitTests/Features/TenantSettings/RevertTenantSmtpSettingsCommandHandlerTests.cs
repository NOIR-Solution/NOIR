using NOIR.Application.Features.TenantSettings.Commands.RevertTenantSmtpSettings;

namespace NOIR.Application.UnitTests.Features.TenantSettings;

/// <summary>
/// Unit tests for RevertTenantSmtpSettingsCommandHandler.
/// Tests reverting tenant SMTP settings to platform defaults.
/// </summary>
public class RevertTenantSmtpSettingsCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<ITenantSettingsService> _settingsServiceMock;
    private readonly Mock<IMultiTenantContextAccessor> _tenantAccessorMock;
    private readonly Mock<ILogger<RevertTenantSmtpSettingsCommandHandler>> _loggerMock;
    private readonly RevertTenantSmtpSettingsCommandHandler _handler;

    private const string TestTenantId = "tenant-abc";

    public RevertTenantSmtpSettingsCommandHandlerTests()
    {
        _settingsServiceMock = new Mock<ITenantSettingsService>();
        _tenantAccessorMock = new Mock<IMultiTenantContextAccessor>();
        _loggerMock = new Mock<ILogger<RevertTenantSmtpSettingsCommandHandler>>();

        SetupTenantContext(TestTenantId);

        _handler = new RevertTenantSmtpSettingsCommandHandler(
            _settingsServiceMock.Object,
            _tenantAccessorMock.Object,
            _loggerMock.Object);
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

    private void SetupPlatformDefaults(Dictionary<string, string>? settings = null)
    {
        var platformSettings = settings ?? new Dictionary<string, string>
        {
            { "smtp:host", "platform-smtp.example.com" },
            { "smtp:port", "25" },
            { "smtp:username", "platform@example.com" },
            { "smtp:password", "platform-password" },
            { "smtp:from_email", "noreply@platform.com" },
            { "smtp:from_name", "Platform" },
            { "smtp:use_ssl", "true" }
        };

        _settingsServiceMock
            .Setup(x => x.GetSettingsAsync(null, "smtp:", It.IsAny<CancellationToken>()))
            .ReturnsAsync(platformSettings);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_ShouldDeleteAllTenantSmtpSettings()
    {
        // Arrange
        SetupPlatformDefaults();
        var command = new RevertTenantSmtpSettingsCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        _settingsServiceMock.Verify(x => x.DeleteSettingAsync(TestTenantId, "smtp:host", It.IsAny<CancellationToken>()), Times.Once);
        _settingsServiceMock.Verify(x => x.DeleteSettingAsync(TestTenantId, "smtp:port", It.IsAny<CancellationToken>()), Times.Once);
        _settingsServiceMock.Verify(x => x.DeleteSettingAsync(TestTenantId, "smtp:username", It.IsAny<CancellationToken>()), Times.Once);
        _settingsServiceMock.Verify(x => x.DeleteSettingAsync(TestTenantId, "smtp:password", It.IsAny<CancellationToken>()), Times.Once);
        _settingsServiceMock.Verify(x => x.DeleteSettingAsync(TestTenantId, "smtp:from_email", It.IsAny<CancellationToken>()), Times.Once);
        _settingsServiceMock.Verify(x => x.DeleteSettingAsync(TestTenantId, "smtp:from_name", It.IsAny<CancellationToken>()), Times.Once);
        _settingsServiceMock.Verify(x => x.DeleteSettingAsync(TestTenantId, "smtp:use_ssl", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnPlatformDefaultSettings()
    {
        // Arrange
        SetupPlatformDefaults();
        var command = new RevertTenantSmtpSettingsCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        var dto = result.Value;
        dto.Host.ShouldBe("platform-smtp.example.com");
        dto.Port.ShouldBe(25);
        dto.Username.ShouldBe("platform@example.com");
        dto.HasPassword.ShouldBe(true);
        dto.FromEmail.ShouldBe("noreply@platform.com");
        dto.FromName.ShouldBe("Platform");
        dto.UseSsl.ShouldBe(true);
        dto.IsInherited.ShouldBe(true);
        dto.IsConfigured.ShouldBe(true);
    }

    [Fact]
    public async Task Handle_WithNoPlatformDefaults_ShouldReturnEmptySettings()
    {
        // Arrange
        _settingsServiceMock
            .Setup(x => x.GetSettingsAsync(null, "smtp:", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, string>());

        var command = new RevertTenantSmtpSettingsCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        var dto = result.Value;
        dto.Host.ShouldBe(string.Empty);
        dto.Port.ShouldBe(25); // Default port
        dto.Username.ShouldBeNull();
        dto.HasPassword.ShouldBe(false);
        dto.FromEmail.ShouldBe(string.Empty);
        dto.FromName.ShouldBe(string.Empty);
        dto.UseSsl.ShouldBe(false);
        dto.IsConfigured.ShouldBe(false);
        dto.IsInherited.ShouldBe(true);
    }

    [Fact]
    public async Task Handle_WithPartialPlatformDefaults_ShouldUseDefaultsForMissing()
    {
        // Arrange
        var partialSettings = new Dictionary<string, string>
        {
            { "smtp:host", "partial-host.com" },
            { "smtp:port", "465" }
        };
        SetupPlatformDefaults(partialSettings);

        var command = new RevertTenantSmtpSettingsCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Host.ShouldBe("partial-host.com");
        result.Value.Port.ShouldBe(465);
        result.Value.Username.ShouldBeNull();
        result.Value.HasPassword.ShouldBe(false);
        result.Value.FromEmail.ShouldBe(string.Empty);
    }

    #endregion

    #region Port Parsing

    [Fact]
    public async Task Handle_WithInvalidPortInPlatformDefaults_ShouldDefaultTo25()
    {
        // Arrange
        var settings = new Dictionary<string, string>
        {
            { "smtp:host", "host.com" },
            { "smtp:port", "not-a-number" }
        };
        SetupPlatformDefaults(settings);

        var command = new RevertTenantSmtpSettingsCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Port.ShouldBe(25);
    }

    #endregion

    #region SSL Parsing

    [Fact]
    public async Task Handle_WithInvalidSslInPlatformDefaults_ShouldDefaultToFalse()
    {
        // Arrange
        var settings = new Dictionary<string, string>
        {
            { "smtp:host", "host.com" },
            { "smtp:use_ssl", "invalid" }
        };
        SetupPlatformDefaults(settings);

        var command = new RevertTenantSmtpSettingsCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.UseSsl.ShouldBe(false);
    }

    #endregion

    #region Tenant Context Validation

    [Fact]
    public async Task Handle_WithNoTenantContext_ShouldReturnValidationError()
    {
        // Arrange
        SetupTenantContext(null);
        var command = new RevertTenantSmtpSettingsCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.Error.Type.ShouldBe(ErrorType.Validation);
    }

    [Fact]
    public async Task Handle_WithNullMultiTenantContext_ShouldReturnValidationError()
    {
        // Arrange
        _tenantAccessorMock.Setup(x => x.MultiTenantContext).Returns((IMultiTenantContext?)null!);
        var command = new RevertTenantSmtpSettingsCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.Error.Type.ShouldBe(ErrorType.Validation);
    }

    #endregion

    #region CancellationToken Scenarios

    [Fact]
    public async Task Handle_ShouldPassCancellationToken()
    {
        // Arrange
        SetupPlatformDefaults();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        var command = new RevertTenantSmtpSettingsCommand();

        // Act
        await _handler.Handle(command, token);

        // Assert
        _settingsServiceMock.Verify(
            x => x.DeleteSettingAsync(TestTenantId, It.IsAny<string>(), token),
            Times.Exactly(7)); // 7 SMTP keys
        _settingsServiceMock.Verify(
            x => x.GetSettingsAsync(null, "smtp:", token),
            Times.Once);
    }

    #endregion
}
