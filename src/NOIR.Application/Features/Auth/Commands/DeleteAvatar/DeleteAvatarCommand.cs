namespace NOIR.Application.Features.Auth.Commands.DeleteAvatar;

/// <summary>
/// DTO returned after successful avatar deletion.
/// </summary>
public sealed record AvatarDeleteResultDto(
    bool Success,
    string Message);

/// <summary>
/// Command to delete a user's avatar image.
/// </summary>
public sealed record DeleteAvatarCommand : IAuditableCommand<AvatarDeleteResultDto>
{
    /// <summary>
    /// User ID to update. Set by the endpoint from current user context.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public object? GetTargetId() => UserId;

    public AuditOperationType OperationType => AuditOperationType.Update;

    public string? GetActionDescription() => "Deleted avatar";
}
