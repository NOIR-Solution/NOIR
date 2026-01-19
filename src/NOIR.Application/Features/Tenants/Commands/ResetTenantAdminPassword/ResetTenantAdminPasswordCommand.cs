namespace NOIR.Application.Features.Tenants.Commands.ResetTenantAdminPassword;

/// <summary>
/// Command to reset the password of a tenant's admin user.
/// Only Platform Admins can execute this command.
/// </summary>
public sealed record ResetTenantAdminPasswordCommand(
    /// <summary>
    /// The tenant ID whose admin password should be reset.
    /// </summary>
    string TenantId,

    /// <summary>
    /// The new password for the tenant admin.
    /// </summary>
    string NewPassword);

/// <summary>
/// Result of the reset tenant admin password operation.
/// </summary>
public sealed record ResetTenantAdminPasswordResult(
    bool Success,
    string Message,
    string? AdminUserId = null,
    string? AdminEmail = null);
