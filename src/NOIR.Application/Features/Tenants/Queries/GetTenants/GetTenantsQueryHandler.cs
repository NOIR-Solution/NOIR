using NOIR.Application.Features.Tenants.DTOs;

namespace NOIR.Application.Features.Tenants.Queries.GetTenants;

/// <summary>
/// Wolverine handler for getting a paginated list of tenants.
/// Supports search and status filtering.
/// Uses Finbuckle's IMultiTenantStore for tenant retrieval.
/// </summary>
public class GetTenantsQueryHandler
{
    private readonly IMultiTenantStore<Tenant> _tenantStore;

    public GetTenantsQueryHandler(IMultiTenantStore<Tenant> tenantStore)
    {
        _tenantStore = tenantStore;
    }

    public async Task<Result<PaginatedList<TenantListDto>>> Handle(GetTenantsQuery query, CancellationToken cancellationToken)
    {
        // Get all tenants from store (Finbuckle handles query)
        var allTenants = await _tenantStore.GetAllAsync();

        // Apply filters in memory (since Finbuckle's GetAllAsync doesn't support filtering)
        var filteredTenants = allTenants
            .Where(t => !t.IsDeleted) // Exclude soft-deleted
            .AsQueryable();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.ToLowerInvariant();
            filteredTenants = filteredTenants.Where(t =>
                (t.Name != null && t.Name.ToLowerInvariant().Contains(search)) ||
                (t.Identifier != null && t.Identifier.ToLowerInvariant().Contains(search)));
        }

        // Apply status filter
        if (query.IsActive.HasValue)
        {
            filteredTenants = filteredTenants.Where(t => t.IsActive == query.IsActive.Value);
        }

        // Get total count before pagination
        var totalCount = filteredTenants.Count();

        // Apply pagination
        var pagedTenants = filteredTenants
            .OrderBy(t => t.Name)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        // Map to DTOs
        var tenantDtos = pagedTenants.Select(t => new TenantListDto(
            t.Id,
            t.Identifier,
            t.Name,
            t.Domain,
            t.IsActive,
            t.CreatedAt
        )).ToList();

        var result = PaginatedList<TenantListDto>.Create(
            tenantDtos,
            totalCount,
            query.Page,
            query.PageSize);

        return Result.Success(result);
    }
}
