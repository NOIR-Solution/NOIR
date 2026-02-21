namespace NOIR.Application.Modules.Core;

public sealed class AuthModuleDefinition : IModuleDefinition, ISingletonService
{
    public string Name => ModuleNames.Core.Auth;
    public string DisplayNameKey => "modules.core.auth";
    public string DescriptionKey => "modules.core.auth.description";
    public string Icon => "Shield";
    public int SortOrder => 1;
    public bool IsCore => true;
    public bool DefaultEnabled => true;
    public IReadOnlyList<FeatureDefinition> Features => [];
}
