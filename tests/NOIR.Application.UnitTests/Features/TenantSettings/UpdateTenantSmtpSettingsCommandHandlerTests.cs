using NOIR.Application.Features.TenantSettings.Commands.UpdateTenantSmtpSettings;

namespace NOIR.Application.UnitTests.Features.TenantSettings;

/// <summary>
/// Unit tests for UpdateTenantSmtpSettingsCommandHandler.
/// Tests Copy-on-Write pattern for tenant SMTP settings.
/// </summary>
public class UpdateTenantSmtpSettingsCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<ITenantSettingsService> _settingsServiceMock;
    private readonly Mock<IMultiTenantContextAccessor> _tenantAccessorMock;
    private readonly Mock<ILogger<UpdateTenantSmtpSettingsCommandHandler>> _loggerMock;
    private readonly UpdateTenantSmtpSettingsCommandHandler _handler;

    private const string TestTenantId = "tenant-abc";

    public UpdateTenantSmtpSettingsCommandHandlerTests()
    {
        _settingsServiceMock = new Mock<ITenantSettingsService>();
        _tenantAccessorMock = new Mock<IMultiTenantContextAccessor>();
        _loggerMock = new Mock<ILogger<UpdateTenantSmtpSettingsCommandHandler>>();

        SetupTenantContext(TestTenantId);

        _handler = new UpdateTenantSmtpSettingsCommandHandler(
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

    private static UpdateTenantSmtpSettingsCommand CreateValidCommand(
        string host = "smtp.example.com",
        int port = 587,
        string? username = "user@example.com",
        string? password = "password123",
        string fromEmail = "noreply@example.com",
        string fromName = "Test App",
        bool useSsl = true)
    {
        return new UpdateTenantSmtpSettingsCommand(
            Host: host,
            Port: port,
            Username: username,
            Password: password,
            FromEmail: fromEmail,
            FromName: fromName,
            UseSsl: useSsl);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidCommand_ShouldUpdateAllSmtpSettings()
    {
        // Arrange
        var command = CreateValidCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        _settingsServiceMock.Verify(x => x.SetSettingAsync(TestTenantId, "smtp:host", "smtp.example.com", "string", It.IsAny<CancellationToken>()), Times.Once);
        _settingsServiceMock.Verify(x => x.SetSettingAsync(TestTenantId, "smtp:port", "587", "int", It.IsAny<CancellationToken>()), Times.Once);
        _settingsServiceMock.Verify(x => x.SetSettingAsync(TestTenantId, "smtp:from_email", "noreply@example.com", "string", It.IsAny<CancellationToken>()), Times.Once);
        _settingsServiceMock.Verify(x => x.SetSettingAsync(TestTenantId, "smtp:from_name", "Test App", "string", It.IsAny<CancellationToken>()), Times.Once);
        _settingsServiceMock.Verify(x => x.SetSettingAsync(TestTenantId, "smtp:use_ssl", "true", "bool", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldReturnCorrectDto()
    {
        // Arrange
        var command = CreateValidCommand(
            host: "mail.company.com",
            port: 465,
            username: "admin@company.com",
            password: "secret",
            fromEmail: "alerts@company.com",
            fromName: "Company Alerts",
            useSsl: true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        var dto = result.Value;
        dto.Host.ShouldBe("mail.company.com");
        dto.Port.ShouldBe(465);
        dto.Username.ShouldBe("admin@company.com");
        dto.HasPassword.ShouldBe(true);
        dto.FromEmail.ShouldBe("alerts@company.com");
        dto.FromName.ShouldBe("Company Alerts");
        dto.UseSsl.ShouldBe(true);
        dto.IsConfigured.ShouldBe(true);
        dto.IsInherited.ShouldBe(false);
    }

    [Fact]
    public async Task Handle_WithUseSslFalse_ShouldStoreFalseValue()
    {
        // Arrange
        var command = CreateValidCommand(useSsl: false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        _settingsServiceMock.Verify(
            x => x.SetSettingAsync(TestTenantId, "smtp:use_ssl", "false", "bool", It.IsAny<CancellationToken>()),
            Times.Once);
        result.Value.UseSsl.ShouldBe(false);
    }

    #endregion

    #region Username/Password Null Handling

    [Fact]
    public async Task Handle_WithNullUsername_ShouldNotUpdateUsername()
    {
        // Arrange
        var command = CreateValidCommand(username: null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        _settingsServiceMock.Verify(
            x => x.SetSettingAsync(TestTenantId, "smtp:username", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithNullPassword_ShouldNotUpdatePassword()
    {
        // Arrange
        var command = CreateValidCommand(password: null);

        // Setup existing password check
        _settingsServiceMock
            .Setup(x => x.GetSettingAsync(TestTenantId, "smtp:password", It.IsAny<CancellationToken>()))
            .ReturnsAsync("existing-password");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        _settingsServiceMock.Verify(
            x => x.SetSettingAsync(TestTenantId, "smtp:password", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithNullPasswordAndExistingPassword_ShouldReportHasPassword()
    {
        // Arrange
        var command = CreateValidCommand(password: null);

        _settingsServiceMock
            .Setup(x => x.GetSettingAsync(TestTenantId, "smtp:password", It.IsAny<CancellationToken>()))
            .ReturnsAsync("existing-password");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.HasPassword.ShouldBe(true);
    }

    [Fact]
    public async Task Handle_WithEmptyPassword_ShouldClearPassword()
    {
        // Arrange
        var command = CreateValidCommand(password: "");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        _settingsServiceMock.Verify(
            x => x.SetSettingAsync(TestTenantId, "smtp:password", "", "string", It.IsAny<CancellationToken>()),
            Times.Once);
        result.Value.HasPassword.ShouldBe(false);
    }

    [Fact]
    public async Task Handle_WithProvidedPassword_ShouldStorePassword()
    {
        // Arrange
        var command = CreateValidCommand(password: "new-password");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        _settingsServiceMock.Verify(
            x => x.SetSettingAsync(TestTenantId, "smtp:password", "new-password", "string", It.IsAny<CancellationToken>()),
            Times.Once);
        result.Value.HasPassword.ShouldBe(true);
    }

    #endregion

    #region Tenant Context Validation

    [Fact]
    public async Task Handle_WithNoTenantContext_ShouldReturnValidationError()
    {
        // Arrange
        SetupTenantContext(null);
        var command = CreateValidCommand();

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
        var command = CreateValidCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.Error.Type.ShouldBe(ErrorType.Validation);
    }

    #endregion
}
