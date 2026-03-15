namespace NOIR.Domain.UnitTests.Entities;

/// <summary>
/// Unit tests for the PermissionTemplate entity and PermissionTemplateItem.
/// Tests factory methods, permission management, and template updates.
/// </summary>
public class PermissionTemplateTests
{
    #region Create Factory Tests

    [Fact]
    public void Create_WithRequiredParameters_ShouldCreateValidTemplate()
    {
        // Arrange
        var name = "Administrator";

        // Act
        var template = PermissionTemplate.CreatePlatformDefault(name);

        // Assert
        template.ShouldNotBeNull();
        template.Id.ShouldNotBe(Guid.Empty);
        template.Name.ShouldBe(name);
        template.IsSystem.ShouldBeFalse();
        template.SortOrder.ShouldBe(0);
        template.Items.ShouldBeEmpty();
    }

    [Fact]
    public void CreateTenantOverride_WithAllParameters_ShouldSetAllProperties()
    {
        // Arrange
        var tenantId = "tenant-123";
        var name = "Content Manager";
        var description = "Can manage content but not users";
        var isSystem = true;
        var iconName = "file-text";
        var color = "#3B82F6";
        var sortOrder = 10;

        // Act
        var template = PermissionTemplate.CreateTenantOverride(
            tenantId, name, description, isSystem, iconName, color, sortOrder);

        // Assert
        template.Name.ShouldBe(name);
        template.Description.ShouldBe(description);
        template.TenantId.ShouldBe(tenantId);
        template.IsSystem.ShouldBeTrue();
        template.IconName.ShouldBe(iconName);
        template.Color.ShouldBe(color);
        template.SortOrder.ShouldBe(sortOrder);
    }

    [Fact]
    public void CreatePlatformDefault_ShouldHaveNullTenantId()
    {
        // Act
        var template = PermissionTemplate.CreatePlatformDefault("Global Template");

        // Assert
        template.TenantId.ShouldBeNull();
    }

    #endregion

    #region Update Tests

    [Fact]
    public void Update_ShouldModifyAllEditableProperties()
    {
        // Arrange
        var template = PermissionTemplate.CreatePlatformDefault("Original");
        var newName = "Updated Name";
        var newDescription = "Updated description";
        var newIconName = "shield";
        var newColor = "#EF4444";
        var newSortOrder = 5;

        // Act
        template.Update(newName, newDescription, newIconName, newColor, newSortOrder);

        // Assert
        template.Name.ShouldBe(newName);
        template.Description.ShouldBe(newDescription);
        template.IconName.ShouldBe(newIconName);
        template.Color.ShouldBe(newColor);
        template.SortOrder.ShouldBe(newSortOrder);
    }

    [Fact]
    public void Update_WithNullOptionalValues_ShouldClearThem()
    {
        // Arrange
        var template = PermissionTemplate.CreatePlatformDefault("Test", "Original Desc", false, "icon", "#000", 1);

        // Act
        template.Update("Test", null, null, null, 0);

        // Assert
        template.Description.ShouldBeNull();
        template.IconName.ShouldBeNull();
        template.Color.ShouldBeNull();
    }

    #endregion

    #region AddPermission Tests

    [Fact]
    public void AddPermission_ShouldAddPermissionToItems()
    {
        // Arrange
        var template = PermissionTemplate.CreatePlatformDefault("Test");
        var permissionId = Guid.NewGuid();

        // Act
        template.AddPermission(permissionId);

        // Assert
        template.Items.Count().ShouldBe(1);
        template.Items.ShouldContain(i => i.PermissionId == permissionId);
    }

    [Fact]
    public void AddPermission_DuplicatePermission_ShouldNotAddAgain()
    {
        // Arrange
        var template = PermissionTemplate.CreatePlatformDefault("Test");
        var permissionId = Guid.NewGuid();

        // Act
        template.AddPermission(permissionId);
        template.AddPermission(permissionId);

        // Assert
        template.Items.Count().ShouldBe(1);
    }

    [Fact]
    public void AddPermission_MultiplePermissions_ShouldAddAll()
    {
        // Arrange
        var template = PermissionTemplate.CreatePlatformDefault("Test");
        var permission1 = Guid.NewGuid();
        var permission2 = Guid.NewGuid();
        var permission3 = Guid.NewGuid();

        // Act
        template.AddPermission(permission1);
        template.AddPermission(permission2);
        template.AddPermission(permission3);

        // Assert
        template.Items.Count().ShouldBe(3);
    }

    #endregion

    #region RemovePermission Tests

