namespace NOIR.Application.UnitTests.Infrastructure;

/// <summary>
/// Unit tests for DomainEventInterceptor.
/// Tests that sync SaveChanges throws and async SaveChanges processes domain events.
/// Note: Since no AggregateRoot entities exist in the domain yet, domain event
/// dispatching is tested through the interceptor's public behavior.
/// </summary>
public class DomainEventInterceptorTests
{
    private readonly Mock<IMessageBus> _messageBusMock;
    private readonly DomainEventInterceptor _sut;

    public DomainEventInterceptorTests()
    {
        _messageBusMock = new Mock<IMessageBus>();
        _sut = new DomainEventInterceptor(_messageBusMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldAcceptMessageBus()
    {
        // Act
        var interceptor = new DomainEventInterceptor(_messageBusMock.Object);

        // Assert
        interceptor.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullMessageBus_ShouldNotThrow()
    {
        // Act - Constructor accepts null but will fail when used
        var act = () => new DomainEventInterceptor(null!);

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region SavedChanges Sync Tests

    [Fact]
    public void SavedChanges_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var eventData = CreateSaveChangesCompletedEventData();

        // Act
        var act = () => _sut.SavedChanges(eventData, 1);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void SavedChanges_ExceptionMessage_ShouldMentionSaveChangesAsync()
    {
        // Arrange
        var eventData = CreateSaveChangesCompletedEventData();

        // Act
        var act = () => _sut.SavedChanges(eventData, 1);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*SaveChangesAsync*");
    }

    [Fact]
    public void SavedChanges_ExceptionMessage_ShouldMentionThreadPoolStarvation()
    {
        // Arrange
        var eventData = CreateSaveChangesCompletedEventData();

        // Act
        var act = () => _sut.SavedChanges(eventData, 1);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*thread pool starvation*");
    }

    [Fact]
    public void SavedChanges_ExceptionMessage_ShouldMentionSynchronous()
    {
        // Arrange
        var eventData = CreateSaveChangesCompletedEventData();

        // Act
        var act = () => _sut.SavedChanges(eventData, 1);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Synchronous*");
    }

    #endregion

    #region SavedChangesAsync Tests

    [Fact]
    public async Task SavedChangesAsync_ShouldReturnResult()
    {
        // Arrange
        var eventData = CreateSaveChangesCompletedEventData();
        var expectedResult = 5;

        // Act
        var result = await _sut.SavedChangesAsync(eventData, expectedResult, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Fact]
    public async Task SavedChangesAsync_WithNullContext_ShouldNotThrow()
    {
        // Arrange
        var eventData = CreateSaveChangesCompletedEventDataWithNullContext();
        Exception? caughtException = null;

        // Act
        try
        {
            await _sut.SavedChangesAsync(eventData, 1, CancellationToken.None);
        }
        catch (Exception ex)
        {
            caughtException = ex;
        }

        // Assert
        caughtException.Should().BeNull();
    }

    [Fact]
    public async Task SavedChangesAsync_WithNullContext_ShouldReturnResult()
    {
        // Arrange
        var eventData = CreateSaveChangesCompletedEventDataWithNullContext();
        var expectedResult = 3;

        // Act
        var result = await _sut.SavedChangesAsync(eventData, expectedResult, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Fact]
    public async Task SavedChangesAsync_WithNullContext_ShouldNotPublishAnyEvents()
    {
        // Arrange
        var eventData = CreateSaveChangesCompletedEventDataWithNullContext();

        // Act
        await _sut.SavedChangesAsync(eventData, 1, CancellationToken.None);

        // Assert
        _messageBusMock.Verify(x => x.PublishAsync(It.IsAny<object>()), Times.Never);
    }

    #endregion

    #region Inheritance Tests

    [Fact]
    public void DomainEventInterceptor_ShouldInheritFromSaveChangesInterceptor()
    {
        // Assert
        _sut.Should().BeAssignableTo<SaveChangesInterceptor>();
    }

    #endregion

    #region Helper Methods

    private static SaveChangesCompletedEventData CreateSaveChangesCompletedEventData()
    {
        var eventDefinition = new EventDefinition(
            Mock.Of<ILoggingOptions>(),
            CoreEventId.SaveChangesCompleted,
            LogLevel.Debug,
            "SaveChangesCompleted",
            level => (logger, ex) => { }
        );

        return new SaveChangesCompletedEventData(
            eventDefinition,
            (d, p) => "",
            null!,
            1
        );
    }

    private static SaveChangesCompletedEventData CreateSaveChangesCompletedEventDataWithNullContext()
    {
        var eventDefinition = new EventDefinition(
            Mock.Of<ILoggingOptions>(),
            CoreEventId.SaveChangesCompleted,
            LogLevel.Debug,
            "SaveChangesCompleted",
            level => (logger, ex) => { }
        );

        return new SaveChangesCompletedEventData(
            eventDefinition,
            (d, p) => "",
            null!,
            1
        );
    }

    #endregion
}
