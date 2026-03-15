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
        Permissions.ClaimType.ShouldBe("permission");
    }

    #endregion

    #region User Permissions Tests

    [Fact]
    public void UsersRead_ShouldHaveCorrectValue()
    {
        // Assert
        Permissions.UsersRead.ShouldBe("users:read");
    }

    [Fact]
    public void UsersCreate_ShouldHaveCorrectValue()
    {
        // Assert
        Permissions.UsersCreate.ShouldBe("users:create");
    }

    [Fact]
    public void UsersUpdate_ShouldHaveCorrectValue()
    {
        // Assert
        Permissions.UsersUpdate.ShouldBe("users:update");
    }

    [Fact]
    public void UsersDelete_ShouldHaveCorrectValue()
    {
        // Assert
        Permissions.UsersDelete.ShouldBe("users:delete");
    }

    [Fact]
    public void UsersManageRoles_ShouldHaveCorrectValue()
    {
        // Assert
        Permissions.UsersManageRoles.ShouldBe("users:manage-roles");
    }

    #endregion

    #region Role Permissions Tests

    [Fact]
    public void RolesRead_ShouldHaveCorrectValue()
    {
        // Assert
        Permissions.RolesRead.ShouldBe("roles:read");
    }

    [Fact]
    public void RolesCreate_ShouldHaveCorrectValue()
    {
        // Assert
        Permissions.RolesCreate.ShouldBe("roles:create");
    }

    [Fact]
    public void RolesUpdate_ShouldHaveCorrectValue()
    {
        // Assert
        Permissions.RolesUpdate.ShouldBe("roles:update");
    }

    [Fact]
    public void RolesDelete_ShouldHaveCorrectValue()
    {
        // Assert
        Permissions.RolesDelete.ShouldBe("roles:delete");
    }

    [Fact]
    public void RolesManagePermissions_ShouldHaveCorrectValue()
    {
        // Assert
        Permissions.RolesManagePermissions.ShouldBe("roles:manage-permissions");
    }

    #endregion

    #region Tenant Permissions Tests

    [Fact]
    public void TenantsRead_ShouldHaveCorrectValue()
    {
        // Assert
        Permissions.TenantsRead.ShouldBe("tenants:read");
    }

    [Fact]
    public void TenantsCreate_ShouldHaveCorrectValue()
    {
        // Assert
        Permissions.TenantsCreate.ShouldBe("tenants:create");
    }

    [Fact]
    public void TenantsUpdate_ShouldHaveCorrectValue()
    {
        // Assert
        Permissions.TenantsUpdate.ShouldBe("tenants:update");
    }

    [Fact]
    public void TenantsDelete_ShouldHaveCorrectValue()
    {
        // Assert
        Permissions.TenantsDelete.ShouldBe("tenants:delete");
    }

    #endregion

    #region System Permissions Tests

    [Fact]
    public void SystemAdmin_ShouldHaveCorrectValue()
    {
        // Assert
        Permissions.SystemAdmin.ShouldBe("system:admin");
    }

    [Fact]
    public void SystemAuditLogs_ShouldHaveCorrectValue()
    {
        // Assert
        Permissions.SystemAuditLogs.ShouldBe("system:audit-logs");
    }

    [Fact]
    public void SystemSettings_ShouldHaveCorrectValue()
    {
        // Assert
        Permissions.SystemSettings.ShouldBe("system:settings");
    }

    [Fact]
    public void HangfireDashboard_ShouldHaveCorrectValue()
    {
        // Assert
        Permissions.HangfireDashboard.ShouldBe("system:hangfire");
    }

    #endregion

    #region Audit Permissions Tests

    [Fact]
    public void AuditRead_ShouldHaveCorrectValue()
    {
        // Assert
        Permissions.AuditRead.ShouldBe("audit:read");
    }

    [Fact]
    public void AuditExport_ShouldHaveCorrectValue()
    {
        // Assert
        Permissions.AuditExport.ShouldBe("audit:export");
    }

    [Fact]
    public void AuditEntityHistory_ShouldHaveCorrectValue()
    {
        // Assert
        Permissions.AuditEntityHistory.ShouldBe("audit:entity-history");
    }

    #endregion

    #region Groups Tests

    [Fact]
    public void Groups_Users_ShouldContainAllUserPermissions()
    {
        // Assert
        Permissions.Groups.Users.Count().ShouldBe(5);
        Permissions.Groups.Users.ShouldContain(Permissions.UsersRead);
        Permissions.Groups.Users.ShouldContain(Permissions.UsersCreate);
        Permissions.Groups.Users.ShouldContain(Permissions.UsersUpdate);
        Permissions.Groups.Users.ShouldContain(Permissions.UsersDelete);
        Permissions.Groups.Users.ShouldContain(Permissions.UsersManageRoles);
    }

    [Fact]
    public void Groups_Roles_ShouldContainAllRolePermissions()
    {
        // Assert
        Permissions.Groups.Roles.Count().ShouldBe(5);
        Permissions.Groups.Roles.ShouldContain(Permissions.RolesRead);
        Permissions.Groups.Roles.ShouldContain(Permissions.RolesCreate);
        Permissions.Groups.Roles.ShouldContain(Permissions.RolesUpdate);
        Permissions.Groups.Roles.ShouldContain(Permissions.RolesDelete);
        Permissions.Groups.Roles.ShouldContain(Permissions.RolesManagePermissions);
    }

    [Fact]
    public void Groups_Tenants_ShouldContainAllTenantPermissions()
    {
        // Assert
        Permissions.Groups.Tenants.Count().ShouldBe(4);
        Permissions.Groups.Tenants.ShouldContain(Permissions.TenantsRead);
        Permissions.Groups.Tenants.ShouldContain(Permissions.TenantsCreate);
        Permissions.Groups.Tenants.ShouldContain(Permissions.TenantsUpdate);
        Permissions.Groups.Tenants.ShouldContain(Permissions.TenantsDelete);
    }

    [Fact]
    public void Groups_System_ShouldContainAllSystemPermissions()
    {
        // Assert
        Permissions.Groups.SystemPermissions.Count().ShouldBe(4);
        Permissions.Groups.SystemPermissions.ShouldContain(Permissions.SystemAdmin);
        Permissions.Groups.SystemPermissions.ShouldContain(Permissions.SystemAuditLogs);
        Permissions.Groups.SystemPermissions.ShouldContain(Permissions.SystemSettings);
        Permissions.Groups.SystemPermissions.ShouldContain(Permissions.HangfireDashboard);
    }

    [Fact]
    public void Groups_Audit_ShouldContainAllAuditPermissions()
    {
        // Assert
        Permissions.Groups.Audit.Count().ShouldBe(7);
        Permissions.Groups.Audit.ShouldContain(Permissions.AuditRead);
        Permissions.Groups.Audit.ShouldContain(Permissions.AuditExport);
        Permissions.Groups.Audit.ShouldContain(Permissions.AuditEntityHistory);
        Permissions.Groups.Audit.ShouldContain(Permissions.AuditPolicyRead);
        Permissions.Groups.Audit.ShouldContain(Permissions.AuditPolicyWrite);
        Permissions.Groups.Audit.ShouldContain(Permissions.AuditPolicyDelete);
        Permissions.Groups.Audit.ShouldContain(Permissions.AuditStream);
    }

    [Fact]
    public void Groups_Orders_ShouldContainAllOrderPermissions()
    {
        // Assert
        Permissions.Groups.Orders.Count().ShouldBe(3);
        Permissions.Groups.Orders.ShouldContain(Permissions.OrdersRead);
        Permissions.Groups.Orders.ShouldContain(Permissions.OrdersWrite);
        Permissions.Groups.Orders.ShouldContain(Permissions.OrdersManage);
    }

    [Fact]
    public void Groups_Customers_ShouldContainAllCustomerPermissions()
    {
        // Assert
        Permissions.Groups.Customers.Count().ShouldBe(5);
        Permissions.Groups.Customers.ShouldContain(Permissions.CustomersRead);
        Permissions.Groups.Customers.ShouldContain(Permissions.CustomersCreate);
        Permissions.Groups.Customers.ShouldContain(Permissions.CustomersUpdate);
        Permissions.Groups.Customers.ShouldContain(Permissions.CustomersDelete);
        Permissions.Groups.Customers.ShouldContain(Permissions.CustomersManage);
    }

    [Fact]
    public void Groups_Wishlists_ShouldContainAllWishlistPermissions()
    {
        // Assert
        Permissions.Groups.Wishlists.Count().ShouldBe(3);
        Permissions.Groups.Wishlists.ShouldContain(Permissions.WishlistsRead);
        Permissions.Groups.Wishlists.ShouldContain(Permissions.WishlistsWrite);
        Permissions.Groups.Wishlists.ShouldContain(Permissions.WishlistsManage);
    }

    [Fact]
    public void Groups_Reports_ShouldContainAllReportPermissions()
    {
        // Assert
        Permissions.Groups.Reports.Count().ShouldBe(1);
        Permissions.Groups.Reports.ShouldContain(Permissions.ReportsRead);
    }

    [Fact]
    public void Groups_Features_ShouldContainAllFeaturePermissions()
    {
        // Assert
        Permissions.Groups.Features.Count().ShouldBe(2);
        Permissions.Groups.Features.ShouldContain(Permissions.FeaturesRead);
        Permissions.Groups.Features.ShouldContain(Permissions.FeaturesUpdate);
    }

    [Fact]
    public void Groups_Webhooks_ShouldContainAllWebhookPermissions()
    {
        // Assert
        Permissions.Groups.Webhooks.Count().ShouldBe(3);
        Permissions.Groups.Webhooks.ShouldContain(Permissions.WebhooksRead);
        Permissions.Groups.Webhooks.ShouldContain(Permissions.WebhooksManage);
        Permissions.Groups.Webhooks.ShouldContain(Permissions.WebhooksTest);
    }

    [Fact]
    public void Groups_Search_ShouldContainAllSearchPermissions()
    {
        // Assert
        Permissions.Groups.Search.Count().ShouldBe(1);
        Permissions.Groups.Search.ShouldContain(Permissions.SearchGlobal);
    }

    [Fact]
    public void Groups_HrEmployees_ShouldContainAllHrEmployeePermissions()
    {
        // Assert
        Permissions.Groups.HrEmployees.Count().ShouldBe(4);
        Permissions.Groups.HrEmployees.ShouldContain(Permissions.HrEmployeesRead);
        Permissions.Groups.HrEmployees.ShouldContain(Permissions.HrEmployeesCreate);
        Permissions.Groups.HrEmployees.ShouldContain(Permissions.HrEmployeesUpdate);
        Permissions.Groups.HrEmployees.ShouldContain(Permissions.HrEmployeesDelete);
    }

    [Fact]
    public void Groups_HrDepartments_ShouldContainAllHrDepartmentPermissions()
    {
        // Assert
        Permissions.Groups.HrDepartments.Count().ShouldBe(4);
        Permissions.Groups.HrDepartments.ShouldContain(Permissions.HrDepartmentsRead);
        Permissions.Groups.HrDepartments.ShouldContain(Permissions.HrDepartmentsCreate);
        Permissions.Groups.HrDepartments.ShouldContain(Permissions.HrDepartmentsUpdate);
        Permissions.Groups.HrDepartments.ShouldContain(Permissions.HrDepartmentsDelete);
    }

    [Fact]
    public void Groups_HrTags_ShouldContainAllHrTagPermissions()
    {
        // Assert
        Permissions.Groups.HrTags.Count().ShouldBe(2);
        Permissions.Groups.HrTags.ShouldContain(Permissions.HrTagsRead);
        Permissions.Groups.HrTags.ShouldContain(Permissions.HrTagsManage);
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
        Permissions.All.Count().ShouldBe(expectedCount);
    }

    [Fact]
    public void All_ShouldContainNoDuplicates()
    {
        // Each permission must appear exactly once so sortOrder is deterministic
        Permissions.All.ShouldBeUnique();
    }

    [Fact]
    public void All_ShouldContainAllUserPermissions()
    {
        // Assert
        foreach (var permission in Permissions.Groups.Users)
        {
            Permissions.All.ShouldContain(permission);
        }
    }

    [Fact]
    public void All_ShouldContainAllRolePermissions()
    {
        // Assert
        foreach (var permission in Permissions.Groups.Roles)
        {
            Permissions.All.ShouldContain(permission);
        }
    }

    [Fact]
    public void All_ShouldContainAllTenantPermissions()
    {
        // Assert
        foreach (var permission in Permissions.Groups.Tenants)
        {
            Permissions.All.ShouldContain(permission);
        }
    }

    [Fact]
    public void All_ShouldContainAllSystemPermissions()
    {
        // Assert
        foreach (var permission in Permissions.Groups.SystemPermissions)
        {
            Permissions.All.ShouldContain(permission);
        }
    }

    [Fact]
    public void All_ShouldContainAllAuditPermissions()
    {
        // Assert
        foreach (var permission in Permissions.Groups.Audit)
        {
            Permissions.All.ShouldContain(permission);
        }
    }

    [Fact]
    public void All_ShouldContainAllCustomerPermissions()
    {
        // Assert
        foreach (var permission in Permissions.Groups.Customers)
        {
            Permissions.All.ShouldContain(permission);
        }
    }

    [Fact]
    public void All_ShouldContainAllWishlistPermissions()
    {
        // Assert
        foreach (var permission in Permissions.Groups.Wishlists)
        {
            Permissions.All.ShouldContain(permission);
        }
    }

    [Fact]
    public void All_ShouldContainAllReportPermissions()
    {
        // Assert
        foreach (var permission in Permissions.Groups.Reports)
        {
            Permissions.All.ShouldContain(permission);
        }
    }

    [Fact]
    public void All_ShouldContainAllSearchPermissions()
    {
        // Assert
        foreach (var permission in Permissions.Groups.Search)
        {
            Permissions.All.ShouldContain(permission);
        }
    }

    [Fact]
    public void All_ShouldContainAllHrEmployeePermissions()
    {
        // Assert
        foreach (var permission in Permissions.Groups.HrEmployees)
        {
            Permissions.All.ShouldContain(permission);
        }
    }

    [Fact]
    public void All_ShouldContainAllHrDepartmentPermissions()
    {
        // Assert
        foreach (var permission in Permissions.Groups.HrDepartments)
        {
            Permissions.All.ShouldContain(permission);
        }
    }

    [Fact]
    public void All_ShouldContainAllHrTagPermissions()
    {
        // Assert
        foreach (var permission in Permissions.Groups.HrTags)
        {
            Permissions.All.ShouldContain(permission);
        }
    }

    #endregion

    #region Default Permissions Tests

    [Fact]
    public void PlatformAdminDefaults_ShouldContainPlatformLevelPermissions()
    {
        // Platform Admin has system-level permissions for managing tenants and platform settings
        Permissions.PlatformAdminDefaults.ShouldContain(Permissions.TenantsRead);
        Permissions.PlatformAdminDefaults.ShouldContain(Permissions.TenantsCreate);
        Permissions.PlatformAdminDefaults.ShouldContain(Permissions.TenantsUpdate);
        Permissions.PlatformAdminDefaults.ShouldContain(Permissions.TenantsDelete);
        Permissions.PlatformAdminDefaults.ShouldContain(Permissions.SystemAdmin);
        Permissions.PlatformAdminDefaults.ShouldContain(Permissions.SystemAuditLogs);
        Permissions.PlatformAdminDefaults.ShouldContain(Permissions.SystemSettings);
        Permissions.PlatformAdminDefaults.ShouldContain(Permissions.HangfireDashboard);
    }

    [Fact]
    public void AdminDefaults_ShouldContainTenantLevelPermissions()
    {
        // Tenant Admin has within-tenant permissions (user management, roles, blog)
        // but NOT system-level permissions (tenants, system)
        Permissions.AdminDefaults.ShouldContain(Permissions.UsersRead);
        Permissions.AdminDefaults.ShouldContain(Permissions.UsersCreate);
        Permissions.AdminDefaults.ShouldContain(Permissions.UsersUpdate);
        Permissions.AdminDefaults.ShouldContain(Permissions.UsersDelete);
        Permissions.AdminDefaults.ShouldContain(Permissions.RolesRead);
        Permissions.AdminDefaults.ShouldContain(Permissions.RolesCreate);
        Permissions.AdminDefaults.ShouldContain(Permissions.BlogPostsRead);

        // Admin should NOT have platform-level permissions
        Permissions.AdminDefaults.ShouldNotContain(Permissions.TenantsRead);
        Permissions.AdminDefaults.ShouldNotContain(Permissions.TenantsCreate);
        Permissions.AdminDefaults.ShouldNotContain(Permissions.SystemAdmin);
        Permissions.AdminDefaults.ShouldNotContain(Permissions.HangfireDashboard);
    }

    [Fact]
    public void UserDefaults_ShouldOnlyContainUsersRead()
    {
        // Assert
        Permissions.UserDefaults.Count().ShouldBe(1);
        Permissions.UserDefaults.ShouldContain(Permissions.UsersRead);
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
            Permissions.AdminDefaults.ShouldNotContain(permission);
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
            permission.ShouldContain(":");
            var parts = permission.Split(':');
            parts.Count().ShouldBe(2);
            parts[0].ShouldNotBeNullOrWhiteSpace("resource should not be empty");
            parts[1].ShouldNotBeNullOrWhiteSpace("action should not be empty");
        }
    }

    #endregion
}
