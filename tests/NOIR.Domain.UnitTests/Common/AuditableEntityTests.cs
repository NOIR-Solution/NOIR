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
        entity.ShouldNotBeNull();
    }

    [Fact]
    public void ConstructorWithId_ShouldSetId()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var entity = new TestAuditableEntity(id);

        // Assert
        entity.Id.ShouldBe(id);
    }

    [Fact]
    public void ParameterlessConstructor_ShouldSetCreatedAtToUtcNow()
    {
        // Act
        var before = DateTimeOffset.UtcNow;
        var entity = new TestAuditableEntity();
        var after = DateTimeOffset.UtcNow;

        // Assert
        entity.CreatedAt.ShouldBeGreaterThanOrEqualTo(before);
        entity.CreatedAt.ShouldBeLessThanOrEqualTo(after);
    }

    #endregion

    #region Audit Properties Tests

    [Fact]
    public void CreatedBy_ShouldBeNull_ByDefault()
    {
        // Act
        var entity = new TestAuditableEntity();

        // Assert
        entity.CreatedBy.ShouldBeNull();
    }

    [Fact]
    public void ModifiedBy_ShouldBeNull_ByDefault()
    {
        // Act
        var entity = new TestAuditableEntity();

        // Assert
        entity.ModifiedBy.ShouldBeNull();
    }

    #endregion

    #region Soft Delete Properties Tests

    [Fact]
    public void IsDeleted_ShouldBeFalse_ByDefault()
    {
        // Act
        var entity = new TestAuditableEntity();

        // Assert
        entity.IsDeleted.ShouldBeFalse();
    }

    [Fact]
    public void DeletedAt_ShouldBeNull_ByDefault()
    {
        // Act
        var entity = new TestAuditableEntity();

        // Assert
        entity.DeletedAt.ShouldBeNull();
    }

    [Fact]
    public void DeletedBy_ShouldBeNull_ByDefault()
    {
        // Act
        var entity = new TestAuditableEntity();

        // Assert
        entity.DeletedBy.ShouldBeNull();
    }

    #endregion

    #region IAuditableEntity Implementation Tests

    [Fact]
    public void AuditableEntity_ShouldImplementIAuditableEntity()
    {
        // Arrange
        var entity = new TestAuditableEntity();

        // Assert
        entity.ShouldBeAssignableTo<IAuditableEntity>();
    }

    [Fact]
    public void AuditableEntity_ShouldInheritFromEntity()
    {
        // Arrange
        var entity = new TestAuditableEntity();

        // Assert
        entity.ShouldBeAssignableTo<Entity<Guid>>();
    }

    #endregion

    #region Different Id Types Tests

    [Fact]
    public void AuditableEntityWithIntId_ShouldWork()
    {
        // Act
        var entity = new TestAuditableEntityWithInt(42);

        // Assert
        entity.Id.ShouldBe(42);
        entity.IsDeleted.ShouldBeFalse();
    }

    [Fact]
    public void AuditableEntityWithIntId_ParameterlessConstructor_ShouldWork()
    {
        // Act
        var entity = new TestAuditableEntityWithInt();

        // Assert
        entity.ShouldNotBeNull();
        entity.Id.ShouldBe(0); // Default int value
    }

    #endregion

    #region Factory Method Tests

    [Fact]
    public void Create_ShouldSetNameAndGenerateId()
    {
        // Act
        var entity = TestAuditableEntity.Create("Test Name");

        // Assert
        entity.Name.ShouldBe("Test Name");
        entity.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void Create_ShouldSetCreatedAt()
    {
        // Act
        var before = DateTimeOffset.UtcNow;
        var entity = TestAuditableEntity.Create("Test");
        var after = DateTimeOffset.UtcNow;

        // Assert
        entity.CreatedAt.ShouldBeGreaterThanOrEqualTo(before);
        entity.CreatedAt.ShouldBeLessThanOrEqualTo(after);
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
        entity1.ShouldBe(entity2);
    }

    [Fact]
    public void TwoEntities_WithDifferentIds_ShouldNotBeEqual()
    {
        // Arrange
        var entity1 = new TestAuditableEntity(Guid.NewGuid()) { Name = "Test" };
        var entity2 = new TestAuditableEntity(Guid.NewGuid()) { Name = "Test" };

        // Assert
        entity1.ShouldNotBe(entity2);
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
        createdByProperty.ShouldNotBeNull();
        createdByProperty!.SetMethod!.IsFamily.ShouldBeTrue("CreatedBy should have protected setter");

        var modifiedByProperty = entityType.GetProperty("ModifiedBy");
        modifiedByProperty.ShouldNotBeNull();
        modifiedByProperty!.SetMethod!.IsFamily.ShouldBeTrue("ModifiedBy should have protected setter");

        var isDeletedProperty = entityType.GetProperty("IsDeleted");
        isDeletedProperty.ShouldNotBeNull();
        isDeletedProperty!.SetMethod!.IsFamily.ShouldBeTrue("IsDeleted should have protected setter");

        var deletedAtProperty = entityType.GetProperty("DeletedAt");
        deletedAtProperty.ShouldNotBeNull();
        deletedAtProperty!.SetMethod!.IsFamily.ShouldBeTrue("DeletedAt should have protected setter");

        var deletedByProperty = entityType.GetProperty("DeletedBy");
        deletedByProperty.ShouldNotBeNull();
        deletedByProperty!.SetMethod!.IsFamily.ShouldBeTrue("DeletedBy should have protected setter");
    }

    #endregion
}
