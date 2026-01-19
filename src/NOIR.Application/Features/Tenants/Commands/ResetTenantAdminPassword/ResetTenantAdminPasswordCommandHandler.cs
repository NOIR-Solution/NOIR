namespace NOIR.Application.Features.Tenants.Commands.ResetTenantAdminPassword;

/// <summary>
/// Handler for resetting a tenant admin's password.
/// Finds the user with Admin role in the specified tenant and resets their password.
/// </summary>
public class ResetTenantAdminPasswordCommandHandler
{
    private readonly IUserIdentityService _userIdentityService;
    private readonly IRoleIdentityService _roleIdentityService;
    private readonly IMultiTenantStore<Tenant> _tenantStore;
    private readonly ILocalizationService _localization;
    private readonly ILogger<ResetTenantAdminPasswordCommandHandler> _logger;

    public ResetTenantAdminPasswordCommandHandler(
        IUserIdentityService userIdentityService,
        IRoleIdentityService roleIdentityService,
        IMultiTenantStore<Tenant> tenantStore,
        ILocalizationService localization,
        ILogger<ResetTenantAdminPasswordCommandHandler> logger)
    {
        _userIdentityService = userIdentityService;
        _roleIdentityService = roleIdentityService;
        _tenantStore = tenantStore;
        _localization = localization;
        _logger = logger;
    }

    public async Task<Result<ResetTenantAdminPasswordResult>> Handle(
        ResetTenantAdminPasswordCommand command,
        CancellationToken cancellationToken)
    {
        // Step 1: Verify tenant exists
        var tenant = await _tenantStore.GetAsync(command.TenantId);
        if (tenant is null)
        {
            return Result.Failure<ResetTenantAdminPasswordResult>(
                Error.NotFound(
                    _localization["errors.notFound"] ?? "Tenant not found",
                    ErrorCodes.Business.NotFound));
        }

        // Step 2: Find users with Admin role in this tenant
        var adminUsers = await _roleIdentityService.GetUsersInRoleAsync(
            Domain.Common.Roles.Admin,
            command.TenantId,
            cancellationToken);

        if (adminUsers.Count == 0)
        {
            return Result.Failure<ResetTenantAdminPasswordResult>(
                Error.NotFound(
                    _localization["errors.notFound"] ?? "No admin user found for this tenant",
                    ErrorCodes.Business.NotFound));
        }

        // Handle multiple admins - log warning and use first one
        if (adminUsers.Count > 1)
        {
            _logger.LogWarning(
                "Multiple admin users found for tenant {TenantId}: {AdminEmails}. Resetting password for first admin {Email}",
                command.TenantId,
                string.Join(", ", adminUsers.Select(u => u.Email)),
                adminUsers.First().Email);
        }

        var adminUser = adminUsers.First();

        // Step 3: Reset the password
        var resetResult = await _userIdentityService.ResetPasswordAsync(
            adminUser.Id,
            command.NewPassword,
            cancellationToken);

        if (!resetResult.Succeeded)
        {
            _logger.LogWarning(
                "Failed to reset password for tenant admin {UserId} in tenant {TenantId}: {Errors}",
                adminUser.Id,
                command.TenantId,
                string.Join(", ", resetResult.Errors ?? []));

            return Result.Failure<ResetTenantAdminPasswordResult>(
                Error.Internal(
                    resetResult.Errors?.FirstOrDefault() ?? _localization["errors.operationFailed"] ?? "Failed to reset password",
                    ErrorCodes.System.InternalError));
        }

        _logger.LogInformation(
            "SECURITY: Password reset for tenant admin {UserId} ({Email}) in tenant {TenantId}",
            adminUser.Id,
            adminUser.Email,
            command.TenantId);

        return Result.Success(new ResetTenantAdminPasswordResult(
            Success: true,
            Message: _localization["messages.saveSuccess"] ?? "Password reset successfully",
            AdminUserId: adminUser.Id,
            AdminEmail: adminUser.Email));
    }
}
