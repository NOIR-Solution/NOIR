namespace NOIR.Application.Features.Auth.Commands.PasswordReset.ResetPassword;

/// <summary>
/// Command to reset password using a valid reset token.
/// </summary>
public sealed record ResetPasswordCommand(
    string ResetToken,
    string NewPassword);
