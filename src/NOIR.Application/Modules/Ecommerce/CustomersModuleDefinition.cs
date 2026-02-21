namespace NOIR.Application.Modules.Ecommerce;

public sealed class CustomersModuleDefinition : IModuleDefinition, ISingletonService
{
    public string Name => ModuleNames.Ecommerce.Customers;
    public string DisplayNameKey => "modules.ecommerce.customers";
    public string DescriptionKey => "modules.ecommerce.customers.description";
    public string Icon => "UserCheck";
    public int SortOrder => 160;
    public bool IsCore => false;
    public bool DefaultEnabled => true;
    public IReadOnlyList<FeatureDefinition> Features => [];
}
