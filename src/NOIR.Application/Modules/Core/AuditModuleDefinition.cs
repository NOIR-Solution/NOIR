namespace NOIR.Application.Modules.Core;

public sealed class AuditModuleDefinition : IModuleDefinition, ISingletonService
{
    public string Name => ModuleNames.Core.Audit;
    public string DisplayNameKey => "modules.core.audit";
    public string DescriptionKey => "modules.core.audit.description";
    public string Icon => "ClipboardList";
    public int SortOrder => 7;
    public bool IsCore => true;
    public bool DefaultEnabled => true;
    public IReadOnlyList<FeatureDefinition> Features => [];
}
