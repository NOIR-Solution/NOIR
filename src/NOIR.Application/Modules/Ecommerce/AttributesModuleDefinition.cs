namespace NOIR.Application.Modules.Ecommerce;

public sealed class AttributesModuleDefinition : IModuleDefinition, ISingletonService
{
    public string Name => ModuleNames.Ecommerce.Attributes;
    public string DisplayNameKey => "modules.ecommerce.attributes";
    public string DescriptionKey => "modules.ecommerce.attributes.description";
    public string Icon => "Tags";
    public int SortOrder => 103;
    public bool IsCore => false;
    public bool DefaultEnabled => true;
    public IReadOnlyList<FeatureDefinition> Features =>
    [
        new("Ecommerce.Attributes.FilterIndex", "modules.ecommerce.attributes.filterindex", "modules.ecommerce.attributes.filterindex.description"),
    ];
}
