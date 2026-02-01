namespace NOIR.Application.UnitTests.Infrastructure;

/// <summary>
/// Tests for domain event dispatching in DomainEventInterceptor.
/// Uses a test DbContext with mock aggregate roots to verify event dispatching behavior.
/// </summary>
public class DomainEventDispatchingTests
{
    private readonly Mock<IMessageBus> _messageBusMock;
    private readonly Mock<ILogger<DomainEventInterceptor>> _loggerMock;
    private readonly DomainEventInterceptor _sut;

    public DomainEventDispatchingTests()
    {
        _messageBusMock = new Mock<IMessageBus>();
        _loggerMock = new Mock<ILogger<DomainEventInterceptor>>();
        _sut = new DomainEventInterceptor(_messageBusMock.Object, _loggerMock.Object);
    }

    #region Test Fixtures

    private record TestDomainEvent(string Message) : DomainEvent;

    private class TestAggregate : AggregateRoot<Guid>
    {
        public string Name { get; private set; } = string.Empty;

        private TestAggregate() : base() { }

        public TestAggregate(Guid id, string name) : base(id)
        {
            Name = name;
        }

        public static TestAggregate Create(string name)
        {
            var aggregate = new TestAggregate(Guid.NewGuid(), name);
            aggregate.RaiseEvent(new TestDomainEvent($"Created: {name}"));
            return aggregate;
        }

        public void UpdateName(string newName)
        {
            var oldName = Name;
            Name = newName;
            RaiseEvent(new TestDomainEvent($"Updated: {oldName} -> {newName}"));
        }

        public void RaiseEvent(IDomainEvent domainEvent) => AddDomainEvent(domainEvent);
    }

    private class TestDbContext : DbContext
    {
        public DbSet<TestAggregate> TestAggregates => Set<TestAggregate>();

        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestAggregate>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(200);

