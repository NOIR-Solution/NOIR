namespace NOIR.Domain.UnitTests.Entities;

/// <summary>
/// Unit tests for PasswordResetOtp entity.
/// Tests factory methods, state transitions, cooldown logic, and computed properties.
/// </summary>
public class PasswordResetOtpTests
{
    private const string ValidEmail = "test@example.com";
    private const string ValidOtpHash = "$2a$10$somevalidbcrypthashvalue";
    private const string ValidSessionToken = "secure-session-token-12345";
    private const int DefaultExpiryMinutes = 5;

    #region Create Factory Tests

    [Fact]
    public void Create_WithRequiredParameters_ShouldCreateValidOtp()
    {
        // Act
        var otp = PasswordResetOtp.Create(
            ValidEmail,
            ValidOtpHash,
            ValidSessionToken,
            DefaultExpiryMinutes);

        // Assert
        otp.Should().NotBeNull();
        otp.Id.Should().NotBe(Guid.Empty);
        otp.Email.Should().Be(ValidEmail.ToLowerInvariant());
        otp.OtpHash.Should().Be(ValidOtpHash);
        otp.SessionToken.Should().Be(ValidSessionToken);
    }

    [Theory]
    [InlineData("TEST@EXAMPLE.COM", "test@example.com")]
    [InlineData("User@Domain.ORG", "user@domain.org")]
    [InlineData("MixedCase@Email.Net", "mixedcase@email.net")]
    public void Create_ShouldNormalizeEmailToLowercase(string input, string expected)
    {
        // Act
        var otp = PasswordResetOtp.Create(input, ValidOtpHash, ValidSessionToken, DefaultExpiryMinutes);

        // Assert
        otp.Email.Should().Be(expected);
    }

    [Fact]
    public void Create_ShouldSetExpirationDate()
    {
        // Arrange
        var beforeCreate = DateTimeOffset.UtcNow;

        // Act
        var otp = PasswordResetOtp.Create(ValidEmail, ValidOtpHash, ValidSessionToken, DefaultExpiryMinutes);

        // Assert
        var afterCreate = DateTimeOffset.UtcNow;
        var expectedMin = beforeCreate.AddMinutes(DefaultExpiryMinutes);
        var expectedMax = afterCreate.AddMinutes(DefaultExpiryMinutes);

        otp.ExpiresAt.Should().BeOnOrAfter(expectedMin).And.BeOnOrBefore(expectedMax);
    }

    [Fact]
    public void Create_WithUserId_ShouldSetUserId()
    {
        // Arrange
        var userId = "user-123";

        // Act
        var otp = PasswordResetOtp.Create(
            ValidEmail,
            ValidOtpHash,
            ValidSessionToken,
            DefaultExpiryMinutes,
            userId: userId);

        // Assert
        otp.UserId.Should().Be(userId);
    }

    [Fact]
    public void Create_WithoutUserId_ShouldHaveNullUserId()
    {
        // Act
        var otp = PasswordResetOtp.Create(ValidEmail, ValidOtpHash, ValidSessionToken, DefaultExpiryMinutes);

        // Assert
        otp.UserId.Should().BeNull();
    }

