namespace NOIR.Application.UnitTests.Infrastructure;

/// <summary>
/// Unit tests for PasswordResetService.
/// Tests the forgot password flow including OTP generation, verification, and password reset.
/// </summary>
public class PasswordResetServiceTests
{
    private readonly Mock<IRepository<PasswordResetOtp, Guid>> _otpRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IOtpService> _otpServiceMock;
    private readonly Mock<ISecureTokenGenerator> _tokenGeneratorMock;
    private readonly Mock<IUserIdentityService> _userIdentityServiceMock;
    private readonly Mock<IRefreshTokenService> _refreshTokenServiceMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<IOptionsMonitor<PasswordResetSettings>> _settingsMock;
    private readonly Mock<ILogger<PasswordResetService>> _loggerMock;
    private readonly PasswordResetService _sut;
    private readonly PasswordResetSettings _settings;

    public PasswordResetServiceTests()
    {
        _otpRepositoryMock = new Mock<IRepository<PasswordResetOtp, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _otpServiceMock = new Mock<IOtpService>();
        _tokenGeneratorMock = new Mock<ISecureTokenGenerator>();
        _userIdentityServiceMock = new Mock<IUserIdentityService>();
        _refreshTokenServiceMock = new Mock<IRefreshTokenService>();
        _emailServiceMock = new Mock<IEmailService>();
        _loggerMock = new Mock<ILogger<PasswordResetService>>();

        _settings = new PasswordResetSettings
        {
            OtpLength = 6,
            OtpExpiryMinutes = 5,
            ResendCooldownSeconds = 60,
            MaxResendCount = 3,
            MaxRequestsPerEmailPerHour = 3,
            ResetTokenExpiryMinutes = 15
        };

        _settingsMock = new Mock<IOptionsMonitor<PasswordResetSettings>>();
        _settingsMock.Setup(x => x.CurrentValue).Returns(_settings);

        _sut = new PasswordResetService(
            _otpRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _otpServiceMock.Object,
            _tokenGeneratorMock.Object,
            _userIdentityServiceMock.Object,
            _refreshTokenServiceMock.Object,
            _emailServiceMock.Object,
            _settingsMock.Object,
            _loggerMock.Object);
    }

    private static UserIdentityDto CreateTestUser(string id = "user-id", string email = "test@example.com", string? firstName = "Test")
    {
        return new UserIdentityDto(
            Id: id,
            Email: email,
            TenantId: "default",
            FirstName: firstName,
            LastName: "User",
            DisplayName: $"{firstName} User",
            FullName: $"{firstName} User",
            PhoneNumber: null,
            AvatarUrl: null,
            IsActive: true,
            IsDeleted: false,
            IsSystemUser: false,
            CreatedAt: DateTimeOffset.UtcNow,
            ModifiedAt: null);
    }

    #region RequestPasswordResetAsync Tests

    [Fact]
    public async Task RequestPasswordResetAsync_WithValidEmail_ShouldReturnSuccess()
    {
        // Arrange
        var email = "test@example.com";
        var otpCode = "123456";
        var otpHash = "hashed_otp";
        var sessionToken = "session_token";

        _otpRepositoryMock
            .Setup(x => x.CountAsync(It.IsAny<ISpecification<PasswordResetOtp>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0); // Not rate limited

        _otpRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ISpecification<PasswordResetOtp>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PasswordResetOtp?)null); // No existing OTP

