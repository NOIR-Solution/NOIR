namespace NOIR.Application.Specifications.RefreshTokens;

/// <summary>
/// Specification to find the oldest active refresh token for a user.
/// Used for session limit enforcement.
/// </summary>
public sealed class OldestActiveRefreshTokenSpec : Specification<RefreshToken>
{
    public OldestActiveRefreshTokenSpec(string userId)
    {
        Query.Where(t => t.UserId == userId)
             .Where(t => !t.RevokedAt.HasValue)
             .Where(t => t.ExpiresAt > DateTimeOffset.UtcNow)
             .OrderBy(t => t.CreatedAt)
             .Take(1)
             .TagWith("OldestActiveRefreshToken");
    }
}
