namespace NOIR.Domain.Common;

/// <summary>
/// Granular permission constants for authorization.
/// Format: "resource:action" (e.g., "users:read", "orders:create")
/// </summary>
public static class Permissions
{
    /// <summary>
    /// Custom claim type for permissions in JWT tokens and role claims.
    /// </summary>
    public const string ClaimType = "permission";

    // Users
    public const string UsersRead = "users:read";
    public const string UsersCreate = "users:create";
    public const string UsersUpdate = "users:update";
    public const string UsersDelete = "users:delete";
    public const string UsersManageRoles = "users:manage-roles";

    // Roles
    public const string RolesRead = "roles:read";
    public const string RolesCreate = "roles:create";
    public const string RolesUpdate = "roles:update";
    public const string RolesDelete = "roles:delete";
    public const string RolesManagePermissions = "roles:manage-permissions";

    // Tenants (for multi-tenancy)
    public const string TenantsRead = "tenants:read";
    public const string TenantsCreate = "tenants:create";
    public const string TenantsUpdate = "tenants:update";
    public const string TenantsDelete = "tenants:delete";

    // System
    public const string SystemAdmin = "system:admin";
    public const string SystemAuditLogs = "system:audit-logs";
    public const string SystemSettings = "system:settings";
    public const string HangfireDashboard = "system:hangfire";

    // Audit (granular permissions)
    public const string AuditRead = "audit:read";
    public const string AuditExport = "audit:export";
    public const string AuditEntityHistory = "audit:entity-history";
    public const string AuditPolicyRead = "audit:policy-read";
    public const string AuditPolicyWrite = "audit:policy-write";
    public const string AuditPolicyDelete = "audit:policy-delete";
    public const string AuditStream = "audit:stream";

    // Email Templates
    public const string EmailTemplatesRead = "email-templates:read";
    public const string EmailTemplatesUpdate = "email-templates:update";

    // Blog Posts
    public const string BlogPostsRead = "blog-posts:read";
    public const string BlogPostsCreate = "blog-posts:create";
    public const string BlogPostsUpdate = "blog-posts:update";
    public const string BlogPostsDelete = "blog-posts:delete";
    public const string BlogPostsPublish = "blog-posts:publish";

    // Blog Categories
    public const string BlogCategoriesRead = "blog-categories:read";
    public const string BlogCategoriesCreate = "blog-categories:create";
    public const string BlogCategoriesUpdate = "blog-categories:update";
    public const string BlogCategoriesDelete = "blog-categories:delete";

    // Blog Tags
    public const string BlogTagsRead = "blog-tags:read";
    public const string BlogTagsCreate = "blog-tags:create";
    public const string BlogTagsUpdate = "blog-tags:update";
    public const string BlogTagsDelete = "blog-tags:delete";

    // Products
    public const string ProductsRead = "products:read";
    public const string ProductsCreate = "products:create";
    public const string ProductsUpdate = "products:update";
    public const string ProductsDelete = "products:delete";
    public const string ProductsPublish = "products:publish";

    // Product Categories
    public const string ProductCategoriesRead = "product-categories:read";
    public const string ProductCategoriesCreate = "product-categories:create";
    public const string ProductCategoriesUpdate = "product-categories:update";
    public const string ProductCategoriesDelete = "product-categories:delete";

    // Orders
    public const string OrdersRead = "orders:read";
    public const string OrdersWrite = "orders:write";
    public const string OrdersManage = "orders:manage";

    // Legal Pages
    public const string LegalPagesRead = "legal-pages:read";
    public const string LegalPagesUpdate = "legal-pages:update";

    // Tenant Settings
    public const string TenantSettingsRead = "tenant-settings:read";
    public const string TenantSettingsUpdate = "tenant-settings:update";

    // Platform Settings
    public const string PlatformSettingsRead = "platform-settings:read";
    public const string PlatformSettingsManage = "platform-settings:manage";

