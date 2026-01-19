namespace NOIR.Application.Features.Tenants.Commands.ProvisionTenant;

/// <summary>
/// Handler for provisioning a new tenant with optional admin user creation.
/// This is the recommended way to create new tenants as it handles all setup in one operation.
/// </summary>
public class ProvisionTenantCommandHandler
{
    private readonly IMultiTenantStore<Tenant> _tenantStore;
    private readonly IUserIdentityService _identityService;
    private readonly ILocalizationService _localization;
    private readonly ILogger<ProvisionTenantCommandHandler> _logger;

    public ProvisionTenantCommandHandler(
        IMultiTenantStore<Tenant> tenantStore,
        IUserIdentityService identityService,
        ILocalizationService localization,
        ILogger<ProvisionTenantCommandHandler> logger)
    {
        _tenantStore = tenantStore;
        _identityService = identityService;
        _localization = localization;
        _logger = logger;
    }

    public async Task<Result<ProvisionTenantResult>> Handle(
        ProvisionTenantCommand command,
        CancellationToken cancellationToken)
    {
        // Step 1: Validate tenant doesn't already exist
        var existingTenant = await _tenantStore.GetByIdentifierAsync(command.Identifier);
        if (existingTenant is not null)
        {
            return Result.Failure<ProvisionTenantResult>(
                Error.Conflict(
                    string.Format(_localization["auth.tenants.identifierExists"], command.Identifier),
                    ErrorCodes.Business.AlreadyExists));
        }

        // Step 2: Check if domain already exists (if provided)
        if (!string.IsNullOrWhiteSpace(command.Domain))
        {
            var allTenants = await _tenantStore.GetAllAsync();
            if (allTenants.Any(t => string.Equals(t.Domain, command.Domain, StringComparison.OrdinalIgnoreCase)))
            {
                return Result.Failure<ProvisionTenantResult>(
                    Error.Conflict(
                        $"Domain '{command.Domain}' is already in use by another tenant.",
                        ErrorCodes.Business.AlreadyExists));
            }
        }

        // Step 3: Create the tenant
        var tenant = Tenant.Create(
            command.Identifier,
            command.Name,
            command.Domain,
            command.Description,
            command.Note,
            isActive: true);

        var tenantCreated = await _tenantStore.AddAsync(tenant);
        if (!tenantCreated)
        {
            return Result.Failure<ProvisionTenantResult>(
                Error.Internal(
                    _localization["auth.tenants.createFailed"],
                    ErrorCodes.System.InternalError));
        }

        _logger.LogInformation(
            "Provisioned new tenant: {TenantId} ({Identifier})",
            tenant.Id, tenant.Identifier);

        // Step 4: Create admin user if requested
        string? adminUserId = null;
        string? adminEmail = null;
        string? adminCreationError = null;
        var adminUserCreated = false;

        if (command.CreateAdminUser && !string.IsNullOrWhiteSpace(command.AdminEmail))
        {
            // Check if admin email already exists in this tenant
            var existingAdmin = await _identityService.FindByEmailAsync(
                command.AdminEmail, tenant.Id, cancellationToken);

            if (existingAdmin is not null)
            {
                adminCreationError = _localization["auth.users.emailExists"];
                _logger.LogWarning(
                    "Admin user with email {Email} already exists in tenant {TenantId}",
                    command.AdminEmail, tenant.Id);
            }
            else
            {
                // Create the admin user
                var createUserDto = new CreateUserDto(
                    command.AdminEmail,
                    command.AdminFirstName ?? "Admin",
                    command.AdminLastName ?? "User",
                    DisplayName: null,
                    TenantId: tenant.Id);

                var createResult = await _identityService.CreateUserAsync(
                    createUserDto,
                    command.AdminPassword!,
                    cancellationToken);

                if (createResult.Succeeded && createResult.UserId is not null)
                {
                    adminUserId = createResult.UserId;
                    adminEmail = command.AdminEmail;
                    adminUserCreated = true;

                    // Assign Admin role
                    var roleResult = await _identityService.AssignRolesAsync(
                        createResult.UserId,
                        [Domain.Common.Roles.Admin],
                        replaceExisting: false,
                        cancellationToken);

                    if (!roleResult.Succeeded)
                    {
                        _logger.LogWarning(
                            "Failed to assign Admin role to user {UserId} in tenant {TenantId}: {Errors}",
                            createResult.UserId,
                            tenant.Id,
                            string.Join(", ", roleResult.Errors ?? []));
                    }
                    else
                    {
                        _logger.LogInformation(
                            "Created admin user {UserId} ({Email}) for tenant {TenantId}",
                            createResult.UserId, command.AdminEmail, tenant.Id);
                    }
                }
                else
                {
                    // Capture the error message for the response
                    adminCreationError = createResult.Errors?.FirstOrDefault()
                        ?? _localization["auth.users.createFailed"];
                    _logger.LogWarning(
                        "Failed to create admin user for tenant {TenantId}: {Errors}",
                        tenant.Id,
                        string.Join(", ", createResult.Errors ?? []));
                }
            }
        }

        return Result.Success(new ProvisionTenantResult(
            TenantId: tenant.Id,
            Identifier: tenant.Identifier,
            Name: tenant.Name!, // Name is always set during Tenant.Create
            Domain: tenant.Domain,
            IsActive: tenant.IsActive,
            CreatedAt: tenant.CreatedAt,
            AdminUserCreated: adminUserCreated,
            AdminUserId: adminUserId,
            AdminEmail: adminEmail,
            AdminCreationError: adminCreationError));
    }
}
