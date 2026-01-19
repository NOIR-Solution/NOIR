namespace NOIR.Domain.UnitTests.Entities;

using NOIR.Domain.Enums;

/// <summary>
/// Unit tests for the UserTenantMembership entity.
/// Tests factory methods, role management, and default tenant handling.
/// </summary>
public class UserTenantMembershipTests
{
    #region Create Factory Tests

    [Fact]
    public void Create_WithRequiredParameters_ShouldCreateValidMembership()
    {
        // Arrange
        var userId = "user-123";
        var tenantId = "tenant-456";
        var role = TenantRole.Member;

        // Act
        var membership = UserTenantMembership.Create(userId, tenantId, role);

        // Assert
        membership.Should().NotBeNull();
        membership.Id.Should().NotBe(Guid.Empty);
        membership.UserId.Should().Be(userId);
        membership.TenantId.Should().Be(tenantId);
        membership.Role.Should().Be(role);
        membership.IsDefault.Should().BeFalse();
    }

    [Fact]
    public void Create_ShouldSetJoinedAtToNow()
    {
        // Arrange
        var beforeCreate = DateTimeOffset.UtcNow;

        // Act
        var membership = UserTenantMembership.Create("user-123", "tenant-456", TenantRole.Member);

        // Assert
        var afterCreate = DateTimeOffset.UtcNow;
        membership.JoinedAt.Should().BeOnOrAfter(beforeCreate).And.BeOnOrBefore(afterCreate);
    }

    [Fact]
    public void Create_WithIsDefaultTrue_ShouldBeDefault()
    {
        // Act
        var membership = UserTenantMembership.Create("user-123", "tenant-456", TenantRole.Member, isDefault: true);

        // Assert
        membership.IsDefault.Should().BeTrue();
    }

