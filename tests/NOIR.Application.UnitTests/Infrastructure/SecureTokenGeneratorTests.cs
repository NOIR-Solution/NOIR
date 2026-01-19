namespace NOIR.Application.UnitTests.Infrastructure;

/// <summary>
/// Unit tests for SecureTokenGenerator.
/// Tests cryptographically secure token generation.
/// </summary>
public class SecureTokenGeneratorTests
{
    private readonly SecureTokenGenerator _sut;

    public SecureTokenGeneratorTests()
    {
        _sut = new SecureTokenGenerator();
    }

    #region GenerateToken Tests - Default Length

    [Fact]
    public void GenerateToken_WithDefaultLength_ShouldReturnNonEmptyString()
    {
        // Act
        var token = _sut.GenerateToken();

        // Assert
        token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateToken_WithDefaultLength_ShouldReturnUrlSafeString()
    {
        // Act
        var token = _sut.GenerateToken();

        // Assert
        token.Should().NotContain("+");
        token.Should().NotContain("/");
        token.Should().NotContain("=");
    }

    [Fact]
    public void GenerateToken_WithDefaultLength_ShouldBeBase64UrlSafe()
    {
        // Act
        var token = _sut.GenerateToken();

        // Assert
        // URL-safe base64 uses - and _ instead of + and /
        token.Should().MatchRegex("^[A-Za-z0-9_-]+$");
    }

    [Fact]
    public void GenerateToken_MultipleCalls_ShouldReturnUniqueTokens()
    {
        // Act
        var tokens = Enumerable.Range(0, 1000)
            .Select(_ => _sut.GenerateToken())
            .ToList();

        // Assert
        tokens.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void GenerateToken_WithDefaultLength_ShouldHaveExpectedLength()
    {
        // 32 bytes = 256 bits
        // Base64 encodes 6 bits per character
        // 256 / 6 = 42.67, rounded up = 43 characters (without padding)
        // But the trimmed output varies slightly due to padding removal

        // Act
        var token = _sut.GenerateToken();

        // Assert
        // 32 bytes in base64 without padding is 43 characters
        token.Length.Should().BeInRange(42, 44);
    }

    #endregion

    #region GenerateToken Tests - Custom Length

    [Theory]
    [InlineData(16)]
    [InlineData(32)]
    [InlineData(64)]
    [InlineData(128)]
    public void GenerateToken_WithValidByteLength_ShouldReturnToken(int byteLength)
    {
        // Act
        var token = _sut.GenerateToken(byteLength);

        // Assert
        token.Should().NotBeNullOrEmpty();
        token.Should().MatchRegex("^[A-Za-z0-9_-]+$");
    }

    [Fact]
    public void GenerateToken_WithMinimumLength_ShouldWork()
    {
        // Act
        var token = _sut.GenerateToken(16); // Minimum allowed

        // Assert
        token.Should().NotBeNullOrEmpty();
        // 16 bytes in base64 without padding is approximately 22 characters
        token.Length.Should().BeInRange(21, 23);
    }

    [Fact]
    public void GenerateToken_WithLargeLength_ShouldWork()
    {
        // Act
        var token = _sut.GenerateToken(256);

        // Assert
        token.Should().NotBeNullOrEmpty();
        // 256 bytes in base64 is approximately 342 characters
        token.Length.Should().BeGreaterThan(300);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(8)]
    [InlineData(15)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void GenerateToken_WithByteLengthLessThan16_ShouldThrow(int invalidByteLength)
    {
        // Act
        var act = () => _sut.GenerateToken(invalidByteLength);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("byteLength")
            .WithMessage("*at least 16 bytes*");
    }

    #endregion

    #region Cryptographic Security Tests

    [Fact]
    public void GenerateToken_ShouldHaveHighEntropy()
    {
        // Generate a large sample of tokens
        var tokens = Enumerable.Range(0, 100)
            .Select(_ => _sut.GenerateToken())
            .ToList();

        // Calculate character distribution
        var allChars = string.Join("", tokens);
        var charFrequency = allChars.GroupBy(c => c)
            .ToDictionary(g => g.Key, g => g.Count());

        // For truly random base64, characters should be roughly evenly distributed
        // We'll check that no single character dominates (appears more than 10% of total)
        var maxFrequency = charFrequency.Values.Max();
        var maxPercentage = (double)maxFrequency / allChars.Length;

        maxPercentage.Should().BeLessThan(0.1, "character distribution should be roughly uniform");
    }

    [Fact]
    public void GenerateToken_ConsecutiveCalls_ShouldNotBeSequential()
    {
        // Act
        var token1 = _sut.GenerateToken();
        var token2 = _sut.GenerateToken();

        // Assert
        // Tokens should not share a common prefix (indicating sequential generation)
        var commonPrefixLength = GetCommonPrefixLength(token1, token2);
        commonPrefixLength.Should().BeLessThan(5, "consecutive tokens should not share significant prefixes");
    }

    [Fact]
    public void GenerateToken_ShouldBeDifferentEachTime()
    {
        // Arrange & Act
        var token1 = _sut.GenerateToken();
        var token2 = _sut.GenerateToken();
        var token3 = _sut.GenerateToken();

        // Assert
        token1.Should().NotBe(token2);
        token2.Should().NotBe(token3);
        token1.Should().NotBe(token3);
    }

    [Fact]
    public void GenerateToken_LargeScale_ShouldHaveNoCollisions()
    {
        // Act - Generate 10,000 tokens to test for collisions
        var tokens = Enumerable.Range(0, 10000)
            .Select(_ => _sut.GenerateToken())
            .ToList();

        // Assert
        tokens.Should().OnlyHaveUniqueItems();
    }

    #endregion

    #region Service Registration Tests

    [Fact]
    public void Service_ShouldImplementISecureTokenGenerator()
    {
        // Assert
        _sut.Should().BeAssignableTo<ISecureTokenGenerator>();
    }

    [Fact]
    public void Service_ShouldImplementISingletonService()
    {
        // Assert
        _sut.Should().BeAssignableTo<ISingletonService>();
    }

    [Fact]
    public void Service_ShouldBeSealed()
    {
        // Assert
        typeof(SecureTokenGenerator).IsSealed.Should().BeTrue();
    }

    [Fact]
    public void Constructor_ShouldCreateInstance()
    {
        // Act
        var generator = new SecureTokenGenerator();

        // Assert
        generator.Should().NotBeNull();
    }

    #endregion

    #region URL Safety Tests

    [Fact]
    public void GenerateToken_ShouldBeUrlSafe()
    {
        // Generate many tokens to ensure URL safety
        for (int i = 0; i < 100; i++)
        {
            // Act
            var token = _sut.GenerateToken();

            // Assert
            // Token should be safe for use in URLs without encoding
            Uri.IsWellFormedUriString($"https://example.com/token/{token}", UriKind.Absolute)
                .Should().BeTrue($"Token '{token}' should be URL-safe");
        }
    }

    [Fact]
    public void GenerateToken_ShouldNotRequireUrlEncoding()
    {
        // Act
        var token = _sut.GenerateToken();
        var urlEncoded = Uri.EscapeDataString(token);

        // Assert
        token.Should().Be(urlEncoded, "token should not change when URL encoded");
    }

    #endregion

    #region Helper Methods

    private static int GetCommonPrefixLength(string a, string b)
    {
        int i = 0;
        int minLength = Math.Min(a.Length, b.Length);

        while (i < minLength && a[i] == b[i])
        {
            i++;
        }

        return i;
    }

    #endregion
}
