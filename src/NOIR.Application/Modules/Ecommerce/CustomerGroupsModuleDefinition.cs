namespace NOIR.Application.Modules.Ecommerce;

public sealed class CustomerGroupsModuleDefinition : IModuleDefinition, ISingletonService
{
    public string Name => ModuleNames.Ecommerce.CustomerGroups;
    public string DisplayNameKey => "modules.ecommerce.customergroups";
    public string DescriptionKey => "modules.ecommerce.customergroups.description";
    public string Icon => "UsersRound";
    public int SortOrder => 161;
    public bool IsCore => false;
    public bool DefaultEnabled => true;
    public IReadOnlyList<FeatureDefinition> Features => [];
}
