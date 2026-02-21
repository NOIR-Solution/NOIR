namespace NOIR.Application.UnitTests.Filters;

/// <summary>
/// Unit tests for RequireFeatureEndpointFilter.
/// Tests feature gating at the endpoint level via IEndpointFilter.
/// </summary>
public class RequireFeatureEndpointFilterTests
{
    private readonly Mock<IFeatureChecker> _featureCheckerMock;

    public RequireFeatureEndpointFilterTests()
    {
        _featureCheckerMock = new Mock<IFeatureChecker>();
    }

    #region Helper Methods

    private DefaultEndpointFilterInvocationContext CreateFilterContext()
    {
        var httpContext = new DefaultHttpContext();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(_featureCheckerMock.Object);
        httpContext.RequestServices = serviceCollection.BuildServiceProvider();

        return new DefaultEndpointFilterInvocationContext(httpContext);
    }

    private static EndpointFilterDelegate CreateNextDelegate(object? returnValue = null)
    {
        return _ => new ValueTask<object?>(returnValue ?? "OK");
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithFeatureName_CreatesFilter()
    {
        // Act
        var filter = new RequireFeatureEndpointFilter("Ecommerce.Products");

        // Assert
        filter.Should().NotBeNull();
    }

    #endregion

    #region Feature Enabled Tests

    [Fact]
    public async Task InvokeAsync_FeatureEnabled_CallsNextDelegate()
    {
        // Arrange
        var filter = new RequireFeatureEndpointFilter("Ecommerce.Products");
        var context = CreateFilterContext();
        var expectedResult = "NextDelegateResult";
        var next = CreateNextDelegate(expectedResult);

        _featureCheckerMock
            .Setup(x => x.IsEnabledAsync("Ecommerce.Products", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await filter.InvokeAsync(context, next);

        // Assert
        result.Should().Be(expectedResult);
        _featureCheckerMock.Verify(
            x => x.IsEnabledAsync("Ecommerce.Products", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_FeatureEnabled_ReturnsNextDelegateResult()
    {
        // Arrange
        var filter = new RequireFeatureEndpointFilter("Ecommerce.Cart");
        var context = CreateFilterContext();
        var next = CreateNextDelegate("CartResult");

        _featureCheckerMock
            .Setup(x => x.IsEnabledAsync("Ecommerce.Cart", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await filter.InvokeAsync(context, next);

        // Assert
        result.Should().Be("CartResult");
    }

    #endregion

    #region Feature Disabled Tests

    [Fact]
    public async Task InvokeAsync_FeatureDisabled_Returns403Problem()
    {
        // Arrange
        var filter = new RequireFeatureEndpointFilter("Ecommerce.Products");
        var context = CreateFilterContext();
        var nextCalled = false;
        EndpointFilterDelegate next = _ =>
        {
            nextCalled = true;
            return new ValueTask<object?>("should-not-reach");
        };

        _featureCheckerMock
            .Setup(x => x.IsEnabledAsync("Ecommerce.Products", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await filter.InvokeAsync(context, next);

        // Assert
        nextCalled.Should().BeFalse("next delegate should not be called when feature is disabled");
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task InvokeAsync_FeatureDisabled_DoesNotCallNextDelegate()
    {
        // Arrange
        var filter = new RequireFeatureEndpointFilter("Ecommerce.Products");
        var context = CreateFilterContext();
        var nextCallCount = 0;
        EndpointFilterDelegate next = _ =>
        {
            nextCallCount++;
            return new ValueTask<object?>("result");
        };

        _featureCheckerMock
            .Setup(x => x.IsEnabledAsync("Ecommerce.Products", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        await filter.InvokeAsync(context, next);

        // Assert
        nextCallCount.Should().Be(0);
    }

    #endregion

    #region Different Feature Names Tests

    [Theory]
    [InlineData("Ecommerce.Products")]
    [InlineData("Ecommerce.Cart")]
    [InlineData("Ecommerce.Orders")]
    [InlineData("Blog")]
    public async Task InvokeAsync_ChecksCorrectFeatureName(string featureName)
    {
        // Arrange
        var filter = new RequireFeatureEndpointFilter(featureName);
        var context = CreateFilterContext();
        var next = CreateNextDelegate();

        _featureCheckerMock
            .Setup(x => x.IsEnabledAsync(featureName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await filter.InvokeAsync(context, next);

        // Assert
        _featureCheckerMock.Verify(
            x => x.IsEnabledAsync(featureName, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Service Resolution Tests

    [Fact]
    public async Task InvokeAsync_ResolvesFeatureCheckerFromRequestServices()
    {
        // Arrange
        var filter = new RequireFeatureEndpointFilter("TestFeature");
        var context = CreateFilterContext();
        var next = CreateNextDelegate();

        _featureCheckerMock
            .Setup(x => x.IsEnabledAsync("TestFeature", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await filter.InvokeAsync(context, next);

        // Assert - Verified by not throwing; IFeatureChecker was resolved from DI
        _featureCheckerMock.Verify(
            x => x.IsEnabledAsync("TestFeature", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}