    // Payments
    public const string PaymentsRead = "payments:read";
    public const string PaymentsCreate = "payments:create";
    public const string PaymentsManage = "payments:manage";
    public const string PaymentGatewaysRead = "payment-gateways:read";
    public const string PaymentGatewaysManage = "payment-gateways:manage";
    public const string PaymentRefundsRead = "payment-refunds:read";
    public const string PaymentRefundsManage = "payment-refunds:manage";
    public const string PaymentWebhooksRead = "payment-webhooks:read";

    /// <summary>
    /// All permissions grouped by resource.
    /// </summary>
    public static class Groups
    {
        public static readonly IReadOnlyList<string> Users =
            [UsersRead, UsersCreate, UsersUpdate, UsersDelete, UsersManageRoles];

        public static readonly IReadOnlyList<string> Roles =
            [RolesRead, RolesCreate, RolesUpdate, RolesDelete, RolesManagePermissions];

        public static readonly IReadOnlyList<string> Tenants =
            [TenantsRead, TenantsCreate, TenantsUpdate, TenantsDelete];

        public static readonly IReadOnlyList<string> SystemPermissions =
            [SystemAdmin, SystemAuditLogs, SystemSettings, HangfireDashboard];

        public static readonly IReadOnlyList<string> Audit =
            [AuditRead, AuditExport, AuditEntityHistory, AuditPolicyRead, AuditPolicyWrite, AuditPolicyDelete, AuditStream];

        public static readonly IReadOnlyList<string> EmailTemplates =
            [EmailTemplatesRead, EmailTemplatesUpdate];

        public static readonly IReadOnlyList<string> BlogPosts =
            [BlogPostsRead, BlogPostsCreate, BlogPostsUpdate, BlogPostsDelete, BlogPostsPublish];

        public static readonly IReadOnlyList<string> BlogCategories =
            [BlogCategoriesRead, BlogCategoriesCreate, BlogCategoriesUpdate, BlogCategoriesDelete];

        public static readonly IReadOnlyList<string> BlogTags =
            [BlogTagsRead, BlogTagsCreate, BlogTagsUpdate, BlogTagsDelete];

        public static readonly IReadOnlyList<string> Products =
            [ProductsRead, ProductsCreate, ProductsUpdate, ProductsDelete, ProductsPublish];

        public static readonly IReadOnlyList<string> ProductCategories =
            [ProductCategoriesRead, ProductCategoriesCreate, ProductCategoriesUpdate, ProductCategoriesDelete];

        public static readonly IReadOnlyList<string> Orders =
            [OrdersRead, OrdersWrite, OrdersManage];

        public static readonly IReadOnlyList<string> LegalPages =
            [LegalPagesRead, LegalPagesUpdate];

        public static readonly IReadOnlyList<string> TenantSettings =
            [TenantSettingsRead, TenantSettingsUpdate];

        public static readonly IReadOnlyList<string> PlatformSettings =
            [PlatformSettingsRead, PlatformSettingsManage];

        public static readonly IReadOnlyList<string> Payments =
            [PaymentsRead, PaymentsCreate, PaymentsManage, PaymentGatewaysRead, PaymentGatewaysManage,
             PaymentRefundsRead, PaymentRefundsManage, PaymentWebhooksRead];
    }

