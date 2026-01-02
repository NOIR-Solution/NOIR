namespace NOIR.Application.Common.Interfaces;

/// <summary>
/// Service for resource-based authorization checks.
/// Supports ownership, direct shares, and inherited permissions.
/// </summary>
public interface IResourceAuthorizationService : IScopedService
{
    /// <summary>
    /// Checks if a user can perform an action on a resource.
    /// Checks in order: ownership, direct share, inherited permissions, global role.
    /// </summary>
    Task<bool> AuthorizeAsync(string userId, IResource resource, string action, CancellationToken ct = default);

    /// <summary>
    /// Checks if a user can perform an action on a resource by type and ID.
    /// Use when you don't have the full resource loaded.
    /// </summary>
    Task<bool> AuthorizeAsync(string userId, string resourceType, Guid resourceId, string action, CancellationToken ct = default);

    /// <summary>
    /// Gets the effective permission level for a user on a resource.
    /// Returns null if user has no access.
    /// </summary>
    Task<SharePermission?> GetEffectivePermissionAsync(string userId, IResource resource, CancellationToken ct = default);

    /// <summary>
    /// Gets all resources of a type that a user has access to.
    /// Returns resource IDs with their permission levels.
    /// </summary>
    Task<IReadOnlyList<(Guid ResourceId, SharePermission Permission)>> GetAccessibleResourcesAsync(
        string userId,
        string resourceType,
        CancellationToken ct = default);
}
