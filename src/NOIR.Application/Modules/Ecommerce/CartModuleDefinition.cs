namespace NOIR.Application.Modules.Ecommerce;

public sealed class CartModuleDefinition : IModuleDefinition, ISingletonService
{
    public string Name => ModuleNames.Ecommerce.Cart;
    public string DisplayNameKey => "modules.ecommerce.cart";
    public string DescriptionKey => "modules.ecommerce.cart.description";
    public string Icon => "ShoppingBag";
    public int SortOrder => 110;
    public bool IsCore => false;
    public bool DefaultEnabled => true;
    public IReadOnlyList<FeatureDefinition> Features =>
    [
        new("Ecommerce.Cart.GuestCart", "modules.ecommerce.cart.guestcart", "modules.ecommerce.cart.guestcart.description"),
        new("Ecommerce.Cart.MergeCart", "modules.ecommerce.cart.mergecart", "modules.ecommerce.cart.mergecart.description"),
    ];
}
