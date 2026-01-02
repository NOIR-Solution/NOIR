namespace NOIR.Application.UnitTests.Common;

/// <summary>
/// Unit tests for TokenPair record.
/// </summary>
public class TokenPairTests
{
    [Fact]
    public void TokenPair_Create_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var accessToken = "access-token-value";
        var refreshToken = "refresh-token-value";
        var expiresAt = DateTimeOffset.UtcNow.AddHours(1);

        // Act
        var tokenPair = new TokenPair(accessToken, refreshToken, expiresAt);

        // Assert
        tokenPair.AccessToken.Should().Be(accessToken);
        tokenPair.RefreshToken.Should().Be(refreshToken);
        tokenPair.ExpiresAt.Should().Be(expiresAt);
    }

    [Fact]
    public void TokenPair_Equality_ShouldWorkCorrectly()
    {
        // Arrange
        var expiresAt = DateTimeOffset.UtcNow.AddHours(1);
        var tokenPair1 = new TokenPair("access", "refresh", expiresAt);
        var tokenPair2 = new TokenPair("access", "refresh", expiresAt);

        // Act & Assert
        tokenPair1.Should().Be(tokenPair2);
        (tokenPair1 == tokenPair2).Should().BeTrue();
    }

    [Fact]
    public void TokenPair_Inequality_ShouldWorkCorrectly()
    {
        // Arrange
        var expiresAt = DateTimeOffset.UtcNow.AddHours(1);
        var tokenPair1 = new TokenPair("access1", "refresh", expiresAt);
        var tokenPair2 = new TokenPair("access2", "refresh", expiresAt);

        // Act & Assert
        tokenPair1.Should().NotBe(tokenPair2);
        (tokenPair1 != tokenPair2).Should().BeTrue();
    }

    [Fact]
    public void TokenPair_Deconstruction_ShouldWorkCorrectly()
    {
        // Arrange
        var expiresAt = DateTimeOffset.UtcNow.AddHours(1);
        var tokenPair = new TokenPair("access", "refresh", expiresAt);

        // Act
        var (accessToken, refreshToken, expires) = tokenPair;

        // Assert
        accessToken.Should().Be("access");
        refreshToken.Should().Be("refresh");
        expires.Should().Be(expiresAt);
    }
}
