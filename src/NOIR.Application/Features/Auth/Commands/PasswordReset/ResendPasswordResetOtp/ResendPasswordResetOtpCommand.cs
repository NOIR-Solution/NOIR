namespace NOIR.Application.Features.Auth.Commands.PasswordReset.ResendPasswordResetOtp;

/// <summary>
/// Command to resend OTP for an existing password reset session.
/// </summary>
public sealed record ResendPasswordResetOtpCommand(
    string SessionToken);
