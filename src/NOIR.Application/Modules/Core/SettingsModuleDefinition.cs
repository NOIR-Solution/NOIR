namespace NOIR.Application.Modules.Core;

public sealed class SettingsModuleDefinition : IModuleDefinition, ISingletonService
{
    public string Name => ModuleNames.Core.Settings;
    public string DisplayNameKey => "modules.core.settings";
    public string DescriptionKey => "modules.core.settings.description";
    public string Icon => "Settings";
    public int SortOrder => 6;
    public bool IsCore => true;
    public bool DefaultEnabled => true;
    public IReadOnlyList<FeatureDefinition> Features => [];
}
