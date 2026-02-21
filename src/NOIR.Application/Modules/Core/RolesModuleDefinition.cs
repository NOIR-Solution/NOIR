namespace NOIR.Application.Modules.Core;

public sealed class RolesModuleDefinition : IModuleDefinition, ISingletonService
{
    public string Name => ModuleNames.Core.Roles;
    public string DisplayNameKey => "modules.core.roles";
    public string DescriptionKey => "modules.core.roles.description";
    public string Icon => "UserCog";
    public int SortOrder => 3;
    public bool IsCore => true;
    public bool DefaultEnabled => true;
    public IReadOnlyList<FeatureDefinition> Features => [];
}
