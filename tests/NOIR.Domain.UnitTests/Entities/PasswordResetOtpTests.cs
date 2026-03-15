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
        otp.ShouldNotBeNull();
        otp.Id.ShouldNotBe(Guid.Empty);
        otp.Email.ShouldBe(ValidEmail.ToLowerInvariant());
        otp.OtpHash.ShouldBe(ValidOtpHash);
        otp.SessionToken.ShouldBe(ValidSessionToken);
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
        otp.Email.ShouldBe(expected);
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

        otp.ExpiresAt.ShouldBeGreaterThanOrEqualTo(expectedMin);


        otp.ExpiresAt.ShouldBeLessThanOrEqualTo(expectedMax);
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
        otp.UserId.ShouldBe(userId);
    }

    [Fact]
    public void Create_WithoutUserId_ShouldHaveNullUserId()
    {
        // Act
        var otp = PasswordResetOtp.Create(ValidEmail, ValidOtpHash, ValidSessionToken, DefaultExpiryMinutes);

        // Assert
        otp.UserId.ShouldBeNull();
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
        otp.TenantId.ShouldBe(tenantId);
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
        otp.CreatedByIp.ShouldBe(ipAddress);
    }

    [Fact]
    public void Create_ShouldInitializeDefaults()
    {
        // Act
        var otp = PasswordResetOtp.Create(ValidEmail, ValidOtpHash, ValidSessionToken, DefaultExpiryMinutes);

        // Assert
        otp.IsUsed.ShouldBeFalse();
        otp.UsedAt.ShouldBeNull();
        otp.AttemptCount.ShouldBe(0);
        otp.ResendCount.ShouldBe(0);
        otp.LastResendAt.ShouldBeNull();
        otp.ResetToken.ShouldBeNull();
        otp.ResetTokenExpiresAt.ShouldBeNull();
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
        Should.Throw<ArgumentException>(act);
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
        otp.ExpiresAt.ShouldBe(expectedMin, TimeSpan.FromSeconds(5));
    }

    #endregion

    #region IsExpired Tests

    [Fact]
    public void IsExpired_FreshOtp_ShouldReturnFalse()
    {
        // Arrange
        var otp = PasswordResetOtp.Create(ValidEmail, ValidOtpHash, ValidSessionToken, DefaultExpiryMinutes);

        // Act & Assert
        otp.IsExpired.ShouldBeFalse();
    }

    [Fact]
    public void IsExpired_ZeroExpiryMinutes_ShouldReturnTrue()
    {
        // Arrange - Create with 0 minutes expiry
        var otp = PasswordResetOtp.Create(ValidEmail, ValidOtpHash, ValidSessionToken, 0);

        // Wait a tiny moment
        Thread.Sleep(10);

        // Act & Assert
        otp.IsExpired.ShouldBeTrue();
    }

    #endregion

    #region IsValid Tests

    [Fact]
    public void IsValid_FreshOtp_ShouldReturnTrue()
    {
        // Arrange
        var otp = PasswordResetOtp.Create(ValidEmail, ValidOtpHash, ValidSessionToken, DefaultExpiryMinutes);

        // Act & Assert
        otp.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void IsValid_UsedOtp_ShouldReturnFalse()
    {
        // Arrange
        var otp = PasswordResetOtp.Create(ValidEmail, ValidOtpHash, ValidSessionToken, DefaultExpiryMinutes);
        otp.MarkAsUsed("reset-token-123", 15);

        // Act & Assert
        otp.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void IsValid_ExpiredOtp_ShouldReturnFalse()
    {
        // Arrange - Create with 0 minutes expiry
        var otp = PasswordResetOtp.Create(ValidEmail, ValidOtpHash, ValidSessionToken, 0);

        // Wait for expiry
        Thread.Sleep(10);

        // Act & Assert
        otp.IsValid.ShouldBeFalse();
    }

    #endregion

    #region RecordFailedAttempt Tests

    [Fact]
    public void RecordFailedAttempt_ShouldIncrementAttemptCount()
    {
        // Arrange
        var otp = PasswordResetOtp.Create(ValidEmail, ValidOtpHash, ValidSessionToken, DefaultExpiryMinutes);
        otp.AttemptCount.ShouldBe(0);

        // Act
        otp.RecordFailedAttempt();

        // Assert
        otp.AttemptCount.ShouldBe(1);
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
        otp.AttemptCount.ShouldBe(3);
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
        otp.IsUsed.ShouldBeTrue();
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
        otp.UsedAt.ShouldNotBeNull();
        otp.UsedAt!.Value.ShouldBeGreaterThanOrEqualTo(beforeUse);

        otp.UsedAt!.Value.ShouldBeLessThanOrEqualTo(afterUse);
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
        otp.ResetToken.ShouldBe(resetToken);
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

        otp.ResetTokenExpiresAt.ShouldNotBeNull();
        otp.ResetTokenExpiresAt!.Value.ShouldBeGreaterThanOrEqualTo(expectedMin);

        otp.ResetTokenExpiresAt!.Value.ShouldBeLessThanOrEqualTo(expectedMax);
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
        Should.Throw<ArgumentException>(act);
    }

    #endregion

    #region IsResetTokenValid Tests

    [Fact]
    public void IsResetTokenValid_FreshOtp_ShouldReturnFalse()
    {
        // Arrange
        var otp = PasswordResetOtp.Create(ValidEmail, ValidOtpHash, ValidSessionToken, DefaultExpiryMinutes);

        // Act & Assert
        otp.IsResetTokenValid.ShouldBeFalse();
    }

    [Fact]
    public void IsResetTokenValid_AfterMarkAsUsed_ShouldReturnTrue()
    {
        // Arrange
        var otp = PasswordResetOtp.Create(ValidEmail, ValidOtpHash, ValidSessionToken, DefaultExpiryMinutes);
        otp.MarkAsUsed("reset-token-123", 15);

        // Act & Assert
        otp.IsResetTokenValid.ShouldBeTrue();
    }

    [Fact]
    public void IsResetTokenValid_AfterInvalidation_ShouldReturnFalse()
    {
        // Arrange
        var otp = PasswordResetOtp.Create(ValidEmail, ValidOtpHash, ValidSessionToken, DefaultExpiryMinutes);
        otp.MarkAsUsed("reset-token-123", 15);
        otp.InvalidateResetToken();

        // Act & Assert
        otp.IsResetTokenValid.ShouldBeFalse();
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
        otp.IsResetTokenValid.ShouldBeFalse();
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
        otp.ResetToken.ShouldBeNull();
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
        otp.ResetTokenExpiresAt.ShouldBeNull();
    }

    [Fact]
    public void InvalidateResetToken_WhenNoResetToken_ShouldNotThrow()
    {
        // Arrange
        var otp = PasswordResetOtp.Create(ValidEmail, ValidOtpHash, ValidSessionToken, DefaultExpiryMinutes);

        // Act
        var act = () => otp.InvalidateResetToken();

        // Assert
        act.ShouldNotThrow();
    }

    #endregion

    #region CanResend Tests

    [Fact]
    public void CanResend_FreshOtp_ShouldReturnFalse_WhenWithinCooldown()
    {
        // Arrange
        var otp = PasswordResetOtp.Create(ValidEmail, ValidOtpHash, ValidSessionToken, DefaultExpiryMinutes);

        // Act
        // Fresh OTP should be in cooldown based on CreatedAt
        var canResend = otp.CanResend(cooldownSeconds: 60, maxResendCount: 3);

        // Assert
        // Cannot resend immediately after creation - must wait for cooldown
        canResend.ShouldBeFalse();
    }

    [Fact]
    public void CanResend_FreshOtp_ShouldReturnTrue_WhenCooldownIsZero()
    {
        // Arrange
        var otp = PasswordResetOtp.Create(ValidEmail, ValidOtpHash, ValidSessionToken, DefaultExpiryMinutes);

        // Act
        // With 0 cooldown, should be able to resend immediately
        var canResend = otp.CanResend(cooldownSeconds: 0, maxResendCount: 3);

        // Assert
        canResend.ShouldBeTrue();
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
        canResend.ShouldBeFalse();
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
        canResend.ShouldBeFalse();
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
        canResend.ShouldBeTrue();
    }

    #endregion

    #region GetRemainingCooldownSeconds Tests

    [Fact]
    public void GetRemainingCooldownSeconds_FreshOtp_ShouldReturnApproximateCooldown()
    {
        // Arrange
        var otp = PasswordResetOtp.Create(ValidEmail, ValidOtpHash, ValidSessionToken, DefaultExpiryMinutes);

        // Act
        // Fresh OTP uses CreatedAt as reference time, so cooldown should be near the full value
        var remaining = otp.GetRemainingCooldownSeconds(cooldownSeconds: 60);

        // Assert
        // Should return approximately 60 seconds (allow 2 seconds tolerance for test execution time)
        Math.Abs(remaining - 60).ShouldBeLessThanOrEqualTo(2);
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
        Math.Abs(remaining - 60).ShouldBeLessThanOrEqualTo(2); // Allow 2 seconds tolerance
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
        remaining.ShouldBe(0);
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
        otp.OtpHash.ShouldBe(newHash);
    }

    [Fact]
    public void Resend_ShouldIncrementResendCount()
    {
        // Arrange
        var otp = PasswordResetOtp.Create(ValidEmail, ValidOtpHash, ValidSessionToken, DefaultExpiryMinutes);
        otp.ResendCount.ShouldBe(0);

        // Act
        otp.Resend("new-hash", DefaultExpiryMinutes);

        // Assert
        otp.ResendCount.ShouldBe(1);
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
        otp.LastResendAt.ShouldNotBeNull();
        otp.LastResendAt!.Value.ShouldBeGreaterThanOrEqualTo(beforeResend);

        otp.LastResendAt!.Value.ShouldBeLessThanOrEqualTo(afterResend);
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
        otp.ExpiresAt.ShouldBeGreaterThanOrEqualTo(expectedMin);
        otp.ExpiresAt.ShouldBeGreaterThan(originalExpiry);
    }

    [Fact]
    public void Resend_ShouldResetAttemptCount()
    {
        // Arrange
        var otp = PasswordResetOtp.Create(ValidEmail, ValidOtpHash, ValidSessionToken, DefaultExpiryMinutes);
        otp.RecordFailedAttempt();
        otp.RecordFailedAttempt();
        otp.AttemptCount.ShouldBe(2);

        // Act
        otp.Resend("new-hash", DefaultExpiryMinutes);

        // Assert
        otp.AttemptCount.ShouldBe(0);
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
        otp.ResendCount.ShouldBe(3);
    }

    #endregion

    #region Session Token Binding Tests

    [Fact]
    public void Create_ShouldBindToSessionToken()
    {
        // Arrange & Act
        var otp = PasswordResetOtp.Create(ValidEmail, ValidOtpHash, ValidSessionToken, DefaultExpiryMinutes);

        // Assert
        otp.SessionToken.ShouldBe(ValidSessionToken);
        otp.SessionToken.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Create_DifferentSessionTokens_ShouldBeDifferentOtps()
    {
        // Arrange & Act
        var otp1 = PasswordResetOtp.Create(ValidEmail, ValidOtpHash, "session-1", DefaultExpiryMinutes);
        var otp2 = PasswordResetOtp.Create(ValidEmail, ValidOtpHash, "session-2", DefaultExpiryMinutes);

        // Assert
        otp1.SessionToken.ShouldNotBe(otp2.SessionToken);
        otp1.Id.ShouldNotBe(otp2.Id);
    }

    #endregion
}
