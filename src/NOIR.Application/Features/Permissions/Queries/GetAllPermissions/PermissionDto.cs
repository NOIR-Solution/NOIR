using DomainPermissions = NOIR.Domain.Common.Permissions;

namespace NOIR.Application.Features.Permissions.Queries.GetAllPermissions;

/// <summary>
/// DTO for permission information with display and scope metadata.
/// </summary>
public sealed record PermissionDto(
    string Id,
    string Resource,
    string Action,
    string? Scope,
    string DisplayName,
    string? Description,
    string? Category,
    bool IsSystem,
    int SortOrder,
    string Name,
    bool IsTenantAllowed);

/// <summary>
/// Static factory for creating PermissionDto from permission constants.
/// </summary>
public static class PermissionDtoFactory
{
    // Category names match sidebar menu sections for consistent UX.
    // Order: Marketing → Orders → Customers → Catalog → Content → Users & Access → Tenant Management → Settings → System → Platform
    private static readonly Dictionary<string, (string DisplayName, string Description, string Category)> _metadata = new()
    {
        // ── Marketing ──────────────────────────────────────────────────
        [DomainPermissions.ReportsRead] = ("View Reports", "View business reports and analytics", "Marketing"),

        [DomainPermissions.PromotionsRead] = ("View Promotions", "View promotions and discounts", "Marketing"),
        [DomainPermissions.PromotionsWrite] = ("Create/Edit Promotions", "Create and edit promotions", "Marketing"),
        [DomainPermissions.PromotionsDelete] = ("Delete Promotions", "Delete promotions", "Marketing"),
        [DomainPermissions.PromotionsManage] = ("Manage Promotions", "Full promotion management access", "Marketing"),

        // ── Orders ─────────────────────────────────────────────────────
        [DomainPermissions.OrdersRead] = ("View Orders", "View order details", "Orders"),
        [DomainPermissions.OrdersWrite] = ("Create Orders", "Create new orders", "Orders"),
        [DomainPermissions.OrdersManage] = ("Manage Orders", "Full order management access", "Orders"),

        [DomainPermissions.PaymentsRead] = ("View Payments", "View payment transactions", "Orders"),
        [DomainPermissions.PaymentsCreate] = ("Create Payments", "Process new payments", "Orders"),
        [DomainPermissions.PaymentsManage] = ("Manage Payments", "Full payment management", "Orders"),
        [DomainPermissions.PaymentGatewaysRead] = ("View Payment Gateways", "View gateway configurations", "Orders"),
        [DomainPermissions.PaymentGatewaysManage] = ("Manage Payment Gateways", "Configure payment gateways", "Orders"),
        [DomainPermissions.PaymentRefundsRead] = ("View Refunds", "View refund requests", "Orders"),
        [DomainPermissions.PaymentRefundsManage] = ("Manage Refunds", "Process refund requests", "Orders"),
        [DomainPermissions.PaymentWebhooksRead] = ("View Payment Webhooks", "View webhook logs", "Orders"),

        [DomainPermissions.InventoryRead] = ("View Inventory", "View stock levels and receipts", "Orders"),
        [DomainPermissions.InventoryWrite] = ("Adjust Inventory", "Create stock adjustments", "Orders"),
        [DomainPermissions.InventoryManage] = ("Manage Inventory", "Full inventory management access", "Orders"),

        // ── Customers ──────────────────────────────────────────────────
        [DomainPermissions.CustomersRead] = ("View Customers", "View customer profiles", "Customers"),
        [DomainPermissions.CustomersCreate] = ("Create Customers", "Create new customer accounts", "Customers"),
        [DomainPermissions.CustomersUpdate] = ("Update Customers", "Edit customer information", "Customers"),
        [DomainPermissions.CustomersDelete] = ("Delete Customers", "Delete customer accounts", "Customers"),
        [DomainPermissions.CustomersManage] = ("Manage Customers", "Full customer management access", "Customers"),

        [DomainPermissions.CustomerGroupsRead] = ("View Customer Groups", "View customer group configurations", "Customers"),
        [DomainPermissions.CustomerGroupsCreate] = ("Create Customer Groups", "Create new customer groups", "Customers"),
        [DomainPermissions.CustomerGroupsUpdate] = ("Update Customer Groups", "Edit customer group settings", "Customers"),
        [DomainPermissions.CustomerGroupsDelete] = ("Delete Customer Groups", "Delete customer groups", "Customers"),
        [DomainPermissions.CustomerGroupsManageMembers] = ("Manage Group Members", "Add or remove members from customer groups", "Customers"),

        [DomainPermissions.ReviewsRead] = ("View Reviews", "View product reviews", "Customers"),
        [DomainPermissions.ReviewsWrite] = ("Write Reviews", "Submit product reviews", "Customers"),
        [DomainPermissions.ReviewsManage] = ("Manage Reviews", "Moderate and manage reviews", "Customers"),

        [DomainPermissions.WishlistsRead] = ("View Wishlists", "View customer wishlists", "Customers"),
        [DomainPermissions.WishlistsWrite] = ("Edit Wishlists", "Edit wishlist items", "Customers"),
        [DomainPermissions.WishlistsManage] = ("Manage Wishlists", "Full wishlist management access", "Customers"),

        // ── Catalog ────────────────────────────────────────────────────
        [DomainPermissions.ProductsRead] = ("View Products", "View product catalog", "Catalog"),
        [DomainPermissions.ProductsCreate] = ("Create Products", "Create new products", "Catalog"),
        [DomainPermissions.ProductsUpdate] = ("Update Products", "Edit product details", "Catalog"),
        [DomainPermissions.ProductsDelete] = ("Delete Products", "Delete products", "Catalog"),
        [DomainPermissions.ProductsPublish] = ("Publish Products", "Publish or unpublish products", "Catalog"),

        [DomainPermissions.ProductCategoriesRead] = ("View Product Categories", "View product categories", "Catalog"),
        [DomainPermissions.ProductCategoriesCreate] = ("Create Product Categories", "Create new product categories", "Catalog"),
        [DomainPermissions.ProductCategoriesUpdate] = ("Update Product Categories", "Edit product categories", "Catalog"),
        [DomainPermissions.ProductCategoriesDelete] = ("Delete Product Categories", "Delete product categories", "Catalog"),

        [DomainPermissions.BrandsRead] = ("View Brands", "View product brands", "Catalog"),
        [DomainPermissions.BrandsCreate] = ("Create Brands", "Create new brands", "Catalog"),
        [DomainPermissions.BrandsUpdate] = ("Update Brands", "Edit brand details", "Catalog"),
        [DomainPermissions.BrandsDelete] = ("Delete Brands", "Delete brands", "Catalog"),

        [DomainPermissions.AttributesRead] = ("View Attributes", "View product attributes", "Catalog"),
        [DomainPermissions.AttributesCreate] = ("Create Attributes", "Create new product attributes", "Catalog"),
        [DomainPermissions.AttributesUpdate] = ("Update Attributes", "Edit product attributes", "Catalog"),
        [DomainPermissions.AttributesDelete] = ("Delete Attributes", "Delete product attributes", "Catalog"),

        [DomainPermissions.MediaRead] = ("View Media", "View media files and library", "Catalog"),
        [DomainPermissions.MediaCreate] = ("Upload Media", "Upload new media files", "Catalog"),
        [DomainPermissions.MediaUpdate] = ("Edit Media", "Rename and edit media files", "Catalog"),
        [DomainPermissions.MediaDelete] = ("Delete Media", "Delete media files", "Catalog"),
        [DomainPermissions.MediaManage] = ("Manage Media", "Full media library access", "Catalog"),

        // ── Content ────────────────────────────────────────────────────
        [DomainPermissions.BlogPostsRead] = ("View Blog Posts", "View blog posts and drafts", "Content"),
        [DomainPermissions.BlogPostsCreate] = ("Create Blog Posts", "Create new blog posts", "Content"),
        [DomainPermissions.BlogPostsUpdate] = ("Update Blog Posts", "Edit existing blog posts", "Content"),
        [DomainPermissions.BlogPostsDelete] = ("Delete Blog Posts", "Delete blog posts", "Content"),
        [DomainPermissions.BlogPostsPublish] = ("Publish Blog Posts", "Publish or unpublish blog posts", "Content"),

        [DomainPermissions.BlogCategoriesRead] = ("View Blog Categories", "View blog categories", "Content"),
        [DomainPermissions.BlogCategoriesCreate] = ("Create Blog Categories", "Create new blog categories", "Content"),
        [DomainPermissions.BlogCategoriesUpdate] = ("Update Blog Categories", "Edit blog categories", "Content"),
        [DomainPermissions.BlogCategoriesDelete] = ("Delete Blog Categories", "Delete blog categories", "Content"),

        [DomainPermissions.BlogTagsRead] = ("View Blog Tags", "View blog tags", "Content"),
        [DomainPermissions.BlogTagsCreate] = ("Create Blog Tags", "Create new blog tags", "Content"),
        [DomainPermissions.BlogTagsUpdate] = ("Update Blog Tags", "Edit blog tags", "Content"),
        [DomainPermissions.BlogTagsDelete] = ("Delete Blog Tags", "Delete blog tags", "Content"),

        // ── Users & Access ─────────────────────────────────────────────
        [DomainPermissions.UsersRead] = ("View Users", "View user profiles and list users", "Users & Access"),
        [DomainPermissions.UsersCreate] = ("Create Users", "Create new user accounts", "Users & Access"),
        [DomainPermissions.UsersUpdate] = ("Update Users", "Modify user information and settings", "Users & Access"),
        [DomainPermissions.UsersDelete] = ("Delete Users", "Delete user accounts", "Users & Access"),
        [DomainPermissions.UsersManageRoles] = ("Manage User Roles", "Assign and remove roles from users", "Users & Access"),

        [DomainPermissions.RolesRead] = ("View Roles", "View role configurations and permissions", "Users & Access"),
        [DomainPermissions.RolesCreate] = ("Create Roles", "Create new roles", "Users & Access"),
        [DomainPermissions.RolesUpdate] = ("Update Roles", "Modify role settings", "Users & Access"),
        [DomainPermissions.RolesDelete] = ("Delete Roles", "Delete roles from the system", "Users & Access"),
        [DomainPermissions.RolesManagePermissions] = ("Manage Role Permissions", "Assign permissions to roles", "Users & Access"),

        // ── Tenant Management (platform-only) ──────────────────────────
        [DomainPermissions.TenantsRead] = ("View Tenants", "View tenant information", "Tenant Management"),
        [DomainPermissions.TenantsCreate] = ("Create Tenants", "Create new tenants", "Tenant Management"),
        [DomainPermissions.TenantsUpdate] = ("Update Tenants", "Modify tenant settings", "Tenant Management"),
        [DomainPermissions.TenantsDelete] = ("Delete Tenants", "Delete tenants", "Tenant Management"),

        // ── Settings ───────────────────────────────────────────────────
        [DomainPermissions.TenantSettingsRead] = ("View Tenant Settings", "View organization settings", "Settings"),
        [DomainPermissions.TenantSettingsUpdate] = ("Update Tenant Settings", "Modify organization settings", "Settings"),

        [DomainPermissions.FeaturesRead] = ("View Features", "View feature module configurations", "Settings"),
        [DomainPermissions.FeaturesUpdate] = ("Update Features", "Enable or disable feature modules", "Settings"),

        [DomainPermissions.EmailTemplatesRead] = ("View Email Templates", "View email template content", "Settings"),
        [DomainPermissions.EmailTemplatesUpdate] = ("Update Email Templates", "Modify email templates", "Settings"),

        [DomainPermissions.LegalPagesRead] = ("View Legal Pages", "View legal documents", "Settings"),
        [DomainPermissions.LegalPagesUpdate] = ("Update Legal Pages", "Edit legal documents", "Settings"),

        // ── System ─────────────────────────────────────────────────────
        [DomainPermissions.SystemAdmin] = ("System Administration", "Full system administration access", "System"),
        [DomainPermissions.SystemAuditLogs] = ("View Audit Logs", "Access system audit logs", "System"),
        [DomainPermissions.SystemSettings] = ("Manage Settings", "Configure system settings", "System"),
        [DomainPermissions.HangfireDashboard] = ("Hangfire Dashboard", "Access background job dashboard", "System"),

        [DomainPermissions.AuditRead] = ("View Audit Records", "View audit trail entries", "System"),
        [DomainPermissions.AuditExport] = ("Export Audit Data", "Export audit records", "System"),
        [DomainPermissions.AuditEntityHistory] = ("View Entity History", "View change history for entities", "System"),
        [DomainPermissions.AuditPolicyRead] = ("View Audit Policies", "View audit policy configurations", "System"),
        [DomainPermissions.AuditPolicyWrite] = ("Manage Audit Policies", "Create and modify audit policies", "System"),
        [DomainPermissions.AuditPolicyDelete] = ("Delete Audit Policies", "Remove audit policies", "System"),
        [DomainPermissions.AuditStream] = ("Stream Audit Events", "Access real-time audit stream", "System"),

        // ── Platform (platform-only) ───────────────────────────────────
        [DomainPermissions.PlatformSettingsRead] = ("View Platform Settings", "View platform configuration", "Platform"),
        [DomainPermissions.PlatformSettingsManage] = ("Manage Platform Settings", "Configure platform settings", "Platform"),

        // ── Search ───────────────────────────────────────────────────────
        [DomainPermissions.SearchGlobal] = ("Global Search", "Search across all entities", "System"),

        // ── Dashboard ────────────────────────────────────────────────────
        [DomainPermissions.DashboardRead] = ("View Dashboard", "View dashboard and widgets", "System"),

        // ── Human Resources ──────────────────────────────────────────────
        [DomainPermissions.HrEmployeesRead] = ("View Employees", "View employee profiles", "Human Resources"),
        [DomainPermissions.HrEmployeesCreate] = ("Create Employees", "Create new employees", "Human Resources"),
        [DomainPermissions.HrEmployeesUpdate] = ("Update Employees", "Edit employee information", "Human Resources"),
        [DomainPermissions.HrEmployeesDelete] = ("Delete Employees", "Delete employee records", "Human Resources"),
        [DomainPermissions.HrDepartmentsRead] = ("View Departments", "View department structure", "Human Resources"),
        [DomainPermissions.HrDepartmentsCreate] = ("Create Departments", "Create new departments", "Human Resources"),
        [DomainPermissions.HrDepartmentsUpdate] = ("Update Departments", "Edit department information", "Human Resources"),
        [DomainPermissions.HrDepartmentsDelete] = ("Delete Departments", "Delete departments", "Human Resources"),
        [DomainPermissions.HrTagsRead] = ("View Employee Tags", "View employee tag categories", "Human Resources"),
        [DomainPermissions.HrTagsManage] = ("Manage Employee Tags", "Create and manage employee tags", "Human Resources"),

        // ── CRM ──────────────────────────────────────────────────────────
        [DomainPermissions.CrmContactsRead] = ("View Contacts", "View CRM contacts", "CRM"),
        [DomainPermissions.CrmContactsCreate] = ("Create Contacts", "Create new contacts", "CRM"),
        [DomainPermissions.CrmContactsUpdate] = ("Update Contacts", "Edit contact information", "CRM"),
        [DomainPermissions.CrmContactsDelete] = ("Delete Contacts", "Delete contacts", "CRM"),
        [DomainPermissions.CrmCompaniesRead] = ("View Companies", "View CRM companies", "CRM"),
        [DomainPermissions.CrmCompaniesCreate] = ("Create Companies", "Create new companies", "CRM"),
        [DomainPermissions.CrmCompaniesUpdate] = ("Update Companies", "Edit company information", "CRM"),
        [DomainPermissions.CrmCompaniesDelete] = ("Delete Companies", "Delete companies", "CRM"),
        [DomainPermissions.CrmLeadsRead] = ("View Leads", "View sales leads", "CRM"),
        [DomainPermissions.CrmLeadsCreate] = ("Create Leads", "Create new leads", "CRM"),
        [DomainPermissions.CrmLeadsUpdate] = ("Update Leads", "Edit lead information", "CRM"),
        [DomainPermissions.CrmLeadsManage] = ("Manage Leads", "Full lead management including pipeline", "CRM"),
        [DomainPermissions.CrmPipelineManage] = ("Manage Pipeline", "Configure sales pipeline stages", "CRM"),
        [DomainPermissions.CrmActivitiesRead] = ("View Activities", "View CRM activities", "CRM"),
        [DomainPermissions.CrmActivitiesCreate] = ("Create Activities", "Log new activities", "CRM"),
        [DomainPermissions.CrmActivitiesUpdate] = ("Update Activities", "Edit activity records", "CRM"),
        [DomainPermissions.CrmActivitiesDelete] = ("Delete Activities", "Delete activity records", "CRM"),

        // ── Project Management ───────────────────────────────────────────
        [DomainPermissions.PmProjectsRead] = ("View Projects", "View project details", "Project Management"),
        [DomainPermissions.PmProjectsCreate] = ("Create Projects", "Create new projects", "Project Management"),
        [DomainPermissions.PmProjectsUpdate] = ("Update Projects", "Edit project settings", "Project Management"),
        [DomainPermissions.PmProjectsDelete] = ("Delete Projects", "Delete projects", "Project Management"),
        [DomainPermissions.PmTasksRead] = ("View Tasks", "View project tasks", "Project Management"),
        [DomainPermissions.PmTasksCreate] = ("Create Tasks", "Create new tasks", "Project Management"),
        [DomainPermissions.PmTasksUpdate] = ("Update Tasks", "Edit task details", "Project Management"),
        [DomainPermissions.PmTasksDelete] = ("Delete Tasks", "Delete tasks", "Project Management"),
        [DomainPermissions.PmTasksManage] = ("Manage Tasks", "Full task management access", "Project Management"),
        [DomainPermissions.PmMembersManage] = ("Manage Members", "Add and remove project members", "Project Management"),

        // ── Webhooks ─────────────────────────────────────────────────────
        [DomainPermissions.WebhooksRead] = ("View Webhooks", "View webhook subscriptions", "Settings"),
        [DomainPermissions.WebhooksManage] = ("Manage Webhooks", "Create and manage webhook subscriptions", "Settings"),
        [DomainPermissions.WebhooksTest] = ("Test Webhooks", "Send test webhook events", "Settings"),

        // ── API Keys ─────────────────────────────────────────────────────
        [DomainPermissions.ApiKeysRead] = ("View API Keys", "View API key list and details", "Settings"),
        [DomainPermissions.ApiKeysCreate] = ("Create API Keys", "Create new API keys", "Settings"),
        [DomainPermissions.ApiKeysDelete] = ("Revoke API Keys", "Revoke and delete API keys", "Settings"),
    };

    /// <summary>
    /// Creates a list of all permissions with metadata.
    /// </summary>
    public static IReadOnlyList<PermissionDto> GetAllPermissions()
    {
        var permissions = new List<PermissionDto>();
        var sortOrder = 0;

        foreach (var permissionName in DomainPermissions.All)
        {
            var parts = permissionName.Split(':');
            var resource = parts.Length > 0 ? parts[0] : permissionName;
            var action = parts.Length > 1 ? parts[1] : "access";
            var scope = parts.Length > 2 ? parts[2] : null;

            var (displayName, description, category) = _metadata.TryGetValue(permissionName, out var meta)
                ? meta
                : (permissionName, null, "Uncategorized")!;

            var isTenantAllowed = DomainPermissions.Scopes.IsTenantAllowed(permissionName);

            permissions.Add(new PermissionDto(
                Id: permissionName, // Use name as ID for static permissions
                Resource: resource,
                Action: action,
                Scope: scope,
                DisplayName: displayName,
                Description: description,
                Category: category,
                IsSystem: true,
                SortOrder: sortOrder++,
                Name: permissionName,
                IsTenantAllowed: isTenantAllowed
            ));
        }

        return permissions;
    }
}
