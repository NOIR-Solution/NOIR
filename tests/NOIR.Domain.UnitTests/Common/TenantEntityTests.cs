namespace NOIR.Domain.UnitTests.Common;

/// <summary>
/// Unit tests for TenantEntity base class.
/// </summary>
public class TenantEntityTests
{
    #region Test Fixtures

    private class TestTenantEntity : TenantEntity<Guid>
    {
        public string Name { get; private set; } = string.Empty;

        private TestTenantEntity() : base() { }

        public TestTenantEntity(Guid id, string? tenantId = null) : base(id, tenantId)
        {
        }

        public static TestTenantEntity Create(string name, string? tenantId = null)
        {
            return new TestTenantEntity(Guid.NewGuid(), tenantId) { Name = name };
        }
    }

    #endregion

    [Fact]
    public void Create_WithTenantId_ShouldSetTenantId()
    {
        // Arrange
        var tenantId = "tenant-123";

        // Act
        var entity = TestTenantEntity.Create("Test", tenantId);

        // Assert
        entity.TenantId.ShouldBe(tenantId);
    }

    [Fact]
    public void Create_WithoutTenantId_ShouldHaveNullTenantId()
    {
        // Act
        var entity = TestTenantEntity.Create("Test");

        // Assert
        entity.TenantId.ShouldBeNull();
    }

    [Fact]
    public void TenantEntity_ShouldInheritFromEntity()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var entity = new TestTenantEntity(id, "tenant");

        // Assert
        entity.Id.ShouldBe(id);
        entity.CreatedAt.ShouldBe(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void TenantEntity_ShouldImplementITenantEntity()
    {
        // Arrange
        var entity = TestTenantEntity.Create("Test", "my-tenant");

        // Act
        ITenantEntity tenantEntity = entity;

        // Assert
        tenantEntity.TenantId.ShouldBe("my-tenant");
    }

    [Fact]
    public void TenantEntity_TenantIdProperty_ShouldHaveProtectedSetter()
    {
        // This test verifies TenantId has a protected setter (for security - prevents accidental cross-tenant access)
        // TenantId can only be set via constructor or by the TenantIdSetterInterceptor using EF Core's property API

        // Arrange
        var tenantIdProperty = typeof(TenantEntity<Guid>).GetProperty(nameof(ITenantEntity.TenantId));

        // Assert
        tenantIdProperty.ShouldNotBeNull();
        tenantIdProperty!.SetMethod.ShouldNotBeNull();
        tenantIdProperty.SetMethod!.IsFamily.ShouldBeTrue("TenantId setter should be protected");
    }

    [Fact]
    public void TenantEntity_ParameterlessConstructor_ShouldExist()
    {
        // Assert - Verify parameterless constructor exists (used by EF Core)
        var constructor = typeof(TenantEntity<Guid>).GetConstructor(
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null, Type.EmptyTypes, null);

        constructor.ShouldNotBeNull("EF Core requires a parameterless constructor");
    }

    [Fact]
    public void TenantEntity_ConstructorWithIdOnly_ShouldSetId()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var entity = new TestTenantEntity(id);

        // Assert
        entity.Id.ShouldBe(id);
        entity.TenantId.ShouldBeNull();
    }
}

/// <summary>
/// Unit tests for TenantAggregateRoot base class.
/// </summary>
public class TenantAggregateRootTests
{
    #region Test Fixtures

    private class TestTenantAggregate : TenantAggregateRoot<Guid>
    {
        public string Name { get; private set; } = string.Empty;

        private TestTenantAggregate() : base() { }

        public TestTenantAggregate(Guid id, string? tenantId = null) : base(id, tenantId)
        {
        }

        public static TestTenantAggregate Create(string name, string? tenantId = null)
        {
            return new TestTenantAggregate(Guid.NewGuid(), tenantId) { Name = name };
        }

        public void RaiseDomainEvent(IDomainEvent domainEvent) => AddDomainEvent(domainEvent);
    }

    private record TestEvent : DomainEvent;

    #endregion

