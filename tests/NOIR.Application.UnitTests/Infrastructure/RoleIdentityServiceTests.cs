using NOIR.Domain.Common;

namespace NOIR.Application.UnitTests.Infrastructure;

/// <summary>
/// Unit tests for RoleIdentityService.
/// Tests role management operations with mocked RoleManager.
/// </summary>
public class RoleIdentityServiceTests
{
    private readonly Mock<RoleManager<ApplicationRole>> _roleManagerMock;
    private readonly Mock<ApplicationDbContext> _dbContextMock;
    private readonly RoleIdentityService _sut;

    public RoleIdentityServiceTests()
    {
        // Setup RoleManager mock
        var roleStoreMock = new Mock<IRoleStore<ApplicationRole>>();
        _roleManagerMock = new Mock<RoleManager<ApplicationRole>>(
            roleStoreMock.Object,
            null!, null!, null!, null!);

        // Setup DbContext mock
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContextMock = new Mock<ApplicationDbContext>(options);

        _sut = new RoleIdentityService(
            _roleManagerMock.Object,
            _dbContextMock.Object);
    }

    #region FindByIdAsync Tests

    [Fact]
    public async Task FindByIdAsync_WithExistingRole_ShouldReturnRoleDto()
    {
        // Arrange
        var roleId = Guid.NewGuid().ToString();
        var role = CreateTestRole(roleId, "Admin");

        _roleManagerMock.Setup(x => x.FindByIdAsync(roleId))
            .ReturnsAsync(role);

        // Act
        var result = await _sut.FindByIdAsync(roleId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(roleId);
        result.Name.Should().Be("Admin");
    }

    [Fact]
    public async Task FindByIdAsync_WithNonExistingRole_ShouldReturnNull()
    {
        // Arrange
        var roleId = Guid.NewGuid().ToString();
        _roleManagerMock.Setup(x => x.FindByIdAsync(roleId))
            .ReturnsAsync((ApplicationRole?)null);

        // Act
        var result = await _sut.FindByIdAsync(roleId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindByIdAsync_ShouldMapAllProperties()
    {
        // Arrange
        var roleId = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var role = new ApplicationRole
        {
            Id = roleId,
            Name = "TestRole",
            NormalizedName = "TESTROLE",
            Description = "Test description",
            ParentRoleId = "parent-id",
            TenantId = tenantId,
            IsSystemRole = true,
            SortOrder = 5,
            IconName = "shield",
            Color = "blue"
        };

        _roleManagerMock.Setup(x => x.FindByIdAsync(roleId))
            .ReturnsAsync(role);

        // Act
        var result = await _sut.FindByIdAsync(roleId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(roleId);
        result.Name.Should().Be("TestRole");
        result.NormalizedName.Should().Be("TESTROLE");
        result.Description.Should().Be("Test description");
        result.ParentRoleId.Should().Be("parent-id");
        result.TenantId.Should().Be(tenantId);
        result.IsSystemRole.Should().BeTrue();
        result.SortOrder.Should().Be(5);
        result.IconName.Should().Be("shield");
        result.Color.Should().Be("blue");
    }

    #endregion

    #region FindByNameAsync Tests

    [Fact]
    public async Task FindByNameAsync_WithExistingRole_ShouldReturnRoleDto()
    {
        // Arrange
        var roleName = "Admin";
        var role = CreateTestRole(name: roleName);

        _roleManagerMock.Setup(x => x.FindByNameAsync(roleName))
            .ReturnsAsync(role);

        // Act
        var result = await _sut.FindByNameAsync(roleName);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be(roleName);
    }

    [Fact]
    public async Task FindByNameAsync_WithNonExistingRole_ShouldReturnNull()
    {
        // Arrange
        var roleName = "NonExistent";
        _roleManagerMock.Setup(x => x.FindByNameAsync(roleName))
            .ReturnsAsync((ApplicationRole?)null);

        // Act
        var result = await _sut.FindByNameAsync(roleName);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region RoleExistsAsync Tests

    [Fact]
    public async Task RoleExistsAsync_WithExistingRole_ShouldReturnTrue()
    {
        // Arrange
        var roleName = "Admin";
        _roleManagerMock.Setup(x => x.RoleExistsAsync(roleName))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.RoleExistsAsync(roleName);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task RoleExistsAsync_WithNonExistingRole_ShouldReturnFalse()
    {
        // Arrange
        var roleName = "NonExistent";
        _roleManagerMock.Setup(x => x.RoleExistsAsync(roleName))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.RoleExistsAsync(roleName);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region CreateRoleAsync Tests (Simple)

    [Fact]
    public async Task CreateRoleAsync_Simple_WithValidName_ShouldReturnSuccess()
    {
        // Arrange
        var roleName = "NewRole";
        _roleManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationRole>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _sut.CreateRoleAsync(roleName);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.UserId.Should().NotBeNullOrEmpty(); // Returns roleId
    }

    [Fact]
    public async Task CreateRoleAsync_Simple_WithDuplicateName_ShouldReturnFailure()
    {
        // Arrange
        var roleName = "ExistingRole";
        var errors = new[] { new IdentityError { Description = "Role name already exists" } };

        _roleManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationRole>()))
            .ReturnsAsync(IdentityResult.Failed(errors));

        // Act
        var result = await _sut.CreateRoleAsync(roleName);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("Role name already exists");
    }

    #endregion

    #region CreateRoleAsync Tests (Full)

    [Fact]
    public async Task CreateRoleAsync_Full_WithAllProperties_ShouldReturnSuccess()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        ApplicationRole? capturedRole = null;

        _roleManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationRole>()))
            .Callback<ApplicationRole>(r => capturedRole = r)
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _sut.CreateRoleAsync(
            "TestRole",
            "Test description",
            "parent-id",
            tenantId,
            true,       // isSystemRole
            false,      // isPlatformRole
            5,
            "shield",
            "blue");

        // Assert
        result.Succeeded.Should().BeTrue();
        capturedRole.Should().NotBeNull();
        capturedRole!.Name.Should().Be("TestRole");
        capturedRole.Description.Should().Be("Test description");
        capturedRole.ParentRoleId.Should().Be("parent-id");
        capturedRole.TenantId.Should().Be(tenantId);
        capturedRole.IsSystemRole.Should().BeTrue();
        capturedRole.SortOrder.Should().Be(5);
        capturedRole.IconName.Should().Be("shield");
        capturedRole.Color.Should().Be("blue");
    }

    [Fact]
    public async Task CreateRoleAsync_Full_WithFailure_ShouldReturnErrors()
    {
        // Arrange
        var errors = new[]
        {
            new IdentityError { Description = "Error 1" },
            new IdentityError { Description = "Error 2" }
        };

        _roleManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationRole>()))
            .ReturnsAsync(IdentityResult.Failed(errors));

        // Act
        var result = await _sut.CreateRoleAsync("TestRole", null, null, null, false, false, 0, null, null);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
        result.Errors.Should().Contain("Error 1");
        result.Errors.Should().Contain("Error 2");
    }

