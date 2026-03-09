namespace NOIR.Application.Modules.Erp;

public sealed class PmModuleDefinition : IModuleDefinition, ISingletonService
{
    public string Name => ModuleNames.Erp.Pm;
    public string DisplayNameKey => "modules.erp.pm";
    public string DescriptionKey => "modules.erp.pm.description";
    public string Icon => "FolderKanban";
    public int SortOrder => 310;
    public bool IsCore => false;
    public bool DefaultEnabled => true;
    public IReadOnlyList<FeatureDefinition> Features =>
    [
        new(ModuleNames.Erp.Pm + ".Projects", "modules.erp.pm.projects", "modules.erp.pm.projects.description"),
    ];
}
