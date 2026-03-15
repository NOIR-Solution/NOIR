namespace NOIR.Domain.UnitTests.Entities;

/// <summary>
/// Unit tests for ResourceShare entity.
/// Tests factory methods, computed properties, permission checks, and state transitions.
/// </summary>
public class ResourceShareTests
{
    #region Create Factory Tests

    [Fact]
    public void Create_WithValidParameters_ReturnsResourceShare()
    {
        // Arrange
        var resourceType = "document";
        var resourceId = Guid.NewGuid();
        var sharedWithUserId = "user-123";
        var permission = SharePermission.Edit;
        var sharedByUserId = "owner-456";

        // Act
        var share = ResourceShare.Create(
            resourceType,
            resourceId,
            sharedWithUserId,
            permission,
            sharedByUserId);

        // Assert
        share.ShouldNotBeNull();
        share.Id.ShouldNotBe(Guid.Empty);
        share.ResourceType.ShouldBe("document"); // lowercase
        share.ResourceId.ShouldBe(resourceId);
        share.SharedWithUserId.ShouldBe(sharedWithUserId);
        share.Permission.ShouldBe(SharePermission.Edit);
        share.SharedByUserId.ShouldBe(sharedByUserId);
        share.ExpiresAt.ShouldBeNull();
    }

    [Fact]
    public void Create_WithExpiration_SetsExpiresAt()
    {
        // Arrange
        var expiresAt = DateTimeOffset.UtcNow.AddDays(7);

        // Act
        var share = ResourceShare.Create(
            "folder",
            Guid.NewGuid(),
            "user-123",
            SharePermission.View,
            expiresAt: expiresAt);

        // Assert
        share.ExpiresAt.ShouldBe(expiresAt);
    }

    [Fact]
    public void Create_NormalizesResourceTypeToLowerCase()
    {
        // Act
        var share = ResourceShare.Create("DOCUMENT", Guid.NewGuid(), "user", SharePermission.View);

        // Assert
        share.ResourceType.ShouldBe("document");
    }

