namespace NOIR.Domain.UnitTests.Common;

/// <summary>
/// Unit tests for Permissions constants and groups.
/// Tests permission constant values and group collections.
/// </summary>
public class PermissionsTests
{
    #region ClaimType Tests

    [Fact]
    public void ClaimType_ShouldBePermission()
    {
        // Assert
        Permissions.ClaimType.Should().Be("permission");
    }

    #endregion

    #region User Permissions Tests

    [Fact]
    public void UsersRead_ShouldHaveCorrectValue()
    {
        // Assert
        Permissions.UsersRead.Should().Be("users:read");
    }

    [Fact]
    public void UsersCreate_ShouldHaveCorrectValue()
    {
        // Assert
        Permissions.UsersCreate.Should().Be("users:create");
    }

    [Fact]
    public void UsersUpdate_ShouldHaveCorrectValue()
    {
        // Assert
        Permissions.UsersUpdate.Should().Be("users:update");
    }

    [Fact]
    public void UsersDelete_ShouldHaveCorrectValue()
    {
        // Assert
        Permissions.UsersDelete.Should().Be("users:delete");
    }

    [Fact]
    public void UsersManageRoles_ShouldHaveCorrectValue()
    {
        // Assert
        Permissions.UsersManageRoles.Should().Be("users:manage-roles");
    }

    #endregion

    #region Role Permissions Tests

    [Fact]
    public void RolesRead_ShouldHaveCorrectValue()
    {
        // Assert
        Permissions.RolesRead.Should().Be("roles:read");
    }

    [Fact]
    public void RolesCreate_ShouldHaveCorrectValue()
    {
        // Assert
        Permissions.RolesCreate.Should().Be("roles:create");
    }

    [Fact]
    public void RolesUpdate_ShouldHaveCorrectValue()
    {
        // Assert
        Permissions.RolesUpdate.Should().Be("roles:update");
    }

    [Fact]
    public void RolesDelete_ShouldHaveCorrectValue()
    {
        // Assert
        Permissions.RolesDelete.Should().Be("roles:delete");
    }

    [Fact]
    public void RolesManagePermissions_ShouldHaveCorrectValue()
    {
        // Assert
        Permissions.RolesManagePermissions.Should().Be("roles:manage-permissions");
    }

    #endregion

    #region Tenant Permissions Tests

    [Fact]
    public void TenantsRead_ShouldHaveCorrectValue()
    {
        // Assert
        Permissions.TenantsRead.Should().Be("tenants:read");
    }

    [Fact]
    public void TenantsCreate_ShouldHaveCorrectValue()
    {
        // Assert
        Permissions.TenantsCreate.Should().Be("tenants:create");
    }

    [Fact]
    public void TenantsUpdate_ShouldHaveCorrectValue()
    {
        // Assert
        Permissions.TenantsUpdate.Should().Be("tenants:update");
    }

    [Fact]
    public void TenantsDelete_ShouldHaveCorrectValue()
    {
        // Assert
        Permissions.TenantsDelete.Should().Be("tenants:delete");
    }

    #endregion

    #region System Permissions Tests

    [Fact]
    public void SystemAdmin_ShouldHaveCorrectValue()
    {
        // Assert
        Permissions.SystemAdmin.Should().Be("system:admin");
    }

    [Fact]
    public void SystemAuditLogs_ShouldHaveCorrectValue()
    {
        // Assert
        Permissions.SystemAuditLogs.Should().Be("system:audit-logs");
    }

    [Fact]
    public void SystemSettings_ShouldHaveCorrectValue()
    {
        // Assert
        Permissions.SystemSettings.Should().Be("system:settings");
    }

    [Fact]
    public void HangfireDashboard_ShouldHaveCorrectValue()
    {
        // Assert
        Permissions.HangfireDashboard.Should().Be("system:hangfire");
    }

    #endregion

    #region Audit Permissions Tests

    [Fact]
    public void AuditRead_ShouldHaveCorrectValue()
    {
        // Assert
        Permissions.AuditRead.Should().Be("audit:read");
    }

    [Fact]
    public void AuditExport_ShouldHaveCorrectValue()
    {
        // Assert
        Permissions.AuditExport.Should().Be("audit:export");
    }

    [Fact]
    public void AuditEntityHistory_ShouldHaveCorrectValue()
    {
        // Assert
        Permissions.AuditEntityHistory.Should().Be("audit:entity-history");
    }

    #endregion

    #region Groups Tests

    [Fact]
    public void Groups_Users_ShouldContainAllUserPermissions()
    {
        // Assert
        Permissions.Groups.Users.Should().HaveCount(5);
        Permissions.Groups.Users.Should().Contain(Permissions.UsersRead);
        Permissions.Groups.Users.Should().Contain(Permissions.UsersCreate);
        Permissions.Groups.Users.Should().Contain(Permissions.UsersUpdate);
        Permissions.Groups.Users.Should().Contain(Permissions.UsersDelete);
        Permissions.Groups.Users.Should().Contain(Permissions.UsersManageRoles);
    }

