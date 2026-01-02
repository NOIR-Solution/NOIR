namespace NOIR.Domain.UnitTests.Common;

/// <summary>
/// Unit tests for AggregateRoot base class.
/// </summary>
public class AggregateRootTests
{
    #region Test Fixtures

    // Concrete implementation for testing
    private class TestAggregate : AggregateRoot<Guid>
    {
        public string Name { get; private set; } = string.Empty;

        private TestAggregate() : base() { }

        public TestAggregate(Guid id) : base(id)
        {
            Name = "Test";
        }

        public static TestAggregate Create(string name)
        {
            var aggregate = new TestAggregate(Guid.NewGuid()) { Name = name };
            aggregate.RaiseDomainEvent(new TestCreatedEvent(aggregate.Id, name));
            return aggregate;
        }

        public void UpdateName(string newName)
        {
            var oldName = Name;
            Name = newName;
            RaiseDomainEvent(new TestUpdatedEvent(Id, oldName, newName));
        }

        // Expose protected methods for testing
        public void RaiseDomainEvent(IDomainEvent domainEvent) => AddDomainEvent(domainEvent);
        public void RemoveEvent(IDomainEvent domainEvent) => RemoveDomainEvent(domainEvent);
    }

    private record TestCreatedEvent(Guid AggregateId, string Name) : DomainEvent;
    private record TestUpdatedEvent(Guid AggregateId, string OldName, string NewName) : DomainEvent;

    #endregion

    [Fact]
    public void Create_ShouldInitializeDomainEventsCollection()
    {
        // Act
        var aggregate = TestAggregate.Create("Test Entity");

        // Assert
        aggregate.DomainEvents.Should().NotBeNull();
    }

    [Fact]
    public void AddDomainEvent_ShouldAddEventToCollection()
    {
        // Arrange
        var aggregate = TestAggregate.Create("Test");

        // Assert - Create already adds one event
        aggregate.DomainEvents.Should().HaveCount(1);
        aggregate.DomainEvents.Should().ContainSingle(e => e is TestCreatedEvent);
    }

    [Fact]
    public void MultipleDomainEvents_ShouldBeCollected()
    {
        // Arrange
        var aggregate = TestAggregate.Create("Original");

        // Act
        aggregate.UpdateName("Updated");

        // Assert
        aggregate.DomainEvents.Should().HaveCount(2);
        aggregate.DomainEvents.Should().Contain(e => e is TestCreatedEvent);
        aggregate.DomainEvents.Should().Contain(e => e is TestUpdatedEvent);
    }

    [Fact]
    public void RemoveDomainEvent_ShouldRemoveEventFromCollection()
    {
        // Arrange
        var aggregate = TestAggregate.Create("Test");
        var eventToRemove = aggregate.DomainEvents.First();

        // Act
        aggregate.RemoveEvent(eventToRemove);

        // Assert
        aggregate.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ClearDomainEvents_ShouldRemoveAllEvents()
    {
        // Arrange
        var aggregate = TestAggregate.Create("Test");
        aggregate.UpdateName("Updated");

        // Act
        aggregate.ClearDomainEvents();

        // Assert
        aggregate.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void DomainEvents_ShouldBeReadOnly()
    {
        // Arrange
        var aggregate = TestAggregate.Create("Test");

        // Assert
        aggregate.DomainEvents.Should().BeOfType<System.Collections.ObjectModel.ReadOnlyCollection<IDomainEvent>>();
    }

    [Fact]
    public void AggregateRoot_ShouldInheritFromEntity()
    {
        // Arrange
        var id = Guid.NewGuid();
        var aggregate = new TestAggregate(id);

        // Assert
        aggregate.Id.Should().Be(id);
        aggregate.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void AggregateRoot_ShouldImplementIAuditableEntity()
    {
        // Arrange
        var aggregate = TestAggregate.Create("Test");

        // Assert - IAuditableEntity properties should be accessible
        aggregate.CreatedBy.Should().BeNull(); // Not set by domain
        aggregate.ModifiedBy.Should().BeNull();
        aggregate.IsDeleted.Should().BeFalse();
        aggregate.DeletedAt.Should().BeNull();
        aggregate.DeletedBy.Should().BeNull();
    }
}

/// <summary>
/// Unit tests for DomainEvent base record.
/// </summary>
public class DomainEventTests
{
    private record TestEvent : DomainEvent;

    [Fact]
    public void DomainEvent_ShouldGenerateUniqueEventId()
    {
        // Act
        var event1 = new TestEvent();
        var event2 = new TestEvent();

        // Assert
        event1.EventId.Should().NotBe(Guid.Empty);
        event2.EventId.Should().NotBe(Guid.Empty);
        event1.EventId.Should().NotBe(event2.EventId);
    }

    [Fact]
    public void DomainEvent_ShouldSetOccurredAtToUtcNow()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var domainEvent = new TestEvent();

        // Assert
        var after = DateTimeOffset.UtcNow;
        domainEvent.OccurredAt.Should().BeOnOrAfter(before);
        domainEvent.OccurredAt.Should().BeOnOrBefore(after);
    }
}
