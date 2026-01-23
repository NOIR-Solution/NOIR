using NOIR.Application.Features.PlatformSettings.DTOs;
using NOIR.Application.Features.PlatformSettings.Commands.UpdateSmtpSettings;

namespace NOIR.Application.UnitTests.Features.PlatformSettings;

/// <summary>
/// Unit tests for UpdateSmtpSettingsCommandHandler.
/// Tests updating platform SMTP settings via tenant settings service.
/// </summary>
public class UpdateSmtpSettingsCommandHandlerTests
{
    private readonly Mock<ITenantSettingsService> _settingsServiceMock;
    private readonly Mock<ILogger<UpdateSmtpSettingsCommandHandler>> _loggerMock;
    private readonly UpdateSmtpSettingsCommandHandler _handler;

    public UpdateSmtpSettingsCommandHandlerTests()
    {
        _settingsServiceMock = new Mock<ITenantSettingsService>();
        _loggerMock = new Mock<ILogger<UpdateSmtpSettingsCommandHandler>>();

        _handler = new UpdateSmtpSettingsCommandHandler(
            _settingsServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithAllFields_ShouldUpdateAllSettings()
    {
        // Arrange
        var command = new UpdateSmtpSettingsCommand(
            Host: "smtp.example.com",
            Port: 587,
            Username: "user@example.com",
            Password: "secret123",
            FromEmail: "noreply@example.com",
            FromName: "NOIR System",
            UseSsl: true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Host.Should().Be("smtp.example.com");
        result.Value.Port.Should().Be(587);
        result.Value.Username.Should().Be("user@example.com");
        result.Value.HasPassword.Should().BeTrue();
        result.Value.FromEmail.Should().Be("noreply@example.com");
        result.Value.FromName.Should().Be("NOIR System");
        result.Value.UseSsl.Should().BeTrue();
        result.Value.IsConfigured.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldCallSetSettingForRequiredFields()
    {
        // Arrange
        var command = new UpdateSmtpSettingsCommand(
            Host: "smtp.example.com",
            Port: 587,
            Username: "user@example.com",
            Password: "secret123",
            FromEmail: "noreply@example.com",
            FromName: "NOIR System",
            UseSsl: true);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert - All required fields should be set at platform level (null tenantId)
        _settingsServiceMock.Verify(x => x.SetSettingAsync(null, "smtp:host", "smtp.example.com", "string", It.IsAny<CancellationToken>()), Times.Once);
        _settingsServiceMock.Verify(x => x.SetSettingAsync(null, "smtp:port", "587", "int", It.IsAny<CancellationToken>()), Times.Once);
        _settingsServiceMock.Verify(x => x.SetSettingAsync(null, "smtp:from_email", "noreply@example.com", "string", It.IsAny<CancellationToken>()), Times.Once);
        _settingsServiceMock.Verify(x => x.SetSettingAsync(null, "smtp:from_name", "NOIR System", "string", It.IsAny<CancellationToken>()), Times.Once);
        _settingsServiceMock.Verify(x => x.SetSettingAsync(null, "smtp:use_ssl", "true", "bool", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNullUsername_ShouldNotUpdateUsername()
    {
        // Arrange
        var command = new UpdateSmtpSettingsCommand(
            Host: "smtp.example.com",
            Port: 25,
            Username: null,
            Password: null,
            FromEmail: "noreply@example.com",
            FromName: "System",
            UseSsl: false);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert - Username should not be updated when null
        _settingsServiceMock.Verify(x => x.SetSettingAsync(null, "smtp:username", It.IsAny<string>(), "string", It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithNullPassword_ShouldNotUpdatePassword()
    {
        // Arrange
        var command = new UpdateSmtpSettingsCommand(
            Host: "smtp.example.com",
            Port: 25,
            Username: "user",
            Password: null, // Keep existing
            FromEmail: "noreply@example.com",
            FromName: "System",
            UseSsl: false);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert - Password should not be updated when null (keeps existing)
        _settingsServiceMock.Verify(x => x.SetSettingAsync(null, "smtp:password", It.IsAny<string>(), "string", It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithEmptyPassword_ShouldClearPassword()
    {
        // Arrange
        var command = new UpdateSmtpSettingsCommand(
            Host: "smtp.example.com",
            Port: 25,
            Username: "user",
            Password: "", // Clear password
            FromEmail: "noreply@example.com",
            FromName: "System",
            UseSsl: false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _settingsServiceMock.Verify(x => x.SetSettingAsync(null, "smtp:password", "", "string", It.IsAny<CancellationToken>()), Times.Once);
        result.Value.HasPassword.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithProvidedPassword_ShouldSetHasPasswordTrue()
    {
        // Arrange
        var command = new UpdateSmtpSettingsCommand(
            Host: "smtp.example.com",
            Port: 25,
            Username: "user",
            Password: "newpassword",
            FromEmail: "noreply@example.com",
            FromName: "System",
            UseSsl: false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Value.HasPassword.Should().BeTrue();
    }

    [Theory]
    [InlineData(true, "true")]
    [InlineData(false, "false")]
    public async Task Handle_UseSsl_ShouldStoreLowercaseString(bool useSsl, string expected)
    {
        // Arrange
        var command = new UpdateSmtpSettingsCommand(
            Host: "smtp.example.com",
            Port: 25,
            Username: null,
            Password: null,
            FromEmail: "noreply@example.com",
            FromName: "System",
            UseSsl: useSsl);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _settingsServiceMock.Verify(x => x.SetSettingAsync(null, "smtp:use_ssl", expected, "bool", It.IsAny<CancellationToken>()), Times.Once);
    }
}
