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

        public static readonly IReadOnlyList<string> System =
            [SystemAdmin, SystemAuditLogs, SystemSettings, HangfireDashboard];

        public static readonly IReadOnlyList<string> Audit =
            [AuditRead, AuditExport, AuditEntityHistory, AuditPolicyRead, AuditPolicyWrite, AuditPolicyDelete, AuditStream];

        public static readonly IReadOnlyList<string> EmailTemplates =
            [EmailTemplatesRead, EmailTemplatesUpdate];
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
        EmailTemplatesRead, EmailTemplatesUpdate
    ];

    /// <summary>
    /// Default permissions for Admin role.
    /// </summary>
    public static IReadOnlyList<string> AdminDefaults => All;

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
            // Email templates are system-only
            EmailTemplatesRead,
            EmailTemplatesUpdate
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
            // Audit within tenant (read and export only)
            AuditRead,
            AuditExport,
            AuditEntityHistory
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
