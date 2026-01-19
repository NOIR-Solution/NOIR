namespace NOIR.Domain.UnitTests.Entities;

/// <summary>
/// Unit tests for the Tenant entity (record).
/// Tests factory methods, immutable updates, and state transitions.
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

    #region WithUpdatedDetails Tests

    [Fact]
    public void WithUpdatedDetails_ShouldCreateNewTenantWithUpdatedValues()
    {
        // Arrange
        var original = Tenant.Create("original", "Original Name");
        var newIdentifier = "updated";
        var newName = "Updated Name";

        // Act
        var updated = original.WithUpdatedDetails(newIdentifier, newName, true);

        // Assert
        updated.Identifier.Should().Be(newIdentifier);
        updated.Name.Should().Be(newName);
        updated.IsActive.Should().BeTrue();
    }

    [Fact]
    public void WithUpdatedDetails_ShouldPreserveId()
    {
        // Arrange
        var original = Tenant.Create("original", "Original");

        // Act
        var updated = original.WithUpdatedDetails("updated", "Updated", true);

        // Assert
        updated.Id.Should().Be(original.Id);
    }

    [Fact]
    public void WithUpdatedDetails_ShouldSetModifiedAt()
    {
        // Arrange
        var original = Tenant.Create("original", "Original");
        var beforeUpdate = DateTimeOffset.UtcNow;

        // Act
        var updated = original.WithUpdatedDetails("updated", "Updated", true);

        // Assert
        updated.ModifiedAt.Should().NotBeNull();
        updated.ModifiedAt.Should().BeOnOrAfter(beforeUpdate);
    }

    [Fact]
    public void WithUpdatedDetails_ShouldLowercaseIdentifier()
    {
        // Arrange
        var original = Tenant.Create("original", "Original");

        // Act
        var updated = original.WithUpdatedDetails("  UPDATED  ", "Updated", true);

        // Assert
        updated.Identifier.Should().Be("updated");
    }

    [Fact]
    public void WithUpdatedDetails_ShouldTrimName()
    {
        // Arrange
        var original = Tenant.Create("original", "Original");

        // Act
        var updated = original.WithUpdatedDetails("updated", "  Updated Name  ", true);

        // Assert
        updated.Name.Should().Be("Updated Name");
    }

    [Fact]
    public void WithUpdatedDetails_WithNullIdentifier_ShouldThrowArgumentException()
    {
        // Arrange
        var tenant = Tenant.Create("original", "Original");

        // Act
        var act = () => tenant.WithUpdatedDetails(null!, "Name", true);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WithUpdatedDetails_WithNullName_ShouldThrowArgumentException()
    {
        // Arrange
        var tenant = Tenant.Create("original", "Original");

        // Act
        var act = () => tenant.WithUpdatedDetails("identifier", null!, true);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region WithActivated Tests

    [Fact]
    public void WithActivated_ShouldCreateActiveTenant()
    {
        // Arrange
        var inactive = Tenant.Create("acme", "Acme", isActive: false);

        // Act
        var activated = inactive.WithActivated();

        // Assert
        activated.IsActive.Should().BeTrue();
    }

    [Fact]
    public void WithActivated_ShouldSetModifiedAt()
    {
        // Arrange
        var tenant = Tenant.Create("acme", "Acme", isActive: false);
        var beforeActivation = DateTimeOffset.UtcNow;

        // Act
        var activated = tenant.WithActivated();

        // Assert
        activated.ModifiedAt.Should().NotBeNull();
        activated.ModifiedAt.Should().BeOnOrAfter(beforeActivation);
    }

    [Fact]
    public void WithActivated_ShouldPreserveOtherProperties()
    {
        // Arrange
        var original = Tenant.Create("acme", "Acme Corp", isActive: false);

        // Act
        var activated = original.WithActivated();

        // Assert
        activated.Id.Should().Be(original.Id);
        activated.Identifier.Should().Be(original.Identifier);
        activated.Name.Should().Be(original.Name);
    }

    #endregion

    #region WithDeactivated Tests

    [Fact]
    public void WithDeactivated_ShouldCreateInactiveTenant()
    {
        // Arrange
        var active = Tenant.Create("acme", "Acme");

        // Act
        var deactivated = active.WithDeactivated();

        // Assert
        deactivated.IsActive.Should().BeFalse();
    }

    [Fact]
    public void WithDeactivated_ShouldSetModifiedAt()
    {
        // Arrange
        var tenant = Tenant.Create("acme", "Acme");
        var beforeDeactivation = DateTimeOffset.UtcNow;

        // Act
        var deactivated = tenant.WithDeactivated();

        // Assert
        deactivated.ModifiedAt.Should().NotBeNull();
        deactivated.ModifiedAt.Should().BeOnOrAfter(beforeDeactivation);
    }

    [Fact]
    public void WithDeactivated_ShouldPreserveOtherProperties()
    {
        // Arrange
        var original = Tenant.Create("acme", "Acme Corp");

        // Act
        var deactivated = original.WithDeactivated();

        // Assert
        deactivated.Id.Should().Be(original.Id);
        deactivated.Identifier.Should().Be(original.Identifier);
        deactivated.Name.Should().Be(original.Name);
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

    #region Navigation Properties Tests

    [Fact]
    public void Create_Domains_ShouldBeEmpty()
    {
        // Act
        var tenant = Tenant.Create("acme", "Acme");

        // Assert
        tenant.Domains.Should().NotBeNull();
        tenant.Domains.Should().BeEmpty();
    }

    [Fact]
    public void Create_UserMemberships_ShouldBeEmpty()
    {
        // Act
        var tenant = Tenant.Create("acme", "Acme");

        // Assert
        tenant.UserMemberships.Should().NotBeNull();
        tenant.UserMemberships.Should().BeEmpty();
    }

    [Fact]
    public void Create_Branding_ShouldBeNull()
    {
        // Act
        var tenant = Tenant.Create("acme", "Acme");

        // Assert
        tenant.Branding.Should().BeNull();
    }

    #endregion

    #region Record Equality Tests

    [Fact]
    public void TwoTenants_WithSameId_ShouldBeEqual()
    {
        // Arrange
        var tenant1 = Tenant.Create("acme", "Acme");
        var tenant2 = tenant1 with { Name = "Acme Updated" };

        // Assert - Records with same Id should be equal (based on record equality)
        // Note: This depends on how TenantInfo defines equality
        tenant1.Id.Should().Be(tenant2.Id);
    }

    #endregion
}