    [Fact]
    public void Create_WithNullUserId_ShouldThrowArgumentException()
    {
        // Act
        var act = () => UserTenantMembership.Create(null!, "tenant-456", TenantRole.Member);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithEmptyUserId_ShouldThrowArgumentException()
    {
        // Act
        var act = () => UserTenantMembership.Create("", "tenant-456", TenantRole.Member);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNullTenantId_ShouldThrowArgumentException()
    {
        // Act
        var act = () => UserTenantMembership.Create("user-123", null!, TenantRole.Member);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithEmptyTenantId_ShouldThrowArgumentException()
    {
        // Act
        var act = () => UserTenantMembership.Create("user-123", "", TenantRole.Member);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(TenantRole.Viewer)]
    [InlineData(TenantRole.Member)]
    [InlineData(TenantRole.Admin)]
    [InlineData(TenantRole.Owner)]
    public void Create_WithVariousRoles_ShouldSetCorrectRole(TenantRole role)
    {
        // Act
        var membership = UserTenantMembership.Create("user-123", "tenant-456", role);

        // Assert
        membership.Role.Should().Be(role);
    }

    #endregion

    #region UpdateRole Tests

    [Fact]
    public void UpdateRole_ShouldChangeRole()
    {
        // Arrange
        var membership = UserTenantMembership.Create("user-123", "tenant-456", TenantRole.Member);

        // Act
        membership.UpdateRole(TenantRole.Admin);

        // Assert
        membership.Role.Should().Be(TenantRole.Admin);
    }

    [Fact]
    public void UpdateRole_FromViewerToOwner_ShouldPromote()
    {
        // Arrange
        var membership = UserTenantMembership.Create("user-123", "tenant-456", TenantRole.Viewer);

        // Act
        membership.UpdateRole(TenantRole.Owner);

        // Assert
        membership.Role.Should().Be(TenantRole.Owner);
    }

    [Fact]
    public void UpdateRole_FromOwnerToViewer_ShouldDemote()
    {
        // Arrange
        var membership = UserTenantMembership.Create("user-123", "tenant-456", TenantRole.Owner);

        // Act
        membership.UpdateRole(TenantRole.Viewer);

        // Assert
        membership.Role.Should().Be(TenantRole.Viewer);
    }

    [Fact]
    public void UpdateRole_ToSameRole_ShouldNotThrow()
    {
        // Arrange
        var membership = UserTenantMembership.Create("user-123", "tenant-456", TenantRole.Member);

        // Act
        var act = () => membership.UpdateRole(TenantRole.Member);

        // Assert
        act.Should().NotThrow();
        membership.Role.Should().Be(TenantRole.Member);
    }

    #endregion

    #region SetAsDefault Tests

    [Fact]
    public void SetAsDefault_ShouldSetIsDefaultToTrue()
    {
        // Arrange
        var membership = UserTenantMembership.Create("user-123", "tenant-456", TenantRole.Member);

        // Act
        membership.SetAsDefault();

        // Assert
        membership.IsDefault.Should().BeTrue();
    }

    [Fact]
    public void SetAsDefault_WhenAlreadyDefault_ShouldRemainDefault()
    {
        // Arrange
        var membership = UserTenantMembership.Create("user-123", "tenant-456", TenantRole.Member, isDefault: true);

        // Act
        membership.SetAsDefault();

        // Assert
        membership.IsDefault.Should().BeTrue();
    }

    #endregion

    #region ClearDefault Tests

    [Fact]
    public void ClearDefault_ShouldSetIsDefaultToFalse()
    {
        // Arrange
        var membership = UserTenantMembership.Create("user-123", "tenant-456", TenantRole.Member, isDefault: true);

        // Act
        membership.ClearDefault();

        // Assert
        membership.IsDefault.Should().BeFalse();
    }

    [Fact]
    public void ClearDefault_WhenNotDefault_ShouldRemainNotDefault()
    {
        // Arrange
        var membership = UserTenantMembership.Create("user-123", "tenant-456", TenantRole.Member);

        // Act
        membership.ClearDefault();

        // Assert
        membership.IsDefault.Should().BeFalse();
    }

    #endregion

    #region HasRoleOrHigher Tests

    [Theory]
    [InlineData(TenantRole.Owner, TenantRole.Owner, true)]
    [InlineData(TenantRole.Owner, TenantRole.Admin, true)]
    [InlineData(TenantRole.Owner, TenantRole.Member, true)]
    [InlineData(TenantRole.Owner, TenantRole.Viewer, true)]
    [InlineData(TenantRole.Admin, TenantRole.Owner, false)]
    [InlineData(TenantRole.Admin, TenantRole.Admin, true)]
    [InlineData(TenantRole.Admin, TenantRole.Member, true)]
    [InlineData(TenantRole.Admin, TenantRole.Viewer, true)]
    [InlineData(TenantRole.Member, TenantRole.Owner, false)]
    [InlineData(TenantRole.Member, TenantRole.Admin, false)]
    [InlineData(TenantRole.Member, TenantRole.Member, true)]
    [InlineData(TenantRole.Member, TenantRole.Viewer, true)]
    [InlineData(TenantRole.Viewer, TenantRole.Owner, false)]
    [InlineData(TenantRole.Viewer, TenantRole.Admin, false)]
    [InlineData(TenantRole.Viewer, TenantRole.Member, false)]
    [InlineData(TenantRole.Viewer, TenantRole.Viewer, true)]
    public void HasRoleOrHigher_ShouldReturnCorrectResult(TenantRole userRole, TenantRole minimumRole, bool expected)
    {
        // Arrange
        var membership = UserTenantMembership.Create("user-123", "tenant-456", userRole);

        // Act
        var result = membership.HasRoleOrHigher(minimumRole);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void HasRoleOrHigher_ViewerCheckingViewer_ShouldReturnTrue()
    {
        // Arrange
        var membership = UserTenantMembership.Create("user-123", "tenant-456", TenantRole.Viewer);

        // Act
        var result = membership.HasRoleOrHigher(TenantRole.Viewer);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasRoleOrHigher_OwnerCheckingViewer_ShouldReturnTrue()
    {
        // Arrange
        var membership = UserTenantMembership.Create("user-123", "tenant-456", TenantRole.Owner);

        // Act
        var result = membership.HasRoleOrHigher(TenantRole.Viewer);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Role Value Tests

    [Fact]
    public void TenantRole_Viewer_ShouldHaveLowestValue()
    {
        // Assert
        ((int)TenantRole.Viewer).Should().BeLessThan((int)TenantRole.Member);
    }

    [Fact]
    public void TenantRole_Owner_ShouldHaveHighestValue()
    {
        // Assert
        ((int)TenantRole.Owner).Should().BeGreaterThan((int)TenantRole.Admin);
        ((int)TenantRole.Admin).Should().BeGreaterThan((int)TenantRole.Member);
        ((int)TenantRole.Member).Should().BeGreaterThan((int)TenantRole.Viewer);
    }

    #endregion

    #region IAuditableEntity Tests

    [Fact]
    public void Create_ShouldInitializeAuditableProperties()
    {
        // Act
        var membership = UserTenantMembership.Create("user-123", "tenant-456", TenantRole.Member);

        // Assert
        membership.IsDeleted.Should().BeFalse();
        membership.DeletedAt.Should().BeNull();
        membership.DeletedBy.Should().BeNull();
        membership.CreatedBy.Should().BeNull();
        membership.ModifiedBy.Should().BeNull();
    }

    #endregion

    #region Multiple Memberships Tests

    [Fact]
    public void Create_SameUserDifferentTenants_ShouldCreateDistinctMemberships()
    {
        // Arrange
        var userId = "user-123";

        // Act
        var membership1 = UserTenantMembership.Create(userId, "tenant-1", TenantRole.Owner);
        var membership2 = UserTenantMembership.Create(userId, "tenant-2", TenantRole.Member);
        var membership3 = UserTenantMembership.Create(userId, "tenant-3", TenantRole.Viewer);

        // Assert
        membership1.Id.Should().NotBe(membership2.Id);
        membership2.Id.Should().NotBe(membership3.Id);
        membership1.TenantId.Should().NotBe(membership2.TenantId);
    }

    #endregion
}