                // Ignore IAuditableEntity properties for this test
                entity.Ignore(e => e.CreatedBy);
                entity.Ignore(e => e.ModifiedBy);
                entity.Ignore(e => e.IsDeleted);
                entity.Ignore(e => e.DeletedAt);
                entity.Ignore(e => e.DeletedBy);
            });
        }
    }

    private static TestDbContext CreateTestContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new TestDbContext(options);
    }

    private SaveChangesCompletedEventData CreateEventDataWithContext(DbContext context)
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
            context,
            1
        );
    }

    #endregion

    #region Domain Event Dispatching Tests

    [Fact]
    public async Task SavedChangesAsync_WithAggregateHavingDomainEvents_ShouldDispatchEvents()
    {
        // Arrange
        using var context = CreateTestContext();
        var aggregate = TestAggregate.Create("Test Entity");
        context.TestAggregates.Add(aggregate);
        await context.SaveChangesAsync(); // First save without interceptor

        // Now test the interceptor
        aggregate.UpdateName("Updated Name");
        var eventData = CreateEventDataWithContext(context);

        // Act
        await _sut.SavedChangesAsync(eventData, 1, CancellationToken.None);

        // Assert - PublishAsync is called with IDomainEvent, not the concrete type
        _messageBusMock.Verify(
            x => x.PublishAsync(It.IsAny<IDomainEvent>(), default),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task SavedChangesAsync_WithMultipleDomainEvents_ShouldDispatchAll()
    {
        // Arrange
        using var context = CreateTestContext();
        var aggregate = TestAggregate.Create("Test");
        aggregate.RaiseEvent(new TestDomainEvent("Event 2"));
        aggregate.RaiseEvent(new TestDomainEvent("Event 3"));
        context.TestAggregates.Add(aggregate);

        var eventData = CreateEventDataWithContext(context);

        // Act
        await _sut.SavedChangesAsync(eventData, 1, CancellationToken.None);

        // Assert - 3 events: 1 from Create + 2 additional
        _messageBusMock.Verify(
            x => x.PublishAsync(It.IsAny<IDomainEvent>(), default),
            Times.Exactly(3));
    }

    [Fact]
    public async Task SavedChangesAsync_WithMultipleAggregates_ShouldDispatchAllEvents()
    {
        // Arrange
        using var context = CreateTestContext();
        var aggregate1 = TestAggregate.Create("Entity 1");
        var aggregate2 = TestAggregate.Create("Entity 2");
        context.TestAggregates.AddRange(aggregate1, aggregate2);

        var eventData = CreateEventDataWithContext(context);

        // Act
        await _sut.SavedChangesAsync(eventData, 2, CancellationToken.None);

        // Assert - 2 events: 1 from each Create
        _messageBusMock.Verify(
            x => x.PublishAsync(It.IsAny<IDomainEvent>(), default),
            Times.Exactly(2));
    }

    [Fact]
    public async Task SavedChangesAsync_ShouldClearDomainEventsAfterDispatching()
    {
        // Arrange
        using var context = CreateTestContext();
        var aggregate = TestAggregate.Create("Test");
        context.TestAggregates.Add(aggregate);

        var eventData = CreateEventDataWithContext(context);

        // Act
        await _sut.SavedChangesAsync(eventData, 1, CancellationToken.None);

        // Assert
        aggregate.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task SavedChangesAsync_WithNoDomainEvents_ShouldNotPublishAnything()
    {
        // Arrange
        using var context = CreateTestContext();
        var aggregate = new TestAggregate(Guid.NewGuid(), "No Events");
        context.TestAggregates.Add(aggregate);

        var eventData = CreateEventDataWithContext(context);

        // Act
        await _sut.SavedChangesAsync(eventData, 1, CancellationToken.None);

        // Assert
        _messageBusMock.Verify(
            x => x.PublishAsync(It.IsAny<object>(), default),
            Times.Never);
    }

    [Fact]
    public async Task SavedChangesAsync_WithEmptyChangeTracker_ShouldNotPublishAnything()
    {
        // Arrange
        using var context = CreateTestContext();
        var eventData = CreateEventDataWithContext(context);

        // Act
        await _sut.SavedChangesAsync(eventData, 0, CancellationToken.None);

        // Assert
        _messageBusMock.Verify(
            x => x.PublishAsync(It.IsAny<object>(), default),
            Times.Never);
    }

    #endregion

    #region Event Order Tests

    [Fact]
    public async Task SavedChangesAsync_ShouldDispatchEventsInOrder()
    {
        // Arrange
        using var context = CreateTestContext();
        var aggregate = TestAggregate.Create("Test");
        aggregate.UpdateName("Name 1");
        aggregate.UpdateName("Name 2");
        context.TestAggregates.Add(aggregate);

        var publishedMessages = new List<string>();
        _messageBusMock
            .Setup(x => x.PublishAsync(It.IsAny<IDomainEvent>(), default))
            .Callback<IDomainEvent, DeliveryOptions?>((msg, _) =>
            {
                if (msg is TestDomainEvent evt)
                    publishedMessages.Add(evt.Message);
            })
            .Returns(ValueTask.CompletedTask);

        var eventData = CreateEventDataWithContext(context);

        // Act
        await _sut.SavedChangesAsync(eventData, 1, CancellationToken.None);

        // Assert - Events should be dispatched in order they were raised
        publishedMessages.Should().HaveCount(3);
        publishedMessages[0].Should().Contain("Created");
        publishedMessages[1].Should().Contain("Name 1");
        publishedMessages[2].Should().Contain("Name 2");
    }

    #endregion

    #region Return Value Tests

    [Fact]
    public async Task SavedChangesAsync_ShouldReturnOriginalResult()
    {
        // Arrange
        using var context = CreateTestContext();
        var aggregate = TestAggregate.Create("Test");
        context.TestAggregates.Add(aggregate);

        var eventData = CreateEventDataWithContext(context);
        var expectedResult = 42;

        // Act
        var result = await _sut.SavedChangesAsync(eventData, expectedResult, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResult);
    }

    #endregion

    #region Wolverine Not Started Tests

    [Fact]
    public async Task SavedChangesAsync_WhenWolverineNotStarted_ShouldNotThrow()
    {
        // Arrange
        using var context = CreateTestContext();
        var aggregate = TestAggregate.Create("Test Entity");
        context.TestAggregates.Add(aggregate);

        // Configure mock to throw WolverineHasNotStartedException
        _messageBusMock
            .Setup(x => x.PublishAsync(It.IsAny<IDomainEvent>(), default))
            .ThrowsAsync(new Wolverine.WolverineHasNotStartedException());

        var eventData = CreateEventDataWithContext(context);

        // Act - Should NOT throw
        var act = async () => await _sut.SavedChangesAsync(eventData, 1, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SavedChangesAsync_WhenWolverineNotStarted_ShouldReturnResult()
    {
        // Arrange
        using var context = CreateTestContext();
        var aggregate = TestAggregate.Create("Test Entity");
        context.TestAggregates.Add(aggregate);

        _messageBusMock
            .Setup(x => x.PublishAsync(It.IsAny<IDomainEvent>(), default))
            .ThrowsAsync(new Wolverine.WolverineHasNotStartedException());

        var eventData = CreateEventDataWithContext(context);
        var expectedResult = 5;

        // Act
        var result = await _sut.SavedChangesAsync(eventData, expectedResult, CancellationToken.None);

        // Assert - Operation completes successfully with correct result
        result.Should().Be(expectedResult);
    }

    [Fact]
    public async Task SavedChangesAsync_WhenWolverineNotStarted_ShouldClearDomainEvents()
    {
        // Arrange
        using var context = CreateTestContext();
        var aggregate = TestAggregate.Create("Test Entity");
        context.TestAggregates.Add(aggregate);

        _messageBusMock
            .Setup(x => x.PublishAsync(It.IsAny<IDomainEvent>(), default))
            .ThrowsAsync(new Wolverine.WolverineHasNotStartedException());

        var eventData = CreateEventDataWithContext(context);

        // Act
        await _sut.SavedChangesAsync(eventData, 1, CancellationToken.None);

        // Assert - Events should still be cleared even when Wolverine not started
        aggregate.DomainEvents.Should().BeEmpty();
    }

    #endregion
}
