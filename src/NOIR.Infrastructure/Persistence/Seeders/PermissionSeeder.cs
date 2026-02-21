namespace NOIR.Infrastructure.Persistence.Seeders;

/// <summary>
/// Seeds Permission entities based on the Permissions constants.
/// These enable database-backed permission management alongside claims.
/// </summary>
public class PermissionSeeder : ISeeder
{
    /// <summary>
    /// Permissions must be seeded before roles (roles reference permissions for templates).
    /// </summary>
    public int Order => 10;

    public async Task SeedAsync(SeederContext context, CancellationToken ct = default)
    {
        var existingPermissions = await context.DbContext.Set<Permission>()
            .IgnoreQueryFilters()
            .TagWith("Seeder:GetExistingPermissions")
            .ToListAsync(ct);

        var existingByName = existingPermissions.ToDictionary(p => p.Name);
        var permissionsToSeed = GetPermissionDefinitions();
        var newPermissions = new List<Permission>();

        foreach (var permission in permissionsToSeed)
        {
            if (!existingByName.ContainsKey(permission.Name))
            {
                newPermissions.Add(permission);
                context.Logger.LogInformation("Seeding permission: {Permission}", permission.Name);
            }
        }

        if (newPermissions.Count > 0)
        {
            await context.DbContext.Set<Permission>().AddRangeAsync(newPermissions, ct);
            await context.DbContext.SaveChangesAsync(ct);
            context.Logger.LogInformation("Seeded {Count} permissions", newPermissions.Count);
        }
    }

