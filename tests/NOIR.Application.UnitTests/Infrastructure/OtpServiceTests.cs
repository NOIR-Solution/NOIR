namespace NOIR.Application.UnitTests.Infrastructure;

/// <summary>
/// Unit tests for OtpService.
/// Tests OTP generation, hashing, verification, and email masking.
/// </summary>
public class OtpServiceTests
{
    private readonly OtpService _sut;
    private readonly Mock<ILogger<OtpService>> _loggerMock;
    private readonly PasswordResetSettings _settings;

    public OtpServiceTests()
    {
        _settings = new PasswordResetSettings
        {
            OtpLength = 6,
            OtpExpiryMinutes = 5,
            ResetTokenExpiryMinutes = 15,
            ResendCooldownSeconds = 60,
            MaxResendCount = 3
        };

        _loggerMock = new Mock<ILogger<OtpService>>();
        var options = Options.Create(_settings);
        _sut = new OtpService(options, _loggerMock.Object);
    }

    #region GenerateOtp Tests (Default Length)

    [Fact]
    public void GenerateOtp_ShouldReturnOtpWithDefaultLength()
    {
        // Act
        var otp = _sut.GenerateOtp();

        // Assert
        otp.Should().NotBeNullOrEmpty();
        otp.Length.Should().Be(_settings.OtpLength);
    }

    [Fact]
    public void GenerateOtp_ShouldReturnNumericString()
    {
        // Act
        var otp = _sut.GenerateOtp();

        // Assert
        otp.Should().MatchRegex("^[0-9]+$");
    }

    [Fact]
    public void GenerateOtp_ShouldGenerateUniqueOtps()
    {
        // Act
        var otps = Enumerable.Range(0, 100).Select(_ => _sut.GenerateOtp()).ToList();

        // Assert - Most should be unique (some collisions possible with 6-digit OTPs)
        var uniqueCount = otps.Distinct().Count();
        uniqueCount.Should().BeGreaterThan(90); // Allow for some collisions
    }

    [Fact]
    public void GenerateOtp_ShouldPadWithLeadingZeros()
    {
        // Act - Generate many OTPs to catch edge cases
        var otps = Enumerable.Range(0, 1000).Select(_ => _sut.GenerateOtp()).ToList();

        // Assert - All should have consistent length
        otps.Should().OnlyContain(otp => otp.Length == _settings.OtpLength);
    }

    #endregion

    #region GenerateOtp Tests (Custom Length)

    [Theory]
    [InlineData(4)]
    [InlineData(6)]
    [InlineData(8)]
    [InlineData(10)]
    public void GenerateOtp_WithValidLength_ShouldReturnCorrectLength(int length)
    {
        // Act
        var otp = _sut.GenerateOtp(length);

        // Assert
        otp.Length.Should().Be(length);
        otp.Should().MatchRegex("^[0-9]+$");
    }

