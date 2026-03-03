namespace NOIR.Application.Features.Tenants.Commands.DeleteTenant;

/// <summary>
/// Wolverine handler for deleting a tenant.
/// Uses Finbuckle's IMultiTenantStore for tenant persistence.
/// Note: Finbuckle's RemoveAsync performs a hard delete.
/// For soft delete, we use TryUpdateAsync with IsDeleted flag.
/// </summary>
public class DeleteTenantCommandHandler
{
    private readonly IMultiTenantStore<Tenant> _tenantStore;
    private readonly ILocalizationService _localization;
    private readonly IEntityUpdateHubContext _entityUpdateHub;

    public DeleteTenantCommandHandler(
        IMultiTenantStore<Tenant> tenantStore,
        ILocalizationService localization,
        IEntityUpdateHubContext entityUpdateHub)
    {
        _tenantStore = tenantStore;
        _localization = localization;
        _entityUpdateHub = entityUpdateHub;
    }

    public async Task<Result<bool>> Handle(DeleteTenantCommand command, CancellationToken cancellationToken)
    {
        // Find tenant by ID
        var tenant = await _tenantStore.GetAsync(command.TenantId.ToString());

        if (tenant is null)
        {
            return Result.Failure<bool>(
                Error.NotFound(
                    _localization["auth.tenants.notFound"],
                    ErrorCodes.Auth.TenantNotFound));
        }

        // Prevent deleting the default tenant
        if (tenant.Identifier == "default")
        {
            return Result.Failure<bool>(
                Error.Validation("identifier",
                    _localization["auth.tenants.cannotDeleteDefault"],
                    ErrorCodes.Business.InvalidState));
        }

        // Soft delete by setting IsDeleted flag and updating via store
        var deletedTenant = tenant.CreateDeleted();

        var success = await _tenantStore.UpdateAsync(deletedTenant);
        if (!success)
        {
            return Result.Failure<bool>(
                Error.Internal(
                    _localization["auth.tenants.deleteFailed"],
                    ErrorCodes.System.InternalError));
        }

        if (Guid.TryParse(deletedTenant.Id, out var tenantGuid))
        {
            await _entityUpdateHub.PublishEntityUpdatedAsync(
                entityType: "Tenant",
                entityId: tenantGuid,
                operation: EntityOperation.Deleted,
                tenantId: deletedTenant.Id,
                ct: cancellationToken);
        }

        return Result.Success(true);
    }
}
