namespace NOIR.Application.Features.Auth.Commands.RefreshToken;

/// <summary>
/// Command to refresh an access token using a refresh token.
/// Handler returns Result of AuthResponse with typed errors.
/// </summary>
/// <param name="AccessToken">Current (possibly expired) access token.</param>
/// <param name="RefreshToken">Valid refresh token. If null with UseCookies, will attempt to read from cookie.</param>
/// <param name="UseCookies">If true, updates HttpOnly cookies with new tokens for browser-based auth.</param>
public sealed record RefreshTokenCommand(
    string AccessToken,
    string? RefreshToken = null,
    bool UseCookies = false);
