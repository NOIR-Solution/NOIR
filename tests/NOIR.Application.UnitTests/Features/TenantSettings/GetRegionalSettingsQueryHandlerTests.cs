using NOIR.Application.Features.TenantSettings.DTOs;
using NOIR.Application.Features.TenantSettings.Queries.GetRegionalSettings;

namespace NOIR.Application.UnitTests.Features.TenantSettings;

/// <summary>
/// Unit tests for GetRegionalSettingsQueryHandler.
/// Tests retrieval of regional settings from tenant settings service.
/// </summary>
public class GetRegionalSettingsQueryHandlerTests
{
    private const string TestTenantId = "test-tenant-id";

    private readonly Mock<ITenantSettingsService> _settingsServiceMock;
    private readonly Mock<IMultiTenantContextAccessor> _tenantAccessorMock;
    private readonly GetRegionalSettingsQueryHandler _handler;

    public GetRegionalSettingsQueryHandlerTests()
    {
        _settingsServiceMock = new Mock<ITenantSettingsService>();
        _tenantAccessorMock = new Mock<IMultiTenantContextAccessor>();

        var mockTenantContext = new Mock<IMultiTenantContext>();
        mockTenantContext.Setup(x => x.TenantInfo).Returns(new Tenant(TestTenantId, "test-tenant", "Test Tenant"));
        _tenantAccessorMock.Setup(x => x.MultiTenantContext).Returns(mockTenantContext.Object);

        _handler = new GetRegionalSettingsQueryHandler(
            _settingsServiceMock.Object,
            _tenantAccessorMock.Object);
    }

    [Fact]
    public async Task Handle_WithAllSettings_ShouldReturnFullDto()
    {
        // Arrange
        var settings = new Dictionary<string, string>
        {
            ["regional:timezone"] = "America/New_York",
            ["regional:language"] = "en-US",
            ["regional:date_format"] = "MM/DD/YYYY"
        };

        _settingsServiceMock
            .Setup(x => x.GetSettingsAsync(TestTenantId, "regional:", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyDictionary<string, string>)settings);

        // Act
        var result = await _handler.Handle(new GetRegionalSettingsQuery(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Timezone.Should().Be("America/New_York");
        result.Value.Language.Should().Be("en-US");
        result.Value.DateFormat.Should().Be("MM/DD/YYYY");
    }

    [Fact]
    public async Task Handle_WithNoSettings_ShouldReturnDefaultValues()
    {
        // Arrange
        var settings = new Dictionary<string, string>();

        _settingsServiceMock
            .Setup(x => x.GetSettingsAsync(TestTenantId, "regional:", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyDictionary<string, string>)settings);

        // Act
        var result = await _handler.Handle(new GetRegionalSettingsQuery(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Default values are defined in the handler
        result.Value.Timezone.Should().Be("UTC");
        result.Value.Language.Should().Be("en");
        result.Value.DateFormat.Should().Be("YYYY-MM-DD");
    }

    [Fact]
    public async Task Handle_WithPartialSettings_ShouldMergeWithDefaults()
    {
        // Arrange
        var settings = new Dictionary<string, string>
        {
            ["regional:timezone"] = "Europe/London"
        };

        _settingsServiceMock
            .Setup(x => x.GetSettingsAsync(TestTenantId, "regional:", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyDictionary<string, string>)settings);

        // Act
        var result = await _handler.Handle(new GetRegionalSettingsQuery(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Timezone.Should().Be("Europe/London");
        result.Value.Language.Should().Be("en"); // Default
        result.Value.DateFormat.Should().Be("YYYY-MM-DD"); // Default
    }

    [Fact]
    public async Task Handle_ShouldCallGetSettingsWithCorrectPrefix()
    {
        // Arrange
        _settingsServiceMock
            .Setup(x => x.GetSettingsAsync(It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyDictionary<string, string>)new Dictionary<string, string>());

        // Act
        await _handler.Handle(new GetRegionalSettingsQuery(), CancellationToken.None);

        // Assert
        _settingsServiceMock.Verify(
            x => x.GetSettingsAsync(TestTenantId, "regional:", It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
