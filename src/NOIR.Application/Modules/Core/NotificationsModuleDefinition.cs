namespace NOIR.Application.Modules.Core;

public sealed class NotificationsModuleDefinition : IModuleDefinition, ISingletonService
{
    public string Name => ModuleNames.Core.Notifications;
    public string DisplayNameKey => "modules.core.notifications";
    public string DescriptionKey => "modules.core.notifications.description";
    public string Icon => "Bell";
    public int SortOrder => 8;
    public bool IsCore => true;
    public bool DefaultEnabled => true;
    public IReadOnlyList<FeatureDefinition> Features => [];
}
