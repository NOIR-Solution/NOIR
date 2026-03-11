namespace NOIR.Application.Features.ApiKeys.Specifications;

/// <summary>
/// Get an API key by ID (read-only).
/// </summary>
public sealed class ApiKeyByIdSpec : Specification<Domain.Entities.ApiKey>
{
    public ApiKeyByIdSpec(Guid id)
    {
        Query.Where(k => k.Id == id)
            .TagWith("ApiKeyById");
    }
}

/// <summary>
/// Get an API key by ID for mutation (with tracking).
/// </summary>
public sealed class ApiKeyByIdForUpdateSpec : Specification<Domain.Entities.ApiKey>
{
    public ApiKeyByIdForUpdateSpec(Guid id)
    {
        Query.Where(k => k.Id == id)
            .AsTracking()
            .TagWith("ApiKeyByIdForUpdate");
    }
}

/// <summary>
/// Get all API keys for a specific user (profile tab).
/// </summary>
public sealed class ApiKeysByUserIdSpec : Specification<Domain.Entities.ApiKey>
{
    public ApiKeysByUserIdSpec(string userId)
    {
        Query.Where(k => k.UserId == userId)
            .OrderByDescending(k => k.CreatedAt)
            .TagWith("ApiKeysByUserId");
    }
}

/// <summary>
/// Get all API keys in a tenant (admin view).
/// </summary>
public sealed class ApiKeysByTenantSpec : Specification<Domain.Entities.ApiKey>
{
    public ApiKeysByTenantSpec()
    {
        Query.OrderByDescending(k => k.CreatedAt)
            .TagWith("ApiKeysByTenant");
    }
}

/// <summary>
/// Count active (non-revoked, non-deleted) API keys for a user.
/// Used for enforcing max keys per user limit.
/// </summary>
public sealed class ActiveApiKeysCountByUserSpec : Specification<Domain.Entities.ApiKey>
{
    public ActiveApiKeysCountByUserSpec(string userId)
    {
        Query.Where(k => k.UserId == userId && !k.IsRevoked)
            .TagWith("ActiveApiKeysCountByUser");
    }
}
