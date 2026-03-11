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
        Permissions.Groups.SystemPermissions.Should().HaveCount(4);
        Permissions.Groups.SystemPermissions.Should().Contain(Permissions.SystemAdmin);
        Permissions.Groups.SystemPermissions.Should().Contain(Permissions.SystemAuditLogs);
        Permissions.Groups.SystemPermissions.Should().Contain(Permissions.SystemSettings);
        Permissions.Groups.SystemPermissions.Should().Contain(Permissions.HangfireDashboard);
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

    [Fact]
    public void Groups_Orders_ShouldContainAllOrderPermissions()
    {
        // Assert
        Permissions.Groups.Orders.Should().HaveCount(3);
        Permissions.Groups.Orders.Should().Contain(Permissions.OrdersRead);
        Permissions.Groups.Orders.Should().Contain(Permissions.OrdersWrite);
        Permissions.Groups.Orders.Should().Contain(Permissions.OrdersManage);
    }

    [Fact]
    public void Groups_Customers_ShouldContainAllCustomerPermissions()
    {
        // Assert
        Permissions.Groups.Customers.Should().HaveCount(5);
        Permissions.Groups.Customers.Should().Contain(Permissions.CustomersRead);
        Permissions.Groups.Customers.Should().Contain(Permissions.CustomersCreate);
        Permissions.Groups.Customers.Should().Contain(Permissions.CustomersUpdate);
        Permissions.Groups.Customers.Should().Contain(Permissions.CustomersDelete);
        Permissions.Groups.Customers.Should().Contain(Permissions.CustomersManage);
    }

    [Fact]
    public void Groups_Wishlists_ShouldContainAllWishlistPermissions()
    {
        // Assert
        Permissions.Groups.Wishlists.Should().HaveCount(3);
        Permissions.Groups.Wishlists.Should().Contain(Permissions.WishlistsRead);
        Permissions.Groups.Wishlists.Should().Contain(Permissions.WishlistsWrite);
        Permissions.Groups.Wishlists.Should().Contain(Permissions.WishlistsManage);
    }

    [Fact]
    public void Groups_Reports_ShouldContainAllReportPermissions()
    {
        // Assert
        Permissions.Groups.Reports.Should().HaveCount(1);
        Permissions.Groups.Reports.Should().Contain(Permissions.ReportsRead);
    }

    [Fact]
    public void Groups_Features_ShouldContainAllFeaturePermissions()
    {
        // Assert
        Permissions.Groups.Features.Should().HaveCount(2);
        Permissions.Groups.Features.Should().Contain(Permissions.FeaturesRead);
        Permissions.Groups.Features.Should().Contain(Permissions.FeaturesUpdate);
    }

    [Fact]
    public void Groups_Webhooks_ShouldContainAllWebhookPermissions()
    {
        // Assert
        Permissions.Groups.Webhooks.Should().HaveCount(3);
        Permissions.Groups.Webhooks.Should().Contain(Permissions.WebhooksRead);
        Permissions.Groups.Webhooks.Should().Contain(Permissions.WebhooksManage);
        Permissions.Groups.Webhooks.Should().Contain(Permissions.WebhooksTest);
    }

    [Fact]
    public void Groups_Search_ShouldContainAllSearchPermissions()
    {
        // Assert
        Permissions.Groups.Search.Should().HaveCount(1);
        Permissions.Groups.Search.Should().Contain(Permissions.SearchGlobal);
    }

    [Fact]
    public void Groups_HrEmployees_ShouldContainAllHrEmployeePermissions()
    {
        // Assert
        Permissions.Groups.HrEmployees.Should().HaveCount(4);
        Permissions.Groups.HrEmployees.Should().Contain(Permissions.HrEmployeesRead);
        Permissions.Groups.HrEmployees.Should().Contain(Permissions.HrEmployeesCreate);
        Permissions.Groups.HrEmployees.Should().Contain(Permissions.HrEmployeesUpdate);
        Permissions.Groups.HrEmployees.Should().Contain(Permissions.HrEmployeesDelete);
    }

    [Fact]
    public void Groups_HrDepartments_ShouldContainAllHrDepartmentPermissions()
    {
        // Assert
        Permissions.Groups.HrDepartments.Should().HaveCount(4);
        Permissions.Groups.HrDepartments.Should().Contain(Permissions.HrDepartmentsRead);
        Permissions.Groups.HrDepartments.Should().Contain(Permissions.HrDepartmentsCreate);
        Permissions.Groups.HrDepartments.Should().Contain(Permissions.HrDepartmentsUpdate);
        Permissions.Groups.HrDepartments.Should().Contain(Permissions.HrDepartmentsDelete);
    }

    [Fact]
    public void Groups_HrTags_ShouldContainAllHrTagPermissions()
    {
        // Assert
        Permissions.Groups.HrTags.Should().HaveCount(2);
        Permissions.Groups.HrTags.Should().Contain(Permissions.HrTagsRead);
        Permissions.Groups.HrTags.Should().Contain(Permissions.HrTagsManage);
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
            + Permissions.Groups.SystemPermissions.Count
            + Permissions.Groups.Audit.Count
            + Permissions.Groups.EmailTemplates.Count
            + Permissions.Groups.LegalPages.Count
            + Permissions.Groups.TenantSettings.Count
            + Permissions.Groups.PlatformSettings.Count
            + Permissions.Groups.BlogPosts.Count
            + Permissions.Groups.BlogCategories.Count
            + Permissions.Groups.BlogTags.Count
            + Permissions.Groups.Products.Count
            + Permissions.Groups.ProductCategories.Count
            + Permissions.Groups.Brands.Count
            + Permissions.Groups.Attributes.Count
            + Permissions.Groups.Media.Count
            + Permissions.Groups.Reviews.Count
            + Permissions.Groups.Orders.Count
            + Permissions.Groups.Promotions.Count
            + Permissions.Groups.Inventory.Count
            + Permissions.Groups.Wishlists.Count
            + Permissions.Groups.Reports.Count
            + Permissions.Groups.Payments.Count
            + Permissions.Groups.Customers.Count
            + Permissions.Groups.CustomerGroups.Count
            + Permissions.Groups.Features.Count
            + Permissions.Groups.Webhooks.Count
            + Permissions.Groups.Dashboard.Count
            + Permissions.Groups.Search.Count
            + Permissions.Groups.HrEmployees.Count
            + Permissions.Groups.HrDepartments.Count
            + Permissions.Groups.HrTags.Count
            + Permissions.Groups.CrmContacts.Count
            + Permissions.Groups.CrmCompanies.Count
            + Permissions.Groups.CrmLeads.Count
            + Permissions.Groups.CrmPipeline.Count
            + Permissions.Groups.CrmActivities.Count
            + Permissions.Groups.PmProjects.Count
            + Permissions.Groups.PmTasks.Count
            + Permissions.Groups.PmMembers.Count
            + Permissions.Groups.ApiKeys.Count;

        // Assert
        Permissions.All.Should().HaveCount(expectedCount);
    }

    [Fact]
    public void All_ShouldContainNoDuplicates()
    {
        // Each permission must appear exactly once so sortOrder is deterministic
        Permissions.All.Should().OnlyHaveUniqueItems();
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
        foreach (var permission in Permissions.Groups.SystemPermissions)
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

    [Fact]
    public void All_ShouldContainAllCustomerPermissions()
    {
        // Assert
        foreach (var permission in Permissions.Groups.Customers)
        {
            Permissions.All.Should().Contain(permission);
        }
    }

    [Fact]
    public void All_ShouldContainAllWishlistPermissions()
    {
        // Assert
        foreach (var permission in Permissions.Groups.Wishlists)
        {
            Permissions.All.Should().Contain(permission);
        }
    }

    [Fact]
    public void All_ShouldContainAllReportPermissions()
    {
        // Assert
        foreach (var permission in Permissions.Groups.Reports)
        {
            Permissions.All.Should().Contain(permission);
        }
    }

    [Fact]
    public void All_ShouldContainAllSearchPermissions()
    {
        // Assert
        foreach (var permission in Permissions.Groups.Search)
        {
            Permissions.All.Should().Contain(permission);
        }
    }

    [Fact]
    public void All_ShouldContainAllHrEmployeePermissions()
    {
        // Assert
        foreach (var permission in Permissions.Groups.HrEmployees)
        {
            Permissions.All.Should().Contain(permission);
        }
    }

    [Fact]
    public void All_ShouldContainAllHrDepartmentPermissions()
    {
        // Assert
        foreach (var permission in Permissions.Groups.HrDepartments)
        {
            Permissions.All.Should().Contain(permission);
        }
    }

    [Fact]
    public void All_ShouldContainAllHrTagPermissions()
    {
        // Assert
        foreach (var permission in Permissions.Groups.HrTags)
        {
            Permissions.All.Should().Contain(permission);
        }
    }

    #endregion

    #region Default Permissions Tests

    [Fact]
    public void PlatformAdminDefaults_ShouldContainPlatformLevelPermissions()
    {
        // Platform Admin has system-level permissions for managing tenants and platform settings
        Permissions.PlatformAdminDefaults.Should().Contain(Permissions.TenantsRead);
        Permissions.PlatformAdminDefaults.Should().Contain(Permissions.TenantsCreate);
        Permissions.PlatformAdminDefaults.Should().Contain(Permissions.TenantsUpdate);
        Permissions.PlatformAdminDefaults.Should().Contain(Permissions.TenantsDelete);
        Permissions.PlatformAdminDefaults.Should().Contain(Permissions.SystemAdmin);
        Permissions.PlatformAdminDefaults.Should().Contain(Permissions.SystemAuditLogs);
        Permissions.PlatformAdminDefaults.Should().Contain(Permissions.SystemSettings);
        Permissions.PlatformAdminDefaults.Should().Contain(Permissions.HangfireDashboard);
    }

    [Fact]
    public void AdminDefaults_ShouldContainTenantLevelPermissions()
    {
        // Tenant Admin has within-tenant permissions (user management, roles, blog)
        // but NOT system-level permissions (tenants, system)
        Permissions.AdminDefaults.Should().Contain(Permissions.UsersRead);
        Permissions.AdminDefaults.Should().Contain(Permissions.UsersCreate);
        Permissions.AdminDefaults.Should().Contain(Permissions.UsersUpdate);
        Permissions.AdminDefaults.Should().Contain(Permissions.UsersDelete);
        Permissions.AdminDefaults.Should().Contain(Permissions.RolesRead);
        Permissions.AdminDefaults.Should().Contain(Permissions.RolesCreate);
        Permissions.AdminDefaults.Should().Contain(Permissions.BlogPostsRead);

        // Admin should NOT have platform-level permissions
        Permissions.AdminDefaults.Should().NotContain(Permissions.TenantsRead);
        Permissions.AdminDefaults.Should().NotContain(Permissions.TenantsCreate);
        Permissions.AdminDefaults.Should().NotContain(Permissions.SystemAdmin);
        Permissions.AdminDefaults.Should().NotContain(Permissions.HangfireDashboard);
    }

    [Fact]
    public void UserDefaults_ShouldOnlyContainUsersRead()
    {
        // Assert
        Permissions.UserDefaults.Should().HaveCount(1);
        Permissions.UserDefaults.Should().Contain(Permissions.UsersRead);
    }

    [Fact]
    public void PlatformAdmin_And_Admin_ShouldHaveDistinctPermissions()
    {
        // Verify the separation of platform-level vs tenant-level permissions
        var platformOnlyPermissions = Permissions.Scopes.SystemOnly;
        var tenantAllowedPermissions = Permissions.Scopes.TenantAllowed;

        // Platform admin defaults should include system-only permissions
        foreach (var permission in platformOnlyPermissions.Intersect(Permissions.PlatformAdminDefaults))
        {
            Permissions.AdminDefaults.Should().NotContain(permission,
                because: $"tenant admin should not have system-only permission '{permission}'");
        }
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
