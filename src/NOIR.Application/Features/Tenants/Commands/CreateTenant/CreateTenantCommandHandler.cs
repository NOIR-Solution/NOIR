using NOIR.Application.Features.Tenants.DTOs;

namespace NOIR.Application.Features.Tenants.Commands.CreateTenant;

/// <summary>
/// Wolverine handler for creating a new tenant.
/// Uses Finbuckle's IMultiTenantStore for tenant persistence.
/// </summary>
public class CreateTenantCommandHandler
{
    private readonly IMultiTenantStore<Tenant> _tenantStore;
    private readonly ILocalizationService _localization;

    public CreateTenantCommandHandler(
        IMultiTenantStore<Tenant> tenantStore,
        ILocalizationService localization)
    {
        _tenantStore = tenantStore;
        _localization = localization;
    }

    public async Task<Result<TenantDto>> Handle(CreateTenantCommand command, CancellationToken cancellationToken)
    {
        // Check if identifier already exists
        var existing = await _tenantStore.GetByIdentifierAsync(command.Identifier);
        if (existing is not null)
        {
            return Result.Failure<TenantDto>(
                Error.Conflict(
                    string.Format(_localization["tenants.identifierExists"], command.Identifier),
                    ErrorCodes.Business.AlreadyExists));
        }

        // Create tenant
        var tenant = Tenant.Create(
            command.Identifier,
            command.Name,
            command.LogoUrl,
            command.PrimaryColor,
            command.AccentColor,
            command.Theme,
            command.IsActive);

        var success = await _tenantStore.AddAsync(tenant);
        if (!success)
        {
            return Result.Failure<TenantDto>(
                Error.Internal(
                    _localization["tenants.createFailed"],
                    ErrorCodes.System.InternalError));
        }

        return Result.Success(MapToDto(tenant));
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
