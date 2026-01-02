namespace NOIR.Domain.UnitTests.Common;

/// <summary>
/// Unit tests for the Entity base class.
/// Tests equality, identity, and hash code behavior.
/// </summary>
public class EntityTests
{
    #region Test Helpers

    private sealed class TestEntity : Entity<Guid>
    {
        public string Name { get; set; } = string.Empty;

        public TestEntity() : base() { }
        public TestEntity(Guid id) : base(id) { }
    }

    private sealed class OtherTestEntity : Entity<Guid>
    {
        public OtherTestEntity(Guid id) : base(id) { }
    }

    private sealed class StringIdEntity : Entity<string>
    {
        public StringIdEntity(string id) : base(id) { }
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equals_SameId_ShouldReturnTrue()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity1 = new TestEntity(id);
        var entity2 = new TestEntity(id);

        // Act & Assert
        entity1.Equals(entity2).Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentId_ShouldReturnFalse()
    {
        // Arrange
        var entity1 = new TestEntity(Guid.NewGuid());
        var entity2 = new TestEntity(Guid.NewGuid());

        // Act & Assert
        entity1.Equals(entity2).Should().BeFalse();
    }

    [Fact]
    public void Equals_DifferentEntityTypes_ShouldReturnFalse()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity1 = new TestEntity(id);
        var entity2 = new OtherTestEntity(id);

        // Act & Assert
        entity1.Equals(entity2).Should().BeFalse();
    }

    [Fact]
    public void Equals_Null_ShouldReturnFalse()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid());

        // Act & Assert
        entity.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void Equals_SameReference_ShouldReturnTrue()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid());

        // Act & Assert
        entity.Equals(entity).Should().BeTrue();
    }

    [Fact]
    public void Equals_ObjectOverload_SameId_ShouldReturnTrue()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity1 = new TestEntity(id);
        object entity2 = new TestEntity(id);

        // Act & Assert
        entity1.Equals(entity2).Should().BeTrue();
    }

    [Fact]
    public void Equals_ObjectOverload_DifferentType_ShouldReturnFalse()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid());
        var notAnEntity = "not an entity";

        // Act & Assert
        entity.Equals(notAnEntity).Should().BeFalse();
    }

    #endregion

    #region GetHashCode Tests

    [Fact]
    public void GetHashCode_SameId_ShouldReturnSameHash()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity1 = new TestEntity(id);
        var entity2 = new TestEntity(id);

        // Act & Assert
        entity1.GetHashCode().Should().Be(entity2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_DifferentId_ShouldReturnDifferentHash()
    {
        // Arrange
        var entity1 = new TestEntity(Guid.NewGuid());
        var entity2 = new TestEntity(Guid.NewGuid());

        // Act & Assert
        // Note: Different IDs typically produce different hashes, but collisions are possible
        // We test the typical case
        entity1.GetHashCode().Should().NotBe(entity2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_ConsistentAcrossMultipleCalls()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid());

        // Act
        var hash1 = entity.GetHashCode();
        var hash2 = entity.GetHashCode();
        var hash3 = entity.GetHashCode();

        // Assert
        hash1.Should().Be(hash2).And.Be(hash3);
    }

    #endregion

    #region Operator Tests

    [Fact]
    public void OperatorEquals_SameId_ShouldReturnTrue()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity1 = new TestEntity(id);
        var entity2 = new TestEntity(id);

        // Act & Assert
        (entity1 == entity2).Should().BeTrue();
    }

    [Fact]
    public void OperatorEquals_DifferentId_ShouldReturnFalse()
    {
        // Arrange
        var entity1 = new TestEntity(Guid.NewGuid());
        var entity2 = new TestEntity(Guid.NewGuid());

        // Act & Assert
        (entity1 == entity2).Should().BeFalse();
    }

    [Fact]
    public void OperatorEquals_BothNull_ShouldReturnTrue()
    {
        // Arrange
        TestEntity? entity1 = null;
        TestEntity? entity2 = null;

        // Act & Assert
        (entity1 == entity2).Should().BeTrue();
    }

    [Fact]
    public void OperatorEquals_LeftNull_ShouldReturnFalse()
    {
        // Arrange
        TestEntity? entity1 = null;
        var entity2 = new TestEntity(Guid.NewGuid());

        // Act & Assert
        (entity1 == entity2).Should().BeFalse();
    }

    [Fact]
    public void OperatorEquals_RightNull_ShouldReturnFalse()
    {
        // Arrange
        var entity1 = new TestEntity(Guid.NewGuid());
        TestEntity? entity2 = null;

        // Act & Assert
        (entity1 == entity2).Should().BeFalse();
    }

    [Fact]
    public void OperatorNotEquals_DifferentId_ShouldReturnTrue()
    {
        // Arrange
        var entity1 = new TestEntity(Guid.NewGuid());
        var entity2 = new TestEntity(Guid.NewGuid());

        // Act & Assert
        (entity1 != entity2).Should().BeTrue();
    }

    [Fact]
    public void OperatorNotEquals_SameId_ShouldReturnFalse()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity1 = new TestEntity(id);
        var entity2 = new TestEntity(id);

        // Act & Assert
        (entity1 != entity2).Should().BeFalse();
    }

    #endregion

    #region Property Tests

    [Fact]
    public void CreatedAt_DefaultsToUtcNow()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var entity = new TestEntity();

        // Assert
        var after = DateTimeOffset.UtcNow;
        entity.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void ModifiedAt_DefaultsToNull()
    {
        // Arrange & Act
        var entity = new TestEntity();

        // Assert
        entity.ModifiedAt.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithId_SetsId()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var entity = new TestEntity(id);

        // Assert
        entity.Id.Should().Be(id);
    }

    [Fact]
    public void DefaultConstructor_IdIsDefault()
    {
        // Arrange & Act
        var entity = new TestEntity();

        // Assert
        entity.Id.Should().Be(Guid.Empty);
    }

    #endregion

    #region String Id Tests

    [Fact]
    public void StringIdEntity_Equals_SameId_ShouldReturnTrue()
    {
        // Arrange
        var entity1 = new StringIdEntity("user-123");
        var entity2 = new StringIdEntity("user-123");

        // Act & Assert
        entity1.Equals(entity2).Should().BeTrue();
    }

    [Fact]
    public void StringIdEntity_Equals_DifferentId_ShouldReturnFalse()
    {
        // Arrange
        var entity1 = new StringIdEntity("user-123");
        var entity2 = new StringIdEntity("user-456");

        // Act & Assert
        entity1.Equals(entity2).Should().BeFalse();
    }

    [Theory]
    [InlineData("user-1", "user-1", true)]
    [InlineData("user-1", "user-2", false)]
    [InlineData("USER-1", "user-1", false)] // Case sensitive
    [InlineData("", "", true)]
    public void StringIdEntity_Equals_VariousInputs(string id1, string id2, bool expected)
    {
        // Arrange
        var entity1 = new StringIdEntity(id1);
        var entity2 = new StringIdEntity(id2);

        // Act & Assert
        entity1.Equals(entity2).Should().Be(expected);
    }

    #endregion
}
