namespace NOIR.Application.Modules.Ecommerce;

public sealed class CheckoutModuleDefinition : IModuleDefinition, ISingletonService
{
    public string Name => ModuleNames.Ecommerce.Checkout;
    public string DisplayNameKey => "modules.ecommerce.checkout";
    public string DescriptionKey => "modules.ecommerce.checkout.description";
    public string Icon => "BadgeCheck";
    public int SortOrder => 111;
    public bool IsCore => false;
    public bool DefaultEnabled => true;
    public IReadOnlyList<FeatureDefinition> Features => [];
}
