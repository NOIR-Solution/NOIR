namespace NOIR.Domain.UnitTests.Common;

/// <summary>
/// Unit tests for AuditableEntity base class.
/// Tests audit fields and soft delete properties.
/// </summary>
public class AuditableEntityTests
{
    #region Test Fixtures

    private class TestAuditableEntity : AuditableEntity<Guid>
    {
        public string? Name { get; set; }

        public TestAuditableEntity() : base() { }

        public TestAuditableEntity(Guid id) : base(id) { }

        public static TestAuditableEntity Create(string name)
        {
            return new TestAuditableEntity(Guid.NewGuid()) { Name = name };
        }
    }

    private class TestAuditableEntityWithInt : AuditableEntity<int>
    {
        public string? Name { get; set; }

        public TestAuditableEntityWithInt() : base() { }

        public TestAuditableEntityWithInt(int id) : base(id) { }
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void ParameterlessConstructor_ShouldCreateEntity()
    {
        // Act
        var entity = new TestAuditableEntity();

        // Assert
        entity.Should().NotBeNull();
    }

    [Fact]
    public void ConstructorWithId_ShouldSetId()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var entity = new TestAuditableEntity(id);

        // Assert
        entity.Id.Should().Be(id);
    }

    [Fact]
    public void ParameterlessConstructor_ShouldSetCreatedAtToUtcNow()
    {
        // Act
        var before = DateTimeOffset.UtcNow;
        var entity = new TestAuditableEntity();
        var after = DateTimeOffset.UtcNow;

        // Assert
        entity.CreatedAt.Should().BeOnOrAfter(before);
        entity.CreatedAt.Should().BeOnOrBefore(after);
    }

    #endregion

    #region Audit Properties Tests

    [Fact]
    public void CreatedBy_ShouldBeNull_ByDefault()
    {
        // Act
        var entity = new TestAuditableEntity();

        // Assert
        entity.CreatedBy.Should().BeNull();
    }

    [Fact]
    public void ModifiedBy_ShouldBeNull_ByDefault()
    {
        // Act
        var entity = new TestAuditableEntity();

        // Assert
        entity.ModifiedBy.Should().BeNull();
    }

    #endregion

    #region Soft Delete Properties Tests

    [Fact]
    public void IsDeleted_ShouldBeFalse_ByDefault()
    {
        // Act
        var entity = new TestAuditableEntity();

        // Assert
        entity.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void DeletedAt_ShouldBeNull_ByDefault()
    {
        // Act
        var entity = new TestAuditableEntity();

        // Assert
        entity.DeletedAt.Should().BeNull();
    }

    [Fact]
    public void DeletedBy_ShouldBeNull_ByDefault()
    {
        // Act
        var entity = new TestAuditableEntity();

        // Assert
        entity.DeletedBy.Should().BeNull();
    }

    #endregion

    #region IAuditableEntity Implementation Tests

    [Fact]
    public void AuditableEntity_ShouldImplementIAuditableEntity()
    {
        // Arrange
        var entity = new TestAuditableEntity();

        // Assert
        entity.Should().BeAssignableTo<IAuditableEntity>();
    }

    [Fact]
    public void AuditableEntity_ShouldInheritFromEntity()
    {
        // Arrange
        var entity = new TestAuditableEntity();

        // Assert
        entity.Should().BeAssignableTo<Entity<Guid>>();
    }

    #endregion

    #region Different Id Types Tests

    [Fact]
    public void AuditableEntityWithIntId_ShouldWork()
    {
        // Act
        var entity = new TestAuditableEntityWithInt(42);

        // Assert
        entity.Id.Should().Be(42);
        entity.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void AuditableEntityWithIntId_ParameterlessConstructor_ShouldWork()
    {
        // Act
        var entity = new TestAuditableEntityWithInt();

        // Assert
        entity.Should().NotBeNull();
        entity.Id.Should().Be(0); // Default int value
    }

    #endregion

    #region Factory Method Tests

    [Fact]
    public void Create_ShouldSetNameAndGenerateId()
    {
        // Act
        var entity = TestAuditableEntity.Create("Test Name");

        // Assert
        entity.Name.Should().Be("Test Name");
        entity.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_ShouldSetCreatedAt()
    {
        // Act
        var before = DateTimeOffset.UtcNow;
        var entity = TestAuditableEntity.Create("Test");
        var after = DateTimeOffset.UtcNow;

        // Assert
        entity.CreatedAt.Should().BeOnOrAfter(before);
        entity.CreatedAt.Should().BeOnOrBefore(after);
    }

    #endregion

    #region Equality Tests (Inherited from Entity)

    [Fact]
    public void TwoEntities_WithSameId_ShouldBeEqual()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity1 = new TestAuditableEntity(id) { Name = "Entity 1" };
        var entity2 = new TestAuditableEntity(id) { Name = "Entity 2" };

        // Assert
        entity1.Should().Be(entity2);
    }

    [Fact]
    public void TwoEntities_WithDifferentIds_ShouldNotBeEqual()
    {
        // Arrange
        var entity1 = new TestAuditableEntity(Guid.NewGuid()) { Name = "Test" };
        var entity2 = new TestAuditableEntity(Guid.NewGuid()) { Name = "Test" };

        // Assert
        entity1.Should().NotBe(entity2);
    }

    #endregion

    #region Protected Setters Verification

    [Fact]
    public void AuditFields_ShouldHaveProtectedSetters()
    {
        // Arrange
        var entityType = typeof(AuditableEntity<>);

        // Act & Assert
        var createdByProperty = entityType.GetProperty("CreatedBy");
        createdByProperty.Should().NotBeNull();
        createdByProperty!.SetMethod!.IsFamily.Should().BeTrue("CreatedBy should have protected setter");

        var modifiedByProperty = entityType.GetProperty("ModifiedBy");
        modifiedByProperty.Should().NotBeNull();
        modifiedByProperty!.SetMethod!.IsFamily.Should().BeTrue("ModifiedBy should have protected setter");

        var isDeletedProperty = entityType.GetProperty("IsDeleted");
        isDeletedProperty.Should().NotBeNull();
        isDeletedProperty!.SetMethod!.IsFamily.Should().BeTrue("IsDeleted should have protected setter");

        var deletedAtProperty = entityType.GetProperty("DeletedAt");
        deletedAtProperty.Should().NotBeNull();
        deletedAtProperty!.SetMethod!.IsFamily.Should().BeTrue("DeletedAt should have protected setter");

        var deletedByProperty = entityType.GetProperty("DeletedBy");
        deletedByProperty.Should().NotBeNull();
        deletedByProperty!.SetMethod!.IsFamily.Should().BeTrue("DeletedBy should have protected setter");
    }

    #endregion
}
