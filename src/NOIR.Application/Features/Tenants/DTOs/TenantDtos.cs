namespace NOIR.Application.Features.Tenants.DTOs;

/// <summary>
/// Full tenant data transfer object with all details.
/// Note: Id is string to match Finbuckle's TenantInfo (stored as GUID string).
/// </summary>
public sealed record TenantDto(
    string? Id,
    string? Identifier,
    string? Name,
    string? Domain,
    string? Description,
    string? Note,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ModifiedAt);

/// <summary>
/// Simplified tenant for listings.
/// Note: Id is string to match Finbuckle's TenantInfo (stored as GUID string).
/// </summary>
public sealed record TenantListDto(
    string? Id,
    string? Identifier,
    string? Name,
    string? Domain,
    bool IsActive,
    DateTimeOffset CreatedAt);

/// <summary>
/// Public tenant info for login dropdown (minimal exposure).
/// </summary>
public sealed record TenantPublicDto(
    string? Identifier,
    string? Name);
