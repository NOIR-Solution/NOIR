namespace NOIR.Application.Modules.Platform;

public sealed class EmailTemplatesModuleDefinition : IModuleDefinition, ISingletonService
{
    public string Name => ModuleNames.Platform.EmailTemplates;
    public string DisplayNameKey => "modules.platform.emailtemplates";
    public string DescriptionKey => "modules.platform.emailtemplates.description";
    public string Icon => "Palette";
    public int SortOrder => 301;
    public bool IsCore => false;
    public bool DefaultEnabled => true;
    public IReadOnlyList<FeatureDefinition> Features => [];
}
