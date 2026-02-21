namespace NOIR.Infrastructure.Services;

/// <summary>
/// Aggregates all code-defined module definitions into an O(1) lookup catalog.
/// Registered as singleton via ISingletonService marker.
/// </summary>
public sealed class ModuleCatalog : IModuleCatalog, ISingletonService
{
    private readonly IReadOnlyList<IModuleDefinition> _modules;
    private readonly Dictionary<string, IModuleDefinition> _modulesByName;
    private readonly Dictionary<string, FeatureDefinition> _featuresByName;
    private readonly HashSet<string> _coreNames;

    public ModuleCatalog(IEnumerable<IModuleDefinition> moduleDefinitions)
    {
        _modules = moduleDefinitions.OrderBy(m => m.SortOrder).ToList();
        _modulesByName = _modules.ToDictionary(m => m.Name, StringComparer.OrdinalIgnoreCase);
        _featuresByName = _modules
            .SelectMany(m => m.Features)
            .ToDictionary(f => f.Name, StringComparer.OrdinalIgnoreCase);
        _coreNames = _modules
            .Where(m => m.IsCore)
            .Select(m => m.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<IModuleDefinition> GetAllModules() => _modules;
    public IModuleDefinition? GetModule(string name) => _modulesByName.GetValueOrDefault(name);
    public FeatureDefinition? GetFeature(string name) => _featuresByName.GetValueOrDefault(name);
    public bool IsCore(string name) => _coreNames.Contains(name);
    public bool Exists(string name) => _modulesByName.ContainsKey(name) || _featuresByName.ContainsKey(name);

    public string? GetParentModuleName(string featureName)
    {
        var lastDot = featureName.LastIndexOf('.');
        while (lastDot > 0)
        {
            var parentName = featureName[..lastDot];
            if (_modulesByName.ContainsKey(parentName))
                return parentName;
            lastDot = parentName.LastIndexOf('.');
        }
        return null;
    }
}
