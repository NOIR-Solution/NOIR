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
