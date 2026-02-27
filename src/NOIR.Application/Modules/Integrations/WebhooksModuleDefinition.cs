namespace NOIR.Application.Modules.Integrations;

public sealed class WebhooksModuleDefinition : IModuleDefinition, ISingletonService
{
    public string Name => ModuleNames.Integrations.Webhooks;
    public string DisplayNameKey => "modules.integrations.webhooks";
    public string DescriptionKey => "modules.integrations.webhooks.description";
    public string Icon => "Webhook";
    public int SortOrder => 400;
    public bool IsCore => false;
    public bool DefaultEnabled => false;
    public IReadOnlyList<FeatureDefinition> Features => [];
}
