namespace NOIR.Application.Specifications.RefreshTokens;

/// <summary>
/// Specification to find a refresh token by its value.
/// </summary>
public class RefreshTokenByValueSpec : Specification<RefreshToken>
{
    public RefreshTokenByValueSpec(string tokenValue)
    {
        Query.Where(t => t.Token == tokenValue)
             .TagWith("RefreshTokenByValue");
    }
}
