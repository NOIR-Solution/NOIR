namespace NOIR.Application.Modules.Ecommerce;

public sealed class OrdersModuleDefinition : IModuleDefinition, ISingletonService
{
    public string Name => ModuleNames.Ecommerce.Orders;
    public string DisplayNameKey => "modules.ecommerce.orders";
    public string DescriptionKey => "modules.ecommerce.orders.description";
    public string Icon => "ShoppingCart";
    public int SortOrder => 120;
    public bool IsCore => false;
    public bool DefaultEnabled => true;
    public IReadOnlyList<FeatureDefinition> Features =>
    [
        new("Ecommerce.Orders.Returns", "modules.ecommerce.orders.returns", "modules.ecommerce.orders.returns.description"),
        new("Ecommerce.Orders.Cancellations", "modules.ecommerce.orders.cancellations", "modules.ecommerce.orders.cancellations.description"),
    ];
}
