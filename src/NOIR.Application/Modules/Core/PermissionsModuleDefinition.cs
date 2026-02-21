namespace NOIR.Application.Modules.Core;

public sealed class PermissionsModuleDefinition : IModuleDefinition, ISingletonService
{
    public string Name => ModuleNames.Core.Permissions;
    public string DisplayNameKey => "modules.core.permissions";
    public string DescriptionKey => "modules.core.permissions.description";
    public string Icon => "Lock";
    public int SortOrder => 4;
    public bool IsCore => true;
    public bool DefaultEnabled => true;
    public IReadOnlyList<FeatureDefinition> Features => [];
}
