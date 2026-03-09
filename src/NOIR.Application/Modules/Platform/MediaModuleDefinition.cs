namespace NOIR.Application.Modules.Platform;

public sealed class MediaModuleDefinition : IModuleDefinition, ISingletonService
{
    public string Name => ModuleNames.Platform.Media;
    public string DisplayNameKey => "modules.platform.media";
    public string DescriptionKey => "modules.platform.media.description";
    public string Icon => "Image";
    public int SortOrder => 175;
    public bool IsCore => false;
    public bool DefaultEnabled => true;
    public IReadOnlyList<FeatureDefinition> Features => [];
}
