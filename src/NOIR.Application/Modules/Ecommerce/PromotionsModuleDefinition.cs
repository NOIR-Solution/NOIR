namespace NOIR.Application.Modules.Ecommerce;

public sealed class PromotionsModuleDefinition : IModuleDefinition, ISingletonService
{
    public string Name => ModuleNames.Ecommerce.Promotions;
    public string DisplayNameKey => "modules.ecommerce.promotions";
    public string DescriptionKey => "modules.ecommerce.promotions.description";
    public string Icon => "Percent";
    public int SortOrder => 140;
    public bool IsCore => false;
    public bool DefaultEnabled => true;
    public IReadOnlyList<FeatureDefinition> Features => [];
}