    [Fact]
    public void Create_WithTenantId_ShouldSetTenantId()
    {
        // Arrange
        var tenantId = "tenant-abc";

        // Act
        var otp = PasswordResetOtp.Create(
            ValidEmail,
            ValidOtpHash,
            ValidSessionToken,
            DefaultExpiryMinutes,
            tenantId: tenantId);

        // Assert
        otp.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void Create_WithIpAddress_ShouldSetCreatedByIp()
    {
        // Arrange
        var ipAddress = "192.168.1.100";

        // Act
        var otp = PasswordResetOtp.Create(
            ValidEmail,
            ValidOtpHash,
            ValidSessionToken,
            DefaultExpiryMinutes,
            ipAddress: ipAddress);

        // Assert
        otp.CreatedByIp.Should().Be(ipAddress);
    }

    [Fact]
    public void Create_ShouldInitializeDefaults()
    {
        // Act
        var otp = PasswordResetOtp.Create(ValidEmail, ValidOtpHash, ValidSessionToken, DefaultExpiryMinutes);

        // Assert
        otp.IsUsed.Should().BeFalse();
        otp.UsedAt.Should().BeNull();
        otp.AttemptCount.Should().Be(0);
        otp.ResendCount.Should().Be(0);
        otp.LastResendAt.Should().BeNull();
        otp.ResetToken.Should().BeNull();
        otp.ResetTokenExpiresAt.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrEmptySessionToken_ShouldThrow(string? invalidSessionToken)
    {
        // Act
        var act = () => PasswordResetOtp.Create(
            ValidEmail,
            ValidOtpHash,
            invalidSessionToken!,
            DefaultExpiryMinutes);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(15)]
    [InlineData(60)]
    public void Create_VariousExpiryMinutes_ShouldSetCorrectExpiry(int minutes)
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var otp = PasswordResetOtp.Create(ValidEmail, ValidOtpHash, ValidSessionToken, minutes);

        // Assert
        var expectedMin = before.AddMinutes(minutes);
        otp.ExpiresAt.Should().BeCloseTo(expectedMin, TimeSpan.FromSeconds(5));
    }

    #endregion

    #region IsExpired Tests

    [Fact]
    public void IsExpired_FreshOtp_ShouldReturnFalse()
    {
        // Arrange
        var otp = PasswordResetOtp.Create(ValidEmail, ValidOtpHash, ValidSessionToken, DefaultExpiryMinutes);

        // Act & Assert
        otp.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_ZeroExpiryMinutes_ShouldReturnTrue()
    {
        // Arrange - Create with 0 minutes expiry
        var otp = PasswordResetOtp.Create(ValidEmail, ValidOtpHash, ValidSessionToken, 0);

        // Wait a tiny moment
        Thread.Sleep(10);

        // Act & Assert
        otp.IsExpired.Should().BeTrue();
    }

    #endregion

    #region IsValid Tests

    [Fact]
    public void IsValid_FreshOtp_ShouldReturnTrue()
    {
        // Arrange
        var otp = PasswordResetOtp.Create(ValidEmail, ValidOtpHash, ValidSessionToken, DefaultExpiryMinutes);

        // Act & Assert
        otp.IsValid.Should().BeTrue();
    }

    [Fact]
    public void IsValid_UsedOtp_ShouldReturnFalse()
    {
        // Arrange
        var otp = PasswordResetOtp.Create(ValidEmail, ValidOtpHash, ValidSessionToken, DefaultExpiryMinutes);
        otp.MarkAsUsed("reset-token-123", 15);

        // Act & Assert
        otp.IsValid.Should().BeFalse();
    }

    [Fact]
    public void IsValid_ExpiredOtp_ShouldReturnFalse()
    {
        // Arrange - Create with 0 minutes expiry
        var otp = PasswordResetOtp.Create(ValidEmail, ValidOtpHash, ValidSessionToken, 0);

        // Wait for expiry
        Thread.Sleep(10);

        // Act & Assert
        otp.IsValid.Should().BeFalse();
    }

    #endregion

    #region RecordFailedAttempt Tests

    [Fact]
    public void RecordFailedAttempt_ShouldIncrementAttemptCount()
    {
        // Arrange
        var otp = PasswordResetOtp.Create(ValidEmail, ValidOtpHash, ValidSessionToken, DefaultExpiryMinutes);
        otp.AttemptCount.Should().Be(0);

        // Act
        otp.RecordFailedAttempt();

        // Assert
        otp.AttemptCount.Should().Be(1);
    }

    [Fact]
    public void RecordFailedAttempt_MultipleCalls_ShouldAccumulate()
    {
        // Arrange
        var otp = PasswordResetOtp.Create(ValidEmail, ValidOtpHash, ValidSessionToken, DefaultExpiryMinutes);

        // Act
        otp.RecordFailedAttempt();
        otp.RecordFailedAttempt();
        otp.RecordFailedAttempt();

        // Assert
        otp.AttemptCount.Should().Be(3);
    }

    #endregion

    #region MarkAsUsed Tests

    [Fact]
    public void MarkAsUsed_ShouldSetIsUsedTrue()
    {
        // Arrange
        var otp = PasswordResetOtp.Create(ValidEmail, ValidOtpHash, ValidSessionToken, DefaultExpiryMinutes);

        // Act
        otp.MarkAsUsed("reset-token-123", 15);

        // Assert
        otp.IsUsed.Should().BeTrue();
    }

    [Fact]
    public void MarkAsUsed_ShouldSetUsedAtTimestamp()
    {
        // Arrange
        var otp = PasswordResetOtp.Create(ValidEmail, ValidOtpHash, ValidSessionToken, DefaultExpiryMinutes);
        var beforeUse = DateTimeOffset.UtcNow;

        // Act
        otp.MarkAsUsed("reset-token-123", 15);

        // Assert
        var afterUse = DateTimeOffset.UtcNow;
        otp.UsedAt.Should().NotBeNull();
        otp.UsedAt.Should().BeOnOrAfter(beforeUse).And.BeOnOrBefore(afterUse);
    }

    [Fact]
    public void MarkAsUsed_ShouldSetResetToken()
    {
        // Arrange
        var otp = PasswordResetOtp.Create(ValidEmail, ValidOtpHash, ValidSessionToken, DefaultExpiryMinutes);
        var resetToken = "secure-reset-token-xyz";

        // Act
        otp.MarkAsUsed(resetToken, 15);

        // Assert
        otp.ResetToken.Should().Be(resetToken);
    }

    [Fact]
    public void MarkAsUsed_ShouldSetResetTokenExpiry()
    {
        // Arrange
        var otp = PasswordResetOtp.Create(ValidEmail, ValidOtpHash, ValidSessionToken, DefaultExpiryMinutes);
        var resetTokenExpiryMinutes = 15;
        var beforeUse = DateTimeOffset.UtcNow;

        // Act
        otp.MarkAsUsed("reset-token-123", resetTokenExpiryMinutes);

        // Assert
        var afterUse = DateTimeOffset.UtcNow;
        var expectedMin = beforeUse.AddMinutes(resetTokenExpiryMinutes);
        var expectedMax = afterUse.AddMinutes(resetTokenExpiryMinutes);

        otp.ResetTokenExpiresAt.Should().NotBeNull();
        otp.ResetTokenExpiresAt.Should().BeOnOrAfter(expectedMin).And.BeOnOrBefore(expectedMax);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MarkAsUsed_WithNullOrEmptyResetToken_ShouldThrow(string? invalidResetToken)
    {
        // Arrange
        var otp = PasswordResetOtp.Create(ValidEmail, ValidOtpHash, ValidSessionToken, DefaultExpiryMinutes);

        // Act
        var act = () => otp.MarkAsUsed(invalidResetToken!, 15);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region IsResetTokenValid Tests

    [Fact]
    public void IsResetTokenValid_FreshOtp_ShouldReturnFalse()
    {
        // Arrange
        var otp = PasswordResetOtp.Create(ValidEmail, ValidOtpHash, ValidSessionToken, DefaultExpiryMinutes);

        // Act & Assert
        otp.IsResetTokenValid.Should().BeFalse();
    }

    [Fact]
    public void IsResetTokenValid_AfterMarkAsUsed_ShouldReturnTrue()
    {
        // Arrange
        var otp = PasswordResetOtp.Create(ValidEmail, ValidOtpHash, ValidSessionToken, DefaultExpiryMinutes);
        otp.MarkAsUsed("reset-token-123", 15);

        // Act & Assert
        otp.IsResetTokenValid.Should().BeTrue();
    }

    [Fact]
    public void IsResetTokenValid_AfterInvalidation_ShouldReturnFalse()
    {
        // Arrange
        var otp = PasswordResetOtp.Create(ValidEmail, ValidOtpHash, ValidSessionToken, DefaultExpiryMinutes);
        otp.MarkAsUsed("reset-token-123", 15);
        otp.InvalidateResetToken();

        // Act & Assert
        otp.IsResetTokenValid.Should().BeFalse();
    }

    [Fact]
    public void IsResetTokenValid_ExpiredResetToken_ShouldReturnFalse()
    {
        // Arrange - Create with 0 minutes reset token expiry
        var otp = PasswordResetOtp.Create(ValidEmail, ValidOtpHash, ValidSessionToken, DefaultExpiryMinutes);
        otp.MarkAsUsed("reset-token-123", 0);

        // Wait for reset token to expire
        Thread.Sleep(10);

        // Act & Assert
        otp.IsResetTokenValid.Should().BeFalse();
    }

    #endregion

    #region InvalidateResetToken Tests

    [Fact]
    public void InvalidateResetToken_ShouldClearResetToken()
    {
        // Arrange
        var otp = PasswordResetOtp.Create(ValidEmail, ValidOtpHash, ValidSessionToken, DefaultExpiryMinutes);
        otp.MarkAsUsed("reset-token-123", 15);

        // Act
        otp.InvalidateResetToken();

        // Assert
        otp.ResetToken.Should().BeNull();
    }

    [Fact]
    public void InvalidateResetToken_ShouldClearResetTokenExpiresAt()
    {
        // Arrange
        var otp = PasswordResetOtp.Create(ValidEmail, ValidOtpHash, ValidSessionToken, DefaultExpiryMinutes);
        otp.MarkAsUsed("reset-token-123", 15);

        // Act
        otp.InvalidateResetToken();

        // Assert
        otp.ResetTokenExpiresAt.Should().BeNull();
    }

    [Fact]
    public void InvalidateResetToken_WhenNoResetToken_ShouldNotThrow()
    {
        // Arrange
        var otp = PasswordResetOtp.Create(ValidEmail, ValidOtpHash, ValidSessionToken, DefaultExpiryMinutes);

        // Act
        var act = () => otp.InvalidateResetToken();

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region CanResend Tests

    [Fact]
    public void CanResend_FreshOtp_ShouldReturnTrue()
    {
        // Arrange
        var otp = PasswordResetOtp.Create(ValidEmail, ValidOtpHash, ValidSessionToken, DefaultExpiryMinutes);

        // Act
        var canResend = otp.CanResend(cooldownSeconds: 60, maxResendCount: 3);

        // Assert
        canResend.Should().BeTrue();
    }

    [Fact]
    public void CanResend_MaxResendCountReached_ShouldReturnFalse()
    {
        // Arrange
        var otp = PasswordResetOtp.Create(ValidEmail, ValidOtpHash, ValidSessionToken, DefaultExpiryMinutes);

        // Resend max times
        otp.Resend("hash1", DefaultExpiryMinutes);
        otp.Resend("hash2", DefaultExpiryMinutes);
        otp.Resend("hash3", DefaultExpiryMinutes);

        // Act
        var canResend = otp.CanResend(cooldownSeconds: 0, maxResendCount: 3);

        // Assert
        canResend.Should().BeFalse();
    }

    [Fact]
    public void CanResend_WithinCooldown_ShouldReturnFalse()
    {
        // Arrange
        var otp = PasswordResetOtp.Create(ValidEmail, ValidOtpHash, ValidSessionToken, DefaultExpiryMinutes);
        otp.Resend("new-hash", DefaultExpiryMinutes);

        // Act - Check immediately (within cooldown)
        var canResend = otp.CanResend(cooldownSeconds: 60, maxResendCount: 3);

        // Assert
        canResend.Should().BeFalse();
    }

    [Fact]
    public void CanResend_AfterCooldown_ShouldReturnTrue()
    {
        // Arrange
        var otp = PasswordResetOtp.Create(ValidEmail, ValidOtpHash, ValidSessionToken, DefaultExpiryMinutes);
        otp.Resend("new-hash", DefaultExpiryMinutes);

        // Act - Check with 0 cooldown (effectively after cooldown)
        var canResend = otp.CanResend(cooldownSeconds: 0, maxResendCount: 3);

        // Assert
        canResend.Should().BeTrue();
    }

    #endregion

    #region GetRemainingCooldownSeconds Tests

    [Fact]
    public void GetRemainingCooldownSeconds_FreshOtp_ShouldReturnZero()
    {
        // Arrange
        var otp = PasswordResetOtp.Create(ValidEmail, ValidOtpHash, ValidSessionToken, DefaultExpiryMinutes);

        // Act
        var remaining = otp.GetRemainingCooldownSeconds(cooldownSeconds: 60);

        // Assert
        remaining.Should().Be(0);
    }

    [Fact]
    public void GetRemainingCooldownSeconds_JustResent_ShouldReturnApproximateCooldown()
    {
        // Arrange
        var otp = PasswordResetOtp.Create(ValidEmail, ValidOtpHash, ValidSessionToken, DefaultExpiryMinutes);
        otp.Resend("new-hash", DefaultExpiryMinutes);

        // Act
        var remaining = otp.GetRemainingCooldownSeconds(cooldownSeconds: 60);

        // Assert
        remaining.Should().BeCloseTo(60, 2); // Allow 2 seconds tolerance
    }

    [Fact]
    public void GetRemainingCooldownSeconds_AfterCooldownExpired_ShouldReturnZero()
    {
        // Arrange
        var otp = PasswordResetOtp.Create(ValidEmail, ValidOtpHash, ValidSessionToken, DefaultExpiryMinutes);
        otp.Resend("new-hash", DefaultExpiryMinutes);

        // Act - Use 0 cooldown (effectively after cooldown)
        var remaining = otp.GetRemainingCooldownSeconds(cooldownSeconds: 0);

        // Assert
        remaining.Should().Be(0);
    }

    #endregion

    #region Resend Tests

    [Fact]
    public void Resend_ShouldUpdateOtpHash()
    {
        // Arrange
        var otp = PasswordResetOtp.Create(ValidEmail, ValidOtpHash, ValidSessionToken, DefaultExpiryMinutes);
        var newHash = "new-bcrypt-hash-value";

        // Act
        otp.Resend(newHash, DefaultExpiryMinutes);

        // Assert
        otp.OtpHash.Should().Be(newHash);
    }

    [Fact]
    public void Resend_ShouldIncrementResendCount()
    {
        // Arrange
        var otp = PasswordResetOtp.Create(ValidEmail, ValidOtpHash, ValidSessionToken, DefaultExpiryMinutes);
        otp.ResendCount.Should().Be(0);

        // Act
        otp.Resend("new-hash", DefaultExpiryMinutes);

        // Assert
        otp.ResendCount.Should().Be(1);
    }

    [Fact]
    public void Resend_ShouldUpdateLastResendAt()
    {
        // Arrange
        var otp = PasswordResetOtp.Create(ValidEmail, ValidOtpHash, ValidSessionToken, DefaultExpiryMinutes);
        var beforeResend = DateTimeOffset.UtcNow;

        // Act
        otp.Resend("new-hash", DefaultExpiryMinutes);

        // Assert
        var afterResend = DateTimeOffset.UtcNow;
        otp.LastResendAt.Should().NotBeNull();
        otp.LastResendAt.Should().BeOnOrAfter(beforeResend).And.BeOnOrBefore(afterResend);
    }

    [Fact]
    public void Resend_ShouldExtendExpiry()
    {
        // Arrange
        var otp = PasswordResetOtp.Create(ValidEmail, ValidOtpHash, ValidSessionToken, 1);
        var originalExpiry = otp.ExpiresAt;

        // Wait a moment
        Thread.Sleep(100);
        var beforeResend = DateTimeOffset.UtcNow;

        // Act
        otp.Resend("new-hash", 10);

        // Assert
        var expectedMin = beforeResend.AddMinutes(10);
        otp.ExpiresAt.Should().BeOnOrAfter(expectedMin);
        otp.ExpiresAt.Should().BeAfter(originalExpiry);
    }

    [Fact]
    public void Resend_ShouldResetAttemptCount()
    {
        // Arrange
        var otp = PasswordResetOtp.Create(ValidEmail, ValidOtpHash, ValidSessionToken, DefaultExpiryMinutes);
        otp.RecordFailedAttempt();
        otp.RecordFailedAttempt();
        otp.AttemptCount.Should().Be(2);

        // Act
        otp.Resend("new-hash", DefaultExpiryMinutes);

        // Assert
        otp.AttemptCount.Should().Be(0);
    }

    [Fact]
    public void Resend_MultipleTimes_ShouldAccumulateResendCount()
    {
        // Arrange
        var otp = PasswordResetOtp.Create(ValidEmail, ValidOtpHash, ValidSessionToken, DefaultExpiryMinutes);

        // Act
        otp.Resend("hash1", DefaultExpiryMinutes);
        otp.Resend("hash2", DefaultExpiryMinutes);
        otp.Resend("hash3", DefaultExpiryMinutes);

        // Assert
        otp.ResendCount.Should().Be(3);
    }

    #endregion

    #region Session Token Binding Tests

    [Fact]
    public void Create_ShouldBindToSessionToken()
    {
        // Arrange & Act
        var otp = PasswordResetOtp.Create(ValidEmail, ValidOtpHash, ValidSessionToken, DefaultExpiryMinutes);

        // Assert
        otp.SessionToken.Should().Be(ValidSessionToken);
        otp.SessionToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Create_DifferentSessionTokens_ShouldBeDifferentOtps()
    {
        // Arrange & Act
        var otp1 = PasswordResetOtp.Create(ValidEmail, ValidOtpHash, "session-1", DefaultExpiryMinutes);
        var otp2 = PasswordResetOtp.Create(ValidEmail, ValidOtpHash, "session-2", DefaultExpiryMinutes);

        // Assert
        otp1.SessionToken.Should().NotBe(otp2.SessionToken);
        otp1.Id.Should().NotBe(otp2.Id);
    }

    #endregion
}
