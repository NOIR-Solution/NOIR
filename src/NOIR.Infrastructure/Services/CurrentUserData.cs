namespace NOIR.Infrastructure.Services;

/// <summary>
/// Complete user data loaded from database for the current request.
/// Cached in HttpContext.Items by CurrentUserLoaderMiddleware.
/// </summary>
public record CurrentUserData(
    string Id,
    string Email,
    string FirstName,
    string LastName,
    string? DisplayName,
    string FullName,
    string? AvatarUrl,
    string? PhoneNumber,
    IEnumerable<string> Roles,
    string? TenantId,
    bool IsActive)
{
    /// <summary>
    /// HttpContext.Items key for caching user data.
    /// </summary>
    public const string CacheKey = "CurrentUserData";
}
