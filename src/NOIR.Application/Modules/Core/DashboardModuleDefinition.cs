namespace NOIR.Application.Modules.Core;

public sealed class DashboardModuleDefinition : IModuleDefinition, ISingletonService
{
    public string Name => ModuleNames.Core.Dashboard;
    public string DisplayNameKey => "modules.core.dashboard";
    public string DescriptionKey => "modules.core.dashboard.description";
    public string Icon => "LayoutDashboard";
    public int SortOrder => 5;
    public bool IsCore => true;
    public bool DefaultEnabled => true;
    public IReadOnlyList<FeatureDefinition> Features => [];
}
