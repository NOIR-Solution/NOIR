namespace NOIR.Domain.UnitTests.Entities;

/// <summary>
/// Unit tests for Permission entity.
/// </summary>
public class PermissionTests
{
    [Fact]
    public void Create_WithAllParameters_ShouldCreatePermission()
    {
        // Act
        var permission = Permission.Create(
            resource: "orders",
            action: "read",
            displayName: "Read Orders",
            scope: "own",
            description: "Allows reading own orders",
            category: "Order Management",
            isSystem: true,
            sortOrder: 10);

        // Assert
        permission.Resource.Should().Be("orders");
        permission.Action.Should().Be("read");
        permission.Scope.Should().Be("own");
        permission.DisplayName.Should().Be("Read Orders");
        permission.Description.Should().Be("Allows reading own orders");
        permission.Category.Should().Be("Order Management");
        permission.IsSystem.Should().BeTrue();
        permission.SortOrder.Should().Be(10);
        permission.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_WithMinimalParameters_ShouldCreatePermission()
    {
        // Act
        var permission = Permission.Create(
            resource: "users",
            action: "create",
            displayName: "Create Users");

        // Assert
        permission.Resource.Should().Be("users");
        permission.Action.Should().Be("create");
        permission.Scope.Should().BeNull();
        permission.DisplayName.Should().Be("Create Users");
        permission.Description.Should().BeNull();
        permission.Category.Should().BeNull();
        permission.IsSystem.Should().BeFalse();
        permission.SortOrder.Should().Be(0);
    }

    [Fact]
    public void Create_ShouldNormalizeResourceAndActionToLowerCase()
    {
        // Act
        var permission = Permission.Create(
            resource: "ORDERS",
            action: "READ",
            displayName: "Read Orders",
            scope: "OWN");

        // Assert
        permission.Resource.Should().Be("orders");
        permission.Action.Should().Be("read");
        permission.Scope.Should().Be("own");
    }

    [Fact]
    public void Name_WithoutScope_ShouldReturnResourceAndAction()
    {
        // Arrange
        var permission = Permission.Create(
            resource: "users",
            action: "delete",
            displayName: "Delete Users");

        // Act
        var name = permission.Name;

        // Assert
        name.Should().Be("users:delete");
    }

    [Fact]
    public void Name_WithScope_ShouldReturnResourceActionAndScope()
    {
        // Arrange
        var permission = Permission.Create(
            resource: "orders",
            action: "read",
            displayName: "Read Orders",
            scope: "own");

        // Act
        var name = permission.Name;

        // Assert
        name.Should().Be("orders:read:own");
    }

    [Fact]
    public void Update_ShouldUpdateEditableProperties()
    {
        // Arrange
        var permission = Permission.Create(
            resource: "orders",
            action: "read",
            displayName: "Original Name",
            description: "Original Description",
            category: "Original Category",
            sortOrder: 1);

        // Act
        permission.Update(
            displayName: "Updated Name",
            description: "Updated Description",
            category: "Updated Category",
            sortOrder: 99);

        // Assert
        permission.DisplayName.Should().Be("Updated Name");
        permission.Description.Should().Be("Updated Description");
        permission.Category.Should().Be("Updated Category");
        permission.SortOrder.Should().Be(99);
        // Resource and Action should remain unchanged
        permission.Resource.Should().Be("orders");
        permission.Action.Should().Be("read");
    }

    [Fact]
    public void RolePermissions_ShouldBeInitialized()
    {
        // Arrange
        var permission = Permission.Create(
            resource: "users",
            action: "create",
            displayName: "Create Users");

        // Assert
        permission.RolePermissions.Should().NotBeNull();
        permission.RolePermissions.Should().BeEmpty();
    }
}

/// <summary>
/// Unit tests for RolePermission entity.
/// </summary>
public class RolePermissionTests
{
    [Fact]
    public void Create_ShouldCreateRolePermission()
    {
        // Arrange
        var roleId = "admin-role-id";
        var permissionId = Guid.NewGuid();

        // Act
        var rolePermission = RolePermission.Create(roleId, permissionId);

        // Assert
        rolePermission.RoleId.Should().Be(roleId);
        rolePermission.PermissionId.Should().Be(permissionId);
    }

    [Fact]
    public void Create_WithDifferentRoles_ShouldCreateDistinctRolePermissions()
    {
        // Arrange
        var permissionId = Guid.NewGuid();

        // Act
        var adminPermission = RolePermission.Create("admin", permissionId);
        var userPermission = RolePermission.Create("user", permissionId);

        // Assert
        adminPermission.RoleId.Should().Be("admin");
        userPermission.RoleId.Should().Be("user");
        adminPermission.PermissionId.Should().Be(permissionId);
        userPermission.PermissionId.Should().Be(permissionId);
    }
}
