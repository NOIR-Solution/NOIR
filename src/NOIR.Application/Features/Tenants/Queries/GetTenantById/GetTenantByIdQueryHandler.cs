using NOIR.Application.Features.Tenants.DTOs;

namespace NOIR.Application.Features.Tenants.Queries.GetTenantById;

/// <summary>
/// Wolverine handler for getting a tenant by ID.
/// Uses Finbuckle's IMultiTenantStore for tenant retrieval.
/// </summary>
public class GetTenantByIdQueryHandler
{
    private readonly IMultiTenantStore<Tenant> _tenantStore;
    private readonly ILocalizationService _localization;

    public GetTenantByIdQueryHandler(
        IMultiTenantStore<Tenant> tenantStore,
        ILocalizationService localization)
    {
        _tenantStore = tenantStore;
        _localization = localization;
    }

    public async Task<Result<TenantDto>> Handle(GetTenantByIdQuery query, CancellationToken cancellationToken)
    {
        var tenant = await _tenantStore.GetAsync(query.TenantId.ToString());

        if (tenant is null || tenant.IsDeleted)
        {
            return Result.Failure<TenantDto>(
                Error.NotFound(
                    _localization["auth.tenants.notFound"],
                    ErrorCodes.Auth.TenantNotFound));
        }

        return Result.Success(MapToDto(tenant));
    }

    private static TenantDto MapToDto(Tenant tenant) => new(
        tenant.Id,
        tenant.Identifier,
        tenant.Name,
        tenant.IsActive,
        tenant.CreatedAt,
        tenant.ModifiedAt);
}
