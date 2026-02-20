namespace NOIR.Application.Common.Interfaces;

/// <summary>
/// Generates unique order numbers using atomic database sequences.
/// </summary>
public interface IOrderNumberGenerator
{
    /// <summary>
    /// Generates the next order number atomically. Thread-safe and race-condition-proof.
    /// Format: ORD-YYYYMMDD-XXXX where XXXX is a zero-padded daily sequence.
    /// </summary>
    Task<string> GenerateNextAsync(string? tenantId, CancellationToken cancellationToken = default);
}
