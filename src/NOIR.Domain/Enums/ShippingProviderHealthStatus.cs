namespace NOIR.Domain.Enums;

/// <summary>
/// Health status of a shipping provider connection.
/// </summary>
public enum ShippingProviderHealthStatus
{
    /// <summary>
    /// Health status has not been checked yet.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Provider API is responding normally.
    /// </summary>
    Healthy = 1,

    /// <summary>
    /// Provider API is responding but with degraded performance.
    /// </summary>
    Degraded = 2,

    /// <summary>
    /// Provider API is not responding or returning errors.
    /// </summary>
    Unhealthy = 3
}
