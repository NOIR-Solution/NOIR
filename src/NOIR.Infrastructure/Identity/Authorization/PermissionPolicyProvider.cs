namespace NOIR.Infrastructure.Identity.Authorization;

/// <summary>
/// Dynamic policy provider for permission-based authorization.
/// Creates policies on-demand for any permission string.
/// Supports both prefixed ("Permission:roles:read") and direct ("roles:read") permission names.
/// </summary>
public class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private const string PolicyPrefix = "Permission:";
    private readonly DefaultAuthorizationPolicyProvider _fallbackPolicyProvider;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        // Handle prefixed permission names (e.g., "Permission:roles:read")
        if (policyName.StartsWith(PolicyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var permission = policyName[PolicyPrefix.Length..];
            return CreatePermissionPolicyAsync(permission);
        }

        // Handle direct permission names (e.g., "roles:read", "users:create")
        // Permission names contain a colon separating resource:action
        if (policyName.Contains(':') && Permissions.All.Contains(policyName))
        {
            return CreatePermissionPolicyAsync(policyName);
        }

        return _fallbackPolicyProvider.GetPolicyAsync(policyName);
    }

    private static Task<AuthorizationPolicy?> CreatePermissionPolicyAsync(string permission)
    {
        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(permission))
            .Build();

        return Task.FromResult<AuthorizationPolicy?>(policy);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() =>
        _fallbackPolicyProvider.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() =>
        _fallbackPolicyProvider.GetFallbackPolicyAsync();
}
