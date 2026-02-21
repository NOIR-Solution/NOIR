namespace NOIR.Application.Modules.Platform;

public sealed class TenantsModuleDefinition : IModuleDefinition, ISingletonService
{
    public string Name => ModuleNames.Platform.Tenants;
    public string DisplayNameKey => "modules.platform.tenants";
    public string DescriptionKey => "modules.platform.tenants.description";
    public string Icon => "Building";
    public int SortOrder => 300;
    public bool IsCore => false;
    public bool DefaultEnabled => true;
    public IReadOnlyList<FeatureDefinition> Features => [];
}
