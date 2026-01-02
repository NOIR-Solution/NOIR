namespace NOIR.Application.Features.Auth.Commands.Logout;

/// <summary>
/// Command to logout a user.
/// Clears authentication cookies and optionally revokes the current refresh token.
/// </summary>
/// <param name="RefreshToken">Optional refresh token to revoke. If not provided, token from cookie is used.</param>
/// <param name="RevokeAllSessions">If true, revokes all refresh tokens for the user (logout from all devices).</param>
public sealed record LogoutCommand(
    string? RefreshToken = null,
    bool RevokeAllSessions = false);