    [Fact]
    public void Create_WithTenantId_ShouldSetTenantId()
    {
        // Arrange
        var tenantId = "tenant-abc";

        // Act
        var aggregate = TestTenantAggregate.Create("Test", tenantId);

        // Assert
        aggregate.TenantId.ShouldBe(tenantId);
    }

    [Fact]
    public void Create_WithoutTenantId_ShouldHaveNullTenantId()
    {
        // Act
        var aggregate = TestTenantAggregate.Create("Test");

        // Assert
        aggregate.TenantId.ShouldBeNull();
    }

    [Fact]
    public void TenantAggregateRoot_ShouldInheritFromAggregateRoot()
    {
        // Arrange
        var aggregate = TestTenantAggregate.Create("Test");

        // Act
        aggregate.RaiseDomainEvent(new TestEvent());

        // Assert
        aggregate.DomainEvents.Count().ShouldBe(1);
    }

    [Fact]
    public void TenantAggregateRoot_ShouldImplementITenantEntity()
    {
        // Arrange
        var aggregate = TestTenantAggregate.Create("Test", "tenant-xyz");

        // Act
        ITenantEntity tenantEntity = aggregate;

        // Assert
        tenantEntity.TenantId.ShouldBe("tenant-xyz");
    }

    [Fact]
    public void TenantAggregateRoot_ShouldImplementIAuditableEntity()
    {
        // Arrange
        var aggregate = TestTenantAggregate.Create("Test", "tenant");

        // Assert
        aggregate.IsDeleted.ShouldBeFalse();
        aggregate.CreatedBy.ShouldBeNull();
        aggregate.ModifiedBy.ShouldBeNull();
    }

    [Fact]
    public void TenantAggregateRoot_ShouldSupportDomainEvents()
    {
        // Arrange
        var aggregate = TestTenantAggregate.Create("Test");
        aggregate.RaiseDomainEvent(new TestEvent());
        aggregate.RaiseDomainEvent(new TestEvent());

        // Act
        aggregate.ClearDomainEvents();

        // Assert
        aggregate.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void TenantAggregateRoot_TenantIdProperty_ShouldHaveProtectedSetter()
    {
        // This test verifies TenantId has a protected setter (for security - prevents accidental cross-tenant access)
        // TenantId can only be set via constructor or by the TenantIdSetterInterceptor using EF Core's property API

        // Arrange
        var tenantIdProperty = typeof(TenantAggregateRoot<Guid>).GetProperty(nameof(ITenantEntity.TenantId));

        // Assert
        tenantIdProperty.ShouldNotBeNull();
        tenantIdProperty!.SetMethod.ShouldNotBeNull();
        tenantIdProperty.SetMethod!.IsFamily.ShouldBeTrue("TenantId setter should be protected");
    }

    [Fact]
    public void TenantAggregateRoot_ParameterlessConstructor_ShouldExist()
    {
        // Assert - Verify parameterless constructor exists (used by EF Core)
        var constructor = typeof(TenantAggregateRoot<Guid>).GetConstructor(
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null, Type.EmptyTypes, null);

        constructor.ShouldNotBeNull("EF Core requires a parameterless constructor");
    }

    [Fact]
    public void TenantAggregateRoot_ConstructorWithIdOnly_ShouldSetId()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var aggregate = new TestTenantAggregate(id);

        // Assert
        aggregate.Id.ShouldBe(id);
        aggregate.TenantId.ShouldBeNull();
    }

    [Fact]
    public void TenantAggregateRoot_DeletedAt_ShouldBeNullByDefault()
    {
        // Act
        var aggregate = TestTenantAggregate.Create("Test");

        // Assert
        aggregate.DeletedAt.ShouldBeNull();
        aggregate.DeletedBy.ShouldBeNull();
    }

    [Fact]
    public void TenantAggregateRoot_ModifiedAt_ShouldBeNullByDefault()
    {
        // Act
        var aggregate = TestTenantAggregate.Create("Test");

        // Assert
        aggregate.ModifiedAt.ShouldBeNull();
    }
}
