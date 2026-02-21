namespace NOIR.Domain.Interfaces;

/// <summary>
/// Aggregates all code-defined module definitions.
/// </summary>
public interface IModuleCatalog
{
    IReadOnlyList<IModuleDefinition> GetAllModules();
    IModuleDefinition? GetModule(string moduleName);
    FeatureDefinition? GetFeature(string featureName);
    bool IsCore(string featureName);
    bool Exists(string featureName);
    /// <summary>
    /// Returns the parent module name for a feature (e.g., "Ecommerce.Products" for "Ecommerce.Products.Variants").
    /// </summary>
    string? GetParentModuleName(string featureName);
}