    #endregion

    #region UpdateRoleAsync Tests (Simple)

    [Fact]
    public async Task UpdateRoleAsync_Simple_WithExistingRole_ShouldReturnSuccess()
    {
        // Arrange
        var roleId = Guid.NewGuid().ToString();
        var role = CreateTestRole(roleId, "OldName");

        _roleManagerMock.Setup(x => x.FindByIdAsync(roleId))
            .ReturnsAsync(role);
        _roleManagerMock.Setup(x => x.UpdateAsync(role))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _sut.UpdateRoleAsync(roleId, "NewName");

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateRoleAsync_Simple_WithNonExistingRole_ShouldReturnFailure()
    {
        // Arrange
        var roleId = Guid.NewGuid().ToString();
        _roleManagerMock.Setup(x => x.FindByIdAsync(roleId))
            .ReturnsAsync((ApplicationRole?)null);

        // Act
        var result = await _sut.UpdateRoleAsync(roleId, "NewName");

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("Role not found.");
    }

    #endregion

    #region UpdateRoleAsync Tests (Full)

    [Fact]
    public async Task UpdateRoleAsync_Full_WithExistingRole_ShouldUpdateAllProperties()
    {
        // Arrange
        var roleId = Guid.NewGuid().ToString();
        var role = CreateTestRole(roleId, "OldName");

        _roleManagerMock.Setup(x => x.FindByIdAsync(roleId))
            .ReturnsAsync(role);
        _roleManagerMock.Setup(x => x.UpdateAsync(role))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _sut.UpdateRoleAsync(
            roleId,
            "NewName",
            "New description",
            "new-parent-id",
            10,
            "users",
            "red");

        // Assert
        result.Succeeded.Should().BeTrue();
        role.Name.Should().Be("NewName");
        role.Description.Should().Be("New description");
        role.ParentRoleId.Should().Be("new-parent-id");
        role.SortOrder.Should().Be(10);
        role.IconName.Should().Be("users");
        role.Color.Should().Be("red");
    }

    [Fact]
    public async Task UpdateRoleAsync_Full_WithSystemRole_TryingToRename_ShouldReturnFailure()
    {
        // Arrange
        var roleId = Guid.NewGuid().ToString();
        var role = CreateTestRole(roleId, "SystemRole", isSystemRole: true);

        _roleManagerMock.Setup(x => x.FindByIdAsync(roleId))
            .ReturnsAsync(role);

        // Act
        var result = await _sut.UpdateRoleAsync(roleId, "NewName", null, null, 0, null, null);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("Cannot rename a system role.");
    }

    [Fact]
    public async Task UpdateRoleAsync_Full_WithSystemRole_SameName_ShouldSucceed()
    {
        // Arrange
        var roleId = Guid.NewGuid().ToString();
        var role = CreateTestRole(roleId, "SystemRole", isSystemRole: true);

        _roleManagerMock.Setup(x => x.FindByIdAsync(roleId))
            .ReturnsAsync(role);
        _roleManagerMock.Setup(x => x.UpdateAsync(role))
            .ReturnsAsync(IdentityResult.Success);

        // Act - Keep same name, but update description
        var result = await _sut.UpdateRoleAsync(roleId, "SystemRole", "New description", null, 0, null, null);

        // Assert
        result.Succeeded.Should().BeTrue();
        role.Description.Should().Be("New description");
    }

    #endregion

    #region DeleteRoleAsync Tests

    // Note: DeleteRoleAsync_WithExistingRole_ShouldSoftDelete test requires complex async queryable mocking
    // that depends on Entity Framework Core internals. This scenario is better tested in integration tests.

    [Fact]
    public async Task DeleteRoleAsync_WithNonExistingRole_ShouldReturnFailure()
    {
        // Arrange
        var roleId = Guid.NewGuid().ToString();
        _roleManagerMock.Setup(x => x.FindByIdAsync(roleId))
            .ReturnsAsync((ApplicationRole?)null);

        // Act
        var result = await _sut.DeleteRoleAsync(roleId);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("Role not found.");
    }

    [Fact]
    public async Task DeleteRoleAsync_WithSystemRole_ShouldReturnFailure()
    {
        // Arrange
        var roleId = Guid.NewGuid().ToString();
        var role = CreateTestRole(roleId, "SystemRole", isSystemRole: true);

        _roleManagerMock.Setup(x => x.FindByIdAsync(roleId))
            .ReturnsAsync(role);

        // Act
        var result = await _sut.DeleteRoleAsync(roleId);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("Cannot delete a system role.");
    }

    // Note: DeleteRoleAsync_WithChildRoles_ShouldReturnFailure test requires complex async queryable mocking
    // that depends on Entity Framework Core internals. This scenario is better tested in integration tests.

    #endregion

    #region Permissions Tests

    [Fact]
    public async Task GetPermissionsAsync_WithExistingRole_ShouldReturnPermissions()
    {
        // Arrange
        var roleId = Guid.NewGuid().ToString();
        var role = CreateTestRole(roleId, "Admin");
        var claims = new List<Claim>
        {
            new(Permissions.ClaimType, "users.read"),
            new(Permissions.ClaimType, "users.write"),
            new("other-claim-type", "other-value") // Should be filtered out
        };

        _roleManagerMock.Setup(x => x.FindByIdAsync(roleId))
            .ReturnsAsync(role);
        _roleManagerMock.Setup(x => x.GetClaimsAsync(role))
            .ReturnsAsync(claims);

        // Act
        var result = await _sut.GetPermissionsAsync(roleId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain("users.read");
        result.Should().Contain("users.write");
    }

    [Fact]
    public async Task GetPermissionsAsync_WithNonExistingRole_ShouldReturnEmptyList()
    {
        // Arrange
        var roleId = Guid.NewGuid().ToString();
        _roleManagerMock.Setup(x => x.FindByIdAsync(roleId))
            .ReturnsAsync((ApplicationRole?)null);

        // Act
        var result = await _sut.GetPermissionsAsync(roleId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task AddPermissionsAsync_WithExistingRole_ShouldAddNewPermissions()
    {
        // Arrange
        var roleId = Guid.NewGuid().ToString();
        var role = CreateTestRole(roleId, "Admin");
        var existingClaims = new List<Claim>
        {
            new(Permissions.ClaimType, "existing.permission")
        };
        var newPermissions = new[] { "new.permission", "existing.permission" }; // existing.permission should be skipped

        _roleManagerMock.Setup(x => x.FindByIdAsync(roleId))
            .ReturnsAsync(role);
        _roleManagerMock.Setup(x => x.GetClaimsAsync(role))
            .ReturnsAsync(existingClaims);
        _roleManagerMock.Setup(x => x.AddClaimAsync(role, It.IsAny<Claim>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _sut.AddPermissionsAsync(roleId, newPermissions);

        // Assert
        result.Succeeded.Should().BeTrue();
        _roleManagerMock.Verify(x => x.AddClaimAsync(role, It.Is<Claim>(c => c.Value == "new.permission")), Times.Once);
        _roleManagerMock.Verify(x => x.AddClaimAsync(role, It.Is<Claim>(c => c.Value == "existing.permission")), Times.Never);
    }

    [Fact]
    public async Task AddPermissionsAsync_WithNonExistingRole_ShouldReturnFailure()
    {
        // Arrange
        var roleId = Guid.NewGuid().ToString();
        _roleManagerMock.Setup(x => x.FindByIdAsync(roleId))
            .ReturnsAsync((ApplicationRole?)null);

        // Act
        var result = await _sut.AddPermissionsAsync(roleId, new[] { "permission" });

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("Role not found.");
    }

    [Fact]
    public async Task RemovePermissionsAsync_WithExistingRole_ShouldRemovePermissions()
    {
        // Arrange
        var roleId = Guid.NewGuid().ToString();
        var role = CreateTestRole(roleId, "Admin");
        var permissionsToRemove = new[] { "permission.to.remove" };

        _roleManagerMock.Setup(x => x.FindByIdAsync(roleId))
            .ReturnsAsync(role);
        _roleManagerMock.Setup(x => x.RemoveClaimAsync(role, It.IsAny<Claim>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _sut.RemovePermissionsAsync(roleId, permissionsToRemove);

        // Assert
        result.Succeeded.Should().BeTrue();
        _roleManagerMock.Verify(x => x.RemoveClaimAsync(role, It.Is<Claim>(c => c.Value == "permission.to.remove")), Times.Once);
    }

    [Fact]
    public async Task RemovePermissionsAsync_WithNonExistingRole_ShouldReturnFailure()
    {
        // Arrange
        var roleId = Guid.NewGuid().ToString();
        _roleManagerMock.Setup(x => x.FindByIdAsync(roleId))
            .ReturnsAsync((ApplicationRole?)null);

        // Act
        var result = await _sut.RemovePermissionsAsync(roleId, new[] { "permission" });

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("Role not found.");
    }

    [Fact]
    public async Task SetPermissionsAsync_WithExistingRole_ShouldReplacePermissions()
    {
        // Arrange
        var roleId = Guid.NewGuid().ToString();
        var role = CreateTestRole(roleId, "Admin");
        var existingClaims = new List<Claim>
        {
            new(Permissions.ClaimType, "old.permission"),
            new(Permissions.ClaimType, "keep.permission")
        };
        var newPermissions = new[] { "keep.permission", "new.permission" };

        _roleManagerMock.Setup(x => x.FindByIdAsync(roleId))
            .ReturnsAsync(role);
        _roleManagerMock.Setup(x => x.GetClaimsAsync(role))
            .ReturnsAsync(existingClaims);
        _roleManagerMock.Setup(x => x.RemoveClaimAsync(role, It.IsAny<Claim>()))
            .ReturnsAsync(IdentityResult.Success);
        _roleManagerMock.Setup(x => x.AddClaimAsync(role, It.IsAny<Claim>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _sut.SetPermissionsAsync(roleId, newPermissions);

        // Assert
        result.Succeeded.Should().BeTrue();
        // Should remove old.permission (not in new set)
        _roleManagerMock.Verify(x => x.RemoveClaimAsync(role, It.Is<Claim>(c => c.Value == "old.permission")), Times.Once);
        // Should add new.permission (not in existing set)
        _roleManagerMock.Verify(x => x.AddClaimAsync(role, It.Is<Claim>(c => c.Value == "new.permission")), Times.Once);
        // Should not touch keep.permission
        _roleManagerMock.Verify(x => x.RemoveClaimAsync(role, It.Is<Claim>(c => c.Value == "keep.permission")), Times.Never);
        _roleManagerMock.Verify(x => x.AddClaimAsync(role, It.Is<Claim>(c => c.Value == "keep.permission")), Times.Never);
    }

    [Fact]
    public async Task SetPermissionsAsync_WithNonExistingRole_ShouldReturnFailure()
    {
        // Arrange
        var roleId = Guid.NewGuid().ToString();
        _roleManagerMock.Setup(x => x.FindByIdAsync(roleId))
            .ReturnsAsync((ApplicationRole?)null);

        // Act
        var result = await _sut.SetPermissionsAsync(roleId, new[] { "permission" });

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("Role not found.");
    }

    #endregion

    #region Effective Permissions Tests

    [Fact]
    public async Task GetEffectivePermissionsAsync_WithNoParent_ShouldReturnDirectPermissions()
    {
        // Arrange
        var roleId = Guid.NewGuid().ToString();
        var role = CreateTestRole(roleId, "Role");
        var claims = new List<Claim>
        {
            new(Permissions.ClaimType, "direct.permission1"),
            new(Permissions.ClaimType, "direct.permission2")
        };

        _roleManagerMock.Setup(x => x.FindByIdAsync(roleId))
            .ReturnsAsync(role);
        _roleManagerMock.Setup(x => x.GetClaimsAsync(role))
            .ReturnsAsync(claims);

        // Act
        var result = await _sut.GetEffectivePermissionsAsync(roleId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain("direct.permission1");
        result.Should().Contain("direct.permission2");
    }

    [Fact]
    public async Task GetEffectivePermissionsAsync_WithParentRole_ShouldIncludeInheritedPermissions()
    {
        // Arrange
        var parentRoleId = Guid.NewGuid().ToString();
        var childRoleId = Guid.NewGuid().ToString();

        var parentRole = CreateTestRole(parentRoleId, "Parent");
        var childRole = CreateTestRole(childRoleId, "Child", parentRoleId: parentRoleId);

        var parentClaims = new List<Claim> { new(Permissions.ClaimType, "parent.permission") };
        var childClaims = new List<Claim> { new(Permissions.ClaimType, "child.permission") };

        _roleManagerMock.Setup(x => x.FindByIdAsync(childRoleId))
            .ReturnsAsync(childRole);
        _roleManagerMock.Setup(x => x.FindByIdAsync(parentRoleId))
            .ReturnsAsync(parentRole);
        _roleManagerMock.Setup(x => x.GetClaimsAsync(childRole))
            .ReturnsAsync(childClaims);
        _roleManagerMock.Setup(x => x.GetClaimsAsync(parentRole))
            .ReturnsAsync(parentClaims);

        // Act
        var result = await _sut.GetEffectivePermissionsAsync(childRoleId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain("child.permission");
        result.Should().Contain("parent.permission");
    }

    [Fact]
    public async Task GetEffectivePermissionsAsync_WithDeletedParent_ShouldNotIncludeParentPermissions()
    {
        // Arrange
        var parentRoleId = Guid.NewGuid().ToString();
        var childRoleId = Guid.NewGuid().ToString();

        var parentRole = CreateTestRole(parentRoleId, "Parent");
        parentRole.IsDeleted = true;
        var childRole = CreateTestRole(childRoleId, "Child", parentRoleId: parentRoleId);

        var childClaims = new List<Claim> { new(Permissions.ClaimType, "child.permission") };

        _roleManagerMock.Setup(x => x.FindByIdAsync(childRoleId))
            .ReturnsAsync(childRole);
        _roleManagerMock.Setup(x => x.FindByIdAsync(parentRoleId))
            .ReturnsAsync(parentRole);
        _roleManagerMock.Setup(x => x.GetClaimsAsync(childRole))
            .ReturnsAsync(childClaims);

        // Act
        var result = await _sut.GetEffectivePermissionsAsync(childRoleId);

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain("child.permission");
    }

    #endregion

    #region GetRoleHierarchyAsync Tests

    [Fact]
    public async Task GetRoleHierarchyAsync_WithNoParent_ShouldReturnSingleRole()
    {
        // Arrange
        var roleId = Guid.NewGuid().ToString();
        var role = CreateTestRole(roleId, "Role");

        _roleManagerMock.Setup(x => x.FindByIdAsync(roleId))
            .ReturnsAsync(role);

        // Act
        var result = await _sut.GetRoleHierarchyAsync(roleId);

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(roleId);
    }

    [Fact]
    public async Task GetRoleHierarchyAsync_WithParentChain_ShouldReturnFullHierarchy()
    {
        // Arrange
        var grandparentId = Guid.NewGuid().ToString();
        var parentId = Guid.NewGuid().ToString();
        var childId = Guid.NewGuid().ToString();

        var grandparent = CreateTestRole(grandparentId, "Grandparent");
        var parent = CreateTestRole(parentId, "Parent", parentRoleId: grandparentId);
        var child = CreateTestRole(childId, "Child", parentRoleId: parentId);

        _roleManagerMock.Setup(x => x.FindByIdAsync(childId)).ReturnsAsync(child);
        _roleManagerMock.Setup(x => x.FindByIdAsync(parentId)).ReturnsAsync(parent);
        _roleManagerMock.Setup(x => x.FindByIdAsync(grandparentId)).ReturnsAsync(grandparent);

        // Act
        var result = await _sut.GetRoleHierarchyAsync(childId);

        // Assert
        result.Should().HaveCount(3);
        result[0].Name.Should().Be("Child");
        result[1].Name.Should().Be("Parent");
        result[2].Name.Should().Be("Grandparent");
    }

    [Fact]
    public async Task GetRoleHierarchyAsync_WithDeletedRole_ShouldStopAtDeletedRole()
    {
        // Arrange
        var parentId = Guid.NewGuid().ToString();
        var childId = Guid.NewGuid().ToString();

        var parent = CreateTestRole(parentId, "Parent");
        parent.IsDeleted = true;
        var child = CreateTestRole(childId, "Child", parentRoleId: parentId);

        _roleManagerMock.Setup(x => x.FindByIdAsync(childId)).ReturnsAsync(child);
        _roleManagerMock.Setup(x => x.FindByIdAsync(parentId)).ReturnsAsync(parent);

        // Act
        var result = await _sut.GetRoleHierarchyAsync(childId);

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Child");
    }

    #endregion

    #region Service Interface Tests

    [Fact]
    public void Service_ShouldImplementIRoleIdentityService()
    {
        // Assert
        _sut.Should().BeAssignableTo<IRoleIdentityService>();
    }

    [Fact]
    public void Service_ShouldImplementIScopedService()
    {
        // Assert
        _sut.Should().BeAssignableTo<IScopedService>();
    }

    #endregion

    #region Helper Methods

    private static ApplicationRole CreateTestRole(
        string? roleId = null,
        string name = "TestRole",
        string? parentRoleId = null,
        bool isSystemRole = false)
    {
        var id = roleId ?? Guid.NewGuid().ToString();
        return new ApplicationRole
        {
            Id = id,
            Name = name,
            NormalizedName = name.ToUpperInvariant(),
            ParentRoleId = parentRoleId,
            IsSystemRole = isSystemRole,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    #endregion
}

