namespace NOIR.Infrastructure.Identity;

/// <summary>
/// Service for managing the password reset flow with OTP verification.
/// Implements database-level rate limiting per email to prevent bypass attacks.
/// </summary>
public class PasswordResetService : IPasswordResetService, IScopedService
{
    private readonly IRepository<PasswordResetOtp, Guid> _otpRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOtpService _otpService;
    private readonly ISecureTokenGenerator _tokenGenerator;
    private readonly IUserIdentityService _userIdentityService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IEmailService _emailService;
    private readonly IOptionsMonitor<PasswordResetSettings> _settings;
    private readonly ILogger<PasswordResetService> _logger;

    public PasswordResetService(
        IRepository<PasswordResetOtp, Guid> otpRepository,
        IUnitOfWork unitOfWork,
        IOtpService otpService,
        ISecureTokenGenerator tokenGenerator,
        IUserIdentityService userIdentityService,
        IRefreshTokenService refreshTokenService,
        IEmailService emailService,
        IOptionsMonitor<PasswordResetSettings> settings,
        ILogger<PasswordResetService> logger)
    {
        _otpRepository = otpRepository;
        _unitOfWork = unitOfWork;
        _otpService = otpService;
        _tokenGenerator = tokenGenerator;
        _userIdentityService = userIdentityService;
        _refreshTokenService = refreshTokenService;
        _emailService = emailService;
        _settings = settings;
        _logger = logger;
    }

    public async Task<Result<PasswordResetRequestResult>> RequestPasswordResetAsync(
        string email,
        string? tenantId = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.ToLowerInvariant().Trim();

        // Check rate limiting per email
        if (await IsRateLimitedAsync(normalizedEmail, cancellationToken))
        {
            _logger.LogWarning("Password reset rate limit exceeded for email {Email}", normalizedEmail);
            return Result.Failure<PasswordResetRequestResult>(
                Error.Failure("NOIR-AUTH-1021", "Too many password reset requests. Please try again later."));
        }

        // Check if there's an active OTP for this email (bypass prevention)
        var activeOtpSpec = new ActivePasswordResetOtpByEmailSpec(normalizedEmail);
        var existingOtp = await _otpRepository.FirstOrDefaultAsync(activeOtpSpec, cancellationToken);

        if (existingOtp != null)
        {
            // Check if cooldown is still active
            var remainingCooldown = existingOtp.GetRemainingCooldownSeconds(_settings.CurrentValue.ResendCooldownSeconds);
            if (remainingCooldown > 0)
            {
                _logger.LogInformation(
                    "Password reset requested but cooldown active for email {Email}, {Seconds}s remaining",
                    normalizedEmail, remainingCooldown);

                // Return the existing session instead of creating a new one
                return Result<PasswordResetRequestResult>.Success(new PasswordResetRequestResult(
                    existingOtp.SessionToken,
                    _otpService.MaskEmail(normalizedEmail),
                    existingOtp.ExpiresAt,
                    _settings.CurrentValue.OtpLength));
            }

            // Cooldown passed, resend using existing session
            return await ResendOtpInternalAsync(existingOtp, cancellationToken);
        }

        // Look up user within tenant (we still send success response even if user doesn't exist for security)
        var user = await _userIdentityService.FindByEmailAsync(normalizedEmail, tenantId, cancellationToken);

        // Generate OTP, hash, and session token
        var otpCode = _otpService.GenerateOtp();
        var otpHash = _otpService.HashOtp(otpCode);
        var sessionToken = _tokenGenerator.GenerateToken(32);

        // Create OTP record
        var otp = PasswordResetOtp.Create(
            normalizedEmail,
            otpHash,
            sessionToken,
            _settings.CurrentValue.OtpExpiryMinutes,
            user?.Id,
            tenantId,
            ipAddress);

        await _otpRepository.AddAsync(otp, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Send email only if user exists (but always return success for security)
        if (user != null)
        {
            await SendOtpEmailAsync(normalizedEmail, otpCode, user.FirstName ?? user.DisplayName, cancellationToken);
        }

        _logger.LogInformation(
            "Password reset OTP created for email {Email}, user exists: {UserExists}",
            normalizedEmail, user != null);

        return Result<PasswordResetRequestResult>.Success(new PasswordResetRequestResult(
            otp.SessionToken,
            _otpService.MaskEmail(normalizedEmail),
            otp.ExpiresAt,
            _settings.CurrentValue.OtpLength));
    }

    public async Task<Result<PasswordResetVerifyResult>> VerifyOtpAsync(
        string sessionToken,
        string otp,
        CancellationToken cancellationToken = default)
    {
        var spec = new PasswordResetOtpBySessionTokenSpec(sessionToken);
        var otpRecord = await _otpRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (otpRecord is null)
        {
            return Result.Failure<PasswordResetVerifyResult>(
                Error.Failure("NOIR-AUTH-1025", "Invalid session. Please request a new password reset."));
        }

        if (otpRecord.IsUsed)
        {
            return Result.Failure<PasswordResetVerifyResult>(
                Error.Failure("NOIR-AUTH-1028", "This OTP has already been used. Please request a new password reset."));
        }

        if (otpRecord.IsExpired)
        {
            return Result.Failure<PasswordResetVerifyResult>(
                Error.Failure("NOIR-AUTH-1022", "OTP has expired. Please request a new password reset."));
        }

        // Verify OTP
        if (!_otpService.VerifyOtp(otp, otpRecord.OtpHash))
        {
            otpRecord.RecordFailedAttempt();
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogWarning(
                "Failed OTP verification attempt for session {SessionToken}, attempt count: {Count}",
                sessionToken, otpRecord.AttemptCount);

            return Result.Failure<PasswordResetVerifyResult>(
                Error.Failure("NOIR-AUTH-1023", "Invalid OTP code. Please try again."));
        }

        // Mark as used with pre-generated reset token (64 bytes for higher security)
        var resetToken = _tokenGenerator.GenerateToken(64);
        otpRecord.MarkAsUsed(resetToken, _settings.CurrentValue.ResetTokenExpiryMinutes);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "OTP verified successfully for session {SessionToken}",
            sessionToken);

        return Result<PasswordResetVerifyResult>.Success(new PasswordResetVerifyResult(
            resetToken,
            otpRecord.ResetTokenExpiresAt!.Value));
    }

