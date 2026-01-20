namespace NOIR.Infrastructure.Identity;

/// <summary>
/// Service for managing the email change flow with OTP verification.
/// Implements rate limiting per user to prevent abuse.
/// </summary>
public class EmailChangeService : IEmailChangeService, IScopedService
{
    private readonly IRepository<EmailChangeOtp, Guid> _otpRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOtpService _otpService;
    private readonly ISecureTokenGenerator _tokenGenerator;
    private readonly IUserIdentityService _userIdentityService;
    private readonly IEmailService _emailService;
    private readonly ILogger<EmailChangeService> _logger;

    // Settings (could be moved to configuration)
    private const int OtpLength = 6;
    private const int OtpExpiryMinutes = 15;
    private const int ResendCooldownSeconds = 60;
    private const int MaxResendCount = 3;
    private const int MaxRequestsPerUserPerHour = 3;

    public EmailChangeService(
        IRepository<EmailChangeOtp, Guid> otpRepository,
        IUnitOfWork unitOfWork,
        IOtpService otpService,
        ISecureTokenGenerator tokenGenerator,
        IUserIdentityService userIdentityService,
        IEmailService emailService,
        ILogger<EmailChangeService> logger)
    {
        _otpRepository = otpRepository;
        _unitOfWork = unitOfWork;
        _otpService = otpService;
        _tokenGenerator = tokenGenerator;
        _userIdentityService = userIdentityService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<Result<EmailChangeRequestResult>> RequestEmailChangeAsync(
        string userId,
        string newEmail,
        string? tenantId = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = newEmail.ToLowerInvariant().Trim();

        // Check rate limiting per user
        if (await IsRateLimitedAsync(userId, cancellationToken))
        {
            _logger.LogWarning("Email change rate limit exceeded for user {UserId}", userId);
            return Result.Failure<EmailChangeRequestResult>(
                Error.Failure(ErrorCodes.Auth.TooManyRequests, "Too many email change requests. Please try again later."));
        }

        // Get current user
        var user = await _userIdentityService.FindByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return Result.Failure<EmailChangeRequestResult>(
                Error.NotFound("User not found.", ErrorCodes.Auth.UserNotFound));
        }

        // Check if new email is same as current
        if (user.Email.Equals(normalizedEmail, StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<EmailChangeRequestResult>(
                Error.Validation("newEmail", "New email must be different from current email.", ErrorCodes.Validation.InvalidInput));
        }

        // Check if new email is already in use within this tenant
        var existingUser = await _userIdentityService.FindByEmailAsync(normalizedEmail, tenantId, cancellationToken);
        if (existingUser is not null)
        {
            return Result.Failure<EmailChangeRequestResult>(
                Error.Validation("newEmail", "This email address is already in use.", ErrorCodes.Auth.DuplicateEmail));
        }

        // Check if there's an active OTP for this user
        var activeOtpSpec = new ActiveEmailChangeOtpByUserIdSpec(userId);
        var existingOtp = await _otpRepository.FirstOrDefaultAsync(activeOtpSpec, cancellationToken);

        if (existingOtp != null)
        {
            var isSameEmail = existingOtp.NewEmail.Equals(normalizedEmail, StringComparison.OrdinalIgnoreCase);
            var remainingCooldown = existingOtp.GetRemainingCooldownSeconds(ResendCooldownSeconds);

            if (isSameEmail)
            {
                // Same email requested - enforce cooldown to prevent spam
                if (remainingCooldown > 0)
                {
                    _logger.LogInformation(
                        "Email change requested but cooldown active for user {UserId}, {Seconds}s remaining",
                        userId, remainingCooldown);

                    // Return the existing session instead of creating a new one (bypass prevention)
                    return Result<EmailChangeRequestResult>.Success(new EmailChangeRequestResult(
                        existingOtp.SessionToken,
                        _otpService.MaskEmail(existingOtp.NewEmail),
                        existingOtp.ExpiresAt,
                        OtpLength));
                }

                // Cooldown passed - resend using existing session (keeps same sessionToken)
                return await ResendOtpInternalAsync(existingOtp, user, cancellationToken);
            }

            // Different email requested - user is changing their mind
            // Mark old OTP as used and create new one (no cooldown restriction for different email)
            _logger.LogInformation(
                "User {UserId} changed target email from {OldEmail} to {NewEmail}, cancelling old OTP",
                userId, existingOtp.NewEmail, normalizedEmail);
            existingOtp.MarkAsUsed();
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        // Generate OTP, hash, and session token
        var otpCode = _otpService.GenerateOtp();
        var otpHash = _otpService.HashOtp(otpCode);
        var sessionToken = _tokenGenerator.GenerateToken(32);

        // Create OTP record
        var otp = EmailChangeOtp.Create(
            userId,
            user.Email,
            normalizedEmail,
            otpHash,
            sessionToken,
            OtpExpiryMinutes,
            tenantId: tenantId,
            ipAddress: ipAddress);

        await _otpRepository.AddAsync(otp, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Send OTP to the new email
        await SendOtpEmailAsync(normalizedEmail, otpCode, user.FirstName ?? user.DisplayName, cancellationToken);

        _logger.LogInformation("Email change OTP created for user {UserId}, new email: {NewEmail}", userId, normalizedEmail);

        return Result<EmailChangeRequestResult>.Success(new EmailChangeRequestResult(
            otp.SessionToken,
            _otpService.MaskEmail(normalizedEmail),
            otp.ExpiresAt,
            OtpLength));
    }

    public async Task<Result<EmailChangeVerifyResult>> VerifyOtpAsync(
        string sessionToken,
        string otp,
        CancellationToken cancellationToken = default)
    {
        var spec = new EmailChangeOtpBySessionTokenSpec(sessionToken);
        var otpRecord = await _otpRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (otpRecord is null)
        {
            return Result.Failure<EmailChangeVerifyResult>(
                Error.Failure(ErrorCodes.Auth.InvalidSession, "Invalid session. Please request a new email change."));
        }

        if (otpRecord.IsUsed)
        {
            return Result.Failure<EmailChangeVerifyResult>(
                Error.Failure(ErrorCodes.Auth.OtpAlreadyUsed, "This OTP has already been used. Please request a new email change."));
        }

        if (otpRecord.IsExpired)
        {
            return Result.Failure<EmailChangeVerifyResult>(
                Error.Failure(ErrorCodes.Auth.OtpExpired, "OTP has expired. Please request a new email change."));
        }

        // Verify OTP
        if (!_otpService.VerifyOtp(otp, otpRecord.OtpHash))
        {
            otpRecord.RecordFailedAttempt();
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogWarning(
                "Invalid OTP attempt for email change session {SessionToken}, attempt {Attempt}",
                sessionToken, otpRecord.AttemptCount);

            return Result.Failure<EmailChangeVerifyResult>(
                Error.Failure(ErrorCodes.Auth.InvalidOtp, "Invalid verification code. Please try again."));
        }

        // Mark OTP as used
        otpRecord.MarkAsUsed();

        // Update user's email
        var updateResult = await _userIdentityService.UpdateEmailAsync(
            otpRecord.UserId,
            otpRecord.NewEmail,
            cancellationToken);

        if (!updateResult.Succeeded)
        {
            var errors = updateResult.Errors ?? [];
            _logger.LogError(
                "Failed to update email for user {UserId}: {Errors}",
                otpRecord.UserId, string.Join(", ", errors));

            return Result.Failure<EmailChangeVerifyResult>(
                Error.Failure(ErrorCodes.Auth.UpdateFailed, string.Join(", ", errors)));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Email changed successfully for user {UserId}, new email: {NewEmail}",
            otpRecord.UserId, otpRecord.NewEmail);

        return Result<EmailChangeVerifyResult>.Success(new EmailChangeVerifyResult(
            otpRecord.NewEmail,
            "Email changed successfully."));
    }

    public async Task<Result<EmailChangeResendResult>> ResendOtpAsync(
        string sessionToken,
        CancellationToken cancellationToken = default)
    {
        var spec = new EmailChangeOtpBySessionTokenSpec(sessionToken);
        var otpRecord = await _otpRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (otpRecord is null)
        {
            return Result.Failure<EmailChangeResendResult>(
                Error.Failure(ErrorCodes.Auth.InvalidSession, "Invalid session. Please request a new email change."));
        }

        if (otpRecord.IsUsed)
        {
            return Result.Failure<EmailChangeResendResult>(
                Error.Failure(ErrorCodes.Auth.OtpAlreadyUsed, "This session has been used. Please request a new email change."));
        }

        if (otpRecord.IsExpired)
        {
            return Result.Failure<EmailChangeResendResult>(
                Error.Failure(ErrorCodes.Auth.OtpExpired, "Session has expired. Please request a new email change."));
        }

        // Check cooldown
        if (!otpRecord.CanResend(ResendCooldownSeconds, MaxResendCount))
        {
            var remainingSeconds = otpRecord.GetRemainingCooldownSeconds(ResendCooldownSeconds);
            return Result.Failure<EmailChangeResendResult>(
                Error.Failure(ErrorCodes.Auth.CooldownActive,
                    $"Please wait {remainingSeconds} seconds before requesting a new code."));
        }

        // Check resend limit
        if (otpRecord.ResendCount >= MaxResendCount)
        {
            return Result.Failure<EmailChangeResendResult>(
                Error.Failure(ErrorCodes.Auth.MaxResendsReached,
                    "Maximum resend attempts reached. Please request a new email change."));
        }

        // Generate new OTP
        var newOtpCode = _otpService.GenerateOtp();
        var newOtpHash = _otpService.HashOtp(newOtpCode);

        // Update OTP record
        otpRecord.Resend(newOtpHash, OtpExpiryMinutes);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Get user for email
        var user = await _userIdentityService.FindByIdAsync(otpRecord.UserId, cancellationToken);

        // Send new OTP email
        await SendOtpEmailAsync(otpRecord.NewEmail, newOtpCode, user?.FirstName ?? user?.DisplayName, cancellationToken);

        _logger.LogInformation(
            "Email change OTP resent for user {UserId}, resend count: {ResendCount}",
            otpRecord.UserId, otpRecord.ResendCount);

        var nextResendAt = DateTimeOffset.UtcNow.AddSeconds(ResendCooldownSeconds);
        return Result<EmailChangeResendResult>.Success(new EmailChangeResendResult(
            true,
            nextResendAt,
            MaxResendCount - otpRecord.ResendCount));
    }

    public async Task<bool> IsRateLimitedAsync(string userId, CancellationToken cancellationToken = default)
    {
        var spec = new RecentEmailChangeOtpsByUserIdSpec(userId, TimeSpan.FromHours(1));
        var count = await _otpRepository.CountAsync(spec, cancellationToken);
        return count >= MaxRequestsPerUserPerHour;
    }

    /// <summary>
    /// Internal method to resend OTP using an existing session (keeps the same sessionToken).
    /// Called when user requests email change again with the same email after cooldown passes.
    /// </summary>
    private async Task<Result<EmailChangeRequestResult>> ResendOtpInternalAsync(
        EmailChangeOtp existingOtp,
        UserIdentityDto user,
        CancellationToken cancellationToken)
    {
        // Check resend limits
        if (!existingOtp.CanResend(ResendCooldownSeconds, MaxResendCount))
        {
            return Result.Failure<EmailChangeRequestResult>(
                Error.Failure(ErrorCodes.Auth.MaxResendsReached, "Maximum resend attempts reached. Please try again later."));
        }

        // Generate new OTP
        var newOtpCode = _otpService.GenerateOtp();
        var newOtpHash = _otpService.HashOtp(newOtpCode);

        existingOtp.Resend(newOtpHash, OtpExpiryMinutes);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Send email
        await SendOtpEmailAsync(existingOtp.NewEmail, newOtpCode, user.FirstName ?? user.DisplayName, cancellationToken);

        _logger.LogInformation(
            "Email change OTP resent via request flow for user {UserId}",
            existingOtp.UserId);

        return Result<EmailChangeRequestResult>.Success(new EmailChangeRequestResult(
            existingOtp.SessionToken,
            _otpService.MaskEmail(existingOtp.NewEmail),
            existingOtp.ExpiresAt,
            OtpLength));
    }

    private async Task SendOtpEmailAsync(
        string email,
        string otpCode,
        string? userName,
        CancellationToken cancellationToken)
    {
        try
        {
            var model = new EmailChangeOtpEmailModel(
                OtpCode: otpCode,
                UserName: userName ?? "User",
                ExpiryMinutes: OtpExpiryMinutes);

            await _emailService.SendTemplateAsync(
                email,
                "Verify your new email address",
                "EmailChangeOtp",
                model,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email change OTP email to {Email}", email);
            // Don't fail the request if email fails - user can resend
        }
    }
}

/// <summary>
/// Model for the email change OTP email template.
/// </summary>
public record EmailChangeOtpEmailModel(
    string OtpCode,
    string UserName,
    int ExpiryMinutes);
