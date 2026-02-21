namespace NOIR.Application.Modules.Analytics;

public sealed class DeveloperLogsModuleDefinition : IModuleDefinition, ISingletonService
{
    public string Name => ModuleNames.Analytics.DeveloperLogs;
    public string DisplayNameKey => "modules.analytics.developerlogs";
    public string DescriptionKey => "modules.analytics.developerlogs.description";
    public string Icon => "Terminal";
    public int SortOrder => 401;
    public bool IsCore => false;
    public bool DefaultEnabled => true;
    public IReadOnlyList<FeatureDefinition> Features => [];
}
