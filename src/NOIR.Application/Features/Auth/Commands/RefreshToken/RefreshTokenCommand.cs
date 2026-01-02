namespace NOIR.Application.Features.Auth.Commands.RefreshToken;

/// <summary>
/// Command to refresh an access token using a refresh token.
/// Handler returns Result of AuthResponse with typed errors.
/// </summary>
public sealed record RefreshTokenCommand(
    string AccessToken,
    string RefreshToken);
