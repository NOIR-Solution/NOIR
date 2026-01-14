namespace NOIR.Domain.Enums;

/// <summary>
/// Defines the role a user has within a specific tenant.
/// Higher values indicate more permissions.
/// </summary>
public enum TenantRole
{
    /// <summary>
    /// Read-only access to tenant resources.
    /// </summary>
    Viewer = 20,

    /// <summary>
    /// Standard access - can create and modify own resources.
    /// </summary>
    Member = 50,

    /// <summary>
    /// Administrative access - can manage users and settings.
    /// </summary>
    Admin = 80,

    /// <summary>
    /// Full control - can delete tenant, transfer ownership.
    /// </summary>
    Owner = 100
}
