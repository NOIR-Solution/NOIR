namespace NOIR.Application.Specifications.RefreshTokens;

/// <summary>
/// Specification to find expired or revoked refresh tokens older than a cutoff date.
/// Used for cleanup operations.
/// </summary>
public sealed class ExpiredRefreshTokensSpec : Specification<RefreshToken>
{
    public ExpiredRefreshTokensSpec(DateTimeOffset cutoffDate)
    {
        Query.Where(t => t.ExpiresAt < DateTimeOffset.UtcNow || t.RevokedAt.HasValue)
             .Where(t => t.CreatedAt < cutoffDate)
             .IgnoreQueryFilters() // Include soft-deleted tokens
             .TagWith("ExpiredRefreshTokens");
    }
}
