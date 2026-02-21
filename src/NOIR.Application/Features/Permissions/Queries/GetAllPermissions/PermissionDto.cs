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
    private static readonly Dictionary<string, (string DisplayName, string Description, string Category)> _metadata = new()
    {
        // Users
        [DomainPermissions.UsersRead] = ("View Users", "View user profiles and list users", "User Management"),
        [DomainPermissions.UsersCreate] = ("Create Users", "Create new user accounts", "User Management"),
        [DomainPermissions.UsersUpdate] = ("Update Users", "Modify user information and settings", "User Management"),
        [DomainPermissions.UsersDelete] = ("Delete Users", "Delete user accounts", "User Management"),
        [DomainPermissions.UsersManageRoles] = ("Manage User Roles", "Assign and remove roles from users", "User Management"),

        // Roles
        [DomainPermissions.RolesRead] = ("View Roles", "View role configurations and permissions", "Role Management"),
        [DomainPermissions.RolesCreate] = ("Create Roles", "Create new roles", "Role Management"),
        [DomainPermissions.RolesUpdate] = ("Update Roles", "Modify role settings", "Role Management"),
        [DomainPermissions.RolesDelete] = ("Delete Roles", "Delete roles from the system", "Role Management"),
        [DomainPermissions.RolesManagePermissions] = ("Manage Role Permissions", "Assign permissions to roles", "Role Management"),

        // Tenants
        [DomainPermissions.TenantsRead] = ("View Tenants", "View tenant information", "Tenant Management"),
        [DomainPermissions.TenantsCreate] = ("Create Tenants", "Create new tenants", "Tenant Management"),
        [DomainPermissions.TenantsUpdate] = ("Update Tenants", "Modify tenant settings", "Tenant Management"),
        [DomainPermissions.TenantsDelete] = ("Delete Tenants", "Delete tenants", "Tenant Management"),

        // System
        [DomainPermissions.SystemAdmin] = ("System Administration", "Full system administration access", "System"),
        [DomainPermissions.SystemAuditLogs] = ("View Audit Logs", "Access system audit logs", "System"),
        [DomainPermissions.SystemSettings] = ("Manage Settings", "Configure system settings", "System"),
        [DomainPermissions.HangfireDashboard] = ("Hangfire Dashboard", "Access background job dashboard", "System"),

        // Audit
        [DomainPermissions.AuditRead] = ("View Audit Records", "View audit trail entries", "Audit"),
        [DomainPermissions.AuditExport] = ("Export Audit Data", "Export audit records", "Audit"),
        [DomainPermissions.AuditEntityHistory] = ("View Entity History", "View change history for entities", "Audit"),
        [DomainPermissions.AuditPolicyRead] = ("View Audit Policies", "View audit policy configurations", "Audit"),
        [DomainPermissions.AuditPolicyWrite] = ("Manage Audit Policies", "Create and modify audit policies", "Audit"),
        [DomainPermissions.AuditPolicyDelete] = ("Delete Audit Policies", "Remove audit policies", "Audit"),
        [DomainPermissions.AuditStream] = ("Stream Audit Events", "Access real-time audit stream", "Audit"),

        // Email Templates
        [DomainPermissions.EmailTemplatesRead] = ("View Email Templates", "View email template content", "Email Templates"),
        [DomainPermissions.EmailTemplatesUpdate] = ("Update Email Templates", "Modify email templates", "Email Templates"),

        // Blog Posts
        [DomainPermissions.BlogPostsRead] = ("View Blog Posts", "View blog posts and drafts", "Blog"),
        [DomainPermissions.BlogPostsCreate] = ("Create Blog Posts", "Create new blog posts", "Blog"),
        [DomainPermissions.BlogPostsUpdate] = ("Update Blog Posts", "Edit existing blog posts", "Blog"),
        [DomainPermissions.BlogPostsDelete] = ("Delete Blog Posts", "Delete blog posts", "Blog"),
        [DomainPermissions.BlogPostsPublish] = ("Publish Blog Posts", "Publish or unpublish blog posts", "Blog"),

        // Blog Categories
        [DomainPermissions.BlogCategoriesRead] = ("View Blog Categories", "View blog categories", "Blog"),
        [DomainPermissions.BlogCategoriesCreate] = ("Create Blog Categories", "Create new blog categories", "Blog"),
        [DomainPermissions.BlogCategoriesUpdate] = ("Update Blog Categories", "Edit blog categories", "Blog"),
        [DomainPermissions.BlogCategoriesDelete] = ("Delete Blog Categories", "Delete blog categories", "Blog"),

        // Blog Tags
        [DomainPermissions.BlogTagsRead] = ("View Blog Tags", "View blog tags", "Blog"),
        [DomainPermissions.BlogTagsCreate] = ("Create Blog Tags", "Create new blog tags", "Blog"),
        [DomainPermissions.BlogTagsUpdate] = ("Update Blog Tags", "Edit blog tags", "Blog"),
        [DomainPermissions.BlogTagsDelete] = ("Delete Blog Tags", "Delete blog tags", "Blog"),

        // Products
        [DomainPermissions.ProductsRead] = ("View Products", "View product catalog", "E-commerce"),
        [DomainPermissions.ProductsCreate] = ("Create Products", "Create new products", "E-commerce"),
        [DomainPermissions.ProductsUpdate] = ("Update Products", "Edit product details", "E-commerce"),
        [DomainPermissions.ProductsDelete] = ("Delete Products", "Delete products", "E-commerce"),
        [DomainPermissions.ProductsPublish] = ("Publish Products", "Publish or unpublish products", "E-commerce"),

        // Product Categories
        [DomainPermissions.ProductCategoriesRead] = ("View Product Categories", "View product categories", "E-commerce"),
        [DomainPermissions.ProductCategoriesCreate] = ("Create Product Categories", "Create new product categories", "E-commerce"),
        [DomainPermissions.ProductCategoriesUpdate] = ("Update Product Categories", "Edit product categories", "E-commerce"),
        [DomainPermissions.ProductCategoriesDelete] = ("Delete Product Categories", "Delete product categories", "E-commerce"),

        // Brands
        [DomainPermissions.BrandsRead] = ("View Brands", "View product brands", "E-commerce"),
        [DomainPermissions.BrandsCreate] = ("Create Brands", "Create new brands", "E-commerce"),
        [DomainPermissions.BrandsUpdate] = ("Update Brands", "Edit brand details", "E-commerce"),
        [DomainPermissions.BrandsDelete] = ("Delete Brands", "Delete brands", "E-commerce"),

        // Product Attributes
        [DomainPermissions.AttributesRead] = ("View Attributes", "View product attributes", "E-commerce"),
        [DomainPermissions.AttributesCreate] = ("Create Attributes", "Create new product attributes", "E-commerce"),
        [DomainPermissions.AttributesUpdate] = ("Update Attributes", "Edit product attributes", "E-commerce"),
        [DomainPermissions.AttributesDelete] = ("Delete Attributes", "Delete product attributes", "E-commerce"),

        // Reviews
        [DomainPermissions.ReviewsRead] = ("View Reviews", "View product reviews", "E-commerce"),
        [DomainPermissions.ReviewsWrite] = ("Write Reviews", "Submit product reviews", "E-commerce"),
        [DomainPermissions.ReviewsManage] = ("Manage Reviews", "Moderate and manage reviews", "E-commerce"),

        // Customer Groups
        [DomainPermissions.CustomerGroupsRead] = ("View Customer Groups", "View customer group configurations", "E-commerce"),
        [DomainPermissions.CustomerGroupsCreate] = ("Create Customer Groups", "Create new customer groups", "E-commerce"),
        [DomainPermissions.CustomerGroupsUpdate] = ("Update Customer Groups", "Edit customer group settings", "E-commerce"),
        [DomainPermissions.CustomerGroupsDelete] = ("Delete Customer Groups", "Delete customer groups", "E-commerce"),
        [DomainPermissions.CustomerGroupsManageMembers] = ("Manage Group Members", "Add or remove members from customer groups", "E-commerce"),

        // Customers
        [DomainPermissions.CustomersRead] = ("View Customers", "View customer profiles", "E-commerce"),
        [DomainPermissions.CustomersCreate] = ("Create Customers", "Create new customer accounts", "E-commerce"),
        [DomainPermissions.CustomersUpdate] = ("Update Customers", "Edit customer information", "E-commerce"),
        [DomainPermissions.CustomersDelete] = ("Delete Customers", "Delete customer accounts", "E-commerce"),
        [DomainPermissions.CustomersManage] = ("Manage Customers", "Full customer management access", "E-commerce"),

        // Orders
        [DomainPermissions.OrdersRead] = ("View Orders", "View order details", "Orders"),
        [DomainPermissions.OrdersWrite] = ("Create Orders", "Create new orders", "Orders"),
        [DomainPermissions.OrdersManage] = ("Manage Orders", "Full order management access", "Orders"),

        // Promotions
        [DomainPermissions.PromotionsRead] = ("View Promotions", "View promotions and discounts", "E-commerce"),
        [DomainPermissions.PromotionsWrite] = ("Create/Edit Promotions", "Create and edit promotions", "E-commerce"),
        [DomainPermissions.PromotionsDelete] = ("Delete Promotions", "Delete promotions", "E-commerce"),
        [DomainPermissions.PromotionsManage] = ("Manage Promotions", "Full promotion management access", "E-commerce"),

        // Inventory
        [DomainPermissions.InventoryRead] = ("View Inventory", "View stock levels and receipts", "E-commerce"),
        [DomainPermissions.InventoryWrite] = ("Adjust Inventory", "Create stock adjustments", "E-commerce"),
        [DomainPermissions.InventoryManage] = ("Manage Inventory", "Full inventory management access", "E-commerce"),

        // Wishlists
        [DomainPermissions.WishlistsRead] = ("View Wishlists", "View customer wishlists", "E-commerce"),
        [DomainPermissions.WishlistsWrite] = ("Edit Wishlists", "Edit wishlist items", "E-commerce"),
        [DomainPermissions.WishlistsManage] = ("Manage Wishlists", "Full wishlist management access", "E-commerce"),

        // Reports
        [DomainPermissions.ReportsRead] = ("View Reports", "View business reports and analytics", "E-commerce"),

        // Legal Pages
        [DomainPermissions.LegalPagesRead] = ("View Legal Pages", "View legal documents", "Legal Pages"),
        [DomainPermissions.LegalPagesUpdate] = ("Update Legal Pages", "Edit legal documents", "Legal Pages"),

        // Tenant Settings
        [DomainPermissions.TenantSettingsRead] = ("View Tenant Settings", "View organization settings", "Settings"),
        [DomainPermissions.TenantSettingsUpdate] = ("Update Tenant Settings", "Modify organization settings", "Settings"),

        // Feature Management
        [DomainPermissions.FeaturesRead] = ("View Features", "View feature module configurations", "Settings"),
        [DomainPermissions.FeaturesUpdate] = ("Update Features", "Enable or disable feature modules", "Settings"),

        // Platform Settings
        [DomainPermissions.PlatformSettingsRead] = ("View Platform Settings", "View platform configuration", "Platform"),
        [DomainPermissions.PlatformSettingsManage] = ("Manage Platform Settings", "Configure platform settings", "Platform"),

        // Payments
        [DomainPermissions.PaymentsRead] = ("View Payments", "View payment transactions", "Payments"),
        [DomainPermissions.PaymentsCreate] = ("Create Payments", "Process new payments", "Payments"),
        [DomainPermissions.PaymentsManage] = ("Manage Payments", "Full payment management", "Payments"),
        [DomainPermissions.PaymentGatewaysRead] = ("View Payment Gateways", "View gateway configurations", "Payments"),
        [DomainPermissions.PaymentGatewaysManage] = ("Manage Payment Gateways", "Configure payment gateways", "Payments"),
        [DomainPermissions.PaymentRefundsRead] = ("View Refunds", "View refund requests", "Payments"),
        [DomainPermissions.PaymentRefundsManage] = ("Manage Refunds", "Process refund requests", "Payments"),
        [DomainPermissions.PaymentWebhooksRead] = ("View Payment Webhooks", "View webhook logs", "Payments"),
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
