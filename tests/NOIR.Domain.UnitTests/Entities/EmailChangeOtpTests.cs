namespace NOIR.Domain.UnitTests.Entities;

/// <summary>
/// Unit tests for EmailChangeOtp entity.
/// Tests factory methods, state transitions, cooldown logic, and computed properties.
/// </summary>
public class EmailChangeOtpTests
{
    private const string ValidUserId = "user-123";
    private const string ValidCurrentEmail = "current@example.com";
    private const string ValidNewEmail = "new@example.com";
    private const string ValidOtpHash = "$2a$10$somevalidbcrypthashvalue";
    private const string ValidSessionToken = "secure-session-token-12345";
    private const int DefaultExpiryMinutes = 15;

    #region Create Factory Tests

    [Fact]
    public void Create_WithRequiredParameters_ShouldCreateValidOtp()
    {
        // Act
        var otp = EmailChangeOtp.Create(
            ValidUserId,
            ValidCurrentEmail,
            ValidNewEmail,
            ValidOtpHash,
            ValidSessionToken,
            DefaultExpiryMinutes);

        // Assert
        otp.Should().NotBeNull();
        otp.Id.Should().NotBe(Guid.Empty);
        otp.UserId.Should().Be(ValidUserId);
        otp.CurrentEmail.Should().Be(ValidCurrentEmail.ToLowerInvariant());
        otp.NewEmail.Should().Be(ValidNewEmail.ToLowerInvariant());
        otp.OtpHash.Should().Be(ValidOtpHash);
        otp.SessionToken.Should().Be(ValidSessionToken);
    }

    [Theory]
    [InlineData("CURRENT@EXAMPLE.COM", "NEW@EXAMPLE.COM", "current@example.com", "new@example.com")]
    [InlineData("User@Domain.ORG", "User2@Domain.NET", "user@domain.org", "user2@domain.net")]
    public void Create_ShouldNormalizeEmailsToLowercase(
        string currentEmail, string newEmail, string expectedCurrent, string expectedNew)
    {
        // Act
        var otp = EmailChangeOtp.Create(
            ValidUserId,
            currentEmail,
            newEmail,
            ValidOtpHash,
            ValidSessionToken,
            DefaultExpiryMinutes);

        // Assert
        otp.CurrentEmail.Should().Be(expectedCurrent);
        otp.NewEmail.Should().Be(expectedNew);
    }

    [Fact]
    public void Create_ShouldSetExpirationDate()
    {
        // Arrange
        var beforeCreate = DateTimeOffset.UtcNow;

        // Act
        var otp = EmailChangeOtp.Create(
            ValidUserId,
            ValidCurrentEmail,
            ValidNewEmail,
            ValidOtpHash,
            ValidSessionToken,
            DefaultExpiryMinutes);

        // Assert
        var afterCreate = DateTimeOffset.UtcNow;
        var expectedMin = beforeCreate.AddMinutes(DefaultExpiryMinutes);
        var expectedMax = afterCreate.AddMinutes(DefaultExpiryMinutes);

        otp.ExpiresAt.Should().BeOnOrAfter(expectedMin).And.BeOnOrBefore(expectedMax);
    }

