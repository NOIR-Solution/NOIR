namespace NOIR.Application.UnitTests.Behaviors;

/// <summary>
/// Unit tests for LoggingMiddleware.
/// Tests structured logging of handler executions.
/// </summary>
public class LoggingMiddlewareTests
{
    private readonly Mock<ILogger<LoggingMiddleware>> _loggerMock;
    private readonly LoggingMiddleware _sut;

    public LoggingMiddlewareTests()
    {
        _loggerMock = new Mock<ILogger<LoggingMiddleware>>();
        _sut = new LoggingMiddleware();
    }

    #region Before Method Tests

    [Fact]
    public void Before_ShouldLogInformation()
    {
        // Arrange
        var envelope = CreateEnvelope(new TestCommand("test-value"));

        // Act
        _sut.Before(_loggerMock.Object, envelope);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Handling")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Before_ShouldLogMessageType()
    {
        // Arrange
        var envelope = CreateEnvelope(new TestCommand("test-value"));

        // Act
        _sut.Before(_loggerMock.Object, envelope);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("TestCommand")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Before_ShouldLogCorrelationId()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();
        var envelope = CreateEnvelopeWithCorrelationId(new TestCommand("test"), correlationId);

        // Act
        _sut.Before(_loggerMock.Object, envelope);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("CorrelationId")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region After Method Tests

    [Fact]
    public void After_ShouldNotLog_LoggingMovedToFinally()
    {
        // After method no longer logs - HTTP response status determines success/failure
        // All logging now happens in Finally method

        // Arrange
        var envelope = CreateEnvelope(new TestCommand("test-value"));
        _sut.Before(_loggerMock.Object, envelope);

        // Reset mock to clear Before's log call
        _loggerMock.Invocations.Clear();

        // Act
        _sut.After(_loggerMock.Object, envelope);

        // Assert - After should NOT log anything
        _loggerMock.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    #endregion

    #region Finally Method Tests

    [Fact]
    public void Finally_ShouldCompleteWithoutException()
    {
        // Arrange
        var envelope = CreateEnvelope(new TestCommand("test-value"));
        _sut.Before(_loggerMock.Object, envelope);

        // Act
        var act = () => _sut.Finally(_loggerMock.Object, envelope);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Finally_AfterBefore_ShouldStopStopwatch()
    {
        // Arrange
        var envelope = CreateEnvelope(new TestCommand("test-value"));
        _sut.Before(_loggerMock.Object, envelope);
        Thread.Sleep(10);

        // Act
        _sut.Finally(_loggerMock.Object, envelope);

        // After Finally, calling After should still work
        // (this tests that the state is properly managed)
        var act = () => _sut.After(_loggerMock.Object, envelope);

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region Full Lifecycle Tests

    [Fact]
    public void FullLifecycle_Success_ShouldLogBeforeAndAfter()
    {
        // Arrange
        var envelope = CreateEnvelope(new TestCommand("test-value"));

        // Act
        _sut.Before(_loggerMock.Object, envelope);
        _sut.After(_loggerMock.Object, envelope);
        _sut.Finally(_loggerMock.Object, envelope);

        // Assert - Should have 2 Information logs (Before and After)
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(2));
    }

    [Fact]
    public void FullLifecycle_WithDifferentMessageTypes_ShouldWork()
    {
        // Arrange
        var queryEnvelope = CreateEnvelope(new TestQuery());
        var commandEnvelope = CreateEnvelope(new TestCommand("value"));

        // Act & Assert - Both should work without exception
        var act1 = () =>
        {
            _sut.Before(_loggerMock.Object, queryEnvelope);
            _sut.After(_loggerMock.Object, queryEnvelope);
            _sut.Finally(_loggerMock.Object, queryEnvelope);
        };

        var act2 = () =>
        {
            _sut.Before(_loggerMock.Object, commandEnvelope);
            _sut.After(_loggerMock.Object, commandEnvelope);
            _sut.Finally(_loggerMock.Object, commandEnvelope);
        };

        act1.Should().NotThrow();
        act2.Should().NotThrow();
    }

    #endregion

    #region Null Message Tests

    [Fact]
    public void Before_WithNullMessage_ShouldLogUnknown()
    {
        // Arrange
        var envelope = CreateEnvelopeWithNullMessage();

        // Act
        _sut.Before(_loggerMock.Object, envelope);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Unknown")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Helper Methods

    private static Envelope CreateEnvelope(object message)
    {
        return new Envelope(message);
    }

    private static Envelope CreateEnvelopeWithCorrelationId(object message, string correlationId)
    {
        var envelope = new Envelope(message)
        {
            CorrelationId = correlationId
        };
        return envelope;
    }

    private static Envelope CreateEnvelopeWithNullMessage()
    {
        // Create envelope and set message to null via reflection for edge case testing
        var envelope = new Envelope(new object());
        var messageProperty = typeof(Envelope).GetProperty("Message");
        messageProperty?.SetValue(envelope, null);
        return envelope;
    }

    #endregion

    #region Test Messages

    private record TestCommand(string Value);
    private record TestQuery;

    #endregion
}
