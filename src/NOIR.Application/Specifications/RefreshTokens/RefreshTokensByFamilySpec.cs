namespace NOIR.Application.Specifications.RefreshTokens;

/// <summary>
/// Specification to find all active refresh tokens in a token family.
/// Used for token theft detection and family revocation.
/// </summary>
public sealed class RefreshTokensByFamilySpec : Specification<RefreshToken>
{
    public RefreshTokensByFamilySpec(Guid tokenFamily)
    {
        Query.Where(t => t.TokenFamily == tokenFamily)
             .Where(t => !t.RevokedAt.HasValue)
             .TagWith("RefreshTokensByFamily");
    }
}
