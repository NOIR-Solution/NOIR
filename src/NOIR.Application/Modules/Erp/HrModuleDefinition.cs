namespace NOIR.Application.Modules.Erp;

public sealed class HrModuleDefinition : IModuleDefinition, ISingletonService
{
    public string Name => ModuleNames.Erp.Hr;
    public string DisplayNameKey => "modules.erp.hr";
    public string DescriptionKey => "modules.erp.hr.description";
    public string Icon => "Users";
    public int SortOrder => 200;
    public bool IsCore => false;
    public bool DefaultEnabled => true;
    public IReadOnlyList<FeatureDefinition> Features =>
    [
        new(ModuleNames.Erp.Hr + ".Employees", "modules.erp.hr.employees", "modules.erp.hr.employees.description"),
        new(ModuleNames.Erp.Hr + ".Departments", "modules.erp.hr.departments", "modules.erp.hr.departments.description"),
        new(ModuleNames.Erp.Hr + ".Tags", "modules.erp.hr.tags", "modules.erp.hr.tags.description"),
    ];
}
