namespace NOIR.Application.Features.Auth.Commands.RevokeSession;

/// <summary>
/// Command to revoke a specific session by its ID.
/// Users can only revoke their own sessions.
/// </summary>
public sealed record RevokeSessionCommand(Guid SessionId, string? IpAddress = null);
