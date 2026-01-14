namespace NOIR.Application.Features.Auth.Commands.PasswordReset.RequestPasswordReset;

/// <summary>
/// Command to initiate a password reset request.
/// Sends OTP to the user's email address.
/// </summary>
public sealed record RequestPasswordResetCommand(
    string Email)
{
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
}