    /// <summary>
    /// Builds a list of Permission entities from the Permissions constants.
    /// </summary>
    private static List<Permission> GetPermissionDefinitions()
    {
        var permissions = new List<Permission>();
        var sortOrder = 0;

        // Users category
        permissions.Add(Permission.Create("users", "read", "View Users", null, "View user profiles and list users", "User Management", true, sortOrder++));
        permissions.Add(Permission.Create("users", "create", "Create Users", null, "Create new user accounts", "User Management", true, sortOrder++));
        permissions.Add(Permission.Create("users", "update", "Update Users", null, "Edit user profiles and settings", "User Management", true, sortOrder++));
        permissions.Add(Permission.Create("users", "delete", "Delete Users", null, "Delete user accounts", "User Management", true, sortOrder++));
        permissions.Add(Permission.Create("users", "manage-roles", "Manage User Roles", null, "Assign and remove roles from users", "User Management", true, sortOrder++));

        // Roles category
        permissions.Add(Permission.Create("roles", "read", "View Roles", null, "View roles and their permissions", "Role Management", true, sortOrder++));
        permissions.Add(Permission.Create("roles", "create", "Create Roles", null, "Create new roles", "Role Management", true, sortOrder++));
        permissions.Add(Permission.Create("roles", "update", "Update Roles", null, "Edit role details", "Role Management", true, sortOrder++));
        permissions.Add(Permission.Create("roles", "delete", "Delete Roles", null, "Delete roles", "Role Management", true, sortOrder++));
        permissions.Add(Permission.Create("roles", "manage-permissions", "Manage Role Permissions", null, "Assign and remove permissions from roles", "Role Management", true, sortOrder++));

        // Tenants category
        permissions.Add(Permission.Create("tenants", "read", "View Tenants", null, "View tenant information", "Tenant Management", true, sortOrder++));
        permissions.Add(Permission.Create("tenants", "create", "Create Tenants", null, "Create new tenants", "Tenant Management", true, sortOrder++));
        permissions.Add(Permission.Create("tenants", "update", "Update Tenants", null, "Edit tenant settings", "Tenant Management", true, sortOrder++));
        permissions.Add(Permission.Create("tenants", "delete", "Delete Tenants", null, "Delete tenants", "Tenant Management", true, sortOrder++));

        // System category
        permissions.Add(Permission.Create("system", "admin", "System Admin", null, "Full system administration access", "System", true, sortOrder++));
        permissions.Add(Permission.Create("system", "audit-logs", "View Audit Logs", null, "Access system audit logs", "System", true, sortOrder++));
        permissions.Add(Permission.Create("system", "settings", "Manage System Settings", null, "Configure system settings", "System", true, sortOrder++));
        permissions.Add(Permission.Create("system", "hangfire", "Hangfire Dashboard", null, "Access Hangfire background job dashboard", "System", true, sortOrder++));

        // Configuration Management category
        permissions.Add(Permission.Create("system", "config:view", "View Configuration", null, "View platform configuration settings", "Configuration Management", true, sortOrder++));
        permissions.Add(Permission.Create("system", "config:edit", "Edit Configuration", null, "Edit platform configuration settings", "Configuration Management", true, sortOrder++));
        permissions.Add(Permission.Create("system", "app:restart", "Restart Application", null, "Restart the application", "Configuration Management", true, sortOrder++));

        // Audit category
        permissions.Add(Permission.Create("audit", "read", "View Audit Data", null, "View audit records", "Audit", true, sortOrder++));
        permissions.Add(Permission.Create("audit", "export", "Export Audit Data", null, "Export audit logs to files", "Audit", true, sortOrder++));
        permissions.Add(Permission.Create("audit", "entity-history", "View Entity History", null, "View change history for entities", "Audit", true, sortOrder++));
        permissions.Add(Permission.Create("audit", "policy-read", "Read Audit Policies", null, "View audit policy configurations", "Audit", true, sortOrder++));
        permissions.Add(Permission.Create("audit", "policy-write", "Write Audit Policies", null, "Create and edit audit policies", "Audit", true, sortOrder++));
        permissions.Add(Permission.Create("audit", "policy-delete", "Delete Audit Policies", null, "Delete audit policies", "Audit", true, sortOrder++));
        permissions.Add(Permission.Create("audit", "stream", "Stream Audit Events", null, "Access real-time audit event stream", "Audit", true, sortOrder++));

        // Email Templates category
        permissions.Add(Permission.Create("email-templates", "read", "View Email Templates", null, "View email templates", "Email Templates", true, sortOrder++));
        permissions.Add(Permission.Create("email-templates", "update", "Update Email Templates", null, "Edit email template content", "Email Templates", true, sortOrder++));

        // Legal Pages category
        permissions.Add(Permission.Create("legal-pages", "read", "View Legal Pages", null, "View legal pages content", "Legal Pages", true, sortOrder++));
        permissions.Add(Permission.Create("legal-pages", "update", "Update Legal Pages", null, "Edit legal pages content", "Legal Pages", true, sortOrder++));

        // Tenant Settings category
        permissions.Add(Permission.Create("tenant-settings", "read", "View Tenant Settings", null, "View tenant-level settings", "Tenant Settings", true, sortOrder++));
        permissions.Add(Permission.Create("tenant-settings", "update", "Update Tenant Settings", null, "Edit tenant-level settings", "Tenant Settings", true, sortOrder++));

        // Feature Management category
        permissions.Add(Permission.Create("features", "read", "View Features", null, "View feature availability and states", "Feature Management", true, sortOrder++));
        permissions.Add(Permission.Create("features", "update", "Update Features", null, "Toggle features on or off", "Feature Management", true, sortOrder++));

        // Platform Settings category
        permissions.Add(Permission.Create("platform-settings", "read", "View Platform Settings", null, "View platform-level settings", "Platform Settings", true, sortOrder++));
        permissions.Add(Permission.Create("platform-settings", "manage", "Manage Platform Settings", null, "Manage platform-level settings", "Platform Settings", true, sortOrder++));

        // Blog Posts category
        permissions.Add(Permission.Create("blog-posts", "read", "View Blog Posts", null, "View blog posts", "Blog", true, sortOrder++));
        permissions.Add(Permission.Create("blog-posts", "create", "Create Blog Posts", null, "Create new blog posts", "Blog", true, sortOrder++));
        permissions.Add(Permission.Create("blog-posts", "update", "Update Blog Posts", null, "Edit blog posts", "Blog", true, sortOrder++));
        permissions.Add(Permission.Create("blog-posts", "delete", "Delete Blog Posts", null, "Delete blog posts", "Blog", true, sortOrder++));
        permissions.Add(Permission.Create("blog-posts", "publish", "Publish Blog Posts", null, "Publish or unpublish blog posts", "Blog", true, sortOrder++));

        // Blog Categories category
        permissions.Add(Permission.Create("blog-categories", "read", "View Blog Categories", null, "View blog categories", "Blog", true, sortOrder++));
        permissions.Add(Permission.Create("blog-categories", "create", "Create Blog Categories", null, "Create new blog categories", "Blog", true, sortOrder++));
        permissions.Add(Permission.Create("blog-categories", "update", "Update Blog Categories", null, "Edit blog categories", "Blog", true, sortOrder++));
        permissions.Add(Permission.Create("blog-categories", "delete", "Delete Blog Categories", null, "Delete blog categories", "Blog", true, sortOrder++));

        // Blog Tags category
        permissions.Add(Permission.Create("blog-tags", "read", "View Blog Tags", null, "View blog tags", "Blog", true, sortOrder++));
        permissions.Add(Permission.Create("blog-tags", "create", "Create Blog Tags", null, "Create new blog tags", "Blog", true, sortOrder++));
        permissions.Add(Permission.Create("blog-tags", "update", "Update Blog Tags", null, "Edit blog tags", "Blog", true, sortOrder++));
        permissions.Add(Permission.Create("blog-tags", "delete", "Delete Blog Tags", null, "Delete blog tags", "Blog", true, sortOrder++));

        // Products category
        permissions.Add(Permission.Create("products", "read", "View Products", null, "View products", "Products", true, sortOrder++));
        permissions.Add(Permission.Create("products", "create", "Create Products", null, "Create new products", "Products", true, sortOrder++));
        permissions.Add(Permission.Create("products", "update", "Update Products", null, "Edit products", "Products", true, sortOrder++));
        permissions.Add(Permission.Create("products", "delete", "Delete Products", null, "Delete products", "Products", true, sortOrder++));
        permissions.Add(Permission.Create("products", "publish", "Publish Products", null, "Publish or unpublish products", "Products", true, sortOrder++));

        // Product Categories category
        permissions.Add(Permission.Create("product-categories", "read", "View Product Categories", null, "View product categories", "Products", true, sortOrder++));
        permissions.Add(Permission.Create("product-categories", "create", "Create Product Categories", null, "Create new product categories", "Products", true, sortOrder++));
        permissions.Add(Permission.Create("product-categories", "update", "Update Product Categories", null, "Edit product categories", "Products", true, sortOrder++));
        permissions.Add(Permission.Create("product-categories", "delete", "Delete Product Categories", null, "Delete product categories", "Products", true, sortOrder++));

        // Brands category
        permissions.Add(Permission.Create("brands", "read", "View Brands", null, "View brands", "Products", true, sortOrder++));
        permissions.Add(Permission.Create("brands", "create", "Create Brands", null, "Create new brands", "Products", true, sortOrder++));
        permissions.Add(Permission.Create("brands", "update", "Update Brands", null, "Edit brands", "Products", true, sortOrder++));
        permissions.Add(Permission.Create("brands", "delete", "Delete Brands", null, "Delete brands", "Products", true, sortOrder++));

        // Attributes category
        permissions.Add(Permission.Create("attributes", "read", "View Attributes", null, "View product attributes", "Products", true, sortOrder++));
        permissions.Add(Permission.Create("attributes", "create", "Create Attributes", null, "Create new product attributes", "Products", true, sortOrder++));
        permissions.Add(Permission.Create("attributes", "update", "Update Attributes", null, "Edit product attributes", "Products", true, sortOrder++));
        permissions.Add(Permission.Create("attributes", "delete", "Delete Attributes", null, "Delete product attributes", "Products", true, sortOrder++));

        // Reviews category
        permissions.Add(Permission.Create("reviews", "read", "View Reviews", null, "View product reviews", "Reviews", true, sortOrder++));
        permissions.Add(Permission.Create("reviews", "write", "Write Reviews", null, "Write product reviews", "Reviews", true, sortOrder++));
        permissions.Add(Permission.Create("reviews", "manage", "Manage Reviews", null, "Moderate and manage product reviews", "Reviews", true, sortOrder++));

        // Customer Groups category
        permissions.Add(Permission.Create("customer-groups", "read", "View Customer Groups", null, "View customer groups", "Customers", true, sortOrder++));
        permissions.Add(Permission.Create("customer-groups", "create", "Create Customer Groups", null, "Create new customer groups", "Customers", true, sortOrder++));
        permissions.Add(Permission.Create("customer-groups", "update", "Update Customer Groups", null, "Edit customer groups", "Customers", true, sortOrder++));
        permissions.Add(Permission.Create("customer-groups", "delete", "Delete Customer Groups", null, "Delete customer groups", "Customers", true, sortOrder++));
        permissions.Add(Permission.Create("customer-groups", "manage-members", "Manage Customer Group Members", null, "Add and remove members from customer groups", "Customers", true, sortOrder++));

        // Customers category
        permissions.Add(Permission.Create("customers", "read", "View Customers", null, "View customer profiles", "Customers", true, sortOrder++));
        permissions.Add(Permission.Create("customers", "create", "Create Customers", null, "Create new customer accounts", "Customers", true, sortOrder++));
        permissions.Add(Permission.Create("customers", "update", "Update Customers", null, "Edit customer profiles", "Customers", true, sortOrder++));
        permissions.Add(Permission.Create("customers", "delete", "Delete Customers", null, "Delete customer accounts", "Customers", true, sortOrder++));
        permissions.Add(Permission.Create("customers", "manage", "Manage Customers", null, "Full customer management access", "Customers", true, sortOrder++));

        // Orders category
        permissions.Add(Permission.Create("orders", "read", "View Orders", null, "View orders", "Orders", true, sortOrder++));
        permissions.Add(Permission.Create("orders", "write", "Write Orders", null, "Create and update orders", "Orders", true, sortOrder++));
        permissions.Add(Permission.Create("orders", "manage", "Manage Orders", null, "Full order management including cancellation and refunds", "Orders", true, sortOrder++));

        // Promotions category
        permissions.Add(Permission.Create("promotions", "read", "View Promotions", null, "View promotions and discounts", "Promotions", true, sortOrder++));
        permissions.Add(Permission.Create("promotions", "write", "Write Promotions", null, "Create and update promotions", "Promotions", true, sortOrder++));
        permissions.Add(Permission.Create("promotions", "delete", "Delete Promotions", null, "Delete promotions", "Promotions", true, sortOrder++));
        permissions.Add(Permission.Create("promotions", "manage", "Manage Promotions", null, "Full promotion management access", "Promotions", true, sortOrder++));

        // Inventory category
        permissions.Add(Permission.Create("inventory", "read", "View Inventory", null, "View inventory levels and receipts", "Inventory", true, sortOrder++));
        permissions.Add(Permission.Create("inventory", "write", "Write Inventory", null, "Create inventory receipts", "Inventory", true, sortOrder++));
        permissions.Add(Permission.Create("inventory", "manage", "Manage Inventory", null, "Full inventory management access", "Inventory", true, sortOrder++));

        // Wishlists category
        permissions.Add(Permission.Create("wishlists", "read", "View Wishlists", null, "View wishlists", "Wishlists", true, sortOrder++));
        permissions.Add(Permission.Create("wishlists", "write", "Write Wishlists", null, "Create and update wishlists", "Wishlists", true, sortOrder++));
        permissions.Add(Permission.Create("wishlists", "manage", "Manage Wishlists", null, "Full wishlist management access", "Wishlists", true, sortOrder++));

        // Reports category
        permissions.Add(Permission.Create("reports", "read", "View Reports", null, "View reports and analytics", "Reports", true, sortOrder++));

        // Payments category
        permissions.Add(Permission.Create("payments", "read", "View Payments", null, "View payment records", "Payments", true, sortOrder++));
        permissions.Add(Permission.Create("payments", "create", "Create Payments", null, "Process new payments", "Payments", true, sortOrder++));
        permissions.Add(Permission.Create("payments", "manage", "Manage Payments", null, "Full payment management access", "Payments", true, sortOrder++));
        permissions.Add(Permission.Create("payment-gateways", "read", "View Payment Gateways", null, "View payment gateway configurations", "Payments", true, sortOrder++));
        permissions.Add(Permission.Create("payment-gateways", "manage", "Manage Payment Gateways", null, "Configure payment gateways", "Payments", true, sortOrder++));
        permissions.Add(Permission.Create("payment-refunds", "read", "View Payment Refunds", null, "View payment refund records", "Payments", true, sortOrder++));
        permissions.Add(Permission.Create("payment-refunds", "manage", "Manage Payment Refunds", null, "Process payment refunds", "Payments", true, sortOrder++));
        permissions.Add(Permission.Create("payment-webhooks", "read", "View Payment Webhooks", null, "View payment webhook logs", "Payments", true, sortOrder++));

        return permissions;
    }
}
