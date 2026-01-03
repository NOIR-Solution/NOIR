namespace NOIR.Application.Specifications.RefreshTokens;

/// <summary>
/// Specification to find active (non-revoked, non-expired) refresh tokens for a user.
/// </summary>
public sealed class ActiveRefreshTokensByUserSpec : Specification<RefreshToken>
{
    public ActiveRefreshTokensByUserSpec(string userId)
    {
        Query.Where(t => t.UserId == userId)
             .Where(t => !t.RevokedAt.HasValue)
             .Where(t => t.ExpiresAt > DateTimeOffset.UtcNow)
             .OrderByDescending(t => t.CreatedAt)
             .TagWith("ActiveRefreshTokensByUser");
    }
}
