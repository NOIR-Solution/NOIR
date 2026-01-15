namespace NOIR.Application.Features.Auth.Commands.UploadAvatar;

/// <summary>
/// DTO returned after successful avatar upload.
/// </summary>
public sealed record AvatarUploadResultDto(
    string AvatarUrl,
    string Message);

/// <summary>
/// Command to upload a user's avatar image.
/// </summary>
public sealed record UploadAvatarCommand(
    string FileName,
    Stream FileStream,
    string ContentType,
    long FileSize) : IAuditableCommand<AvatarUploadResultDto>
{
    /// <summary>
    /// User ID to update. Set by the endpoint from current user context.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public object? GetTargetId() => UserId;

    public AuditOperationType OperationType => AuditOperationType.Update;

    public string? GetActionDescription() => "Uploaded avatar";
}
