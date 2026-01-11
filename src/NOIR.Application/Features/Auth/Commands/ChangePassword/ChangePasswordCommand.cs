namespace NOIR.Application.Features.Auth.Commands.ChangePassword;

/// <summary>
/// Command to change the authenticated user's password.
/// Requires current password verification for security.
/// All sessions are revoked after successful password change.
/// </summary>
/// <param name="CurrentPassword">The user's current password for verification.</param>
/// <param name="NewPassword">The new password to set.</param>
public sealed record ChangePasswordCommand(
    string CurrentPassword,
    string NewPassword);
