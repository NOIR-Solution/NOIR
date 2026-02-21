namespace NOIR.Application.Modules.Analytics;

public sealed class ReportsModuleDefinition : IModuleDefinition, ISingletonService
{
    public string Name => ModuleNames.Analytics.Reports;
    public string DisplayNameKey => "modules.analytics.reports";
    public string DescriptionKey => "modules.analytics.reports.description";
    public string Icon => "BarChart3";
    public int SortOrder => 400;
    public bool IsCore => false;
    public bool DefaultEnabled => true;
    public IReadOnlyList<FeatureDefinition> Features => [];
}
