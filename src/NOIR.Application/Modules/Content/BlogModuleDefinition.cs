namespace NOIR.Application.Modules.Content;

public sealed class BlogModuleDefinition : IModuleDefinition, ISingletonService
{
    public string Name => ModuleNames.Content.Blog;
    public string DisplayNameKey => "modules.content.blog";
    public string DescriptionKey => "modules.content.blog.description";
    public string Icon => "FileText";
    public int SortOrder => 200;
    public bool IsCore => false;
    public bool DefaultEnabled => true;
    public IReadOnlyList<FeatureDefinition> Features => [];
}
