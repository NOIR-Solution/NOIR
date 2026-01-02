namespace NOIR.Application.UnitTests.Infrastructure;

/// <summary>
/// Unit tests for HasPermissionAttribute.
/// Tests that the attribute correctly constructs authorization policies.
/// </summary>
public class HasPermissionAttributeTests
{
    [Fact]
    public void Constructor_WithPermission_ShouldSetPolicy()
    {
        // Arrange
        var permission = "users:read";

        // Act
        var attribute = new HasPermissionAttribute(permission);

        // Assert
        attribute.Policy.Should().Be($"Permission:{permission}");
    }

    [Fact]
    public void Constructor_ShouldInheritFromAuthorizeAttribute()
    {
        // Arrange
        var attribute = new HasPermissionAttribute("test:permission");

        // Assert
        attribute.Should().BeAssignableTo<AuthorizeAttribute>();
    }

    [Fact]
    public void Constructor_WithDifferentPermissions_ShouldCreateDistinctPolicies()
    {
        // Arrange & Act
        var attr1 = new HasPermissionAttribute("users:read");
        var attr2 = new HasPermissionAttribute("users:write");
        var attr3 = new HasPermissionAttribute("roles:manage");

        // Assert
        attr1.Policy.Should().Be("Permission:users:read");
        attr2.Policy.Should().Be("Permission:users:write");
        attr3.Policy.Should().Be("Permission:roles:manage");
    }

    [Fact]
    public void Constructor_WithEmptyPermission_ShouldStillSetPolicy()
    {
        // Arrange & Act
        var attribute = new HasPermissionAttribute("");

        // Assert
        attribute.Policy.Should().Be("Permission:");
    }

    [Theory]
    [InlineData("users:read")]
    [InlineData("roles:create")]
    [InlineData("system:admin")]
    [InlineData("tenants:update")]
    public void Constructor_WithVariousPermissions_ShouldSetCorrectPolicy(string permission)
    {
        // Act
        var attribute = new HasPermissionAttribute(permission);

        // Assert
        attribute.Policy.Should().Be($"Permission:{permission}");
    }
}
