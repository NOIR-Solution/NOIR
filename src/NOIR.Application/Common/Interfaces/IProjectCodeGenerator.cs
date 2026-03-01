namespace NOIR.Application.Common.Interfaces;

/// <summary>
/// Generates unique project codes per tenant using atomic database-level increment.
/// </summary>
public interface IProjectCodeGenerator
{
    Task<string> GenerateNextAsync(string? tenantId, CancellationToken ct = default);
}
