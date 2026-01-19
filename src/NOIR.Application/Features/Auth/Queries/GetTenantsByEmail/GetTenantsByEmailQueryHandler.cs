namespace NOIR.Application.Features.Auth.Queries.GetTenantsByEmail;

/// <summary>
/// Handler for GetTenantsByEmailQuery.
/// Finds all tenants where a user with the given email exists.
/// </summary>
public class GetTenantsByEmailQueryHandler
{
    private readonly IUserIdentityService _identityService;
    private readonly ILocalizationService _localization;

    public GetTenantsByEmailQueryHandler(
        IUserIdentityService identityService,
        ILocalizationService localization)
    {
        _identityService = identityService;
        _localization = localization;
    }

    public async Task<Result<GetTenantsByEmailResponse>> Handle(
        GetTenantsByEmailQuery query,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query.Email))
        {
            return Result.Failure<GetTenantsByEmailResponse>(
                Error.Validation("Email", _localization["validation.required"], ErrorCodes.Validation.Required));
        }

        var normalizedEmail = _identityService.NormalizeEmail(query.Email);
        var tenantInfos = await _identityService.FindTenantsByEmailAsync(normalizedEmail, cancellationToken);

        if (tenantInfos.Count == 0)
        {
            // Email not found - return empty response (don't reveal if email exists)
            // We still return success but with empty tenants to avoid timing attacks
            return Result.Success(new GetTenantsByEmailResponse(
                Email: query.Email,
                Tenants: [],
                SingleTenant: false,
                AutoSelectedTenantId: null,
                AutoSelectedTenantIdentifier: null));
        }

        var tenantOptions = tenantInfos
            .Select(t => new TenantOption(t.TenantId, t.TenantIdentifier, t.TenantName))
            .ToList();

        // Check for single tenant scenario (auto-select)
        var isSingleTenant = tenantOptions.Count == 1;
        var autoSelectedTenantId = isSingleTenant ? tenantOptions[0].TenantId : null;
        var autoSelectedTenantIdentifier = isSingleTenant ? tenantOptions[0].Identifier : null;

        return Result.Success(new GetTenantsByEmailResponse(
            Email: query.Email,
            Tenants: tenantOptions,
            SingleTenant: isSingleTenant,
            AutoSelectedTenantId: autoSelectedTenantId,
            AutoSelectedTenantIdentifier: autoSelectedTenantIdentifier));
    }
}
