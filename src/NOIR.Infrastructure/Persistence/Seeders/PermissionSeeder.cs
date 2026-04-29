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

        // Dashboard category
        permissions.Add(Permission.Create("dashboard", "read", "View Dashboard", null, "Access dashboard overview", "Dashboard", true, sortOrder++));

        // Search category
        permissions.Add(Permission.Create("search", "global", "Global Search", null, "Use the global search across the platform", "Search", true, sortOrder++));

        // Media category
        permissions.Add(Permission.Create("media", "read", "View Media", null, "View media library files", "Media", true, sortOrder++));
        permissions.Add(Permission.Create("media", "create", "Create Media", null, "Upload new media files", "Media", true, sortOrder++));
        permissions.Add(Permission.Create("media", "update", "Update Media", null, "Edit media file metadata", "Media", true, sortOrder++));
        permissions.Add(Permission.Create("media", "delete", "Delete Media", null, "Delete media files", "Media", true, sortOrder++));
        permissions.Add(Permission.Create("media", "manage", "Manage Media", null, "Full media library management access", "Media", true, sortOrder++));

        // API Keys category
        permissions.Add(Permission.Create("api-keys", "read", "View API Keys", null, "View API keys", "API Keys", true, sortOrder++));
        permissions.Add(Permission.Create("api-keys", "create", "Create API Keys", null, "Generate new API keys", "API Keys", true, sortOrder++));
        permissions.Add(Permission.Create("api-keys", "delete", "Delete API Keys", null, "Revoke API keys", "API Keys", true, sortOrder++));

        // Webhooks category
        permissions.Add(Permission.Create("webhooks", "read", "View Webhooks", null, "View webhook subscriptions", "Webhooks", true, sortOrder++));
        permissions.Add(Permission.Create("webhooks", "manage", "Manage Webhooks", null, "Create, edit, and delete webhook subscriptions", "Webhooks", true, sortOrder++));
        permissions.Add(Permission.Create("webhooks", "test", "Test Webhooks", null, "Send test deliveries to webhook endpoints", "Webhooks", true, sortOrder++));

        // HR — Employees
        permissions.Add(Permission.Create("hr-employees", "read", "View Employees", null, "View employee profiles", "Human Resources", true, sortOrder++));
        permissions.Add(Permission.Create("hr-employees", "create", "Create Employees", null, "Create new employee records", "Human Resources", true, sortOrder++));
        permissions.Add(Permission.Create("hr-employees", "update", "Update Employees", null, "Edit employee profiles", "Human Resources", true, sortOrder++));
        permissions.Add(Permission.Create("hr-employees", "delete", "Delete Employees", null, "Delete employee records", "Human Resources", true, sortOrder++));

        // HR — Departments
        permissions.Add(Permission.Create("hr-departments", "read", "View Departments", null, "View departments", "Human Resources", true, sortOrder++));
        permissions.Add(Permission.Create("hr-departments", "create", "Create Departments", null, "Create new departments", "Human Resources", true, sortOrder++));
        permissions.Add(Permission.Create("hr-departments", "update", "Update Departments", null, "Edit departments", "Human Resources", true, sortOrder++));
        permissions.Add(Permission.Create("hr-departments", "delete", "Delete Departments", null, "Delete departments", "Human Resources", true, sortOrder++));

        // HR — Tags
        permissions.Add(Permission.Create("hr-tags", "read", "View Employee Tags", null, "View employee tag library", "Human Resources", true, sortOrder++));
        permissions.Add(Permission.Create("hr-tags", "manage", "Manage Employee Tags", null, "Create, edit, delete, and assign employee tags", "Human Resources", true, sortOrder++));

        // CRM — Contacts
        permissions.Add(Permission.Create("crm-contacts", "read", "View Contacts", null, "View CRM contacts", "CRM", true, sortOrder++));
        permissions.Add(Permission.Create("crm-contacts", "create", "Create Contacts", null, "Create new CRM contacts", "CRM", true, sortOrder++));
        permissions.Add(Permission.Create("crm-contacts", "update", "Update Contacts", null, "Edit CRM contacts", "CRM", true, sortOrder++));
        permissions.Add(Permission.Create("crm-contacts", "delete", "Delete Contacts", null, "Delete CRM contacts", "CRM", true, sortOrder++));

        // CRM — Companies
        permissions.Add(Permission.Create("crm-companies", "read", "View Companies", null, "View CRM companies", "CRM", true, sortOrder++));
        permissions.Add(Permission.Create("crm-companies", "create", "Create Companies", null, "Create new CRM companies", "CRM", true, sortOrder++));
        permissions.Add(Permission.Create("crm-companies", "update", "Update Companies", null, "Edit CRM companies", "CRM", true, sortOrder++));
        permissions.Add(Permission.Create("crm-companies", "delete", "Delete Companies", null, "Delete CRM companies", "CRM", true, sortOrder++));

        // CRM — Leads
        permissions.Add(Permission.Create("crm-leads", "read", "View Leads", null, "View sales leads", "CRM", true, sortOrder++));
        permissions.Add(Permission.Create("crm-leads", "create", "Create Leads", null, "Create new sales leads", "CRM", true, sortOrder++));
        permissions.Add(Permission.Create("crm-leads", "update", "Update Leads", null, "Edit sales leads", "CRM", true, sortOrder++));
        permissions.Add(Permission.Create("crm-leads", "manage", "Manage Leads", null, "Full lead management including stage transitions and assignment", "CRM", true, sortOrder++));

        // CRM — Pipeline
        permissions.Add(Permission.Create("crm-pipeline", "manage", "Manage Sales Pipeline", null, "Configure pipeline stages and probabilities", "CRM", true, sortOrder++));

        // CRM — Activities
        permissions.Add(Permission.Create("crm-activities", "read", "View CRM Activities", null, "View CRM activity records", "CRM", true, sortOrder++));
        permissions.Add(Permission.Create("crm-activities", "create", "Create CRM Activities", null, "Log new CRM activities", "CRM", true, sortOrder++));
        permissions.Add(Permission.Create("crm-activities", "update", "Update CRM Activities", null, "Edit CRM activities", "CRM", true, sortOrder++));
        permissions.Add(Permission.Create("crm-activities", "delete", "Delete CRM Activities", null, "Delete CRM activities", "CRM", true, sortOrder++));

        // PM — Projects
        permissions.Add(Permission.Create("pm-projects", "read", "View Projects", null, "View projects", "Project Management", true, sortOrder++));
        permissions.Add(Permission.Create("pm-projects", "create", "Create Projects", null, "Create new projects", "Project Management", true, sortOrder++));
        permissions.Add(Permission.Create("pm-projects", "update", "Update Projects", null, "Edit projects", "Project Management", true, sortOrder++));
        permissions.Add(Permission.Create("pm-projects", "delete", "Delete Projects", null, "Delete projects", "Project Management", true, sortOrder++));

        // PM — Tasks
        permissions.Add(Permission.Create("pm-tasks", "read", "View Tasks", null, "View project tasks", "Project Management", true, sortOrder++));
        permissions.Add(Permission.Create("pm-tasks", "create", "Create Tasks", null, "Create new tasks", "Project Management", true, sortOrder++));
        permissions.Add(Permission.Create("pm-tasks", "update", "Update Tasks", null, "Edit tasks", "Project Management", true, sortOrder++));
        permissions.Add(Permission.Create("pm-tasks", "delete", "Delete Tasks", null, "Delete tasks", "Project Management", true, sortOrder++));
        permissions.Add(Permission.Create("pm-tasks", "manage", "Manage Tasks", null, "Full task management including bulk operations and reordering", "Project Management", true, sortOrder++));

        // PM — Members
        permissions.Add(Permission.Create("pm-members", "manage", "Manage Project Members", null, "Add and remove project members", "Project Management", true, sortOrder++));

        return permissions;
    }
}
