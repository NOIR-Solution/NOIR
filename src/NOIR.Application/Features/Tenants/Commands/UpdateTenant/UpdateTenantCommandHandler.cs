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
                    _localization["auth.tenants.notFound"],
                    ErrorCodes.Auth.TenantNotFound));
        }

        // Check if identifier is being changed and if new identifier already exists
        var normalizedIdentifier = command.Identifier.ToLowerInvariant().Trim();
        if (!string.Equals(tenant.Identifier, normalizedIdentifier, StringComparison.OrdinalIgnoreCase))
        {
            var existing = await _tenantStore.GetByIdentifierAsync(normalizedIdentifier);
            if (existing is not null)
            {
                return Result.Failure<TenantDto>(
                    Error.Conflict(
                        string.Format(_localization["auth.tenants.identifierExists"], command.Identifier),
                        ErrorCodes.Business.AlreadyExists));
            }
        }

        // Create updated tenant using record 'with' expression
        var updatedTenant = tenant.WithUpdatedDetails(
            command.Identifier,
            command.Name,
            command.IsActive);

        var success = await _tenantStore.UpdateAsync(updatedTenant);
        if (!success)
        {
            return Result.Failure<TenantDto>(
                Error.Internal(
                    _localization["auth.tenants.updateFailed"],
                    ErrorCodes.System.InternalError));
        }

        return Result.Success(MapToDto(updatedTenant));
    }

    private static TenantDto MapToDto(Tenant tenant) => new(
        tenant.Id,
        tenant.Identifier,
        tenant.Name,
        tenant.IsActive,
        tenant.CreatedAt,
        tenant.ModifiedAt);
}
