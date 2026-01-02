namespace NOIR.Application.Specifications.RefreshTokens;

/// <summary>
/// Specification to find all active refresh tokens for a user.
/// Active means not revoked and not expired.
/// </summary>
public class ActiveRefreshTokensByUserSpec : Specification<RefreshToken>
{
    public ActiveRefreshTokensByUserSpec(string userId)
    {
        Query.Where(t => t.UserId == userId)
             .Where(t => t.RevokedAt == null)
             .Where(t => t.ExpiresAt > DateTimeOffset.UtcNow)
             .TagWith("ActiveRefreshTokensByUser");
    }
}
