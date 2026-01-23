using NOIR.Application.Features.TenantSettings.DTOs;
using NOIR.Application.Features.TenantSettings.Commands.UpdateRegionalSettings;

namespace NOIR.Application.UnitTests.Features.TenantSettings;

/// <summary>
/// Unit tests for UpdateRegionalSettingsCommandHandler.
/// Tests updating regional settings via tenant settings service.
/// </summary>
public class UpdateRegionalSettingsCommandHandlerTests
{
    #region Test Setup

    private const string TestTenantId = "test-tenant-id";

    private readonly Mock<ITenantSettingsService> _settingsServiceMock;
    private readonly Mock<IMultiTenantContextAccessor> _tenantAccessorMock;
    private readonly Mock<ILogger<UpdateRegionalSettingsCommandHandler>> _loggerMock;
    private readonly UpdateRegionalSettingsCommandHandler _handler;

    public UpdateRegionalSettingsCommandHandlerTests()
    {
        _settingsServiceMock = new Mock<ITenantSettingsService>();
        _tenantAccessorMock = new Mock<IMultiTenantContextAccessor>();
        _loggerMock = new Mock<ILogger<UpdateRegionalSettingsCommandHandler>>();

        SetupTenantContext(TestTenantId);

        _handler = new UpdateRegionalSettingsCommandHandler(
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
        var command = new UpdateRegionalSettingsCommand(
            Timezone: "America/New_York",
            Language: "en-US",
            DateFormat: "MM/DD/YYYY");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Timezone.Should().Be("America/New_York");
        result.Value.Language.Should().Be("en-US");
        result.Value.DateFormat.Should().Be("MM/DD/YYYY");
    }

    [Fact]
    public async Task Handle_ShouldCallSetSettingForEachField()
    {
        // Arrange
        var command = new UpdateRegionalSettingsCommand(
            Timezone: "Europe/London",
            Language: "en-GB",
            DateFormat: "DD/MM/YYYY");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _settingsServiceMock.Verify(x => x.SetSettingAsync(TestTenantId, "regional:timezone", "Europe/London", "string", It.IsAny<CancellationToken>()), Times.Once);
        _settingsServiceMock.Verify(x => x.SetSettingAsync(TestTenantId, "regional:language", "en-GB", "string", It.IsAny<CancellationToken>()), Times.Once);
        _settingsServiceMock.Verify(x => x.SetSettingAsync(TestTenantId, "regional:date_format", "DD/MM/YYYY", "string", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("UTC", "en", "YYYY-MM-DD")]
    [InlineData("Asia/Tokyo", "ja", "YYYY/MM/DD")]
    [InlineData("Europe/Paris", "fr", "DD.MM.YYYY")]
    public async Task Handle_WithVariousFormats_ShouldStoreCorrectly(string timezone, string language, string dateFormat)
    {
        // Arrange
        var command = new UpdateRegionalSettingsCommand(
            Timezone: timezone,
            Language: language,
            DateFormat: dateFormat);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Timezone.Should().Be(timezone);
        result.Value.Language.Should().Be(language);
        result.Value.DateFormat.Should().Be(dateFormat);
    }

    #endregion
}