    /// <summary>
    /// All available permissions.
    /// </summary>
    public static IReadOnlyList<string> All =>
    [
        // Users
        UsersRead, UsersCreate, UsersUpdate, UsersDelete, UsersManageRoles,
        // Roles
        RolesRead, RolesCreate, RolesUpdate, RolesDelete, RolesManagePermissions,
        // Tenants
        TenantsRead, TenantsCreate, TenantsUpdate, TenantsDelete,
        // System
        SystemAdmin, SystemAuditLogs, SystemSettings, HangfireDashboard,
        // Audit
        AuditRead, AuditExport, AuditEntityHistory, AuditPolicyRead, AuditPolicyWrite, AuditPolicyDelete, AuditStream,
        // Email Templates
        EmailTemplatesRead, EmailTemplatesUpdate,
        // Blog Posts
        BlogPostsRead, BlogPostsCreate, BlogPostsUpdate, BlogPostsDelete, BlogPostsPublish,
        // Blog Categories
        BlogCategoriesRead, BlogCategoriesCreate, BlogCategoriesUpdate, BlogCategoriesDelete,
        // Blog Tags
        BlogTagsRead, BlogTagsCreate, BlogTagsUpdate, BlogTagsDelete,
        // Products
        ProductsRead, ProductsCreate, ProductsUpdate, ProductsDelete, ProductsPublish,
        // Product Categories
        ProductCategoriesRead, ProductCategoriesCreate, ProductCategoriesUpdate, ProductCategoriesDelete,
        // Orders
        OrdersRead, OrdersWrite, OrdersManage,
        // Legal Pages
        LegalPagesRead, LegalPagesUpdate,
        // Tenant Settings
        TenantSettingsRead, TenantSettingsUpdate,
        // Platform Settings
        PlatformSettingsRead, PlatformSettingsManage,
        // Payments
        PaymentsRead, PaymentsCreate, PaymentsManage, PaymentGatewaysRead, PaymentGatewaysManage,
        PaymentRefundsRead, PaymentRefundsManage, PaymentWebhooksRead
    ];

    /// <summary>
    /// Default permissions for PlatformAdmin role.
    /// Platform admins have all system-level permissions for managing tenants and platform settings.
    /// </summary>
    public static IReadOnlyList<string> PlatformAdminDefaults =>
    [
        // Full tenant management
        TenantsRead, TenantsCreate, TenantsUpdate, TenantsDelete,
        // System administration
        SystemAdmin, SystemAuditLogs, SystemSettings, HangfireDashboard,
        // Platform-level email templates
        EmailTemplatesRead, EmailTemplatesUpdate,
        // Platform-level legal pages
        LegalPagesRead, LegalPagesUpdate,
        // Platform-level audit (all tenants)
        AuditRead, AuditExport, AuditEntityHistory, AuditPolicyRead, AuditPolicyWrite, AuditPolicyDelete, AuditStream,
        // Platform settings
        PlatformSettingsRead, PlatformSettingsManage
    ];

    /// <summary>
    /// Default permissions for Admin role (tenant-level).
    /// Tenant admins have full access within their own tenant.
    /// </summary>
    public static IReadOnlyList<string> AdminDefaults =>
    [
        // User management within tenant
        UsersRead, UsersCreate, UsersUpdate, UsersDelete, UsersManageRoles,
        // Role management within tenant
        RolesRead, RolesCreate, RolesUpdate, RolesDelete, RolesManagePermissions,
        // Tenant-level email templates (copy-on-write)
        EmailTemplatesRead, EmailTemplatesUpdate,
        // Tenant-level legal pages (copy-on-write)
        LegalPagesRead, LegalPagesUpdate,
        // Tenant settings (branding, contact, regional)
        TenantSettingsRead, TenantSettingsUpdate,
        // Audit within tenant
        AuditRead, AuditExport, AuditEntityHistory,
        // Blog within tenant
        BlogPostsRead, BlogPostsCreate, BlogPostsUpdate, BlogPostsDelete, BlogPostsPublish,
        BlogCategoriesRead, BlogCategoriesCreate, BlogCategoriesUpdate, BlogCategoriesDelete,
        BlogTagsRead, BlogTagsCreate, BlogTagsUpdate, BlogTagsDelete,
        // Products within tenant
        ProductsRead, ProductsCreate, ProductsUpdate, ProductsDelete, ProductsPublish,
        ProductCategoriesRead, ProductCategoriesCreate, ProductCategoriesUpdate, ProductCategoriesDelete,
        // Orders within tenant
        OrdersRead, OrdersWrite, OrdersManage,
        // Payments within tenant
        PaymentsRead, PaymentsCreate, PaymentsManage, PaymentGatewaysRead, PaymentGatewaysManage,
        PaymentRefundsRead, PaymentRefundsManage, PaymentWebhooksRead
    ];

