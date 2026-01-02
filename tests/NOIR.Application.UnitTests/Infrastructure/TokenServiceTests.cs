namespace NOIR.Application.UnitTests.Infrastructure;

/// <summary>
/// Unit tests for TokenService.
/// Tests JWT generation, validation, and refresh token handling.
/// </summary>
public class TokenServiceTests
{
    private readonly TokenService _sut;
    private readonly JwtSettings _jwtSettings;
    private readonly Mock<IDateTime> _dateTimeMock;

    public TokenServiceTests()
    {
        _jwtSettings = new JwtSettings
        {
            Secret = "ThisIsAVeryLongSecretKeyForTestingPurposesThatIsAtLeast32Characters",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpirationInMinutes = 60,
            RefreshTokenExpirationInDays = 7
        };

        _dateTimeMock = new Mock<IDateTime>();
        _dateTimeMock.Setup(x => x.UtcNow).Returns(DateTimeOffset.UtcNow);

        var options = Options.Create(_jwtSettings);
        _sut = new TokenService(options, _dateTimeMock.Object);
    }

    #region GenerateAccessToken Tests

    [Fact]
    public void GenerateAccessToken_ShouldReturnValidJwt()
    {
        // Act
        var token = _sut.GenerateAccessToken("user123", "test@example.com");

        // Assert
        token.Should().NotBeNullOrEmpty();
        token.Split('.').Should().HaveCount(3); // JWT has 3 parts
    }

    [Fact]
    public void GenerateAccessToken_WithTenantId_ShouldIncludeTenantClaim()
    {
        // Act
        var token = _sut.GenerateAccessToken("user123", "test@example.com", "tenant-1");

        // Assert
        token.Should().NotBeNullOrEmpty();

        // Decode and verify tenant claim exists
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        jwtToken.Claims.Should().Contain(c => c.Type == "tenant_id" && c.Value == "tenant-1");
    }

    [Fact]
    public void GenerateAccessToken_ShouldIncludeUserIdClaim()
    {
        // Act
        var token = _sut.GenerateAccessToken("user123", "test@example.com");

        // Assert
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        jwtToken.Claims.Should().Contain(c =>
            c.Type == ClaimTypes.NameIdentifier && c.Value == "user123");
    }

    [Fact]
    public void GenerateAccessToken_ShouldIncludeEmailClaim()
    {
        // Act
        var token = _sut.GenerateAccessToken("user123", "test@example.com");

        // Assert
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        jwtToken.Claims.Should().Contain(c =>
            c.Type == ClaimTypes.Email && c.Value == "test@example.com");
    }

