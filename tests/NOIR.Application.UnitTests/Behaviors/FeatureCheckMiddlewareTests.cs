namespace NOIR.Application.UnitTests.Behaviors;

/// <summary>
/// Unit tests for FeatureCheckMiddleware.
/// Tests feature gating of Wolverine command/query pipeline via [RequiresFeature] attribute.
/// </summary>
public class FeatureCheckMiddlewareTests
{
    private readonly FeatureCheckMiddleware _sut;
    private readonly Mock<IFeatureChecker> _featureCheckerMock;
    private readonly Mock<ILogger<FeatureCheckMiddleware>> _loggerMock;

    public FeatureCheckMiddlewareTests()
    {
        _sut = new FeatureCheckMiddleware();
        _featureCheckerMock = new Mock<IFeatureChecker>();
        _loggerMock = new Mock<ILogger<FeatureCheckMiddleware>>();
    }

    #region Test Messages

    [RequiresFeature("Ecommerce.Products")]
    private sealed record TestCommandWithFeature;

    [RequiresFeature("Ecommerce.Products", "Ecommerce.Cart")]
    private sealed record TestCommandWithMultipleFeatures;

    private sealed record TestCommandWithoutFeature;

    #endregion

    #region Helper Methods

    private static Envelope CreateEnvelope(object message)
    {
        return new Envelope(message);
    }

    private static Envelope CreateEnvelopeWithNullMessage()
    {
        var envelope = new Envelope(new object());
        var messageProperty = typeof(Envelope).GetProperty("Message");
        messageProperty?.SetValue(envelope, null);
        return envelope;
    }

    #endregion

    #region No Attribute Tests

    [Fact]
    public async Task BeforeAsync_NoAttribute_DoesNotCheckFeatures()
    {
        // Arrange
        var envelope = CreateEnvelope(new TestCommandWithoutFeature());

        // Act
        await _sut.BeforeAsync(envelope, _featureCheckerMock.Object, _loggerMock.Object);

        // Assert
        _featureCheckerMock.Verify(
            x => x.IsEnabledAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Single Feature Tests

    [Fact]
    public async Task BeforeAsync_SingleFeatureEnabled_PassesWithoutException()
    {
        // Arrange
        var envelope = CreateEnvelope(new TestCommandWithFeature());
        _featureCheckerMock
            .Setup(x => x.IsEnabledAsync("Ecommerce.Products", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _sut.BeforeAsync(envelope, _featureCheckerMock.Object, _loggerMock.Object);

        // Assert
        _featureCheckerMock.Verify(
            x => x.IsEnabledAsync("Ecommerce.Products", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task BeforeAsync_SingleFeatureDisabled_ThrowsFeatureNotAvailableException()
    {
        // Arrange
        var envelope = CreateEnvelope(new TestCommandWithFeature());
        _featureCheckerMock
            .Setup(x => x.IsEnabledAsync("Ecommerce.Products", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = () => _sut.BeforeAsync(envelope, _featureCheckerMock.Object, _loggerMock.Object);

        // Assert
        var exception = await act.Should().ThrowAsync<FeatureNotAvailableException>();
        exception.Which.FeatureName.Should().Be("Ecommerce.Products");
    }

    #endregion

    #region Multiple Feature Tests

    [Fact]
    public async Task BeforeAsync_MultipleFeaturesAllEnabled_PassesWithoutException()
    {
        // Arrange
        var envelope = CreateEnvelope(new TestCommandWithMultipleFeatures());
        _featureCheckerMock
            .Setup(x => x.IsEnabledAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _sut.BeforeAsync(envelope, _featureCheckerMock.Object, _loggerMock.Object);

        // Assert
        _featureCheckerMock.Verify(
            x => x.IsEnabledAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        _featureCheckerMock.Verify(
            x => x.IsEnabledAsync("Ecommerce.Products", It.IsAny<CancellationToken>()),
            Times.Once);
        _featureCheckerMock.Verify(
            x => x.IsEnabledAsync("Ecommerce.Cart", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task BeforeAsync_MultipleFeaturesFirstDisabled_ThrowsForFirstFeature()
    {
        // Arrange
        var envelope = CreateEnvelope(new TestCommandWithMultipleFeatures());
        _featureCheckerMock
            .Setup(x => x.IsEnabledAsync("Ecommerce.Products", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = () => _sut.BeforeAsync(envelope, _featureCheckerMock.Object, _loggerMock.Object);

        // Assert
        var exception = await act.Should().ThrowAsync<FeatureNotAvailableException>();
        exception.Which.FeatureName.Should().Be("Ecommerce.Products");
        // Should not check the second feature since the first is disabled
        _featureCheckerMock.Verify(
            x => x.IsEnabledAsync("Ecommerce.Cart", It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task BeforeAsync_MultipleFeaturesSecondDisabled_ThrowsForSecondFeature()
    {
        // Arrange
        var envelope = CreateEnvelope(new TestCommandWithMultipleFeatures());
        _featureCheckerMock
            .Setup(x => x.IsEnabledAsync("Ecommerce.Products", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _featureCheckerMock
            .Setup(x => x.IsEnabledAsync("Ecommerce.Cart", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = () => _sut.BeforeAsync(envelope, _featureCheckerMock.Object, _loggerMock.Object);

        // Assert
        var exception = await act.Should().ThrowAsync<FeatureNotAvailableException>();
        exception.Which.FeatureName.Should().Be("Ecommerce.Cart");
    }

    #endregion

    #region Null Message Tests

    [Fact]
    public async Task BeforeAsync_NullMessage_DoesNotCheckFeatures()
    {
        // Arrange
        var envelope = CreateEnvelopeWithNullMessage();

        // Act
        await _sut.BeforeAsync(envelope, _featureCheckerMock.Object, _loggerMock.Object);

        // Assert
        _featureCheckerMock.Verify(
            x => x.IsEnabledAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Logging Tests

    [Fact]
    public async Task BeforeAsync_FeatureAttributePresent_LogsDebugForFeatureCheck()
    {
        // Arrange
        var envelope = CreateEnvelope(new TestCommandWithFeature());
        _featureCheckerMock
            .Setup(x => x.IsEnabledAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _sut.BeforeAsync(envelope, _featureCheckerMock.Object, _loggerMock.Object);

        // Assert - Should log debug messages for checking and completion
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Checking features")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task BeforeAsync_AllFeaturesEnabled_LogsDebugForCompletion()
    {
        // Arrange
        var envelope = CreateEnvelope(new TestCommandWithFeature());
        _featureCheckerMock
            .Setup(x => x.IsEnabledAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _sut.BeforeAsync(envelope, _featureCheckerMock.Object, _loggerMock.Object);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("All features enabled")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task BeforeAsync_FeatureDisabled_DoesNotLogCompletion()
    {
        // Arrange
        var envelope = CreateEnvelope(new TestCommandWithFeature());
        _featureCheckerMock
            .Setup(x => x.IsEnabledAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        try
        {
            await _sut.BeforeAsync(envelope, _featureCheckerMock.Object, _loggerMock.Object);
        }
        catch (FeatureNotAvailableException)
        {
            // Expected
        }

        // Assert - Should log "Checking features" but NOT "All features enabled"
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("All features enabled")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    #endregion
}