    /// <summary>
    /// Default permissions for User role.
    /// </summary>
    public static IReadOnlyList<string> UserDefaults =>
        [UsersRead];

    /// <summary>
    /// Permission scope definitions for multi-tenant validation.
    /// </summary>
    public static class Scopes
    {
        /// <summary>
        /// Permissions that can ONLY be assigned to system roles (TenantId = null).
        /// These permissions affect cross-tenant or platform-level operations.
        /// Note: Email templates are NOT system-only because tenants have copy-on-write templates.
        /// </summary>
        public static IReadOnlySet<string> SystemOnly { get; } = new HashSet<string>
        {
            // Tenant management is system-only
            TenantsRead,
            TenantsCreate,
            TenantsUpdate,
            TenantsDelete,
            // System administration is system-only
            SystemAdmin,
            SystemAuditLogs,
            SystemSettings,
            HangfireDashboard,
            // Platform settings management
            PlatformSettingsRead,
            PlatformSettingsManage
        };

        /// <summary>
        /// Permissions that can be assigned to tenant-specific roles.
        /// These permissions are scoped to within-tenant operations.
        /// </summary>
        public static IReadOnlySet<string> TenantAllowed { get; } = new HashSet<string>
        {
            // Users within tenant
            UsersRead,
            UsersCreate,
            UsersUpdate,
            UsersDelete,
            UsersManageRoles,
            // Roles within tenant
            RolesRead,
            RolesCreate,
            RolesUpdate,
            RolesDelete,
            RolesManagePermissions,
            // Email templates within tenant (copy-on-write)
            EmailTemplatesRead,
            EmailTemplatesUpdate,
            // Legal pages within tenant (copy-on-write)
            LegalPagesRead,
            LegalPagesUpdate,
            // Tenant settings (branding, contact, regional)
            TenantSettingsRead,
            TenantSettingsUpdate,
            // Audit within tenant (read and export only)
            AuditRead,
            AuditExport,
            AuditEntityHistory,
            // Blog within tenant
            BlogPostsRead,
            BlogPostsCreate,
            BlogPostsUpdate,
            BlogPostsDelete,
            BlogPostsPublish,
            BlogCategoriesRead,
            BlogCategoriesCreate,
            BlogCategoriesUpdate,
            BlogCategoriesDelete,
            BlogTagsRead,
            BlogTagsCreate,
            BlogTagsUpdate,
            BlogTagsDelete,
            // Products within tenant
            ProductsRead,
            ProductsCreate,
            ProductsUpdate,
            ProductsDelete,
            ProductsPublish,
            ProductCategoriesRead,
            ProductCategoriesCreate,
            ProductCategoriesUpdate,
            ProductCategoriesDelete,
            // Orders within tenant
            OrdersRead,
            OrdersWrite,
            OrdersManage,
            // Payments within tenant
            PaymentsRead,
            PaymentsCreate,
            PaymentsManage,
            PaymentGatewaysRead,
            PaymentGatewaysManage,
            PaymentRefundsRead,
            PaymentRefundsManage,
            PaymentWebhooksRead
        };

        /// <summary>
        /// Checks if a permission is allowed for tenant-scoped roles.
        /// </summary>
        public static bool IsTenantAllowed(string permission) => TenantAllowed.Contains(permission);

        /// <summary>
        /// Checks if a permission is system-only.
        /// </summary>
        public static bool IsSystemOnly(string permission) => SystemOnly.Contains(permission);

        /// <summary>
        /// Validates that all permissions are valid for the given tenant context.
        /// Returns the list of invalid permissions if any.
        /// </summary>
        public static IReadOnlyList<string> ValidateForTenant(IEnumerable<string> permissions, Guid? tenantId)
        {
            if (!tenantId.HasValue)
            {
                // System roles can have any permission
                return [];
            }

            // Tenant-specific roles can only have tenant-allowed permissions
            return permissions.Where(p => !TenantAllowed.Contains(p)).ToList();
        }
    }
}
