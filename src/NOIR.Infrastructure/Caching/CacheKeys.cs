namespace NOIR.Infrastructure.Caching;

/// <summary>
/// Centralized cache key definitions.
/// All keys follow the pattern: "{prefix}:{identifier}" for easy invalidation by prefix.
/// Tags follow the pattern: "{entity}:{id}" or "{category}" for bulk invalidation.
/// </summary>
public static class CacheKeys
{
    // Prefixes for different cache categories
    private const string PermissionPrefix = "perm";
    private const string UserPrefix = "user";
    private const string RolePrefix = "role";
    private const string TenantPrefix = "tenant";
    private const string BlogPrefix = "blog";
    private const string SettingsPrefix = "settings";

    #region Permission Keys

    /// <summary>
    /// Cache key for user permissions.
    /// Tags: ["permissions", "user:{userId}"]
    /// </summary>
    public static string UserPermissions(string userId) => $"{PermissionPrefix}:user:{userId}";

    /// <summary>
    /// Cache key for role permissions.
    /// Tags: ["permissions", "role:{roleId}"]
    /// </summary>
    public static string RolePermissions(string roleId) => $"{PermissionPrefix}:role:{roleId}";

    #endregion

    #region User Keys

    /// <summary>
    /// Cache key for user profile by ID.
    /// Tags: ["user:{userId}"]
    /// </summary>
    public static string UserProfile(string userId) => $"{UserPrefix}:profile:{userId}";

    /// <summary>
    /// Cache key for user by ID.
    /// Tags: ["user:{userId}"]
    /// </summary>
    public static string UserById(string userId) => $"{UserPrefix}:id:{userId}";

    /// <summary>
    /// Cache key for user by email.
    /// Tags: ["user:{userId}"]
    /// </summary>
    public static string UserByEmail(string email) => $"{UserPrefix}:email:{email.ToLowerInvariant()}";

    #endregion

    #region Role Keys

    /// <summary>
    /// Cache key for role by ID.
    /// Tags: ["role:{roleId}"]
    /// </summary>
    public static string RoleById(string roleId) => $"{RolePrefix}:id:{roleId}";

    /// <summary>
    /// Cache key for all roles list.
    /// Tags: ["roles"]
    /// </summary>
    public static string AllRoles() => $"{RolePrefix}:all";

    #endregion

    #region Tenant Keys

    /// <summary>
    /// Cache key for tenant by ID.
    /// Tags: ["tenant:{tenantId}"]
    /// </summary>
    public static string TenantById(string tenantId) => $"{TenantPrefix}:id:{tenantId}";

    /// <summary>
    /// Cache key for tenant settings.
    /// Tags: ["tenant:{tenantId}", "settings"]
    /// </summary>
    public static string TenantSettings(string tenantId) => $"{SettingsPrefix}:tenant:{tenantId}";

    #endregion

    #region Blog Keys

    /// <summary>
    /// Cache key for published blog post by slug.
    /// Tags: ["blog", "post:{postId}"]
    /// </summary>
    public static string PostBySlug(string slug) => $"{BlogPrefix}:post:slug:{slug}";

    /// <summary>
    /// Cache key for blog post by ID.
    /// Tags: ["blog", "post:{postId}"]
    /// </summary>
    public static string PostById(Guid postId) => $"{BlogPrefix}:post:id:{postId}";

    /// <summary>
    /// Cache key for blog posts list (paginated).
    /// Tags: ["blog", "posts-list"]
    /// </summary>
    public static string PostsList(int page, int pageSize, string? categorySlug = null)
    {
        var key = $"{BlogPrefix}:posts:p{page}:s{pageSize}";
        if (!string.IsNullOrEmpty(categorySlug))
            key += $":c{categorySlug}";
        return key;
    }

    /// <summary>
    /// Cache key for blog categories list.
    /// Tags: ["blog", "categories"]
    /// </summary>
    public static string BlogCategories() => $"{BlogPrefix}:categories";

    /// <summary>
    /// Cache key for blog tags list.
    /// Tags: ["blog", "tags"]
    /// </summary>
    public static string BlogTags() => $"{BlogPrefix}:tags";

    /// <summary>
    /// Cache key for RSS feed.
    /// Tags: ["blog", "feed"]
    /// </summary>
    public static string RssFeed() => $"{BlogPrefix}:feed:rss";

    /// <summary>
    /// Cache key for sitemap.
    /// Tags: ["blog", "sitemap"]
    /// </summary>
    public static string Sitemap() => $"{BlogPrefix}:sitemap";

    #endregion

    #region Email Template Keys

    /// <summary>
    /// Cache key for email template by name and tenant.
    /// Tags: ["email_template", "tenant:{tenantId}"]
    /// </summary>
    public static string EmailTemplate(string templateName, string? tenantId = null)
    {
        var tenantKey = tenantId ?? "platform";
        return $"email_template:{templateName}:{tenantKey}";
    }

    #endregion

    #region SMTP Settings Keys

    /// <summary>
    /// Cache key for SMTP settings by tenant.
    /// Tags: ["smtp", "tenant:{tenantId}"]
    /// </summary>
    public static string SmtpSettings(string? tenantId = null)
    {
        var tenantKey = tenantId ?? "platform";
        return $"smtp_settings:{tenantKey}";
    }

    #endregion

    #region Tag Helpers

    /// <summary>
    /// Get tags for user-related cache entries.
    /// </summary>
    public static string[] UserTags(string userId) => [$"user:{userId}"];

    /// <summary>
    /// Get tags for permission-related cache entries.
    /// </summary>
    public static string[] PermissionTags(string userId) => ["permissions", $"user:{userId}"];

    /// <summary>
    /// Get tags for role-related cache entries.
    /// </summary>
    public static string[] RoleTags(string roleId) => [$"role:{roleId}", "roles"];

    /// <summary>
    /// Get tags for blog post cache entries.
    /// </summary>
    public static string[] PostTags(Guid postId) => ["blog", $"post:{postId}"];

    /// <summary>
    /// Get tags for blog list cache entries.
    /// </summary>
    public static string[] BlogListTags() => ["blog", "posts-list"];

    #endregion
}
