namespace NOIR.Application.Features.Auth.DTOs;

/// <summary>
/// Response returned after successful authentication.
/// </summary>
public sealed record AuthResponse(
    string UserId,
    string Email,
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt);

/// <summary>
/// Response for login that supports both direct login and tenant selection scenarios.
/// </summary>
public sealed record LoginResponse
{
    /// <summary>
    /// Whether login was successful (false if tenant selection is required).
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Whether the user needs to select a tenant before completing login.
    /// True when password matched for multiple tenants.
    /// </summary>
    public bool RequiresTenantSelection { get; init; }

    /// <summary>
    /// Available tenants for selection (only populated when RequiresTenantSelection is true).
    /// </summary>
    public IReadOnlyList<TenantOptionDto>? AvailableTenants { get; init; }

    /// <summary>
    /// Auth response (only populated on successful login).
    /// </summary>
    public AuthResponse? Auth { get; init; }

    /// <summary>
    /// Create a successful login response.
    /// </summary>
    public static LoginResponse Authenticated(AuthResponse auth) => new()
    {
        Success = true,
        RequiresTenantSelection = false,
        Auth = auth
    };

    /// <summary>
    /// Create a response requiring tenant selection.
    /// </summary>
    public static LoginResponse SelectTenant(IReadOnlyList<TenantOptionDto> tenants) => new()
    {
        Success = false,
        RequiresTenantSelection = true,
        AvailableTenants = tenants
    };
}

/// <summary>
/// Tenant option for selection during login.
/// </summary>
public sealed record TenantOptionDto(
    string? TenantId,
    string? Identifier,
    string Name);