    [Fact]
    public void Groups_Roles_ShouldContainAllRolePermissions()
    {
        // Assert
        Permissions.Groups.Roles.Should().HaveCount(5);
        Permissions.Groups.Roles.Should().Contain(Permissions.RolesRead);
        Permissions.Groups.Roles.Should().Contain(Permissions.RolesCreate);
        Permissions.Groups.Roles.Should().Contain(Permissions.RolesUpdate);
        Permissions.Groups.Roles.Should().Contain(Permissions.RolesDelete);
        Permissions.Groups.Roles.Should().Contain(Permissions.RolesManagePermissions);
    }

    [Fact]
    public void Groups_Tenants_ShouldContainAllTenantPermissions()
    {
        // Assert
        Permissions.Groups.Tenants.Should().HaveCount(4);
        Permissions.Groups.Tenants.Should().Contain(Permissions.TenantsRead);
        Permissions.Groups.Tenants.Should().Contain(Permissions.TenantsCreate);
        Permissions.Groups.Tenants.Should().Contain(Permissions.TenantsUpdate);
        Permissions.Groups.Tenants.Should().Contain(Permissions.TenantsDelete);
    }

    [Fact]
    public void Groups_System_ShouldContainAllSystemPermissions()
    {
        // Assert
        Permissions.Groups.System.Should().HaveCount(4);
        Permissions.Groups.System.Should().Contain(Permissions.SystemAdmin);
        Permissions.Groups.System.Should().Contain(Permissions.SystemAuditLogs);
        Permissions.Groups.System.Should().Contain(Permissions.SystemSettings);
        Permissions.Groups.System.Should().Contain(Permissions.HangfireDashboard);
    }

    [Fact]
    public void Groups_Audit_ShouldContainAllAuditPermissions()
    {
        // Assert
        Permissions.Groups.Audit.Should().HaveCount(7);
        Permissions.Groups.Audit.Should().Contain(Permissions.AuditRead);
        Permissions.Groups.Audit.Should().Contain(Permissions.AuditExport);
        Permissions.Groups.Audit.Should().Contain(Permissions.AuditEntityHistory);
        Permissions.Groups.Audit.Should().Contain(Permissions.AuditPolicyRead);
        Permissions.Groups.Audit.Should().Contain(Permissions.AuditPolicyWrite);
        Permissions.Groups.Audit.Should().Contain(Permissions.AuditPolicyDelete);
        Permissions.Groups.Audit.Should().Contain(Permissions.AuditStream);
    }

    #endregion

    #region All Permissions Tests

    [Fact]
    public void All_ShouldContainAllPermissions()
    {
        // Calculate expected count dynamically from all groups to stay in sync
        var expectedCount = Permissions.Groups.Users.Count
            + Permissions.Groups.Roles.Count
            + Permissions.Groups.Tenants.Count
            + Permissions.Groups.System.Count
            + Permissions.Groups.Audit.Count
            + Permissions.Groups.EmailTemplates.Count
            + Permissions.Groups.BlogPosts.Count
            + Permissions.Groups.BlogCategories.Count
            + Permissions.Groups.BlogTags.Count;

        // Assert
        Permissions.All.Should().HaveCount(expectedCount);
    }

    [Fact]
    public void All_ShouldContainAllUserPermissions()
    {
        // Assert
        foreach (var permission in Permissions.Groups.Users)
        {
            Permissions.All.Should().Contain(permission);
        }
    }

    [Fact]
    public void All_ShouldContainAllRolePermissions()
    {
        // Assert
        foreach (var permission in Permissions.Groups.Roles)
        {
            Permissions.All.Should().Contain(permission);
        }
    }

    [Fact]
    public void All_ShouldContainAllTenantPermissions()
    {
        // Assert
        foreach (var permission in Permissions.Groups.Tenants)
        {
            Permissions.All.Should().Contain(permission);
        }
    }

    [Fact]
    public void All_ShouldContainAllSystemPermissions()
    {
        // Assert
        foreach (var permission in Permissions.Groups.System)
        {
            Permissions.All.Should().Contain(permission);
        }
    }

    [Fact]
    public void All_ShouldContainAllAuditPermissions()
    {
        // Assert
        foreach (var permission in Permissions.Groups.Audit)
        {
            Permissions.All.Should().Contain(permission);
        }
    }

    #endregion

    #region Default Permissions Tests

    [Fact]
    public void AdminDefaults_ShouldEqualAll()
    {
        // Assert
        Permissions.AdminDefaults.Should().BeEquivalentTo(Permissions.All);
    }

    [Fact]
    public void UserDefaults_ShouldOnlyContainUsersRead()
    {
        // Assert
        Permissions.UserDefaults.Should().HaveCount(1);
        Permissions.UserDefaults.Should().Contain(Permissions.UsersRead);
    }

    #endregion

    #region Permission Format Tests

    [Fact]
    public void AllPermissions_ShouldFollowResourceActionFormat()
    {
        // Assert
        foreach (var permission in Permissions.All)
        {
            permission.Should().Contain(":");
            var parts = permission.Split(':');
            parts.Should().HaveCount(2);
            parts[0].Should().NotBeNullOrWhiteSpace("resource should not be empty");
            parts[1].Should().NotBeNullOrWhiteSpace("action should not be empty");
        }
    }

    #endregion
}
