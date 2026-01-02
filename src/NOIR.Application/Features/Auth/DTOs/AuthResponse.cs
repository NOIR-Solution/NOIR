namespace NOIR.Application.Features.Auth.DTOs;

/// <summary>
/// Response returned after successful authentication.
/// </summary>
public sealed record AuthResponse(
    string UserId,
    string Email,
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt);
