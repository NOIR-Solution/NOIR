namespace NOIR.Application.Modules.Content;

public sealed class BlogTagsModuleDefinition : IModuleDefinition, ISingletonService
{
    public string Name => ModuleNames.Content.BlogTags;
    public string DisplayNameKey => "modules.content.blogtags";
    public string DescriptionKey => "modules.content.blogtags.description";
    public string Icon => "Tag";
    public int SortOrder => 202;
    public bool IsCore => false;
    public bool DefaultEnabled => true;
    public IReadOnlyList<FeatureDefinition> Features => [];
}
