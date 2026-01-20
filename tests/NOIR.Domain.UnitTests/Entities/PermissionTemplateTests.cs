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
        var template = PermissionTemplate.Create(name);

        // Assert
        template.Should().NotBeNull();
        template.Id.Should().NotBe(Guid.Empty);
        template.Name.Should().Be(name);
        template.IsSystem.Should().BeFalse();
        template.SortOrder.Should().Be(0);
        template.Items.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithAllParameters_ShouldSetAllProperties()
    {
        // Arrange
        var name = "Content Manager";
        var description = "Can manage content but not users";
        var tenantId = "tenant-123";
        var isSystem = true;
        var iconName = "file-text";
        var color = "#3B82F6";
        var sortOrder = 10;

        // Act
        var template = PermissionTemplate.Create(
            name, description, tenantId, isSystem, iconName, color, sortOrder);

        // Assert
        template.Name.Should().Be(name);
        template.Description.Should().Be(description);
        template.TenantId.Should().Be(tenantId);
        template.IsSystem.Should().BeTrue();
        template.IconName.Should().Be(iconName);
        template.Color.Should().Be(color);
        template.SortOrder.Should().Be(sortOrder);
    }

    [Fact]
    public void Create_WithoutTenantId_ShouldBeSystemTemplate()
    {
        // Act
        var template = PermissionTemplate.Create("Global Template");

        // Assert
        template.TenantId.Should().BeNull();
    }

    #endregion

    #region Update Tests

    [Fact]
    public void Update_ShouldModifyAllEditableProperties()
    {
        // Arrange
        var template = PermissionTemplate.Create("Original");
        var newName = "Updated Name";
        var newDescription = "Updated description";
        var newIconName = "shield";
        var newColor = "#EF4444";
        var newSortOrder = 5;

        // Act
        template.Update(newName, newDescription, newIconName, newColor, newSortOrder);

        // Assert
        template.Name.Should().Be(newName);
        template.Description.Should().Be(newDescription);
        template.IconName.Should().Be(newIconName);
        template.Color.Should().Be(newColor);
        template.SortOrder.Should().Be(newSortOrder);
    }

    [Fact]
    public void Update_WithNullOptionalValues_ShouldClearThem()
    {
        // Arrange
        var template = PermissionTemplate.Create("Test", "Original Desc", null, false, "icon", "#000", 1);

        // Act
        template.Update("Test", null, null, null, 0);

        // Assert
        template.Description.Should().BeNull();
        template.IconName.Should().BeNull();
        template.Color.Should().BeNull();
    }

    #endregion

    #region AddPermission Tests

    [Fact]
    public void AddPermission_ShouldAddPermissionToItems()
    {
        // Arrange
        var template = PermissionTemplate.Create("Test");
        var permissionId = Guid.NewGuid();

        // Act
        template.AddPermission(permissionId);

        // Assert
        template.Items.Should().HaveCount(1);
        template.Items.Should().Contain(i => i.PermissionId == permissionId);
    }

    [Fact]
    public void AddPermission_DuplicatePermission_ShouldNotAddAgain()
    {
        // Arrange
        var template = PermissionTemplate.Create("Test");
        var permissionId = Guid.NewGuid();

        // Act
        template.AddPermission(permissionId);
        template.AddPermission(permissionId);

        // Assert
        template.Items.Should().HaveCount(1);
    }

    [Fact]
    public void AddPermission_MultiplePermissions_ShouldAddAll()
    {
        // Arrange
        var template = PermissionTemplate.Create("Test");
        var permission1 = Guid.NewGuid();
        var permission2 = Guid.NewGuid();
        var permission3 = Guid.NewGuid();

        // Act
        template.AddPermission(permission1);
        template.AddPermission(permission2);
        template.AddPermission(permission3);

        // Assert
        template.Items.Should().HaveCount(3);
    }

    #endregion

    #region RemovePermission Tests

    [Fact]
    public void RemovePermission_ExistingPermission_ShouldRemove()
    {
        // Arrange
        var template = PermissionTemplate.Create("Test");
        var permissionId = Guid.NewGuid();
        template.AddPermission(permissionId);

        // Act
        template.RemovePermission(permissionId);

        // Assert
        template.Items.Should().BeEmpty();
    }

    [Fact]
    public void RemovePermission_NonExistentPermission_ShouldNotThrow()
    {
        // Arrange
        var template = PermissionTemplate.Create("Test");
        var permissionId = Guid.NewGuid();

        // Act
        var act = () => template.RemovePermission(permissionId);

        // Assert
        act.Should().NotThrow();
        template.Items.Should().BeEmpty();
    }

    [Fact]
    public void RemovePermission_PartialRemoval_ShouldOnlyRemoveSpecified()
    {
        // Arrange
        var template = PermissionTemplate.Create("Test");
        var permission1 = Guid.NewGuid();
        var permission2 = Guid.NewGuid();
        template.AddPermission(permission1);
        template.AddPermission(permission2);

        // Act
        template.RemovePermission(permission1);

        // Assert
        template.Items.Should().HaveCount(1);
        template.Items.Should().Contain(i => i.PermissionId == permission2);
    }

    #endregion

    #region SetPermissions Tests

    [Fact]
    public void SetPermissions_ShouldReplaceAllPermissions()
    {
        // Arrange
        var template = PermissionTemplate.Create("Test");
        template.AddPermission(Guid.NewGuid());
        template.AddPermission(Guid.NewGuid());

        var newPermissions = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

        // Act
        template.SetPermissions(newPermissions);

        // Assert
        template.Items.Should().HaveCount(3);
        template.Items.Select(i => i.PermissionId).Should().BeEquivalentTo(newPermissions);
    }

    [Fact]
    public void SetPermissions_WithEmptyList_ShouldClearAllPermissions()
    {
        // Arrange
        var template = PermissionTemplate.Create("Test");
        template.AddPermission(Guid.NewGuid());
        template.AddPermission(Guid.NewGuid());

        // Act
        template.SetPermissions(Array.Empty<Guid>());

        // Assert
        template.Items.Should().BeEmpty();
    }

    [Fact]
    public void SetPermissions_ShouldSetCorrectTemplateId()
    {
        // Arrange
        var template = PermissionTemplate.Create("Test");
        var permissionId = Guid.NewGuid();

        // Act
        template.SetPermissions(new[] { permissionId });

        // Assert
        template.Items.Should().OnlyContain(i => i.TemplateId == template.Id);
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
        item.Should().NotBeNull();
        item.Id.Should().NotBe(Guid.Empty);
        item.TemplateId.Should().Be(templateId);
        item.PermissionId.Should().Be(permissionId);
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
        item1.Id.Should().NotBe(item2.Id);
    }

    #endregion

    #region IAuditableEntity Tests

    [Fact]
    public void Create_ShouldInitializeAuditableProperties()
    {
        // Act
        var template = PermissionTemplate.Create("Test");

        // Assert
        template.IsDeleted.Should().BeFalse();
        template.DeletedAt.Should().BeNull();
        template.DeletedBy.Should().BeNull();
        template.CreatedBy.Should().BeNull();
        template.ModifiedBy.Should().BeNull();
    }

    #endregion

    #region IsSystem Tests

    [Fact]
    public void Create_WithIsSystemTrue_ShouldBeSystemTemplate()
    {
        // Act
        var template = PermissionTemplate.Create("Admin", isSystem: true);

        // Assert
        template.IsSystem.Should().BeTrue();
    }

    [Fact]
    public void Create_WithIsSystemFalse_ShouldNotBeSystemTemplate()
    {
        // Act
        var template = PermissionTemplate.Create("Custom", isSystem: false);

        // Assert
        template.IsSystem.Should().BeFalse();
    }

    #endregion
}
