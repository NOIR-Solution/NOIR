namespace NOIR.Domain.UnitTests.Entities;

/// <summary>
/// Unit tests for the Tenant entity.
/// Tests factory methods, mutation methods, and state transitions.
/// </summary>
public class TenantTests
{
    #region Create Factory Tests

    [Fact]
    public void Create_WithRequiredParameters_ShouldCreateValidTenant()
    {
        // Arrange
        var identifier = "acme-corp";
        var name = "Acme Corporation";

        // Act
        var tenant = Tenant.Create(identifier, name);

        // Assert
        tenant.Should().NotBeNull();
        tenant.Id.Should().NotBeNullOrEmpty();
        tenant.Identifier.Should().Be(identifier);
        tenant.Name.Should().Be(name);
        tenant.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldLowercaseAndTrimIdentifier()
    {
        // Act
        var tenant = Tenant.Create("  ACME-Corp  ", "Acme");

        // Assert
        tenant.Identifier.Should().Be("acme-corp");
    }

    [Fact]
    public void Create_ShouldTrimName()
    {
        // Act
        var tenant = Tenant.Create("acme", "  Acme Corporation  ");

        // Assert
        tenant.Name.Should().Be("Acme Corporation");
    }

    [Fact]
    public void Create_WithInactiveStatus_ShouldBeInactive()
    {
        // Act
        var tenant = Tenant.Create("acme", "Acme", isActive: false);

        // Assert
        tenant.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Create_WithNullIdentifier_ShouldThrowArgumentException()
    {
        // Act
        var act = () => Tenant.Create(null!, "Name");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithEmptyIdentifier_ShouldThrowArgumentException()
    {
        // Act
        var act = () => Tenant.Create("", "Name");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithWhitespaceIdentifier_ShouldThrowArgumentException()
    {
        // Act
        var act = () => Tenant.Create("   ", "Name");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNullName_ShouldThrowArgumentException()
    {
        // Act
        var act = () => Tenant.Create("identifier", null!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithEmptyName_ShouldThrowArgumentException()
    {
        // Act
        var act = () => Tenant.Create("identifier", "");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_ShouldGenerateValidGuidId()
    {
        // Act
        var tenant = Tenant.Create("acme", "Acme");

        // Assert
        Guid.TryParse(tenant.Id, out _).Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldSetCreatedAtToNow()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var tenant = Tenant.Create("acme", "Acme");

        // Assert
        var after = DateTimeOffset.UtcNow;
        tenant.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    #endregion

    #region GetGuidId Tests

    [Fact]
    public void GetGuidId_ShouldReturnValidGuid()
    {
        // Arrange
        var tenant = Tenant.Create("acme", "Acme");

        // Act
        var guidId = tenant.GetGuidId();

        // Assert
        guidId.Should().NotBe(Guid.Empty);
        guidId.ToString().Should().Be(tenant.Id);
    }

    #endregion

    #region CreateUpdated Tests

    [Fact]
    public void CreateUpdated_ShouldReturnNewTenantWithUpdatedValues()
    {
        // Arrange
        var original = Tenant.Create("original", "Original Name");
        var newIdentifier = "updated";
        var newName = "Updated Name";

        // Act
        var updated = original.CreateUpdated(newIdentifier, newName, null, null, null, true);

        // Assert
        updated.Identifier.Should().Be(newIdentifier);
        updated.Name.Should().Be(newName);
        updated.IsActive.Should().BeTrue();
    }

    [Fact]
    public void CreateUpdated_ShouldPreserveId()
    {
        // Arrange
        var original = Tenant.Create("original", "Original");

        // Act
        var updated = original.CreateUpdated("updated", "Updated", null, null, null, true);

        // Assert
        updated.Id.Should().Be(original.Id);
    }

    [Fact]
    public void CreateUpdated_ShouldSetModifiedAt()
    {
        // Arrange
        var original = Tenant.Create("original", "Original");
        var beforeUpdate = DateTimeOffset.UtcNow;

        // Act
        var updated = original.CreateUpdated("updated", "Updated", null, null, null, true);

        // Assert
        updated.ModifiedAt.Should().NotBeNull();
        updated.ModifiedAt.Should().BeOnOrAfter(beforeUpdate);
    }

    [Fact]
    public void CreateUpdated_ShouldLowercaseIdentifier()
    {
        // Arrange
        var original = Tenant.Create("original", "Original");

        // Act
        var updated = original.CreateUpdated("  UPDATED  ", "Updated", null, null, null, true);

        // Assert
        updated.Identifier.Should().Be("updated");
    }

    [Fact]
    public void CreateUpdated_ShouldTrimName()
    {
        // Arrange
        var original = Tenant.Create("original", "Original");

        // Act
        var updated = original.CreateUpdated("updated", "  Updated Name  ", null, null, null, true);

        // Assert
        updated.Name.Should().Be("Updated Name");
    }

    [Fact]
    public void CreateUpdated_WithNullIdentifier_ShouldThrowArgumentException()
    {
        // Arrange
        var tenant = Tenant.Create("original", "Original");

        // Act
        var act = () => tenant.CreateUpdated(null!, "Name", null, null, null, true);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateUpdated_WithNullName_ShouldThrowArgumentException()
    {
        // Arrange
        var tenant = Tenant.Create("original", "Original");

        // Act
        var act = () => tenant.CreateUpdated("identifier", null!, null, null, null, true);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region CreateActivated Tests

    [Fact]
    public void CreateActivated_ShouldReturnActiveTenant()
    {
        // Arrange
        var tenant = Tenant.Create("acme", "Acme", isActive: false);

        // Act
        var activated = tenant.CreateActivated();

        // Assert
        activated.IsActive.Should().BeTrue();
    }

    [Fact]
    public void CreateActivated_ShouldSetModifiedAt()
    {
        // Arrange
        var tenant = Tenant.Create("acme", "Acme", isActive: false);
        var beforeActivation = DateTimeOffset.UtcNow;

        // Act
        var activated = tenant.CreateActivated();

        // Assert
        activated.ModifiedAt.Should().NotBeNull();
        activated.ModifiedAt.Should().BeOnOrAfter(beforeActivation);
    }

    [Fact]
    public void CreateActivated_ShouldPreserveOtherProperties()
    {
        // Arrange
        var tenant = Tenant.Create("acme", "Acme Corp", isActive: false);

        // Act
        var activated = tenant.CreateActivated();

        // Assert
        activated.Id.Should().Be(tenant.Id);
        activated.Identifier.Should().Be(tenant.Identifier);
        activated.Name.Should().Be(tenant.Name);
    }

    #endregion

    #region CreateDeactivated Tests

    [Fact]
    public void CreateDeactivated_ShouldReturnInactiveTenant()
    {
        // Arrange
        var tenant = Tenant.Create("acme", "Acme");

        // Act
        var deactivated = tenant.CreateDeactivated();

        // Assert
        deactivated.IsActive.Should().BeFalse();
    }

    [Fact]
    public void CreateDeactivated_ShouldSetModifiedAt()
    {
        // Arrange
        var tenant = Tenant.Create("acme", "Acme");
        var beforeDeactivation = DateTimeOffset.UtcNow;

        // Act
        var deactivated = tenant.CreateDeactivated();

        // Assert
        deactivated.ModifiedAt.Should().NotBeNull();
        deactivated.ModifiedAt.Should().BeOnOrAfter(beforeDeactivation);
    }

    [Fact]
    public void CreateDeactivated_ShouldPreserveOtherProperties()
    {
        // Arrange
        var tenant = Tenant.Create("acme", "Acme Corp");

        // Act
        var deactivated = tenant.CreateDeactivated();

        // Assert
        deactivated.Id.Should().Be(tenant.Id);
        deactivated.Identifier.Should().Be(tenant.Identifier);
        deactivated.Name.Should().Be(tenant.Name);
    }

    #endregion

    #region CreateDeleted Tests

    [Fact]
    public void CreateDeleted_ShouldReturnDeletedTenant()
    {
        // Arrange
        var tenant = Tenant.Create("acme", "Acme");

        // Act
        var deleted = tenant.CreateDeleted();

        // Assert
        deleted.IsDeleted.Should().BeTrue();
        deleted.DeletedAt.Should().NotBeNull();
        deleted.DeletedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void CreateDeleted_WithDeletedBy_ShouldSetDeletedBy()
    {
        // Arrange
        var tenant = Tenant.Create("acme", "Acme");
        var deletedBy = "user-123";

        // Act
        var deleted = tenant.CreateDeleted(deletedBy);

        // Assert
        deleted.DeletedBy.Should().Be(deletedBy);
    }

    [Fact]
    public void CreateDeleted_ShouldPreserveOtherProperties()
    {
        // Arrange
        var tenant = Tenant.Create("acme", "Acme Corp");

        // Act
        var deleted = tenant.CreateDeleted("admin");

        // Assert
        deleted.Id.Should().Be(tenant.Id);
        deleted.Identifier.Should().Be(tenant.Identifier);
        deleted.Name.Should().Be(tenant.Name);
        deleted.IsActive.Should().Be(tenant.IsActive);
    }

    [Fact]
    public void CreateDeleted_ShouldSetModifiedAt()
    {
        // Arrange
        var tenant = Tenant.Create("acme", "Acme");
        var beforeDeletion = DateTimeOffset.UtcNow;

        // Act
        var deleted = tenant.CreateDeleted();

        // Assert
        deleted.ModifiedAt.Should().NotBeNull();
        deleted.ModifiedAt.Should().BeOnOrAfter(beforeDeletion);
    }

    #endregion

    #region IAuditableEntity Tests

    [Fact]
    public void Create_ShouldInitializeAuditableProperties()
    {
        // Act
        var tenant = Tenant.Create("acme", "Acme");

        // Assert
        tenant.IsDeleted.Should().BeFalse();
        tenant.DeletedAt.Should().BeNull();
        tenant.DeletedBy.Should().BeNull();
        tenant.CreatedBy.Should().BeNull();
        tenant.ModifiedBy.Should().BeNull();
    }

    #endregion
}
