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
        permission.Resource.ShouldBe("orders");
        permission.Action.ShouldBe("read");
        permission.Scope.ShouldBe("own");
        permission.DisplayName.ShouldBe("Read Orders");
        permission.Description.ShouldBe("Allows reading own orders");
        permission.Category.ShouldBe("Order Management");
        permission.IsSystem.ShouldBeTrue();
        permission.SortOrder.ShouldBe(10);
        permission.Id.ShouldNotBe(Guid.Empty);
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
        permission.Resource.ShouldBe("users");
        permission.Action.ShouldBe("create");
        permission.Scope.ShouldBeNull();
        permission.DisplayName.ShouldBe("Create Users");
        permission.Description.ShouldBeNull();
        permission.Category.ShouldBeNull();
        permission.IsSystem.ShouldBeFalse();
        permission.SortOrder.ShouldBe(0);
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
        permission.Resource.ShouldBe("orders");
        permission.Action.ShouldBe("read");
        permission.Scope.ShouldBe("own");
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
        name.ShouldBe("users:delete");
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
        name.ShouldBe("orders:read:own");
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
        permission.DisplayName.ShouldBe("Updated Name");
        permission.Description.ShouldBe("Updated Description");
        permission.Category.ShouldBe("Updated Category");
        permission.SortOrder.ShouldBe(99);
        // Resource and Action should remain unchanged
        permission.Resource.ShouldBe("orders");
        permission.Action.ShouldBe("read");
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
        permission.RolePermissions.ShouldNotBeNull();
        permission.RolePermissions.ShouldBeEmpty();
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
        rolePermission.RoleId.ShouldBe(roleId);
        rolePermission.PermissionId.ShouldBe(permissionId);
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
        adminPermission.RoleId.ShouldBe("admin");
        userPermission.RoleId.ShouldBe("user");
        adminPermission.PermissionId.ShouldBe(permissionId);
        userPermission.PermissionId.ShouldBe(permissionId);
    }
}