    [Fact]
    public void Create_WithMixedCaseResourceType_NormalizesToLowerCase()
    {
        // Act
        var share = ResourceShare.Create("FoLdEr", Guid.NewGuid(), "user", SharePermission.View);

        // Assert
        share.ResourceType.ShouldBe("folder");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrEmptyResourceType_ThrowsArgumentException(string? resourceType)
    {
        // Act
        var act = () => ResourceShare.Create(resourceType!, Guid.NewGuid(), "user", SharePermission.View);

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrEmptyUserId_ThrowsArgumentException(string? userId)
    {
        // Act
        var act = () => ResourceShare.Create("document", Guid.NewGuid(), userId!, SharePermission.View);

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Create_WithEmptyResourceId_ThrowsArgumentException()
    {
        // Act
        var act = () => ResourceShare.Create("document", Guid.Empty, "user", SharePermission.View);

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Create_WithoutSharedByUserId_SetsToNull()
    {
        // Act
        var share = ResourceShare.Create("document", Guid.NewGuid(), "user", SharePermission.View);

        // Assert
        share.SharedByUserId.ShouldBeNull();
    }

    [Fact]
    public void Create_GeneratesUniqueIds()
    {
        // Act
        var share1 = ResourceShare.Create("doc", Guid.NewGuid(), "user1", SharePermission.View);
        var share2 = ResourceShare.Create("doc", Guid.NewGuid(), "user2", SharePermission.View);

        // Assert
        share1.Id.ShouldNotBe(share2.Id);
    }

    [Theory]
    [InlineData(SharePermission.View)]
    [InlineData(SharePermission.Comment)]
    [InlineData(SharePermission.Edit)]
    [InlineData(SharePermission.Admin)]
    public void Create_WithDifferentPermissions_SetsCorrectPermission(SharePermission permission)
    {
        // Act
        var share = ResourceShare.Create("document", Guid.NewGuid(), "user", permission);

        // Assert
        share.Permission.ShouldBe(permission);
    }

    #endregion

    #region IsValid Tests

    [Fact]
    public void IsValid_WithNoExpiration_ReturnsTrue()
    {
        // Arrange
        var share = ResourceShare.Create("doc", Guid.NewGuid(), "user", SharePermission.View);

        // Act & Assert
        share.IsValid().ShouldBeTrue();
    }

    [Fact]
    public void IsValid_WithFutureExpiration_ReturnsTrue()
    {
        // Arrange
        var share = ResourceShare.Create(
            "doc",
            Guid.NewGuid(),
            "user",
            SharePermission.View,
            expiresAt: DateTimeOffset.UtcNow.AddHours(1));

        // Act & Assert
        share.IsValid().ShouldBeTrue();
    }

    [Fact]
    public void IsValid_WithPastExpiration_ReturnsFalse()
    {
        // Arrange
        var share = ResourceShare.Create(
            "doc",
            Guid.NewGuid(),
            "user",
            SharePermission.View,
            expiresAt: DateTimeOffset.UtcNow.AddHours(-1));

        // Act & Assert
        share.IsValid().ShouldBeFalse();
    }

    [Fact]
    public void IsValid_WithDistantFutureExpiration_ReturnsTrue()
    {
        // Arrange
        var share = ResourceShare.Create(
            "doc",
            Guid.NewGuid(),
            "user",
            SharePermission.View,
            expiresAt: DateTimeOffset.UtcNow.AddYears(1));

        // Act & Assert
        share.IsValid().ShouldBeTrue();
    }

    #endregion

    #region AllowsAction Tests

    [Theory]
    [InlineData(SharePermission.View, "read", true)]
    [InlineData(SharePermission.View, "view", true)]
    [InlineData(SharePermission.View, "edit", false)]
    [InlineData(SharePermission.Edit, "read", true)]
    [InlineData(SharePermission.Edit, "edit", true)]
    [InlineData(SharePermission.Edit, "delete", false)]
    [InlineData(SharePermission.Admin, "read", true)]
    [InlineData(SharePermission.Admin, "edit", true)]
    [InlineData(SharePermission.Admin, "delete", true)]
    [InlineData(SharePermission.Admin, "share", true)]
    public void AllowsAction_ChecksPermissionLevel(SharePermission permission, string action, bool expected)
    {
        // Arrange
        var share = ResourceShare.Create("doc", Guid.NewGuid(), "user", permission);

        // Act
        var result = share.AllowsAction(action);

        // Assert
        result.ShouldBe(expected);
    }

    [Fact]
    public void AllowsAction_WhenExpired_ReturnsFalse()
    {
        // Arrange
        var share = ResourceShare.Create(
            "doc",
            Guid.NewGuid(),
            "user",
            SharePermission.Admin,
            expiresAt: DateTimeOffset.UtcNow.AddHours(-1));

        // Act & Assert
        share.AllowsAction("read").ShouldBeFalse();
    }

    [Fact]
    public void AllowsAction_WhenValid_WithAdminPermission_AllowsAllActions()
    {
        // Arrange
        var share = ResourceShare.Create(
            "doc",
            Guid.NewGuid(),
            "user",
            SharePermission.Admin,
            expiresAt: DateTimeOffset.UtcNow.AddDays(1));

        // Act & Assert
        share.AllowsAction("read").ShouldBeTrue();
        share.AllowsAction("edit").ShouldBeTrue();
        share.AllowsAction("delete").ShouldBeTrue();
        share.AllowsAction("share").ShouldBeTrue();
    }

    [Fact]
    public void AllowsAction_WithUnknownAction_ReturnsFalse()
    {
        // Arrange
        var share = ResourceShare.Create("doc", Guid.NewGuid(), "user", SharePermission.Admin);

        // Act & Assert
        share.AllowsAction("unknown_action").ShouldBeFalse();
    }

    [Fact]
    public void AllowsAction_IsCaseInsensitive()
    {
        // Arrange
        var share = ResourceShare.Create("doc", Guid.NewGuid(), "user", SharePermission.View);

        // Act & Assert
        share.AllowsAction("READ").ShouldBeTrue();
        share.AllowsAction("Read").ShouldBeTrue();
        share.AllowsAction("read").ShouldBeTrue();
    }

    #endregion

    #region UpdatePermission Tests

    [Fact]
    public void UpdatePermission_ChangesPermission()
    {
        // Arrange
        var share = ResourceShare.Create("doc", Guid.NewGuid(), "user", SharePermission.View);

        // Act
        share.UpdatePermission(SharePermission.Edit);

        // Assert
        share.Permission.ShouldBe(SharePermission.Edit);
    }

    [Fact]
    public void UpdatePermission_CanUpgradePermission()
    {
        // Arrange
        var share = ResourceShare.Create("doc", Guid.NewGuid(), "user", SharePermission.View);

        // Act
        share.UpdatePermission(SharePermission.Admin);

        // Assert
        share.Permission.ShouldBe(SharePermission.Admin);
    }

    [Fact]
    public void UpdatePermission_CanDowngradePermission()
    {
        // Arrange
        var share = ResourceShare.Create("doc", Guid.NewGuid(), "user", SharePermission.Admin);

        // Act
        share.UpdatePermission(SharePermission.View);

        // Assert
        share.Permission.ShouldBe(SharePermission.View);
    }

    [Fact]
    public void UpdatePermission_DoesNotAffectOtherProperties()
    {
        // Arrange
        var resourceId = Guid.NewGuid();
        var expiresAt = DateTimeOffset.UtcNow.AddDays(7);
        var share = ResourceShare.Create(
            "document",
            resourceId,
            "user-123",
            SharePermission.View,
            "owner-456",
            expiresAt);

        // Act
        share.UpdatePermission(SharePermission.Edit);

        // Assert
        share.ResourceType.ShouldBe("document");
        share.ResourceId.ShouldBe(resourceId);
        share.SharedWithUserId.ShouldBe("user-123");
        share.SharedByUserId.ShouldBe("owner-456");
        share.ExpiresAt.ShouldBe(expiresAt);
    }

    #endregion

    #region UpdateExpiration Tests

    [Fact]
    public void UpdateExpiration_ChangesExpiration()
    {
        // Arrange
        var share = ResourceShare.Create("doc", Guid.NewGuid(), "user", SharePermission.View);
        var newExpiration = DateTimeOffset.UtcNow.AddDays(30);

        // Act
        share.UpdateExpiration(newExpiration);

        // Assert
        share.ExpiresAt.ShouldBe(newExpiration);
    }

    [Fact]
    public void UpdateExpiration_ToNull_RemovesExpiration()
    {
        // Arrange
        var share = ResourceShare.Create(
            "doc",
            Guid.NewGuid(),
            "user",
            SharePermission.View,
            expiresAt: DateTimeOffset.UtcNow.AddDays(1));

        // Act
        share.UpdateExpiration(null);

        // Assert
        share.ExpiresAt.ShouldBeNull();
        share.IsValid().ShouldBeTrue();
    }

    [Fact]
    public void UpdateExpiration_CanExtendExpiration()
    {
        // Arrange
        var originalExpiration = DateTimeOffset.UtcNow.AddDays(1);
        var share = ResourceShare.Create(
            "doc",
            Guid.NewGuid(),
            "user",
            SharePermission.View,
            expiresAt: originalExpiration);
        var newExpiration = DateTimeOffset.UtcNow.AddDays(30);

        // Act
        share.UpdateExpiration(newExpiration);

        // Assert
        share.ExpiresAt.ShouldBe(newExpiration);
        share.ExpiresAt!.Value.ShouldBeGreaterThan(originalExpiration);
    }

    [Fact]
    public void UpdateExpiration_CanShortenExpiration()
    {
        // Arrange
        var originalExpiration = DateTimeOffset.UtcNow.AddDays(30);
        var share = ResourceShare.Create(
            "doc",
            Guid.NewGuid(),
            "user",
            SharePermission.View,
            expiresAt: originalExpiration);
        var newExpiration = DateTimeOffset.UtcNow.AddDays(1);

        // Act
        share.UpdateExpiration(newExpiration);

        // Assert
        share.ExpiresAt.ShouldBe(newExpiration);
        share.ExpiresAt!.Value.ShouldBeLessThan(originalExpiration);
    }

    [Fact]
    public void UpdateExpiration_DoesNotAffectOtherProperties()
    {
        // Arrange
        var resourceId = Guid.NewGuid();
        var share = ResourceShare.Create(
            "document",
            resourceId,
            "user-123",
            SharePermission.Edit,
            "owner-456");

        // Act
        share.UpdateExpiration(DateTimeOffset.UtcNow.AddDays(7));

        // Assert
        share.ResourceType.ShouldBe("document");
        share.ResourceId.ShouldBe(resourceId);
        share.SharedWithUserId.ShouldBe("user-123");
        share.SharedByUserId.ShouldBe("owner-456");
        share.Permission.ShouldBe(SharePermission.Edit);
    }

    #endregion

    #region IAuditableEntity Tests

    [Fact]
    public void AuditableProperties_ShouldBeInitializedToDefaults()
    {
        // Arrange & Act
        var share = ResourceShare.Create("doc", Guid.NewGuid(), "user", SharePermission.View);

        // Assert
        share.CreatedBy.ShouldBeNull();
        share.ModifiedBy.ShouldBeNull();
        share.IsDeleted.ShouldBeFalse();
        share.DeletedAt.ShouldBeNull();
        share.DeletedBy.ShouldBeNull();
    }

    #endregion
}
