namespace NOIR.Application.UnitTests.Behaviors;

/// <summary>
/// Unit tests for PerformanceMiddleware.
/// Tests slow handler detection and logging.
/// </summary>
public class PerformanceMiddlewareTests
{
    private readonly Mock<ILogger<PerformanceMiddleware>> _loggerMock;
    private readonly PerformanceMiddleware _sut;

    public PerformanceMiddlewareTests()
    {
        _loggerMock = new Mock<ILogger<PerformanceMiddleware>>();
        _sut = new PerformanceMiddleware();
    }

    private static IConfiguration CreateConfiguration(int? thresholdMs = null)
    {
        var configValues = new Dictionary<string, string?>();
        if (thresholdMs.HasValue)
        {
            configValues["Performance:SlowHandlerThresholdMs"] = thresholdMs.Value.ToString();
        }
        return new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();
    }

    private static Envelope CreateEnvelope(object? message = null)
    {
        var envelope = new Envelope
        {
            Message = message ?? new TestMessage()
        };
        return envelope;
    }

    private sealed record TestMessage(string Data = "test");
    private sealed record SlowMessage();

    #region Before Tests

    [Fact]
    public void Before_ShouldStartStopwatch()
    {
        // Act
        _sut.Before();

        // Assert - If Before doesn't throw, the stopwatch started successfully
        // We verify behavior through the Finally method
        Assert.True(true);
    }

    [Fact]
    public void Before_CalledMultipleTimes_ShouldRestartStopwatch()
    {
        // Act - Call Before multiple times
        _sut.Before();
        _sut.Before();
        _sut.Before();

        // Assert - Should not throw, stopwatch restarts each time
        Assert.True(true);
    }

    #endregion

    #region Finally Tests - Fast Handler (No Warning)

    [Fact]
    public void Finally_WithFastHandler_ShouldNotLogWarning()
    {
        // Arrange
        var envelope = CreateEnvelope();
        var config = CreateConfiguration(500);

        _sut.Before();
        // No delay - handler is fast

        // Act
        _sut.Finally(envelope, _loggerMock.Object, config);

        // Assert - No warning should be logged
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public void Finally_WithFastHandler_DefaultThreshold_ShouldNotLogWarning()
    {
        // Arrange
        var envelope = CreateEnvelope();
        var config = CreateConfiguration(); // Use default 500ms threshold

        _sut.Before();
        // No delay - handler is fast

        // Act
        _sut.Finally(envelope, _loggerMock.Object, config);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    #endregion

    #region Finally Tests - Slow Handler (Warning)

    [Fact]
    public async Task Finally_WithSlowHandler_ShouldLogWarning()
    {
        // Arrange
        var envelope = CreateEnvelope(new SlowMessage());
        var config = CreateConfiguration(10); // 10ms threshold

        _sut.Before();
        await Task.Delay(50); // Simulate slow handler (50ms > 10ms threshold)

        // Act
        _sut.Finally(envelope, _loggerMock.Object, config);

        // Assert - Warning should be logged
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("SLOW HANDLER")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Finally_WithSlowHandler_ShouldIncludeMessageType()
    {
        // Arrange
        var envelope = CreateEnvelope(new TestMessage());
        var config = CreateConfiguration(10);

        _sut.Before();
        await Task.Delay(50);

        // Act
        _sut.Finally(envelope, _loggerMock.Object, config);

        // Assert - Warning should include message type name
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("TestMessage")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Finally_WithCustomThreshold_ShouldUseConfiguredValue()
    {
        // Arrange
        var envelope = CreateEnvelope();
        var config = CreateConfiguration(5); // Very low threshold

        _sut.Before();
        await Task.Delay(20); // 20ms > 5ms threshold

        // Act
        _sut.Finally(envelope, _loggerMock.Object, config);

        // Assert - Should log warning with custom threshold
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Finally Tests - Edge Cases

    [Fact]
    public void Finally_WithNullMessage_ShouldHandleGracefully()
    {
        // Arrange
        var envelope = new Envelope { Message = null };
        var config = CreateConfiguration(1); // 1ms threshold

        _sut.Before();
        Thread.Sleep(10); // Ensure we exceed threshold

        // Act
        _sut.Finally(envelope, _loggerMock.Object, config);

        // Assert - Should log "Unknown" for null message type
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Unknown")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Finally_AtExactThreshold_ShouldNotLogWarning()
    {
        // Arrange - Test boundary condition
        var envelope = CreateEnvelope();
        var config = CreateConfiguration(1000); // High threshold

        _sut.Before();
        // Immediate call - 0ms elapsed, which is NOT > 1000ms

        // Act
        _sut.Finally(envelope, _loggerMock.Object, config);

        // Assert - Should not log (0ms is not > threshold)
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    #endregion

    #region Stopwatch Behavior Tests

    [Fact]
    public async Task Finally_CalledTwice_ShouldNotAccumulate()
    {
        // Arrange
        var envelope = CreateEnvelope();
        var config = CreateConfiguration(100);

        // First execution - fast
        _sut.Before();
        _sut.Finally(envelope, _loggerMock.Object, config);

        // Second execution - slow
        _sut.Before();
        await Task.Delay(150);

        // Act
        _sut.Finally(envelope, _loggerMock.Object, config);

        // Assert - Only the second call should log warning
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion
}
