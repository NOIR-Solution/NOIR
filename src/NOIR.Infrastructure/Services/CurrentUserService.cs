namespace NOIR.Infrastructure.Services;

using NOIR.Domain.Common;

/// <summary>
/// Implementation of ICurrentUser that reads from HttpContext.Items cache.
/// User data is loaded once per request by CurrentUserLoaderMiddleware.
///
/// IMPORTANT: This service is safe to use in non-HTTP contexts (e.g., Hangfire background jobs).
/// When HttpContext is null or user data not loaded:
/// - UserId, Email, TenantId return null or fallback from JWT claims
/// - IsAuthenticated returns false
/// - Roles returns empty collection
/// - IsInRole returns false
/// - IsPlatformAdmin returns false
///
/// Callers should handle null UserId/TenantId appropriately for background processing.
/// </summary>
public class CurrentUserService : ICurrentUser, IScopedService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMultiTenantContextAccessor<Tenant> _tenantContextAccessor;

    public CurrentUserService(
        IHttpContextAccessor httpContextAccessor,
        IMultiTenantContextAccessor<Tenant> tenantContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
        _tenantContextAccessor = tenantContextAccessor;
    }

    private CurrentUserData? GetCachedUserData() =>
        _httpContextAccessor.HttpContext?.Items.TryGetValue(
            CurrentUserData.CacheKey, out var data) == true
            ? data as CurrentUserData
            : null;

    public string? UserId =>
        GetCachedUserData()?.Id
        ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

    public string? Email =>
        GetCachedUserData()?.Email
        ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email);

    public string? TenantId =>
        _tenantContextAccessor.MultiTenantContext?.TenantInfo?.Id;

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public IEnumerable<string> Roles =>
        GetCachedUserData()?.Roles ?? [];

    public bool IsInRole(string role) =>
        Roles.Contains(role, StringComparer.OrdinalIgnoreCase);

    public bool IsPlatformAdmin => IsInRole(Domain.Common.Roles.PlatformAdmin);
}
