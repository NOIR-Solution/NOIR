namespace NOIR.Infrastructure.Identity;

/// <summary>
/// JWT token generation and validation service.
/// </summary>
public class TokenService : ITokenService, IScopedService
{
    private readonly JwtSettings _jwtSettings;
    private readonly IDateTime _dateTime;

    public TokenService(IOptions<JwtSettings> jwtSettings, IDateTime dateTime)
    {
        _jwtSettings = jwtSettings.Value;
        _dateTime = dateTime;
    }

    public string GenerateAccessToken(string userId, string email, string? tenantId = null)
    {
        // Minimal JWT: only userId, email, tenantId
        // Roles and permissions are queried from database on each request
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Add tenant claim for multi-tenancy (used by Finbuckle's ClaimStrategy)
        if (!string.IsNullOrEmpty(tenantId))
        {
            claims.Add(new Claim("tenant_id", tenantId));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: _dateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationInMinutes).UtcDateTime,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    /// <summary>
    /// Generates both access and refresh tokens as a pair.
    /// Use this to avoid duplicating token generation logic in handlers.
    /// </summary>
    public TokenPair GenerateTokenPair(string userId, string email, string? tenantId = null)
    {
        var accessToken = GenerateAccessToken(userId, email, tenantId);
        var refreshToken = GenerateRefreshToken();
        var expiresAt = _dateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationInMinutes);

        return new TokenPair(accessToken, refreshToken, expiresAt);
    }

    /// <summary>
    /// Gets the expiration time for a new refresh token.
    /// </summary>
    public DateTimeOffset GetRefreshTokenExpiry()
    {
        return _dateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationInDays);
    }

    /// <summary>
    /// Validates that the refresh token has a valid format (non-empty, base64 encoded).
    /// Note: Actual token verification (matching stored token, expiry) is done in the handler.
    /// </summary>
    public bool IsRefreshTokenFormatValid(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        // Validate base64 format (refresh tokens are base64 encoded)
        try
        {
            var decoded = Convert.FromBase64String(token);
            return decoded.Length == 64; // Our refresh tokens are always 64 bytes
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = false, // Don't validate lifetime - token may be expired
            ValidateIssuerSigningKey = true,
            ValidIssuer = _jwtSettings.Issuer,
            ValidAudience = _jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret))
        };

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            return principal;
        }
        catch
        {
            return null;
        }
    }
}
