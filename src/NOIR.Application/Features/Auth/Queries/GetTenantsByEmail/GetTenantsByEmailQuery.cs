namespace NOIR.Application.Features.Auth.Queries.GetTenantsByEmail;

/// <summary>
/// Query to find all tenants where a user with the given email exists.
/// Used in progressive login flow - step 1: email entry → tenant detection.
/// </summary>
/// <param name="Email">The email address to search for.</param>
public sealed record GetTenantsByEmailQuery(string Email);

/// <summary>
/// Response containing information about tenants where the email exists.
/// </summary>
public sealed record GetTenantsByEmailResponse(
    /// <summary>
    /// The email address that was searched.
    /// </summary>
    string Email,

    /// <summary>
    /// List of tenants where a user with this email exists.
    /// </summary>
    IReadOnlyList<TenantOption> Tenants,

    /// <summary>
    /// Whether the email was found in exactly one tenant (auto-select scenario).
    /// </summary>
    bool SingleTenant,

    /// <summary>
    /// If SingleTenant is true, this is the tenant ID to use.
    /// </summary>
    string? AutoSelectedTenantId,

    /// <summary>
    /// If SingleTenant is true, this is the tenant identifier (slug).
    /// </summary>
    string? AutoSelectedTenantIdentifier);

/// <summary>
/// Represents a tenant option in the login flow.
/// </summary>
public sealed record TenantOption(
    /// <summary>
    /// The tenant's internal ID.
    /// </summary>
    string? TenantId,

    /// <summary>
    /// The tenant's identifier (slug used in URLs).
    /// </summary>
    string? Identifier,

    /// <summary>
    /// The tenant's display name.
    /// </summary>
    string Name);
