namespace NOIR.Application.Modules.Ecommerce;

public sealed class InventoryModuleDefinition : IModuleDefinition, ISingletonService
{
    public string Name => ModuleNames.Ecommerce.Inventory;
    public string DisplayNameKey => "modules.ecommerce.inventory";
    public string DescriptionKey => "modules.ecommerce.inventory.description";
    public string Icon => "Warehouse";
    public int SortOrder => 130;
    public bool IsCore => false;
    public bool DefaultEnabled => true;
    public IReadOnlyList<FeatureDefinition> Features =>
    [
        new("Ecommerce.Inventory.StockIn", "modules.ecommerce.inventory.stockin", "modules.ecommerce.inventory.stockin.description"),
        new("Ecommerce.Inventory.StockOut", "modules.ecommerce.inventory.stockout", "modules.ecommerce.inventory.stockout.description"),
    ];
}