    [Fact]
    public void RemovePermission_ExistingPermission_ShouldRemove()
    {
        // Arrange
        var template = PermissionTemplate.CreatePlatformDefault("Test");
        var permissionId = Guid.NewGuid();
        template.AddPermission(permissionId);

        // Act
        template.RemovePermission(permissionId);

        // Assert
        template.Items.ShouldBeEmpty();
    }

    [Fact]
    public void RemovePermission_NonExistentPermission_ShouldNotThrow()
    {
        // Arrange
        var template = PermissionTemplate.CreatePlatformDefault("Test");
        var permissionId = Guid.NewGuid();

        // Act
        var act = () => template.RemovePermission(permissionId);

        // Assert
        act.ShouldNotThrow();
        template.Items.ShouldBeEmpty();
    }

    [Fact]
    public void RemovePermission_PartialRemoval_ShouldOnlyRemoveSpecified()
    {
        // Arrange
        var template = PermissionTemplate.CreatePlatformDefault("Test");
        var permission1 = Guid.NewGuid();
        var permission2 = Guid.NewGuid();
        template.AddPermission(permission1);
        template.AddPermission(permission2);

        // Act
        template.RemovePermission(permission1);

        // Assert
        template.Items.Count().ShouldBe(1);
        template.Items.ShouldContain(i => i.PermissionId == permission2);
    }

    #endregion

    #region SetPermissions Tests

    [Fact]
    public void SetPermissions_ShouldReplaceAllPermissions()
    {
        // Arrange
        var template = PermissionTemplate.CreatePlatformDefault("Test");
        template.AddPermission(Guid.NewGuid());
        template.AddPermission(Guid.NewGuid());

        var newPermissions = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

        // Act
        template.SetPermissions(newPermissions);

        // Assert
        template.Items.Count().ShouldBe(3);
        template.Items.Select(i => i.PermissionId).ShouldBe(newPermissions);
    }

    [Fact]
    public void SetPermissions_WithEmptyList_ShouldClearAllPermissions()
    {
        // Arrange
        var template = PermissionTemplate.CreatePlatformDefault("Test");
        template.AddPermission(Guid.NewGuid());
        template.AddPermission(Guid.NewGuid());

        // Act
        template.SetPermissions(Array.Empty<Guid>());

        // Assert
        template.Items.ShouldBeEmpty();
    }

    [Fact]
    public void SetPermissions_ShouldSetCorrectTemplateId()
    {
        // Arrange
        var template = PermissionTemplate.CreatePlatformDefault("Test");
        var permissionId = Guid.NewGuid();

        // Act
        template.SetPermissions(new[] { permissionId });

        // Assert
        template.Items.ShouldAllBe(i => i.TemplateId == template.Id);
    }

    #endregion

    #region PermissionTemplateItem Tests

    [Fact]
    public void PermissionTemplateItem_Create_ShouldSetProperties()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();

        // Act
        var item = PermissionTemplateItem.Create(templateId, permissionId);

        // Assert
        item.ShouldNotBeNull();
        item.Id.ShouldNotBe(Guid.Empty);
        item.TemplateId.ShouldBe(templateId);
        item.PermissionId.ShouldBe(permissionId);
    }

    [Fact]
    public void PermissionTemplateItem_Create_MultipleTimes_ShouldHaveUniqueIds()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();

        // Act
        var item1 = PermissionTemplateItem.Create(templateId, permissionId);
        var item2 = PermissionTemplateItem.Create(templateId, permissionId);

        // Assert
        item1.Id.ShouldNotBe(item2.Id);
    }

    #endregion

    #region IAuditableEntity Tests

    [Fact]
    public void Create_ShouldInitializeAuditableProperties()
    {
        // Act
        var template = PermissionTemplate.CreatePlatformDefault("Test");

        // Assert
        template.IsDeleted.ShouldBeFalse();
        template.DeletedAt.ShouldBeNull();
        template.DeletedBy.ShouldBeNull();
        template.CreatedBy.ShouldBeNull();
        template.ModifiedBy.ShouldBeNull();
    }

    #endregion

    #region IsSystem Tests

    [Fact]
    public void CreatePlatformDefault_WithIsSystemTrue_ShouldBeSystemTemplate()
    {
        // Act
        var template = PermissionTemplate.CreatePlatformDefault("Admin", isSystem: true);

        // Assert
        template.IsSystem.ShouldBeTrue();
    }

    [Fact]
    public void CreatePlatformDefault_WithIsSystemFalse_ShouldNotBeSystemTemplate()
    {
        // Act
        var template = PermissionTemplate.CreatePlatformDefault("Custom", isSystem: false);

        // Assert
        template.IsSystem.ShouldBeFalse();
    }

    #endregion
}