    [Fact]
    public void Create_WithTenantId_ShouldSetTenantId()
    {
        // Arrange
        var tenantId = "tenant-abc";

        // Act
        var otp = EmailChangeOtp.Create(
            ValidUserId,
            ValidCurrentEmail,
            ValidNewEmail,
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
        var otp = EmailChangeOtp.Create(
            ValidUserId,
            ValidCurrentEmail,
            ValidNewEmail,
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
        var otp = EmailChangeOtp.Create(
            ValidUserId,
            ValidCurrentEmail,
            ValidNewEmail,
            ValidOtpHash,
            ValidSessionToken,
            DefaultExpiryMinutes);

        // Assert
        otp.IsUsed.Should().BeFalse();
        otp.UsedAt.Should().BeNull();
        otp.AttemptCount.Should().Be(0);
        otp.ResendCount.Should().Be(0);
        otp.LastResendAt.Should().BeNull();
    }

    [Theory]
    [InlineData(null, "current@example.com", "new@example.com", "session")]
    [InlineData("", "current@example.com", "new@example.com", "session")]
    [InlineData("   ", "current@example.com", "new@example.com", "session")]
    public void Create_WithNullOrEmptyUserId_ShouldThrow(
        string? userId, string currentEmail, string newEmail, string sessionToken)
    {
        // Act
        var act = () => EmailChangeOtp.Create(
            userId!,
            currentEmail,
            newEmail,
            ValidOtpHash,
            sessionToken,
            DefaultExpiryMinutes);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("user-123", null, "new@example.com", "session")]
    [InlineData("user-123", "", "new@example.com", "session")]
    [InlineData("user-123", "   ", "new@example.com", "session")]
    public void Create_WithNullOrEmptyCurrentEmail_ShouldThrow(
        string userId, string? currentEmail, string newEmail, string sessionToken)
    {
        // Act
        var act = () => EmailChangeOtp.Create(
            userId,
            currentEmail!,
            newEmail,
            ValidOtpHash,
            sessionToken,
            DefaultExpiryMinutes);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("user-123", "current@example.com", null, "session")]
    [InlineData("user-123", "current@example.com", "", "session")]
    [InlineData("user-123", "current@example.com", "   ", "session")]
    public void Create_WithNullOrEmptyNewEmail_ShouldThrow(
        string userId, string currentEmail, string? newEmail, string sessionToken)
    {
        // Act
        var act = () => EmailChangeOtp.Create(
            userId,
            currentEmail,
            newEmail!,
            ValidOtpHash,
            sessionToken,
            DefaultExpiryMinutes);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("user-123", "current@example.com", "new@example.com", null)]
    [InlineData("user-123", "current@example.com", "new@example.com", "")]
    [InlineData("user-123", "current@example.com", "new@example.com", "   ")]
    public void Create_WithNullOrEmptySessionToken_ShouldThrow(
        string userId, string currentEmail, string newEmail, string? sessionToken)
    {
        // Act
        var act = () => EmailChangeOtp.Create(
            userId,
            currentEmail,
            newEmail,
            ValidOtpHash,
            sessionToken!,
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
        var otp = EmailChangeOtp.Create(
            ValidUserId,
            ValidCurrentEmail,
            ValidNewEmail,
            ValidOtpHash,
            ValidSessionToken,
            minutes);

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
        var otp = EmailChangeOtp.Create(
            ValidUserId,
            ValidCurrentEmail,
            ValidNewEmail,
            ValidOtpHash,
            ValidSessionToken,
            DefaultExpiryMinutes);

        // Act & Assert
        otp.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_ZeroExpiryMinutes_ShouldReturnTrue()
    {
        // Arrange - Create with 0 minutes expiry
        var otp = EmailChangeOtp.Create(
            ValidUserId,
            ValidCurrentEmail,
            ValidNewEmail,
            ValidOtpHash,
            ValidSessionToken,
            0);

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
        var otp = EmailChangeOtp.Create(
            ValidUserId,
            ValidCurrentEmail,
            ValidNewEmail,
            ValidOtpHash,
            ValidSessionToken,
            DefaultExpiryMinutes);

        // Act & Assert
        otp.IsValid.Should().BeTrue();
    }

    [Fact]
    public void IsValid_UsedOtp_ShouldReturnFalse()
    {
        // Arrange
        var otp = EmailChangeOtp.Create(
            ValidUserId,
            ValidCurrentEmail,
            ValidNewEmail,
            ValidOtpHash,
            ValidSessionToken,
            DefaultExpiryMinutes);
        otp.MarkAsUsed();

        // Act & Assert
        otp.IsValid.Should().BeFalse();
    }

    [Fact]
    public void IsValid_ExpiredOtp_ShouldReturnFalse()
    {
        // Arrange - Create with 0 minutes expiry
        var otp = EmailChangeOtp.Create(
            ValidUserId,
            ValidCurrentEmail,
            ValidNewEmail,
            ValidOtpHash,
            ValidSessionToken,
            0);

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
        var otp = EmailChangeOtp.Create(
            ValidUserId,
            ValidCurrentEmail,
            ValidNewEmail,
            ValidOtpHash,
            ValidSessionToken,
            DefaultExpiryMinutes);
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
        var otp = EmailChangeOtp.Create(
            ValidUserId,
            ValidCurrentEmail,
            ValidNewEmail,
            ValidOtpHash,
            ValidSessionToken,
            DefaultExpiryMinutes);

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
        var otp = EmailChangeOtp.Create(
            ValidUserId,
            ValidCurrentEmail,
            ValidNewEmail,
            ValidOtpHash,
            ValidSessionToken,
            DefaultExpiryMinutes);

        // Act
        otp.MarkAsUsed();

        // Assert
        otp.IsUsed.Should().BeTrue();
    }

    [Fact]
    public void MarkAsUsed_ShouldSetUsedAtTimestamp()
    {
        // Arrange
        var otp = EmailChangeOtp.Create(
            ValidUserId,
            ValidCurrentEmail,
            ValidNewEmail,
            ValidOtpHash,
            ValidSessionToken,
            DefaultExpiryMinutes);
        var beforeUse = DateTimeOffset.UtcNow;

        // Act
        otp.MarkAsUsed();

        // Assert
        var afterUse = DateTimeOffset.UtcNow;
        otp.UsedAt.Should().NotBeNull();
        otp.UsedAt.Should().BeOnOrAfter(beforeUse).And.BeOnOrBefore(afterUse);
    }

    #endregion

    #region CanResend Tests

    [Fact]
    public void CanResend_FreshOtp_ShouldReturnFalse_WhenWithinCooldown()
    {
        // Arrange
        var otp = EmailChangeOtp.Create(
            ValidUserId,
            ValidCurrentEmail,
            ValidNewEmail,
            ValidOtpHash,
            ValidSessionToken,
            DefaultExpiryMinutes);

        // Act
        // Fresh OTP should be in cooldown based on CreatedAt
        var canResend = otp.CanResend(cooldownSeconds: 60, maxResendCount: 3);

        // Assert
        // Cannot resend immediately after creation - must wait for cooldown
        canResend.Should().BeFalse();
    }

    [Fact]
    public void CanResend_FreshOtp_ShouldReturnTrue_WhenCooldownIsZero()
    {
        // Arrange
        var otp = EmailChangeOtp.Create(
            ValidUserId,
            ValidCurrentEmail,
            ValidNewEmail,
            ValidOtpHash,
            ValidSessionToken,
            DefaultExpiryMinutes);

        // Act
        // With 0 cooldown, should be able to resend immediately
        var canResend = otp.CanResend(cooldownSeconds: 0, maxResendCount: 3);

        // Assert
        canResend.Should().BeTrue();
    }

    [Fact]
    public void CanResend_MaxResendCountReached_ShouldReturnFalse()
    {
        // Arrange
        var otp = EmailChangeOtp.Create(
            ValidUserId,
            ValidCurrentEmail,
            ValidNewEmail,
            ValidOtpHash,
            ValidSessionToken,
            DefaultExpiryMinutes);

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
        var otp = EmailChangeOtp.Create(
            ValidUserId,
            ValidCurrentEmail,
            ValidNewEmail,
            ValidOtpHash,
            ValidSessionToken,
            DefaultExpiryMinutes);
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
        var otp = EmailChangeOtp.Create(
            ValidUserId,
            ValidCurrentEmail,
            ValidNewEmail,
            ValidOtpHash,
            ValidSessionToken,
            DefaultExpiryMinutes);
        otp.Resend("new-hash", DefaultExpiryMinutes);

        // Act - Check with 0 cooldown (effectively after cooldown)
        var canResend = otp.CanResend(cooldownSeconds: 0, maxResendCount: 3);

        // Assert
        canResend.Should().BeTrue();
    }

    #endregion

    #region GetRemainingCooldownSeconds Tests

    [Fact]
    public void GetRemainingCooldownSeconds_FreshOtp_ShouldReturnApproximateCooldown()
    {
        // Arrange
        var otp = EmailChangeOtp.Create(
            ValidUserId,
            ValidCurrentEmail,
            ValidNewEmail,
            ValidOtpHash,
            ValidSessionToken,
            DefaultExpiryMinutes);

        // Act
        // Fresh OTP uses CreatedAt as reference time, so cooldown should be near the full value
        var remaining = otp.GetRemainingCooldownSeconds(cooldownSeconds: 60);

        // Assert
        // Should return approximately 60 seconds (allow 2 seconds tolerance for test execution time)
        remaining.Should().BeCloseTo(60, 2);
    }

    [Fact]
    public void GetRemainingCooldownSeconds_JustResent_ShouldReturnApproximateCooldown()
    {
        // Arrange
        var otp = EmailChangeOtp.Create(
            ValidUserId,
            ValidCurrentEmail,
            ValidNewEmail,
            ValidOtpHash,
            ValidSessionToken,
            DefaultExpiryMinutes);
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
        var otp = EmailChangeOtp.Create(
            ValidUserId,
            ValidCurrentEmail,
            ValidNewEmail,
            ValidOtpHash,
            ValidSessionToken,
            DefaultExpiryMinutes);
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
        var otp = EmailChangeOtp.Create(
            ValidUserId,
            ValidCurrentEmail,
            ValidNewEmail,
            ValidOtpHash,
            ValidSessionToken,
            DefaultExpiryMinutes);
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
        var otp = EmailChangeOtp.Create(
            ValidUserId,
            ValidCurrentEmail,
            ValidNewEmail,
            ValidOtpHash,
            ValidSessionToken,
            DefaultExpiryMinutes);
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
        var otp = EmailChangeOtp.Create(
            ValidUserId,
            ValidCurrentEmail,
            ValidNewEmail,
            ValidOtpHash,
            ValidSessionToken,
            DefaultExpiryMinutes);
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
        var otp = EmailChangeOtp.Create(
            ValidUserId,
            ValidCurrentEmail,
            ValidNewEmail,
            ValidOtpHash,
            ValidSessionToken,
            1);
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
        var otp = EmailChangeOtp.Create(
            ValidUserId,
            ValidCurrentEmail,
            ValidNewEmail,
            ValidOtpHash,
            ValidSessionToken,
            DefaultExpiryMinutes);
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
        var otp = EmailChangeOtp.Create(
            ValidUserId,
            ValidCurrentEmail,
            ValidNewEmail,
            ValidOtpHash,
            ValidSessionToken,
            DefaultExpiryMinutes);

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
        var otp = EmailChangeOtp.Create(
            ValidUserId,
            ValidCurrentEmail,
            ValidNewEmail,
            ValidOtpHash,
            ValidSessionToken,
            DefaultExpiryMinutes);

        // Assert
        otp.SessionToken.Should().Be(ValidSessionToken);
        otp.SessionToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Create_DifferentSessionTokens_ShouldBeDifferentOtps()
    {
        // Arrange & Act
        var otp1 = EmailChangeOtp.Create(
            ValidUserId,
            ValidCurrentEmail,
            ValidNewEmail,
            ValidOtpHash,
            "session-1",
            DefaultExpiryMinutes);
        var otp2 = EmailChangeOtp.Create(
            ValidUserId,
            ValidCurrentEmail,
            ValidNewEmail,
            ValidOtpHash,
            "session-2",
            DefaultExpiryMinutes);

        // Assert
        otp1.SessionToken.Should().NotBe(otp2.SessionToken);
        otp1.Id.Should().NotBe(otp2.Id);
    }

    #endregion

    #region Email Preservation Tests

    [Fact]
    public void Create_ShouldPreserveEmailAddresses()
    {
        // Arrange
        var currentEmail = "old-email@example.com";
        var newEmail = "new-email@example.com";

        // Act
        var otp = EmailChangeOtp.Create(
            ValidUserId,
            currentEmail,
            newEmail,
            ValidOtpHash,
            ValidSessionToken,
            DefaultExpiryMinutes);

        // Assert
        otp.CurrentEmail.Should().Be(currentEmail);
        otp.NewEmail.Should().Be(newEmail);
        otp.CurrentEmail.Should().NotBe(otp.NewEmail);
    }

    [Fact]
    public void Create_ShouldPreserveUserId()
    {
        // Arrange
        var userId = "specific-user-id-12345";

        // Act
        var otp = EmailChangeOtp.Create(
            userId,
            ValidCurrentEmail,
            ValidNewEmail,
            ValidOtpHash,
            ValidSessionToken,
            DefaultExpiryMinutes);

        // Assert
        otp.UserId.Should().Be(userId);
    }

    #endregion
}
