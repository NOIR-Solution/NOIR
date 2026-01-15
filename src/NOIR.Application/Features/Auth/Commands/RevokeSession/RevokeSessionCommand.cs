namespace NOIR.Application.Features.Auth.Commands.RevokeSession;

/// <summary>
/// Result of a session revocation operation.
/// </summary>
public sealed record RevokeSessionResult(bool Success, string Message);

/// <summary>
/// Command to revoke a specific session by its ID.
/// Users can only revoke their own sessions.
/// </summary>
public sealed record RevokeSessionCommand(
    Guid SessionId,
    string? IpAddress = null) : IAuditableCommand<RevokeSessionResult>
{
    /// <summary>
    /// User ID revoking the session. Set by the endpoint from current user context.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public object? GetTargetId() => UserId;
    public AuditOperationType OperationType => AuditOperationType.Delete;
    public string? GetActionDescription() => "Revoked session";
}
