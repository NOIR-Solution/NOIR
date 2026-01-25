namespace NOIR.Domain.Enums;

/// <summary>
/// Health status of a payment gateway.
/// </summary>
public enum GatewayHealthStatus
{
    /// <summary>
    /// Health status is unknown (not yet checked).
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Gateway is healthy and operational.
    /// </summary>
    Healthy = 1,

    /// <summary>
    /// Gateway is experiencing degraded performance.
    /// </summary>
    Degraded = 2,

    /// <summary>
    /// Gateway is unhealthy or unavailable.
    /// </summary>
    Unhealthy = 3
}
