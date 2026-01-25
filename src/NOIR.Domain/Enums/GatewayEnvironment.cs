namespace NOIR.Domain.Enums;

/// <summary>
/// Environment configuration for payment gateway.
/// </summary>
public enum GatewayEnvironment
{
    /// <summary>
    /// Sandbox/test environment.
    /// </summary>
    Sandbox = 0,

    /// <summary>
    /// Production environment.
    /// </summary>
    Production = 1
}