    [Fact]
    public void GenerateAccessToken_ShouldSetCorrectExpiration()
    {
        // Arrange
        var now = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
        _dateTimeMock.Setup(x => x.UtcNow).Returns(now);

        // Act
        var token = _sut.GenerateAccessToken("user123", "test@example.com");

        // Assert
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        jwtToken.ValidTo.Should().BeCloseTo(
            now.AddMinutes(_jwtSettings.ExpirationInMinutes).UtcDateTime,
            TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void GenerateAccessToken_ShouldSetCorrectIssuerAndAudience()
    {
        // Act
        var token = _sut.GenerateAccessToken("user123", "test@example.com");

        // Assert
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        jwtToken.Issuer.Should().Be(_jwtSettings.Issuer);
        jwtToken.Audiences.Should().Contain(_jwtSettings.Audience);
    }

    #endregion

    #region GenerateRefreshToken Tests

    [Fact]
    public void GenerateRefreshToken_ShouldReturnNonEmptyString()
    {
        // Act
        var token = _sut.GenerateRefreshToken();

        // Assert
        token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnBase64String()
    {
        // Act
        var token = _sut.GenerateRefreshToken();

        // Assert
        var action = () => Convert.FromBase64String(token);
        action.Should().NotThrow();
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturn64BytesWhenDecoded()
    {
        // Act
        var token = _sut.GenerateRefreshToken();

        // Assert
        var decoded = Convert.FromBase64String(token);
        decoded.Should().HaveCount(64);
    }

    [Fact]
    public void GenerateRefreshToken_ShouldGenerateUniqueTokens()
    {
        // Act
        var tokens = Enumerable.Range(0, 10).Select(_ => _sut.GenerateRefreshToken()).ToList();

        // Assert
        tokens.Should().OnlyHaveUniqueItems();
    }

    #endregion

    #region GenerateTokenPair Tests

    [Fact]
    public void GenerateTokenPair_ShouldReturnBothTokens()
    {
        // Act
        var pair = _sut.GenerateTokenPair("user123", "test@example.com");

        // Assert
        pair.AccessToken.Should().NotBeNullOrEmpty();
        pair.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateTokenPair_ShouldSetCorrectExpiration()
    {
        // Arrange
        var now = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
        _dateTimeMock.Setup(x => x.UtcNow).Returns(now);

        // Act
        var pair = _sut.GenerateTokenPair("user123", "test@example.com");

        // Assert
        pair.ExpiresAt.Should().BeCloseTo(
            now.AddMinutes(_jwtSettings.ExpirationInMinutes),
            TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void GenerateTokenPair_WithTenantId_ShouldIncludeTenantInAccessToken()
    {
        // Act
        var pair = _sut.GenerateTokenPair("user123", "test@example.com", "tenant-1");

        // Assert
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(pair.AccessToken);
        jwtToken.Claims.Should().Contain(c => c.Type == "tenant_id" && c.Value == "tenant-1");
    }

    #endregion

    #region GetRefreshTokenExpiry Tests

    [Fact]
    public void GetRefreshTokenExpiry_ShouldReturnCorrectExpiration()
    {
        // Arrange
        var now = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
        _dateTimeMock.Setup(x => x.UtcNow).Returns(now);

        // Act
        var expiry = _sut.GetRefreshTokenExpiry();

        // Assert
        expiry.Should().Be(now.AddDays(_jwtSettings.RefreshTokenExpirationInDays));
    }

    #endregion

    #region IsRefreshTokenFormatValid Tests

    [Fact]
    public void IsRefreshTokenFormatValid_WithValidToken_ShouldReturnTrue()
    {
        // Arrange
        var validToken = _sut.GenerateRefreshToken();

        // Act
        var result = _sut.IsRefreshTokenFormatValid(validToken);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsRefreshTokenFormatValid_WithEmptyString_ShouldReturnFalse()
    {
        // Act
        var result = _sut.IsRefreshTokenFormatValid("");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsRefreshTokenFormatValid_WithNull_ShouldReturnFalse()
    {
        // Act
        var result = _sut.IsRefreshTokenFormatValid(null!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsRefreshTokenFormatValid_WithWhitespace_ShouldReturnFalse()
    {
        // Act
        var result = _sut.IsRefreshTokenFormatValid("   ");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsRefreshTokenFormatValid_WithInvalidBase64_ShouldReturnFalse()
    {
        // Act
        var result = _sut.IsRefreshTokenFormatValid("not-valid-base64!");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsRefreshTokenFormatValid_WithWrongLength_ShouldReturnFalse()
    {
        // Arrange - Valid base64 but wrong length (not 64 bytes)
        var shortToken = Convert.ToBase64String(new byte[32]);

        // Act
        var result = _sut.IsRefreshTokenFormatValid(shortToken);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetPrincipalFromExpiredToken Tests

    [Fact]
    public void GetPrincipalFromExpiredToken_WithValidToken_ShouldReturnPrincipal()
    {
        // Arrange
        var token = _sut.GenerateAccessToken("user123", "test@example.com");

        // Act
        var principal = _sut.GetPrincipalFromExpiredToken(token);

        // Assert
        principal.Should().NotBeNull();
        principal!.FindFirst(ClaimTypes.NameIdentifier)?.Value.Should().Be("user123");
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_WithInvalidToken_ShouldReturnNull()
    {
        // Act
        var principal = _sut.GetPrincipalFromExpiredToken("invalid-token");

        // Assert
        principal.Should().BeNull();
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_WithEmptyToken_ShouldReturnNull()
    {
        // Act
        var principal = _sut.GetPrincipalFromExpiredToken("");

        // Assert
        principal.Should().BeNull();
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_WithMalformedJwt_ShouldReturnNull()
    {
        // Act
        var principal = _sut.GetPrincipalFromExpiredToken("part1.part2.part3");

        // Assert
        principal.Should().BeNull();
    }

    #endregion
}
