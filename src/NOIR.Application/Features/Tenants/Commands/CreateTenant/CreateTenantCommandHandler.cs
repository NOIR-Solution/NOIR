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
                    string.Format(_localization["auth.tenants.identifierExists"], command.Identifier),
                    ErrorCodes.Business.AlreadyExists));
        }

        // Check if domain already exists (if provided)
        if (!string.IsNullOrWhiteSpace(command.Domain))
        {
            var existingByDomain = await _tenantStore.GetAllAsync();
            if (existingByDomain.Any(t => string.Equals(t.Domain, command.Domain, StringComparison.OrdinalIgnoreCase)))
            {
                return Result.Failure<TenantDto>(
                    Error.Conflict(
                        $"Domain '{command.Domain}' is already in use by another tenant.",
                        ErrorCodes.Business.AlreadyExists));
            }
        }

        // Create tenant
        var tenant = Tenant.Create(
            command.Identifier,
            command.Name,
            command.Domain,
            command.Description,
            command.Note,
            command.IsActive);

        var success = await _tenantStore.AddAsync(tenant);
        if (!success)
        {
            return Result.Failure<TenantDto>(
                Error.Internal(
                    _localization["auth.tenants.createFailed"],
                    ErrorCodes.System.InternalError));
        }

        return Result.Success(MapToDto(tenant));
    }

    private static TenantDto MapToDto(Tenant tenant) => new(
        tenant.Id,
        tenant.Identifier,
        tenant.Name,
        tenant.Domain,
        tenant.Description,
        tenant.Note,
        tenant.IsActive,
        tenant.CreatedAt,
        tenant.ModifiedAt);
}
