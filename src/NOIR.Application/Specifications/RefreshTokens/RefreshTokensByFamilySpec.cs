namespace NOIR.Application.Specifications.RefreshTokens;

/// <summary>
/// Specification to find all refresh tokens in a token family.
/// Used for detecting token reuse and revoking entire chains.
/// </summary>
public class RefreshTokensByFamilySpec : Specification<RefreshToken>
{
    public RefreshTokensByFamilySpec(Guid tokenFamily)
    {
        Query.Where(t => t.TokenFamily == tokenFamily)
             .OrderByDescending(t => t.CreatedAt)
             .TagWith("RefreshTokensByFamily");
    }
}
