namespace NOIR.Application.Features.FeatureManagement.Queries.GetModuleCatalog;

/// <summary>
/// Wolverine handler for retrieving the full module catalog.
/// </summary>
public class GetModuleCatalogQueryHandler
{
    private readonly IModuleCatalog _catalog;

    public GetModuleCatalogQueryHandler(IModuleCatalog catalog)
    {
        _catalog = catalog;
    }

    public Task<Result<ModuleCatalogDto>> Handle(
        GetModuleCatalogQuery query,
        CancellationToken ct)
    {
        var modules = _catalog.GetAllModules().Select(m => new ModuleDto(
            m.Name,
            m.DisplayNameKey,
            m.DescriptionKey,
            m.Icon,
            m.SortOrder,
            m.IsCore,
            m.DefaultEnabled,
            m.Features.Select(f => new FeatureDto(
                f.Name,
                f.DisplayNameKey,
                f.DescriptionKey,
                f.DefaultEnabled
            )).ToList()
        )).ToList();

        return Task.FromResult(Result.Success(new ModuleCatalogDto(modules)));
    }
}
