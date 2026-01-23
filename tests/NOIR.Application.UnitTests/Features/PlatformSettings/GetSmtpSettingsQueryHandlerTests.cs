using NOIR.Application.Features.PlatformSettings.DTOs;
using NOIR.Application.Features.PlatformSettings.Queries.GetSmtpSettings;

namespace NOIR.Application.UnitTests.Features.PlatformSettings;

/// <summary>
/// Unit tests for GetSmtpSettingsQueryHandler.
/// Tests retrieval of platform SMTP settings from tenant settings service.
/// </summary>
public class GetSmtpSettingsQueryHandlerTests
{
    private readonly Mock<ITenantSettingsService> _settingsServiceMock;
    private readonly GetSmtpSettingsQueryHandler _handler;

    public GetSmtpSettingsQueryHandlerTests()
    {
        _settingsServiceMock = new Mock<ITenantSettingsService>();
        _handler = new GetSmtpSettingsQueryHandler(_settingsServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WithAllSettings_ShouldReturnFullDto()
    {
        // Arrange
        var settings = new Dictionary<string, string>
        {
            ["smtp:host"] = "smtp.example.com",
            ["smtp:port"] = "587",
            ["smtp:username"] = "user@example.com",
            ["smtp:password"] = "secret123",
            ["smtp:from_email"] = "noreply@example.com",
            ["smtp:from_name"] = "NOIR System",
            ["smtp:use_ssl"] = "true"
        };

        _settingsServiceMock
            .Setup(x => x.GetSettingsAsync(null, "smtp:", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyDictionary<string, string>)settings);

        // Act
        var result = await _handler.Handle(new GetSmtpSettingsQuery(), CancellationToken.None);

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
    public async Task Handle_WithNoSettings_ShouldReturnDefaults()
    {
        // Arrange
        var settings = new Dictionary<string, string>();

        _settingsServiceMock
            .Setup(x => x.GetSettingsAsync(null, "smtp:", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyDictionary<string, string>)settings);

        // Act
        var result = await _handler.Handle(new GetSmtpSettingsQuery(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Host.Should().BeEmpty();
        result.Value.Port.Should().Be(25); // Default port
        result.Value.Username.Should().BeNull();
        result.Value.HasPassword.Should().BeFalse();
        result.Value.FromEmail.Should().BeEmpty();
        result.Value.FromName.Should().BeEmpty();
        result.Value.UseSsl.Should().BeFalse();
        result.Value.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithEmptyPassword_ShouldSetHasPasswordFalse()
    {
        // Arrange
        var settings = new Dictionary<string, string>
        {
            ["smtp:host"] = "smtp.example.com",
            ["smtp:password"] = ""
        };

        _settingsServiceMock
            .Setup(x => x.GetSettingsAsync(null, "smtp:", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyDictionary<string, string>)settings);

        // Act
        var result = await _handler.Handle(new GetSmtpSettingsQuery(), CancellationToken.None);

        // Assert
        result.Value.HasPassword.Should().BeFalse();
        result.Value.IsConfigured.Should().BeTrue(); // Has at least one setting
    }

    [Theory]
    [InlineData("25", 25)]
    [InlineData("587", 587)]
    [InlineData("465", 465)]
    [InlineData("invalid", 25)] // Falls back to default
    [InlineData("", 25)] // Falls back to default
    public async Task Handle_PortParsing_ShouldHandleVariousInputs(string portValue, int expected)
    {
        // Arrange
        var settings = new Dictionary<string, string> { ["smtp:port"] = portValue };

        _settingsServiceMock
            .Setup(x => x.GetSettingsAsync(null, "smtp:", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyDictionary<string, string>)settings);

        // Act
        var result = await _handler.Handle(new GetSmtpSettingsQuery(), CancellationToken.None);

        // Assert
        result.Value.Port.Should().Be(expected);
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("True", true)]
    [InlineData("false", false)]
    [InlineData("invalid", false)] // Falls back to false
    [InlineData("", false)]
    public async Task Handle_UseSslParsing_ShouldHandleVariousInputs(string sslValue, bool expected)
    {
        // Arrange
        var settings = new Dictionary<string, string> { ["smtp:use_ssl"] = sslValue };

        _settingsServiceMock
            .Setup(x => x.GetSettingsAsync(null, "smtp:", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyDictionary<string, string>)settings);

        // Act
        var result = await _handler.Handle(new GetSmtpSettingsQuery(), CancellationToken.None);

        // Assert
        result.Value.UseSsl.Should().Be(expected);
    }

    [Fact]
    public async Task Handle_ShouldCallGetSettingsWithNullTenantId()
    {
        // Arrange
        _settingsServiceMock
            .Setup(x => x.GetSettingsAsync(It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyDictionary<string, string>)new Dictionary<string, string>());

        // Act
        await _handler.Handle(new GetSmtpSettingsQuery(), CancellationToken.None);

        // Assert - Should use null tenantId for platform-level settings
        _settingsServiceMock.Verify(
            x => x.GetSettingsAsync(null, "smtp:", It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
