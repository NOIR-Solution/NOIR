namespace NOIR.Application.Modules.Ecommerce;

public sealed class BrandsModuleDefinition : IModuleDefinition, ISingletonService
{
    public string Name => ModuleNames.Ecommerce.Brands;
    public string DisplayNameKey => "modules.ecommerce.brands";
    public string DescriptionKey => "modules.ecommerce.brands.description";
    public string Icon => "Award";
    public int SortOrder => 102;
    public bool IsCore => false;
    public bool DefaultEnabled => true;
    public IReadOnlyList<FeatureDefinition> Features => [];
}
