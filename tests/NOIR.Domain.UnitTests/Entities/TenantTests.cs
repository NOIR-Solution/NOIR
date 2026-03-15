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
        tenant.ShouldNotBeNull();
        tenant.Id.ShouldNotBeNullOrEmpty();
        tenant.Identifier.ShouldBe(identifier);
        tenant.Name.ShouldBe(name);
        tenant.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Create_ShouldLowercaseAndTrimIdentifier()
    {
        // Act
        var tenant = Tenant.Create("  ACME-Corp  ", "Acme");

        // Assert
        tenant.Identifier.ShouldBe("acme-corp");
    }

    [Fact]
    public void Create_ShouldTrimName()
    {
        // Act
        var tenant = Tenant.Create("acme", "  Acme Corporation  ");

        // Assert
        tenant.Name.ShouldBe("Acme Corporation");
    }

    [Fact]
    public void Create_WithInactiveStatus_ShouldBeInactive()
    {
        // Act
        var tenant = Tenant.Create("acme", "Acme", isActive: false);

        // Assert
        tenant.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void Create_WithNullIdentifier_ShouldThrowArgumentException()
    {
        // Act
        var act = () => Tenant.Create(null!, "Name");

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Create_WithEmptyIdentifier_ShouldThrowArgumentException()
    {
        // Act
        var act = () => Tenant.Create("", "Name");

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Create_WithWhitespaceIdentifier_ShouldThrowArgumentException()
    {
        // Act
        var act = () => Tenant.Create("   ", "Name");

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Create_WithNullName_ShouldThrowArgumentException()
    {
        // Act
        var act = () => Tenant.Create("identifier", null!);

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Create_WithEmptyName_ShouldThrowArgumentException()
    {
        // Act
        var act = () => Tenant.Create("identifier", "");

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Create_ShouldGenerateValidGuidId()
    {
        // Act
        var tenant = Tenant.Create("acme", "Acme");

        // Assert
        Guid.TryParse(tenant.Id, out _).ShouldBeTrue();
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
        tenant.CreatedAt.ShouldBeGreaterThanOrEqualTo(before);

        tenant.CreatedAt.ShouldBeLessThanOrEqualTo(after);
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
        guidId.ShouldNotBe(Guid.Empty);
        guidId.ToString().ShouldBe(tenant.Id);
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
        updated.Identifier.ShouldBe(newIdentifier);
        updated.Name.ShouldBe(newName);
        updated.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void CreateUpdated_ShouldPreserveId()
    {
        // Arrange
        var original = Tenant.Create("original", "Original");

        // Act
        var updated = original.CreateUpdated("updated", "Updated", null, null, null, true);

        // Assert
        updated.Id.ShouldBe(original.Id);
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
        updated.ModifiedAt.ShouldNotBeNull();
        updated.ModifiedAt!.Value.ShouldBeGreaterThanOrEqualTo(beforeUpdate);
    }

    [Fact]
    public void CreateUpdated_ShouldLowercaseIdentifier()
    {
        // Arrange
        var original = Tenant.Create("original", "Original");

        // Act
        var updated = original.CreateUpdated("  UPDATED  ", "Updated", null, null, null, true);

        // Assert
        updated.Identifier.ShouldBe("updated");
    }

    [Fact]
    public void CreateUpdated_ShouldTrimName()
    {
        // Arrange
        var original = Tenant.Create("original", "Original");

        // Act
        var updated = original.CreateUpdated("updated", "  Updated Name  ", null, null, null, true);

        // Assert
        updated.Name.ShouldBe("Updated Name");
    }

    [Fact]
    public void CreateUpdated_WithNullIdentifier_ShouldThrowArgumentException()
    {
        // Arrange
        var tenant = Tenant.Create("original", "Original");

        // Act
        var act = () => tenant.CreateUpdated(null!, "Name", null, null, null, true);

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void CreateUpdated_WithNullName_ShouldThrowArgumentException()
    {
        // Arrange
        var tenant = Tenant.Create("original", "Original");

        // Act
        var act = () => tenant.CreateUpdated("identifier", null!, null, null, null, true);

        // Assert
        Should.Throw<ArgumentException>(act);
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
        activated.IsActive.ShouldBeTrue();
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
        activated.ModifiedAt.ShouldNotBeNull();
        activated.ModifiedAt!.Value.ShouldBeGreaterThanOrEqualTo(beforeActivation);
    }

    [Fact]
    public void CreateActivated_ShouldPreserveOtherProperties()
    {
        // Arrange
        var tenant = Tenant.Create("acme", "Acme Corp", isActive: false);

        // Act
        var activated = tenant.CreateActivated();

        // Assert
        activated.Id.ShouldBe(tenant.Id);
        activated.Identifier.ShouldBe(tenant.Identifier);
        activated.Name.ShouldBe(tenant.Name);
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
        deactivated.IsActive.ShouldBeFalse();
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
        deactivated.ModifiedAt.ShouldNotBeNull();
        deactivated.ModifiedAt!.Value.ShouldBeGreaterThanOrEqualTo(beforeDeactivation);
    }

    [Fact]
    public void CreateDeactivated_ShouldPreserveOtherProperties()
    {
        // Arrange
        var tenant = Tenant.Create("acme", "Acme Corp");

        // Act
        var deactivated = tenant.CreateDeactivated();

        // Assert
        deactivated.Id.ShouldBe(tenant.Id);
        deactivated.Identifier.ShouldBe(tenant.Identifier);
        deactivated.Name.ShouldBe(tenant.Name);
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
        deleted.IsDeleted.ShouldBeTrue();
        deleted.DeletedAt.ShouldNotBeNull();
        deleted.DeletedAt!.Value.ShouldBe(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
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
        deleted.DeletedBy.ShouldBe(deletedBy);
    }

    [Fact]
    public void CreateDeleted_ShouldPreserveOtherProperties()
    {
        // Arrange
        var tenant = Tenant.Create("acme", "Acme Corp");

        // Act
        var deleted = tenant.CreateDeleted("admin");

        // Assert
        deleted.Id.ShouldBe(tenant.Id);
        deleted.Identifier.ShouldBe(tenant.Identifier);
        deleted.Name.ShouldBe(tenant.Name);
        deleted.IsActive.ShouldBe(tenant.IsActive);
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
        deleted.ModifiedAt.ShouldNotBeNull();
        deleted.ModifiedAt!.Value.ShouldBeGreaterThanOrEqualTo(beforeDeletion);
    }

    #endregion

    #region IAuditableEntity Tests

    [Fact]
    public void Create_ShouldInitializeAuditableProperties()
    {
        // Act
        var tenant = Tenant.Create("acme", "Acme");

        // Assert
        tenant.IsDeleted.ShouldBeFalse();
        tenant.DeletedAt.ShouldBeNull();
        tenant.DeletedBy.ShouldBeNull();
        tenant.CreatedBy.ShouldBeNull();
        tenant.ModifiedBy.ShouldBeNull();
    }

    #endregion
}
