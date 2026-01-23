using NOIR.Application.Features.TenantSettings.DTOs;
using NOIR.Application.Features.TenantSettings.Queries.GetContactSettings;

namespace NOIR.Application.UnitTests.Features.TenantSettings;

/// <summary>
/// Unit tests for GetContactSettingsQueryHandler.
/// Tests retrieval of contact settings from tenant settings service.
/// </summary>
public class GetContactSettingsQueryHandlerTests
{
    private const string TestTenantId = "test-tenant-id";

    private readonly Mock<ITenantSettingsService> _settingsServiceMock;
    private readonly Mock<IMultiTenantContextAccessor> _tenantAccessorMock;
    private readonly GetContactSettingsQueryHandler _handler;

    public GetContactSettingsQueryHandlerTests()
    {
        _settingsServiceMock = new Mock<ITenantSettingsService>();
        _tenantAccessorMock = new Mock<IMultiTenantContextAccessor>();

        var mockTenantContext = new Mock<IMultiTenantContext>();
        mockTenantContext.Setup(x => x.TenantInfo).Returns(new Tenant(TestTenantId, "test-tenant", "Test Tenant"));
        _tenantAccessorMock.Setup(x => x.MultiTenantContext).Returns(mockTenantContext.Object);

        _handler = new GetContactSettingsQueryHandler(
            _settingsServiceMock.Object,
            _tenantAccessorMock.Object);
    }

    [Fact]
    public async Task Handle_WithAllSettings_ShouldReturnFullDto()
    {
        // Arrange
        var settings = new Dictionary<string, string>
        {
            ["contact:email"] = "contact@example.com",
            ["contact:phone"] = "+1 555-123-4567",
            ["contact:address"] = "123 Main St, City"
        };

        _settingsServiceMock
            .Setup(x => x.GetSettingsAsync(TestTenantId, "contact:", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyDictionary<string, string>)settings);

        // Act
        var result = await _handler.Handle(new GetContactSettingsQuery(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be("contact@example.com");
        result.Value.Phone.Should().Be("+1 555-123-4567");
        result.Value.Address.Should().Be("123 Main St, City");
    }

    [Fact]
    public async Task Handle_WithNoSettings_ShouldReturnNullValues()
    {
        // Arrange
        var settings = new Dictionary<string, string>();

        _settingsServiceMock
            .Setup(x => x.GetSettingsAsync(TestTenantId, "contact:", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyDictionary<string, string>)settings);

        // Act
        var result = await _handler.Handle(new GetContactSettingsQuery(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().BeNull();
        result.Value.Phone.Should().BeNull();
        result.Value.Address.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldCallGetSettingsWithCorrectPrefix()
    {
        // Arrange
        _settingsServiceMock
            .Setup(x => x.GetSettingsAsync(It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyDictionary<string, string>)new Dictionary<string, string>());

        // Act
        await _handler.Handle(new GetContactSettingsQuery(), CancellationToken.None);

        // Assert
        _settingsServiceMock.Verify(
            x => x.GetSettingsAsync(TestTenantId, "contact:", It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
