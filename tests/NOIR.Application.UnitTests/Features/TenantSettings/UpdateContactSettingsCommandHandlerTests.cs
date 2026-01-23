using NOIR.Application.Features.TenantSettings.DTOs;
using NOIR.Application.Features.TenantSettings.Commands.UpdateContactSettings;

namespace NOIR.Application.UnitTests.Features.TenantSettings;

/// <summary>
/// Unit tests for UpdateContactSettingsCommandHandler.
/// Tests updating contact settings via tenant settings service.
/// </summary>
public class UpdateContactSettingsCommandHandlerTests
{
    #region Test Setup

    private const string TestTenantId = "test-tenant-id";

    private readonly Mock<ITenantSettingsService> _settingsServiceMock;
    private readonly Mock<IMultiTenantContextAccessor> _tenantAccessorMock;
    private readonly Mock<ILogger<UpdateContactSettingsCommandHandler>> _loggerMock;
    private readonly UpdateContactSettingsCommandHandler _handler;

    public UpdateContactSettingsCommandHandlerTests()
    {
        _settingsServiceMock = new Mock<ITenantSettingsService>();
        _tenantAccessorMock = new Mock<IMultiTenantContextAccessor>();
        _loggerMock = new Mock<ILogger<UpdateContactSettingsCommandHandler>>();

        SetupTenantContext(TestTenantId);

        _handler = new UpdateContactSettingsCommandHandler(
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
        var command = new UpdateContactSettingsCommand(
            Email: "contact@example.com",
            Phone: "+1 555-123-4567",
            Address: "123 Main St, City");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be("contact@example.com");
        result.Value.Phone.Should().Be("+1 555-123-4567");
        result.Value.Address.Should().Be("123 Main St, City");
    }

    [Fact]
    public async Task Handle_ShouldCallSetSettingForEachField()
    {
        // Arrange
        var command = new UpdateContactSettingsCommand(
            Email: "contact@example.com",
            Phone: "+1 555-123-4567",
            Address: "123 Main St, City");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _settingsServiceMock.Verify(x => x.SetSettingAsync(TestTenantId, "contact:email", "contact@example.com", "string", It.IsAny<CancellationToken>()), Times.Once);
        _settingsServiceMock.Verify(x => x.SetSettingAsync(TestTenantId, "contact:phone", "+1 555-123-4567", "string", It.IsAny<CancellationToken>()), Times.Once);
        _settingsServiceMock.Verify(x => x.SetSettingAsync(TestTenantId, "contact:address", "123 Main St, City", "string", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNullValues_ShouldStoreEmptyStrings()
    {
        // Arrange
        var command = new UpdateContactSettingsCommand(
            Email: null,
            Phone: null,
            Address: null);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _settingsServiceMock.Verify(x => x.SetSettingAsync(TestTenantId, "contact:email", string.Empty, "string", It.IsAny<CancellationToken>()), Times.Once);
        _settingsServiceMock.Verify(x => x.SetSettingAsync(TestTenantId, "contact:phone", string.Empty, "string", It.IsAny<CancellationToken>()), Times.Once);
        _settingsServiceMock.Verify(x => x.SetSettingAsync(TestTenantId, "contact:address", string.Empty, "string", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithPartialValues_ShouldStoreMixed()
    {
        // Arrange
        var command = new UpdateContactSettingsCommand(
            Email: "contact@example.com",
            Phone: null,
            Address: "123 Main St");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be("contact@example.com");
        result.Value.Phone.Should().BeNull();
        result.Value.Address.Should().Be("123 Main St");

        _settingsServiceMock.Verify(x => x.SetSettingAsync(TestTenantId, "contact:email", "contact@example.com", "string", It.IsAny<CancellationToken>()), Times.Once);
        _settingsServiceMock.Verify(x => x.SetSettingAsync(TestTenantId, "contact:phone", string.Empty, "string", It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
