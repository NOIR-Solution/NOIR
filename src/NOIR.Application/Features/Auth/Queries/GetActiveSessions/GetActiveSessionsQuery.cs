namespace NOIR.Application.Features.Auth.Queries.GetActiveSessions;

/// <summary>
/// Query to get active sessions for the current user.
/// Returns a list of active refresh tokens (sessions) with device info.
/// </summary>
public sealed record GetActiveSessionsQuery(string? CurrentRefreshToken = null);

/// <summary>
/// Represents an active session/device.
/// </summary>
public sealed record ActiveSessionDto(
    Guid Id,
    string? DeviceName,
    string? UserAgent,
    string? IpAddress,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    bool IsCurrent);
