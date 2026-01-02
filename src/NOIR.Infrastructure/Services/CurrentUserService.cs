namespace NOIR.Infrastructure.Services;

/// <summary>
/// Implementation of ICurrentUser that extracts user information from HTTP context.
///
/// IMPORTANT: This service is safe to use in non-HTTP contexts (e.g., Hangfire background jobs).
/// When HttpContext is null:
/// - UserId, Email, TenantId return null
/// - IsAuthenticated returns false
/// - Roles returns empty collection
/// - IsInRole returns false
///
/// Callers should handle null UserId/TenantId appropriately for background processing.
/// </summary>
public class CurrentUserService : ICurrentUser, IScopedService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMultiTenantContextAccessor<TenantInfo> _tenantContextAccessor;

    public CurrentUserService(
        IHttpContextAccessor httpContextAccessor,
        IMultiTenantContextAccessor<TenantInfo> tenantContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
        _tenantContextAccessor = tenantContextAccessor;
    }

    public string? UserId =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

    public string? Email =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email);

    public string? TenantId =>
        _tenantContextAccessor.MultiTenantContext?.TenantInfo?.Id;

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public IEnumerable<string> Roles =>
        _httpContextAccessor.HttpContext?.User?.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value) ?? [];

    public bool IsInRole(string role) =>
        _httpContextAccessor.HttpContext?.User?.IsInRole(role) ?? false;
}
