namespace NOIR.Application.Common.Interfaces;

/// <summary>
/// Invalidates feature cache for a tenant and notifies connected clients.
/// </summary>
public interface IFeatureCacheInvalidator
{
    Task InvalidateAsync(string tenantId, CancellationToken ct = default);
}
