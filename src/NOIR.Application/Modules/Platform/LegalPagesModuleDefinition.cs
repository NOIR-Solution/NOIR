namespace NOIR.Application.Modules.Platform;

public sealed class LegalPagesModuleDefinition : IModuleDefinition, ISingletonService
{
    public string Name => ModuleNames.Platform.LegalPages;
    public string DisplayNameKey => "modules.platform.legalpages";
    public string DescriptionKey => "modules.platform.legalpages.description";
    public string Icon => "Scale";
    public int SortOrder => 302;
    public bool IsCore => false;
    public bool DefaultEnabled => true;
    public IReadOnlyList<FeatureDefinition> Features => [];
}
