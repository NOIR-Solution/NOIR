namespace NOIR.Application.Modules.Core;

public sealed class UsersModuleDefinition : IModuleDefinition, ISingletonService
{
    public string Name => ModuleNames.Core.Users;
    public string DisplayNameKey => "modules.core.users";
    public string DescriptionKey => "modules.core.users.description";
    public string Icon => "Users";
    public int SortOrder => 2;
    public bool IsCore => true;
    public bool DefaultEnabled => true;
    public IReadOnlyList<FeatureDefinition> Features => [];
}
