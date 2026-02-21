namespace NOIR.Application.Modules.Ecommerce;

public sealed class ProductsModuleDefinition : IModuleDefinition, ISingletonService
{
    public string Name => ModuleNames.Ecommerce.Products;
    public string DisplayNameKey => "modules.ecommerce.products";
    public string DescriptionKey => "modules.ecommerce.products.description";
    public string Icon => "Package";
    public int SortOrder => 100;
    public bool IsCore => false;
    public bool DefaultEnabled => true;
    public IReadOnlyList<FeatureDefinition> Features =>
    [
        new("Ecommerce.Products.Variants", "modules.ecommerce.products.variants", "modules.ecommerce.products.variants.description"),
        new("Ecommerce.Products.Options", "modules.ecommerce.products.options", "modules.ecommerce.products.options.description"),
        new("Ecommerce.Products.Import", "modules.ecommerce.products.import", "modules.ecommerce.products.import.description"),
        new("Ecommerce.Products.Export", "modules.ecommerce.products.export", "modules.ecommerce.products.export.description"),
    ];
}
