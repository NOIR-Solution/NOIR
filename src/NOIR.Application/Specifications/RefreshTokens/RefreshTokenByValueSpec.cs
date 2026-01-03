namespace NOIR.Application.Specifications.RefreshTokens;

/// <summary>
/// Specification to find a refresh token by its value.
/// </summary>
public sealed class RefreshTokenByValueSpec : Specification<RefreshToken>
{
    public RefreshTokenByValueSpec(string token)
    {
        Query.Where(t => t.Token == token)
             .AsTracking()  // Required for entity modification (rotation, revocation)
             .TagWith("RefreshTokenByValue");
    }
}
