namespace NOIR.Application.Features.FeatureManagement.Queries.GetCurrentTenantFeatures;

/// <summary>
/// Wolverine handler for retrieving all effective feature states for the current tenant.
/// </summary>
public class GetCurrentTenantFeaturesQueryHandler
{
    private readonly IFeatureChecker _featureChecker;

    public GetCurrentTenantFeaturesQueryHandler(IFeatureChecker featureChecker)
    {
        _featureChecker = featureChecker;
    }

    public async Task<Result<IReadOnlyDictionary<string, EffectiveFeatureState>>> Handle(
        GetCurrentTenantFeaturesQuery query,
        CancellationToken ct)
    {
        var states = await _featureChecker.GetAllStatesAsync(ct);
        return Result.Success(states);
    }
}