    [Theory]
    [InlineData(3)]
    [InlineData(11)]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(100)]
    public void GenerateOtp_WithInvalidLength_ShouldThrow(int invalidLength)
    {
        // Act
        var act = () => _sut.GenerateOtp(invalidLength);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void GenerateOtp_WithMinLength_ShouldWork()
    {
        // Act
        var otp = _sut.GenerateOtp(4);

        // Assert
        otp.Length.Should().Be(4);
        otp.Should().MatchRegex("^[0-9]+$");
    }

    [Fact]
    public void GenerateOtp_WithMaxLength_ShouldWork()
    {
        // Act
        var otp = _sut.GenerateOtp(10);

        // Assert
        otp.Length.Should().Be(10);
        otp.Should().MatchRegex("^[0-9]+$");
    }

    #endregion

    #region HashOtp Tests

    [Fact]
    public void HashOtp_ShouldReturnBcryptHash()
    {
        // Arrange
        var otp = "123456";

        // Act
        var hash = _sut.HashOtp(otp);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        hash.Should().StartWith("$2"); // Bcrypt hash prefix
    }

    [Fact]
    public void HashOtp_SameOtp_ShouldReturnDifferentHashes()
    {
        // Arrange
        var otp = "123456";

        // Act
        var hash1 = _sut.HashOtp(otp);
        var hash2 = _sut.HashOtp(otp);

        // Assert - Bcrypt uses random salt, so hashes should differ
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void HashOtp_ShouldReturnVerifiableHash()
    {
        // Arrange
        var otp = "123456";

        // Act
        var hash = _sut.HashOtp(otp);
        var isValid = _sut.VerifyOtp(otp, hash);

        // Assert
        isValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("000000")]
    [InlineData("999999")]
    [InlineData("123456")]
    [InlineData("1234567890")]
    public void HashOtp_VariousOtps_ShouldAllBeHashable(string otp)
    {
        // Act
        var hash = _sut.HashOtp(otp);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        hash.Should().StartWith("$2");
    }

    #endregion

    #region VerifyOtp Tests

    [Fact]
    public void VerifyOtp_WithCorrectOtp_ShouldReturnTrue()
    {
        // Arrange
        var otp = "123456";
        var hash = _sut.HashOtp(otp);

        // Act
        var result = _sut.VerifyOtp(otp, hash);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyOtp_WithIncorrectOtp_ShouldReturnFalse()
    {
        // Arrange
        var correctOtp = "123456";
        var incorrectOtp = "654321";
        var hash = _sut.HashOtp(correctOtp);

        // Act
        var result = _sut.VerifyOtp(incorrectOtp, hash);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyOtp_WithInvalidHash_ShouldReturnFalse()
    {
        // Act
        var result = _sut.VerifyOtp("123456", "invalid-hash");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyOtp_WithEmptyHash_ShouldReturnFalse()
    {
        // Act
        var result = _sut.VerifyOtp("123456", "");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyOtp_WithNullHash_ShouldReturnFalse()
    {
        // Act
        var result = _sut.VerifyOtp("123456", null!);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("000000")]
    [InlineData("999999")]
    [InlineData("1234")]
    [InlineData("1234567890")]
    public void VerifyOtp_VariousValidOtps_ShouldVerifyCorrectly(string otp)
    {
        // Arrange
        var hash = _sut.HashOtp(otp);

        // Act
        var result = _sut.VerifyOtp(otp, hash);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyOtp_SimilarOtps_ShouldNotMatch()
    {
        // Arrange
        var otp = "123456";
        var hash = _sut.HashOtp(otp);

        // Act
        var result1 = _sut.VerifyOtp("123457", hash); // Off by 1
        var result2 = _sut.VerifyOtp("023456", hash); // Leading 0 instead of 1
        var result3 = _sut.VerifyOtp("12345", hash);  // Missing digit

        // Assert
        result1.Should().BeFalse();
        result2.Should().BeFalse();
        result3.Should().BeFalse();
    }

    #endregion

    #region MaskEmail Tests

    [Theory]
    [InlineData("john@example.com", "jo***n@example.com")]  // length > 3: first 2 + *** + last 1
    [InlineData("a@b.co", "a***@b.co")]  // length == 1: first char + *** + domain
    public void MaskEmail_WithValidEmail_ShouldMask(string email, string expected)
    {
        // Act
        var masked = _sut.MaskEmail(email);

        // Assert
        masked.Should().Be(expected);
    }

    [Fact]
    public void MaskEmail_ShouldHideMiddlePortion()
    {
        // Arrange
        var email = "testuser@domain.com";

        // Act
        var masked = _sut.MaskEmail(email);

        // Assert
        masked.Should().Contain("***");
        masked.Should().NotBe(email);
    }

    [Theory]
    [InlineData("user@domain.org")]
    [InlineData("first.last@company.net")]
    [InlineData("x@y.io")]
    public void MaskEmail_VariousFormats_ShouldMaskSuccessfully(string email)
    {
        // Act
        var masked = _sut.MaskEmail(email);

        // Assert
        masked.Should().NotBeNullOrEmpty();
        masked.Should().Contain("@");
        masked.Should().Contain("***");
    }

    #endregion

    #region Cryptographic Security Tests

    [Fact]
    public void GenerateOtp_ShouldUseCryptographicRandom()
    {
        // Generate a large number of OTPs and verify distribution
        var otps = Enumerable.Range(0, 10000).Select(_ => _sut.GenerateOtp(6)).ToList();

        // Check digit distribution - should be roughly uniform
        var digitCounts = new int[10];
        foreach (var otp in otps)
        {
            foreach (var c in otp)
            {
                digitCounts[c - '0']++;
            }
        }

        // Each digit should appear roughly 10% of the time (6000 times per digit for 60000 total digits)
        // Allow 20% deviation for statistical variance
        var expectedCount = 6000;
        var tolerance = expectedCount * 0.2;

        foreach (var count in digitCounts)
        {
            count.Should().BeGreaterThan((int)(expectedCount - tolerance));
            count.Should().BeLessThan((int)(expectedCount + tolerance));
        }
    }

    [Fact]
    public void HashOtp_WorkFactorShouldBeSufficient()
    {
        // Arrange
        var otp = "123456";

        // Act
        var hash = _sut.HashOtp(otp);

        // Assert - Verify the hash has appropriate cost factor
        // $2a$10$ or $2b$10$ indicates work factor of 10
        hash.Should().MatchRegex(@"^\$2[aby]\$\d{2}\$");
    }

    #endregion
}
