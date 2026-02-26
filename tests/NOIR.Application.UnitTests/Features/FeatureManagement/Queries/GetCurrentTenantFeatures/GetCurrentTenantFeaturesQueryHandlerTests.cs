using NOIR.Application.Features.FeatureManagement.Queries.GetCurrentTenantFeatures;

namespace NOIR.Application.UnitTests.Features.FeatureManagement.Queries.GetCurrentTenantFeatures;

/// <summary>
/// Unit tests for GetCurrentTenantFeaturesQueryHandler.
/// Tests retrieval of effective feature states for the current tenant.
/// </summary>
public class GetCurrentTenantFeaturesQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IFeatureChecker> _featureCheckerMock;
    private readonly GetCurrentTenantFeaturesQueryHandler _handler;

    public GetCurrentTenantFeaturesQueryHandlerTests()
    {
        _featureCheckerMock = new Mock<IFeatureChecker>();
        _handler = new GetCurrentTenantFeaturesQueryHandler(_featureCheckerMock.Object);
    }

    #endregion

    #region Success Cases

    [Fact]
    public async Task Handle_ShouldReturnAllStatesFromFeatureChecker()
    {
        // Arrange
        var states = new Dictionary<string, EffectiveFeatureState>
        {
            ["Ecommerce.Products"] = new(true, true, true, false),
            ["Ecommerce.Orders"] = new(true, false, false, false),
            ["Auth"] = new(true, true, true, true)
        };
        _featureCheckerMock.Setup(x => x.GetAllStatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(states);
        var query = new GetCurrentTenantFeaturesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value["Ecommerce.Products"].IsEffective.Should().BeTrue();
        result.Value["Ecommerce.Orders"].IsEffective.Should().BeFalse();
        result.Value["Auth"].IsCore.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenNoFeatures_ShouldReturnEmptyDictionary()
    {
        // Arrange
        var states = new Dictionary<string, EffectiveFeatureState>();
        _featureCheckerMock.Setup(x => x.GetAllStatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(states);
        var query = new GetCurrentTenantFeaturesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccessResult()
    {
        // Arrange
        var states = new Dictionary<string, EffectiveFeatureState>
        {
            ["Dashboard"] = new(true, true, true, true)
        };
        _featureCheckerMock.Setup(x => x.GetAllStatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(states);
        var query = new GetCurrentTenantFeaturesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeAssignableTo<IReadOnlyDictionary<string, EffectiveFeatureState>>();
    }

    #endregion
}
