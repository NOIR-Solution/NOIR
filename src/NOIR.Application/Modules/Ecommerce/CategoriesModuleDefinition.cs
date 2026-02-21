namespace NOIR.Application.Modules.Ecommerce;

public sealed class CategoriesModuleDefinition : IModuleDefinition, ISingletonService
{
    public string Name => ModuleNames.Ecommerce.Categories;
    public string DisplayNameKey => "modules.ecommerce.categories";
    public string DescriptionKey => "modules.ecommerce.categories.description";
    public string Icon => "FolderTree";
    public int SortOrder => 101;
    public bool IsCore => false;
    public bool DefaultEnabled => true;
    public IReadOnlyList<FeatureDefinition> Features =>
    [
        new("Ecommerce.Categories.Hierarchy", "modules.ecommerce.categories.hierarchy", "modules.ecommerce.categories.hierarchy.description"),
        new("Ecommerce.Categories.SEO", "modules.ecommerce.categories.seo", "modules.ecommerce.categories.seo.description"),
    ];
}
