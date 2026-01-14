namespace NOIR.Application.Features.Auth.Commands.PasswordReset.VerifyPasswordResetOtp;

/// <summary>
/// Command to verify OTP for password reset.
/// Returns a reset token if valid.
/// </summary>
public sealed record VerifyPasswordResetOtpCommand(
    string SessionToken,
    string Otp);