        _userIdentityServiceMock
            .Setup(x => x.FindByEmailAsync(email, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestUser("user-id", email, "Test"));

        _otpServiceMock.Setup(x => x.GenerateOtp()).Returns(otpCode);
        _otpServiceMock.Setup(x => x.HashOtp(otpCode)).Returns(otpHash);
        _otpServiceMock.Setup(x => x.MaskEmail(It.IsAny<string>())).Returns("te***t@example.com");
        _tokenGeneratorMock.Setup(x => x.GenerateToken(32)).Returns(sessionToken);

        _emailServiceMock
            .Setup(x => x.SendTemplateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.RequestPasswordResetAsync(email);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.SessionToken.Should().Be(sessionToken);
        result.Value.MaskedEmail.Should().Be("te***t@example.com");
        result.Value.OtpLength.Should().Be(6);

        _otpRepositoryMock.Verify(x => x.AddAsync(It.IsAny<PasswordResetOtp>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RequestPasswordResetAsync_WhenRateLimited_ShouldReturnFailure()
    {
        // Arrange
        var email = "test@example.com";

        _otpRepositoryMock
            .Setup(x => x.CountAsync(It.IsAny<ISpecification<PasswordResetOtp>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(3); // At rate limit

        // Act
        var result = await _sut.RequestPasswordResetAsync(email);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-AUTH-1021");

        _otpRepositoryMock.Verify(x => x.AddAsync(It.IsAny<PasswordResetOtp>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RequestPasswordResetAsync_WhenUserNotFound_ShouldStillReturnSuccess()
    {
        // Arrange - Security: Don't reveal if user exists
        var email = "nonexistent@example.com";
        var otpCode = "123456";
        var sessionToken = "session_token";

        _otpRepositoryMock
            .Setup(x => x.CountAsync(It.IsAny<ISpecification<PasswordResetOtp>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _otpRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ISpecification<PasswordResetOtp>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PasswordResetOtp?)null);

        _userIdentityServiceMock
            .Setup(x => x.FindByEmailAsync(email, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserIdentityDto?)null); // User doesn't exist

        _otpServiceMock.Setup(x => x.GenerateOtp()).Returns(otpCode);
        _otpServiceMock.Setup(x => x.HashOtp(otpCode)).Returns("hashed");
        _otpServiceMock.Setup(x => x.MaskEmail(It.IsAny<string>())).Returns("no***t@example.com");
        _tokenGeneratorMock.Setup(x => x.GenerateToken(32)).Returns(sessionToken);

        // Act
        var result = await _sut.RequestPasswordResetAsync(email);

        // Assert - Should still return success (prevent email enumeration)
        result.IsSuccess.Should().BeTrue();

        // But email should NOT be sent
        _emailServiceMock.Verify(
            x => x.SendTemplateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RequestPasswordResetAsync_WithActiveOtpInCooldown_ShouldReturnExistingSession()
    {
        // Arrange
        var email = "test@example.com";
        var existingOtp = PasswordResetOtp.Create(
            email,
            "hash",
            "existing_session",
            5,
            "user-id",
            null,
            null);

        _otpRepositoryMock
            .Setup(x => x.CountAsync(It.IsAny<ISpecification<PasswordResetOtp>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _otpRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ISpecification<PasswordResetOtp>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingOtp);

        _otpServiceMock.Setup(x => x.MaskEmail(It.IsAny<string>())).Returns("te***t@example.com");

        // Act
        var result = await _sut.RequestPasswordResetAsync(email);

        // Assert - Should return existing session
        result.IsSuccess.Should().BeTrue();
        result.Value.SessionToken.Should().Be("existing_session");

        // Should NOT create a new OTP
        _otpRepositoryMock.Verify(x => x.AddAsync(It.IsAny<PasswordResetOtp>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region VerifyOtpAsync Tests

    [Fact]
    public async Task VerifyOtpAsync_WithValidOtp_ShouldReturnResetToken()
    {
        // Arrange
        var sessionToken = "session_token";
        var otp = "123456";
        var resetToken = "reset_token_64_bytes";

        var otpRecord = PasswordResetOtp.Create(
            "test@example.com",
            "hashed_otp",
            sessionToken,
            5,
            "user-id",
            null,
            null);

        _otpRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ISpecification<PasswordResetOtp>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(otpRecord);

        _otpServiceMock.Setup(x => x.VerifyOtp(otp, "hashed_otp")).Returns(true);
        _tokenGeneratorMock.Setup(x => x.GenerateToken(64)).Returns(resetToken);

        // Act
        var result = await _sut.VerifyOtpAsync(sessionToken, otp);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ResetToken.Should().Be(resetToken);

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task VerifyOtpAsync_WithInvalidSession_ShouldReturnFailure()
    {
        // Arrange
        _otpRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ISpecification<PasswordResetOtp>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PasswordResetOtp?)null);

        // Act
        var result = await _sut.VerifyOtpAsync("invalid_session", "123456");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-AUTH-1025");
    }

    [Fact]
    public async Task VerifyOtpAsync_WithInvalidOtp_ShouldReturnFailureAndRecordAttempt()
    {
        // Arrange
        var sessionToken = "session_token";
        var otpRecord = PasswordResetOtp.Create(
            "test@example.com",
            "hashed_otp",
            sessionToken,
            5,
            "user-id",
            null,
            null);

        _otpRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ISpecification<PasswordResetOtp>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(otpRecord);

        _otpServiceMock.Setup(x => x.VerifyOtp("wrong_otp", "hashed_otp")).Returns(false);

        // Act
        var result = await _sut.VerifyOtpAsync(sessionToken, "wrong_otp");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-AUTH-1023");

        // Should save the failed attempt
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task VerifyOtpAsync_WithUsedOtp_ShouldReturnFailure()
    {
        // Arrange
        var sessionToken = "session_token";
        var otpRecord = PasswordResetOtp.Create(
            "test@example.com",
            "hashed_otp",
            sessionToken,
            5,
            "user-id",
            null,
            null);

        // Mark as used
        otpRecord.MarkAsUsed("old_reset_token", 15);

        _otpRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ISpecification<PasswordResetOtp>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(otpRecord);

        // Act
        var result = await _sut.VerifyOtpAsync(sessionToken, "123456");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-AUTH-1028");
    }

    #endregion

    #region ResendOtpAsync Tests

    [Fact]
    public async Task ResendOtpAsync_WithValidSession_ShouldReturnSuccess()
    {
        // Arrange
        var sessionToken = "session_token";
        // Use helper that sets CreatedAt in the past so cooldown has passed
        var otpRecord = CreatePasswordResetOtpWithCooldownPassed(
            "test@example.com",
            "old_hash",
            sessionToken,
            5,
            "user-id",
            _settings.ResendCooldownSeconds);

        _otpRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ISpecification<PasswordResetOtp>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(otpRecord);

        _otpServiceMock.Setup(x => x.GenerateOtp()).Returns("654321");
        _otpServiceMock.Setup(x => x.HashOtp("654321")).Returns("new_hash");

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync("user-id", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestUser("user-id", "test@example.com", "Test"));

        _emailServiceMock
            .Setup(x => x.SendTemplateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.ResendOtpAsync(sessionToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().BeTrue();

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResendOtpAsync_WithInvalidSession_ShouldReturnFailure()
    {
        // Arrange
        _otpRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ISpecification<PasswordResetOtp>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PasswordResetOtp?)null);

        // Act
        var result = await _sut.ResendOtpAsync("invalid_session");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-AUTH-1025");
    }

    [Fact]
    public async Task ResendOtpAsync_WhenUsed_ShouldReturnFailure()
    {
        // Arrange
        var otpRecord = PasswordResetOtp.Create(
            "test@example.com",
            "hash",
            "session",
            5,
            "user-id",
            null,
            null);

        otpRecord.MarkAsUsed("reset_token", 15);

        _otpRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ISpecification<PasswordResetOtp>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(otpRecord);

        // Act
        var result = await _sut.ResendOtpAsync("session");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-AUTH-1028");
    }

    #endregion

    #region ResetPasswordAsync Tests

    [Fact]
    public async Task ResetPasswordAsync_WithValidToken_ShouldResetPassword()
    {
        // Arrange
        var resetToken = "valid_reset_token";
        var newPassword = "NewSecurePassword123!";

        var otpRecord = PasswordResetOtp.Create(
            "test@example.com",
            "hash",
            "session",
            5,
            "user-id",
            null,
            null);

        // Mark as verified with reset token
        otpRecord.MarkAsUsed(resetToken, 15);

        _otpRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ISpecification<PasswordResetOtp>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(otpRecord);

        _userIdentityServiceMock
            .Setup(x => x.ResetPasswordAsync("user-id", newPassword, It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success());

        // Act
        var result = await _sut.ResetPasswordAsync(resetToken, newPassword);

        // Assert
        result.IsSuccess.Should().BeTrue();

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        // Should revoke all refresh tokens for security
        _refreshTokenServiceMock.Verify(
            x => x.RevokeAllUserTokensAsync(
                "user-id",
                null,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ResetPasswordAsync_WithInvalidToken_ShouldReturnFailure()
    {
        // Arrange
        _otpRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ISpecification<PasswordResetOtp>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PasswordResetOtp?)null);

        // Act
        var result = await _sut.ResetPasswordAsync("invalid_token", "NewPassword123!");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-AUTH-1025");
    }

    [Fact]
    public async Task ResetPasswordAsync_WhenPasswordResetFails_ShouldReturnFailure()
    {
        // Arrange
        var resetToken = "valid_reset_token";

        var otpRecord = PasswordResetOtp.Create(
            "test@example.com",
            "hash",
            "session",
            5,
            "user-id",
            null,
            null);

        otpRecord.MarkAsUsed(resetToken, 15);

        _otpRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ISpecification<PasswordResetOtp>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(otpRecord);

        _userIdentityServiceMock
            .Setup(x => x.ResetPasswordAsync("user-id", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Failure("Password does not meet requirements"));

        // Act
        var result = await _sut.ResetPasswordAsync(resetToken, "weak");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-AUTH-1030");
    }

    #endregion

    #region IsRateLimitedAsync Tests

    [Fact]
    public async Task IsRateLimitedAsync_WhenUnderLimit_ShouldReturnFalse()
    {
        // Arrange
        _otpRepositoryMock
            .Setup(x => x.CountAsync(It.IsAny<ISpecification<PasswordResetOtp>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(2); // Under limit of 3

        // Act
        var result = await _sut.IsRateLimitedAsync("test@example.com");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsRateLimitedAsync_WhenAtLimit_ShouldReturnTrue()
    {
        // Arrange
        _otpRepositoryMock
            .Setup(x => x.CountAsync(It.IsAny<ISpecification<PasswordResetOtp>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(3); // At limit of 3

        // Act
        var result = await _sut.IsRateLimitedAsync("test@example.com");

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates an OTP with CreatedAt set in the past so cooldown has passed.
    /// This is needed for resend tests since cooldown now considers CreatedAt.
    /// </summary>
    private static PasswordResetOtp CreatePasswordResetOtpWithCooldownPassed(
        string email,
        string otpHash,
        string sessionToken,
        int expiryMinutes,
        string? userId = null,
        int cooldownSeconds = 60)
    {
        var otp = PasswordResetOtp.Create(
            email,
            otpHash,
            sessionToken,
            expiryMinutes,
            userId,
            null,
            null);

        // Set CreatedAt to past using reflection (cooldown + 1 second ago)
        // PasswordResetOtp -> TenantAggregateRoot -> AggregateRoot -> Entity (has CreatedAt)
        var createdAtProperty = typeof(PasswordResetOtp).BaseType?.BaseType?.BaseType?.GetProperty("CreatedAt");
        createdAtProperty?.SetValue(otp, DateTimeOffset.UtcNow.AddSeconds(-(cooldownSeconds + 1)));

        return otp;
    }

    #endregion
}
