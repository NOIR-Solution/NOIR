namespace NOIR.Application.Features.Auth.Commands.ChangeEmail;

/// <summary>
/// Command to initiate an email change request.
/// Sends OTP to the new email address.
/// </summary>
public sealed record RequestEmailChangeCommand(
    string NewEmail) : IAuditableCommand<EmailChangeRequestResult>
{
    /// <summary>
    /// User ID requesting the change. Set by the endpoint from current user context.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    /// <summary>
    /// Tenant ID for multi-tenancy. Set by the endpoint.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string? TenantId { get; init; }

    /// <summary>
    /// IP address of the requester. Set by the endpoint.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string? IpAddress { get; init; }

    public object? GetTargetId() => UserId;

    public AuditOperationType OperationType => AuditOperationType.Update;
}
