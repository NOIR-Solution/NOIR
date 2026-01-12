namespace NOIR.Application.Features.Auth.Commands.UpdateUserProfile;

/// <summary>
/// Command to update the current user's profile.
/// Implements IAuditableCommand to enable DTO-level diff tracking.
/// </summary>
public sealed record UpdateUserProfileCommand(
    string? FirstName,
    string? LastName,
    string? DisplayName,
    string? PhoneNumber) : IAuditableCommand<UserProfileDto>
{
    /// <summary>
    /// User ID to update. Set by the endpoint from current user context.
    /// Excluded from JSON serialization since it's set programmatically.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public object? GetTargetId() => UserId;

    public AuditOperationType OperationType => AuditOperationType.Update;
}
