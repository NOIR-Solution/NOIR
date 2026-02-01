namespace NOIR.Application.Features.Tenants.Commands.ProvisionTenant;

/// <summary>
/// Command to provision a new tenant with optional admin user creation.
/// This is the preferred way to create tenants as it handles all setup in one operation.
/// </summary>
public sealed record ProvisionTenantCommand(
    /// <summary>
    /// Unique identifier for the tenant (used in URLs and tenant resolution).
    /// </summary>
    string Identifier,

    /// <summary>
    /// Display name for the tenant.
    /// </summary>
    string Name,

    /// <summary>
    /// Optional domain for the tenant (e.g., "acme.noir.local").
    /// Used for domain-based tenant resolution.
    /// </summary>
    string? Domain = null,

    /// <summary>
    /// Optional description for the tenant.
    /// </summary>
    string? Description = null,

    /// <summary>
    /// Optional internal notes about the tenant.
    /// </summary>
    string? Note = null,

    /// <summary>
    /// Whether to create an admin user for the tenant.
    /// </summary>
    bool CreateAdminUser = true,

    /// <summary>
    /// Admin user email (required if CreateAdminUser is true).
    /// </summary>
    string? AdminEmail = null,

    /// <summary>
    /// Admin user password (required if CreateAdminUser is true).
    /// </summary>
    string? AdminPassword = null,

    /// <summary>
    /// Admin user first name (optional).
    /// </summary>
    string? AdminFirstName = null,

    /// <summary>
    /// Admin user last name (optional).
    /// </summary>
    string? AdminLastName = null
) : IAuditableCommand
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Create;
    public object? GetTargetId() => Identifier;
    public string? GetTargetDisplayName() => Name;
    public string? GetActionDescription() => $"Provisioned tenant '{Name}'";
}

/// <summary>
/// Result of provisioning a tenant.
/// </summary>
public sealed record ProvisionTenantResult(
    /// <summary>
    /// The created tenant's ID.
    /// </summary>
    string TenantId,

    /// <summary>
    /// The created tenant's identifier.
    /// </summary>
    string Identifier,

    /// <summary>
    /// The created tenant's name.
    /// </summary>
    string Name,

    /// <summary>
    /// The created tenant's domain (if set).
    /// </summary>
    string? Domain,

    /// <summary>
    /// Whether the tenant is active.
    /// </summary>
    bool IsActive,

    /// <summary>
    /// When the tenant was created.
    /// </summary>
    DateTimeOffset CreatedAt,

    /// <summary>
    /// Whether an admin user was created.
    /// </summary>
    bool AdminUserCreated,

    /// <summary>
    /// The admin user's ID (if created).
    /// </summary>
    string? AdminUserId,

    /// <summary>
    /// The admin user's email (if created).
    /// </summary>
    string? AdminEmail,

    /// <summary>
    /// Error message if admin user creation failed (null if successful or not attempted).
    /// </summary>
    string? AdminCreationError = null
);
