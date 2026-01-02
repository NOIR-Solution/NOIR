namespace NOIR.Domain.UnitTests.Common;

/// <summary>
/// Unit tests for SharePermission enum and SharePermissionExtensions.
/// Tests permission level checks and action-to-permission mapping.
/// </summary>
public class SharePermissionTests
{
    #region Allows Extension Method Tests

    [Theory]
    [InlineData(SharePermission.View, "read", true)]
    [InlineData(SharePermission.View, "view", true)]
    [InlineData(SharePermission.View, "READ", true)]  // case insensitive
    [InlineData(SharePermission.View, "VIEW", true)]  // case insensitive
    [InlineData(SharePermission.View, "edit", false)]
    [InlineData(SharePermission.View, "comment", false)]
    [InlineData(SharePermission.View, "delete", false)]
    public void Allows_ViewPermission_ReturnsExpectedResult(SharePermission permission, string action, bool expected)
    {
        // Act
        var result = permission.Allows(action);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(SharePermission.Comment, "read", true)]
    [InlineData(SharePermission.Comment, "view", true)]
    [InlineData(SharePermission.Comment, "comment", true)]
    [InlineData(SharePermission.Comment, "COMMENT", true)]  // case insensitive
    [InlineData(SharePermission.Comment, "edit", false)]
    [InlineData(SharePermission.Comment, "delete", false)]
    public void Allows_CommentPermission_ReturnsExpectedResult(SharePermission permission, string action, bool expected)
    {
        // Act
        var result = permission.Allows(action);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(SharePermission.Edit, "read", true)]
    [InlineData(SharePermission.Edit, "view", true)]
    [InlineData(SharePermission.Edit, "comment", true)]
    [InlineData(SharePermission.Edit, "edit", true)]
    [InlineData(SharePermission.Edit, "update", true)]
    [InlineData(SharePermission.Edit, "write", true)]
    [InlineData(SharePermission.Edit, "EDIT", true)]  // case insensitive
    [InlineData(SharePermission.Edit, "UPDATE", true)]  // case insensitive
    [InlineData(SharePermission.Edit, "WRITE", true)]  // case insensitive
    [InlineData(SharePermission.Edit, "delete", false)]
    [InlineData(SharePermission.Edit, "share", false)]
    [InlineData(SharePermission.Edit, "manage", false)]
    [InlineData(SharePermission.Edit, "admin", false)]
    public void Allows_EditPermission_ReturnsExpectedResult(SharePermission permission, string action, bool expected)
    {
        // Act
        var result = permission.Allows(action);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(SharePermission.Admin, "read", true)]
    [InlineData(SharePermission.Admin, "view", true)]
    [InlineData(SharePermission.Admin, "comment", true)]
    [InlineData(SharePermission.Admin, "edit", true)]
    [InlineData(SharePermission.Admin, "update", true)]
    [InlineData(SharePermission.Admin, "write", true)]
    [InlineData(SharePermission.Admin, "delete", true)]
    [InlineData(SharePermission.Admin, "admin", true)]
    [InlineData(SharePermission.Admin, "share", true)]
    [InlineData(SharePermission.Admin, "manage", true)]
    [InlineData(SharePermission.Admin, "DELETE", true)]  // case insensitive
    [InlineData(SharePermission.Admin, "ADMIN", true)]  // case insensitive
    [InlineData(SharePermission.Admin, "SHARE", true)]  // case insensitive
    [InlineData(SharePermission.Admin, "MANAGE", true)]  // case insensitive
    public void Allows_AdminPermission_ReturnsExpectedResult(SharePermission permission, string action, bool expected)
    {
        // Act
        var result = permission.Allows(action);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void Allows_WithUnknownAction_ReturnsFalse()
    {
        // Act
        var result = SharePermission.Admin.Allows("unknown_action");

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("xyz")]
    [InlineData("readwrite")]
    [InlineData("super_admin")]
    public void Allows_WithInvalidAction_ReturnsFalse(string action)
    {
        // Act & Assert
        SharePermission.Admin.Allows(action).Should().BeFalse();
    }

    [Fact]
    public void Allows_PermissionHierarchy_HigherLevelsIncludeLower()
    {
        // Assert View level
        SharePermission.View.Allows("read").Should().BeTrue();
        SharePermission.View.Allows("comment").Should().BeFalse();
        SharePermission.View.Allows("edit").Should().BeFalse();
        SharePermission.View.Allows("delete").Should().BeFalse();

        // Assert Comment level includes View
        SharePermission.Comment.Allows("read").Should().BeTrue();
        SharePermission.Comment.Allows("comment").Should().BeTrue();
        SharePermission.Comment.Allows("edit").Should().BeFalse();
        SharePermission.Comment.Allows("delete").Should().BeFalse();

        // Assert Edit level includes Comment and View
        SharePermission.Edit.Allows("read").Should().BeTrue();
        SharePermission.Edit.Allows("comment").Should().BeTrue();
        SharePermission.Edit.Allows("edit").Should().BeTrue();
        SharePermission.Edit.Allows("delete").Should().BeFalse();

        // Assert Admin level includes all
        SharePermission.Admin.Allows("read").Should().BeTrue();
        SharePermission.Admin.Allows("comment").Should().BeTrue();
        SharePermission.Admin.Allows("edit").Should().BeTrue();
        SharePermission.Admin.Allows("delete").Should().BeTrue();
    }

    #endregion

    #region FromAction Extension Method Tests

    [Theory]
    [InlineData("read", SharePermission.View)]
    [InlineData("view", SharePermission.View)]
    [InlineData("READ", SharePermission.View)]  // case insensitive
    [InlineData("VIEW", SharePermission.View)]  // case insensitive
    public void FromAction_ViewActions_ReturnsViewPermission(string action, SharePermission expected)
    {
        // Act
        var result = SharePermissionExtensions.FromAction(action);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("comment", SharePermission.Comment)]
    [InlineData("COMMENT", SharePermission.Comment)]  // case insensitive
    public void FromAction_CommentActions_ReturnsCommentPermission(string action, SharePermission expected)
    {
        // Act
        var result = SharePermissionExtensions.FromAction(action);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("edit", SharePermission.Edit)]
    [InlineData("update", SharePermission.Edit)]
    [InlineData("write", SharePermission.Edit)]
    [InlineData("EDIT", SharePermission.Edit)]  // case insensitive
    [InlineData("UPDATE", SharePermission.Edit)]  // case insensitive
    [InlineData("WRITE", SharePermission.Edit)]  // case insensitive
    public void FromAction_EditActions_ReturnsEditPermission(string action, SharePermission expected)
    {
        // Act
        var result = SharePermissionExtensions.FromAction(action);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("delete", SharePermission.Admin)]
    [InlineData("admin", SharePermission.Admin)]
    [InlineData("share", SharePermission.Admin)]
    [InlineData("manage", SharePermission.Admin)]
    [InlineData("DELETE", SharePermission.Admin)]  // case insensitive
    [InlineData("ADMIN", SharePermission.Admin)]  // case insensitive
    [InlineData("SHARE", SharePermission.Admin)]  // case insensitive
    [InlineData("MANAGE", SharePermission.Admin)]  // case insensitive
    public void FromAction_AdminActions_ReturnsAdminPermission(string action, SharePermission expected)
    {
        // Act
        var result = SharePermissionExtensions.FromAction(action);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void FromAction_WithUnknownAction_ReturnsNull()
    {
        // Act
        var result = SharePermissionExtensions.FromAction("unknown");

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("xyz")]
    [InlineData("readwrite")]
    [InlineData("super_admin")]
    [InlineData("viewer")]
    [InlineData("editor")]
    public void FromAction_WithInvalidAction_ReturnsNull(string action)
    {
        // Act
        var result = SharePermissionExtensions.FromAction(action);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Enum Value Tests

    [Fact]
    public void SharePermission_View_ShouldBeZero()
    {
        // Assert
        ((int)SharePermission.View).Should().Be(0);
    }

    [Fact]
    public void SharePermission_Comment_ShouldBeOne()
    {
        // Assert
        ((int)SharePermission.Comment).Should().Be(1);
    }

    [Fact]
    public void SharePermission_Edit_ShouldBeTwo()
    {
        // Assert
        ((int)SharePermission.Edit).Should().Be(2);
    }

    [Fact]
    public void SharePermission_Admin_ShouldBeThree()
    {
        // Assert
        ((int)SharePermission.Admin).Should().Be(3);
    }

    [Fact]
    public void SharePermission_ValuesAreOrdered()
    {
        // Assert - Values should be in ascending order for comparison
        ((int)SharePermission.View).Should().BeLessThan((int)SharePermission.Comment);
        ((int)SharePermission.Comment).Should().BeLessThan((int)SharePermission.Edit);
        ((int)SharePermission.Edit).Should().BeLessThan((int)SharePermission.Admin);
    }

    #endregion

    #region Bidirectional Mapping Tests

    [Theory]
    [InlineData("read")]
    [InlineData("view")]
    [InlineData("comment")]
    [InlineData("edit")]
    [InlineData("update")]
    [InlineData("write")]
    [InlineData("delete")]
    [InlineData("admin")]
    [InlineData("share")]
    [InlineData("manage")]
    public void FromAction_AndAllows_ShouldBeConsistent(string action)
    {
        // Get the minimum permission required for this action
        var permission = SharePermissionExtensions.FromAction(action);

        // Assert that FromAction returned a valid permission
        permission.Should().NotBeNull();

        // Assert that the returned permission allows the action
        permission!.Value.Allows(action).Should().BeTrue();
    }

    [Fact]
    public void AllKnownActions_ShouldMapToPermission()
    {
        // Arrange
        var knownActions = new[]
        {
            "read", "view", "comment", "edit", "update", "write",
            "delete", "admin", "share", "manage"
        };

        // Act & Assert
        foreach (var action in knownActions)
        {
            SharePermissionExtensions.FromAction(action).Should().NotBeNull(
                $"Action '{action}' should map to a permission");
        }
    }

    #endregion
}
