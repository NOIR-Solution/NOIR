namespace NOIR.Application.Modules.Content;

public sealed class BlogCategoriesModuleDefinition : IModuleDefinition, ISingletonService
{
    public string Name => ModuleNames.Content.BlogCategories;
    public string DisplayNameKey => "modules.content.blogcategories";
    public string DescriptionKey => "modules.content.blogcategories.description";
    public string Icon => "FolderTree";
    public int SortOrder => 201;
    public bool IsCore => false;
    public bool DefaultEnabled => true;
    public IReadOnlyList<FeatureDefinition> Features => [];
}