    public async Task<Result<PasswordResetResendResult>> ResendOtpAsync(
        string sessionToken,
        CancellationToken cancellationToken = default)
    {
        var spec = new PasswordResetOtpBySessionTokenSpec(sessionToken);
        var otpRecord = await _otpRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (otpRecord is null)
        {
            return Result.Failure<PasswordResetResendResult>(
                Error.Failure("NOIR-AUTH-1025", "Invalid session. Please request a new password reset."));
        }

        if (otpRecord.IsUsed)
        {
            return Result.Failure<PasswordResetResendResult>(
                Error.Failure("NOIR-AUTH-1028", "This session has already been completed. Please request a new password reset."));
        }

        // Check resend limits
        if (!otpRecord.CanResend(_settings.CurrentValue.ResendCooldownSeconds, _settings.CurrentValue.MaxResendCount))
        {
            var remainingCooldown = otpRecord.GetRemainingCooldownSeconds(_settings.CurrentValue.ResendCooldownSeconds);
            if (remainingCooldown > 0)
            {
                return Result.Failure<PasswordResetResendResult>(
                    Error.Failure("NOIR-AUTH-1020", $"Please wait {remainingCooldown} seconds before requesting a new code."));
            }

            return Result.Failure<PasswordResetResendResult>(
                Error.Failure("NOIR-AUTH-1021", "Maximum resend attempts reached. Please request a new password reset."));
        }

        // Generate new OTP
        var newOtpCode = _otpService.GenerateOtp();
        var newOtpHash = _otpService.HashOtp(newOtpCode);

        otpRecord.Resend(newOtpHash, _settings.CurrentValue.OtpExpiryMinutes);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Send email only if user exists
        if (otpRecord.UserId != null)
        {
            var user = await _userIdentityService.FindByIdAsync(otpRecord.UserId, cancellationToken);
            if (user != null)
            {
                await SendOtpEmailAsync(otpRecord.Email, newOtpCode, user.FirstName ?? user.DisplayName, cancellationToken);
            }
        }

        _logger.LogInformation(
            "OTP resent for session {SessionToken}, resend count: {Count}",
            sessionToken, otpRecord.ResendCount);

        return Result<PasswordResetResendResult>.Success(new PasswordResetResendResult(
            true,
            DateTimeOffset.UtcNow.AddSeconds(_settings.CurrentValue.ResendCooldownSeconds),
            _settings.CurrentValue.MaxResendCount - otpRecord.ResendCount));
    }

