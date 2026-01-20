namespace NOIR.Domain.Common;

/// <summary>
/// Database-related constants for consistent schema configuration.
/// </summary>
public static class DatabaseConstants
{
    /// <summary>
    /// Maximum length for TenantId columns across all entities.
    /// Accommodates UUID with prefix (e.g., "tenant_550e8400-e29b-41d4-a716-446655440000").
    /// </summary>
    public const int TenantIdMaxLength = 64;

    /// <summary>
    /// Maximum length for UserId columns (matches ASP.NET Identity).
    /// </summary>
    public const int UserIdMaxLength = 450;
}
