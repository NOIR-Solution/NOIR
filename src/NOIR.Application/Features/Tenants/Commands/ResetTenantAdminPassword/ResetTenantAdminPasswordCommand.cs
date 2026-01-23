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
    string NewPassword) : IAuditableCommand<ResetTenantAdminPasswordResult>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => TenantId;
    public string? GetTargetDisplayName() => $"Tenant {TenantId}";
    public string? GetActionDescription() => $"Reset admin password for tenant '{TenantId}'";
}

/// <summary>
/// Result of the reset tenant admin password operation.
/// </summary>
public sealed record ResetTenantAdminPasswordResult(
    bool Success,
    string Message,
    string? AdminUserId = null,
    string? AdminEmail = null);
