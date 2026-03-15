namespace NOIR.Domain.UnitTests.Common;

/// <summary>
/// Extended tests for AggregateRoot to improve code coverage.
/// Tests parameterless constructor invocation and edge cases.
/// </summary>
public class AggregateRootExtendedTests
{
    #region Test Fixtures

    private class TestAggregate : AggregateRoot<Guid>
    {
        public string Name { get; private set; } = string.Empty;

        // Expose parameterless constructor for testing
        public TestAggregate() : base() { }

        public TestAggregate(Guid id) : base(id)
        {
            Name = "Test";
        }

        public void RaiseEvent(IDomainEvent domainEvent) => AddDomainEvent(domainEvent);
        public void RemoveEvent(IDomainEvent domainEvent) => RemoveDomainEvent(domainEvent);
    }

    private record TestEvent(string Message) : DomainEvent;

    #endregion

    #region Parameterless Constructor Tests

    [Fact]
    public void ParameterlessConstructor_ShouldCreateValidAggregate()
    {
        // Act
        var aggregate = new TestAggregate();

        // Assert
        aggregate.ShouldNotBeNull();
        aggregate.DomainEvents.ShouldNotBeNull();
        aggregate.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void ParameterlessConstructor_ShouldInitializeCreatedAt()
    {
        // Act
        var aggregate = new TestAggregate();

        // Assert
        aggregate.CreatedAt.ShouldBe(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ParameterlessConstructor_ShouldHaveDefaultId()
    {
        // Act
        var aggregate = new TestAggregate();

        // Assert
        aggregate.Id.ShouldBe(default(Guid));
    }

    [Fact]
    public void ParameterlessConstructor_ShouldInitializeAuditableProperties()
    {
        // Act
        var aggregate = new TestAggregate();

        // Assert
        aggregate.CreatedBy.ShouldBeNull();
        aggregate.ModifiedBy.ShouldBeNull();
        aggregate.ModifiedAt.ShouldBeNull();
        aggregate.IsDeleted.ShouldBeFalse();
        aggregate.DeletedAt.ShouldBeNull();
        aggregate.DeletedBy.ShouldBeNull();
    }

    #endregion

    #region Domain Events Edge Cases

    [Fact]
    public void RemoveDomainEvent_NonExistentEvent_ShouldNotThrow()
    {
        // Arrange
        var aggregate = new TestAggregate();
        var nonExistentEvent = new TestEvent("Non-existent");

        // Act
        var act = () => aggregate.RemoveEvent(nonExistentEvent);

        // Assert
        act.ShouldNotThrow();
        aggregate.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void AddDomainEvent_MultipleSameEvent_ShouldAddAll()
    {
        // Arrange
        var aggregate = new TestAggregate();
        var sameEvent = new TestEvent("Same");

        // Act
        aggregate.RaiseEvent(sameEvent);
        aggregate.RaiseEvent(sameEvent);

        // Assert
        aggregate.DomainEvents.Count().ShouldBe(2);
    }

    [Fact]
    public void ClearDomainEvents_WhenEmpty_ShouldNotThrow()
    {
        // Arrange
        var aggregate = new TestAggregate();

        // Act
        var act = () => aggregate.ClearDomainEvents();

        // Assert
        act.ShouldNotThrow();
        aggregate.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void DomainEvents_AfterClear_ShouldAllowNewEvents()
    {
        // Arrange
        var aggregate = new TestAggregate();
        aggregate.RaiseEvent(new TestEvent("First"));
        aggregate.ClearDomainEvents();

        // Act
        aggregate.RaiseEvent(new TestEvent("Second"));

        // Assert
        aggregate.DomainEvents.Count().ShouldBe(1);
        aggregate.DomainEvents.First().ShouldBeOfType<TestEvent>()
            .Message.ShouldBe("Second");
    }

    #endregion
}

/// <summary>
/// Extended tests for TenantEntity and TenantAggregateRoot to improve coverage.
/// Tests parameterless constructor invocation.
/// </summary>
public class TenantEntityExtendedTests
{
    #region Test Fixtures

    private class TestTenantEntity : TenantEntity<Guid>
    {
        public string Name { get; set; } = string.Empty;

        // Expose parameterless constructor for testing
        public TestTenantEntity() : base() { }

        public TestTenantEntity(Guid id, string? tenantId = null) : base(id, tenantId) { }
    }

    private class TestTenantAggregate : TenantAggregateRoot<Guid>
    {
        public string Name { get; set; } = string.Empty;

        // Expose parameterless constructor for testing
        public TestTenantAggregate() : base() { }

        public TestTenantAggregate(Guid id, string? tenantId = null) : base(id, tenantId) { }
    }

    #endregion

    #region TenantEntity Parameterless Constructor Tests

    [Fact]
    public void TenantEntity_ParameterlessConstructor_ShouldCreateValidEntity()
    {
        // Act
        var entity = new TestTenantEntity();

        // Assert
        entity.ShouldNotBeNull();
        entity.Id.ShouldBe(default(Guid));
        entity.TenantId.ShouldBeNull();
    }

    [Fact]
    public void TenantEntity_ParameterlessConstructor_ShouldInitializeCreatedAt()
    {
        // Act
        var entity = new TestTenantEntity();

        // Assert
        entity.CreatedAt.ShouldBe(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    #endregion

    #region TenantAggregateRoot Parameterless Constructor Tests

    [Fact]
    public void TenantAggregateRoot_ParameterlessConstructor_ShouldCreateValidAggregate()
    {
        // Act
        var aggregate = new TestTenantAggregate();

        // Assert
        aggregate.ShouldNotBeNull();
        aggregate.Id.ShouldBe(default(Guid));
        aggregate.TenantId.ShouldBeNull();
        aggregate.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void TenantAggregateRoot_ParameterlessConstructor_ShouldInitializeCreatedAt()
    {
        // Act
        var aggregate = new TestTenantAggregate();

        // Assert
        aggregate.CreatedAt.ShouldBe(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void TenantAggregateRoot_ParameterlessConstructor_ShouldInitializeAuditableProperties()
    {
        // Act
        var aggregate = new TestTenantAggregate();

        // Assert
        aggregate.IsDeleted.ShouldBeFalse();
        aggregate.CreatedBy.ShouldBeNull();
        aggregate.ModifiedBy.ShouldBeNull();
        aggregate.DeletedAt.ShouldBeNull();
        aggregate.DeletedBy.ShouldBeNull();
    }

    #endregion

    #region TenantId Protected Setter Verification Tests

    [Fact]
    public void TenantEntity_TenantIdSetter_ShouldBeProtected()
    {
        // Verify TenantId has a protected setter for security (prevents accidental cross-tenant access)
        // TenantId can only be set via constructor or by EF Core using property API

        // Arrange
        var tenantIdProperty = typeof(TenantEntity<Guid>).GetProperty(nameof(ITenantEntity.TenantId));

        // Assert
        tenantIdProperty.ShouldNotBeNull();
        tenantIdProperty!.SetMethod.ShouldNotBeNull();
        tenantIdProperty.SetMethod!.IsFamily.ShouldBeTrue("TenantId setter should be protected");
    }

    [Fact]
    public void TenantAggregateRoot_TenantIdSetter_ShouldBeProtected()
    {
        // Verify TenantId has a protected setter for security (prevents accidental cross-tenant access)
        // TenantId can only be set via constructor or by EF Core using property API

        // Arrange
        var tenantIdProperty = typeof(TenantAggregateRoot<Guid>).GetProperty(nameof(ITenantEntity.TenantId));

        // Assert
        tenantIdProperty.ShouldNotBeNull();
        tenantIdProperty!.SetMethod.ShouldNotBeNull();
        tenantIdProperty.SetMethod!.IsFamily.ShouldBeTrue("TenantId setter should be protected");
    }

    [Fact]
    public void TenantEntity_Constructor_ShouldSetTenantId()
    {
        // Arrange
        var tenantId = "test-tenant";

        // Act
        var entity = new TestTenantEntity(Guid.NewGuid(), tenantId);

        // Assert
        entity.TenantId.ShouldBe(tenantId);
    }

    [Fact]
    public void TenantAggregateRoot_Constructor_ShouldSetTenantId()
    {
        // Arrange
        var tenantId = "test-tenant";

        // Act
        var aggregate = new TestTenantAggregate(Guid.NewGuid(), tenantId);

        // Assert
        aggregate.TenantId.ShouldBe(tenantId);
    }

    #endregion
}
