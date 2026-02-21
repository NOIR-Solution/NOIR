namespace NOIR.Application.Modules.Ecommerce;

public sealed class WishlistModuleDefinition : IModuleDefinition, ISingletonService
{
    public string Name => ModuleNames.Ecommerce.Wishlist;
    public string DisplayNameKey => "modules.ecommerce.wishlist";
    public string DescriptionKey => "modules.ecommerce.wishlist.description";
    public string Icon => "Heart";
    public int SortOrder => 170;
    public bool IsCore => false;
    public bool DefaultEnabled => true;
    public IReadOnlyList<FeatureDefinition> Features => [];
}