    public async Task<Result> ResetPasswordAsync(
        string resetToken,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        var spec = new PasswordResetOtpByResetTokenSpec(resetToken);
        var otpRecord = await _otpRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (otpRecord is null)
        {
            return Result.Failure(
                Error.Failure("NOIR-AUTH-1025", "Invalid reset token. Please request a new password reset."));
        }

        if (!otpRecord.IsResetTokenValid)
        {
            return Result.Failure(
                Error.Failure("NOIR-AUTH-1027", "Reset token has expired. Please request a new password reset."));
        }

        if (otpRecord.UserId is null)
        {
            // This shouldn't happen in normal flow, but handle it
            return Result.Failure(
                Error.Failure("NOIR-AUTH-1025", "Invalid reset request. Please request a new password reset."));
        }

        // Reset the password
        var resetResult = await _userIdentityService.ResetPasswordAsync(
            otpRecord.UserId,
            newPassword,
            cancellationToken);

        if (!resetResult.Succeeded)
        {
            return Result.Failure(
                Error.Failure("NOIR-AUTH-1030", string.Join(", ", resetResult.Errors ?? ["Password reset failed."])));
        }

        // Invalidate the reset token
        otpRecord.InvalidateResetToken();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Revoke all refresh tokens for security
        await _refreshTokenService.RevokeAllUserTokensAsync(
            otpRecord.UserId,
            null,
            "Password reset - all sessions revoked for security",
            cancellationToken);

        _logger.LogInformation(
            "Password reset successful for user {UserId}",
            otpRecord.UserId);

        return Result.Success();
    }

    public async Task<bool> IsRateLimitedAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.ToLowerInvariant().Trim();
        var spec = new RecentPasswordResetOtpsByEmailSpec(normalizedEmail, 1);
        var recentCount = await _otpRepository.CountAsync(spec, cancellationToken);

        return recentCount >= _settings.CurrentValue.MaxRequestsPerEmailPerHour;
    }

    private async Task<Result<PasswordResetRequestResult>> ResendOtpInternalAsync(
        PasswordResetOtp existingOtp,
        CancellationToken cancellationToken)
    {
        // Check resend limits
        if (!existingOtp.CanResend(_settings.CurrentValue.ResendCooldownSeconds, _settings.CurrentValue.MaxResendCount))
        {
            return Result.Failure<PasswordResetRequestResult>(
                Error.Failure("NOIR-AUTH-1021", "Maximum resend attempts reached. Please try again later."));
        }

        // Generate new OTP
        var newOtpCode = _otpService.GenerateOtp();
        var newOtpHash = _otpService.HashOtp(newOtpCode);

        existingOtp.Resend(newOtpHash, _settings.CurrentValue.OtpExpiryMinutes);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Send email only if user exists
        if (existingOtp.UserId != null)
        {
            var user = await _userIdentityService.FindByIdAsync(existingOtp.UserId, cancellationToken);
            if (user != null)
            {
                await SendOtpEmailAsync(existingOtp.Email, newOtpCode, user.FirstName ?? user.DisplayName, cancellationToken);
            }
        }

        _logger.LogInformation(
            "OTP resent via request flow for session {SessionToken}",
            existingOtp.SessionToken);

        return Result<PasswordResetRequestResult>.Success(new PasswordResetRequestResult(
            existingOtp.SessionToken,
            _otpService.MaskEmail(existingOtp.Email),
            existingOtp.ExpiresAt,
            _settings.CurrentValue.OtpLength));
    }

    private async Task SendOtpEmailAsync(
        string email,
        string otpCode,
        string? userName,
        CancellationToken cancellationToken)
    {
        try
        {
            var model = new PasswordResetOtpEmailModel(
                otpCode,
                userName ?? "User",
                _settings.CurrentValue.OtpExpiryMinutes);

            await _emailService.SendTemplateAsync(
                email,
                "Your Password Reset Code",
                "PasswordResetOtp",
                model,
                cancellationToken);
        }
        catch (Exception ex)
        {
            // Log but don't fail the request - email delivery shouldn't block the flow
            _logger.LogError(ex, "Failed to send password reset OTP email to {Email}", email);
        }
    }
}

/// <summary>
/// Model for the password reset OTP email template.
/// </summary>
public record PasswordResetOtpEmailModel(
    string OtpCode,
    string UserName,
    int ExpiryMinutes);
