namespace NOIR.Application.Features.FeatureManagement.Queries.GetTenantFeatureStates;

/// <summary>
/// Wolverine handler for retrieving module states for a specific tenant.
/// </summary>
public class GetTenantFeatureStatesQueryHandler
{
    private readonly IModuleCatalog _catalog;
    private readonly IApplicationDbContext _dbContext;

    public GetTenantFeatureStatesQueryHandler(
        IModuleCatalog catalog,
        IApplicationDbContext dbContext)
    {
        _catalog = catalog;
        _dbContext = dbContext;
    }

    public async Task<Result<ModuleCatalogDto>> Handle(
        GetTenantFeatureStatesQuery query,
        CancellationToken ct)
    {
        var dbStates = await _dbContext.TenantModuleStates
            .Where(x => x.TenantId == query.TenantId)
            .TagWith("GetTenantFeatureStates")
            .ToListAsync(ct);

        var stateMap = dbStates.ToDictionary(
            s => s.FeatureName,
            StringComparer.OrdinalIgnoreCase);

        var modules = _catalog.GetAllModules().Select(m =>
        {
            stateMap.TryGetValue(m.Name, out var moduleState);
            var isAvailable = moduleState?.IsAvailable ?? true;
            var isEnabled = moduleState?.IsEnabled ?? m.DefaultEnabled;
            var isEffective = isAvailable && isEnabled;

            return new ModuleDto(
                m.Name,
                m.DisplayNameKey,
                m.DescriptionKey,
                m.Icon,
                m.SortOrder,
                m.IsCore,
                m.DefaultEnabled,
                m.Features.Select(f =>
                {
                    stateMap.TryGetValue(f.Name, out var featureState);
                    var fAvailable = featureState?.IsAvailable ?? true;
                    var fEnabled = featureState?.IsEnabled ?? f.DefaultEnabled;
                    var fEffective = isEffective && fAvailable && fEnabled;
                    return new FeatureDto(
                        f.Name, f.DisplayNameKey, f.DescriptionKey, f.DefaultEnabled,
                        fAvailable, fEnabled, fEffective);
                }).ToList(),
                isAvailable, isEnabled, m.IsCore ? true : isEffective);
        }).ToList();

        return Result.Success(new ModuleCatalogDto(modules));
    }
}
