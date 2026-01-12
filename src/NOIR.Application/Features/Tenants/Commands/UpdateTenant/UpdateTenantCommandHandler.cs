using NOIR.Application.Features.Tenants.DTOs;

namespace NOIR.Application.Features.Tenants.Commands.UpdateTenant;

/// <summary>
/// Wolverine handler for updating an existing tenant.
/// Uses Finbuckle's IMultiTenantStore for tenant persistence.
/// </summary>
public class UpdateTenantCommandHandler
{
    private readonly IMultiTenantStore<Tenant> _tenantStore;
    private readonly ILocalizationService _localization;

    public UpdateTenantCommandHandler(
        IMultiTenantStore<Tenant> tenantStore,
        ILocalizationService localization)
    {
        _tenantStore = tenantStore;
        _localization = localization;
    }

    public async Task<Result<TenantDto>> Handle(UpdateTenantCommand command, CancellationToken cancellationToken)
    {
        // Find tenant by ID
        var tenant = await _tenantStore.GetAsync(command.TenantId.ToString());

        if (tenant is null)
        {
            return Result.Failure<TenantDto>(
                Error.NotFound(
                    _localization["tenants.notFound"],
                    ErrorCodes.Auth.TenantNotFound));
        }

        // Create updated tenant using record 'with' expression
        var updatedTenant = tenant.WithUpdatedDetails(
            command.Name,
            command.LogoUrl,
            command.PrimaryColor,
            command.AccentColor,
            command.Theme,
            command.IsActive);

        var success = await _tenantStore.UpdateAsync(updatedTenant);
        if (!success)
        {
            return Result.Failure<TenantDto>(
                Error.Internal(
                    _localization["tenants.updateFailed"],
                    ErrorCodes.System.InternalError));
        }

        return Result.Success(MapToDto(updatedTenant));
    }

    private static TenantDto MapToDto(Tenant tenant) => new(
        tenant.Id,
        tenant.Identifier,
        tenant.Name,
        tenant.LogoUrl,
        tenant.PrimaryColor,
        tenant.AccentColor,
        tenant.Theme,
        tenant.IsActive,
        tenant.CreatedAt,
        tenant.ModifiedAt);
}
