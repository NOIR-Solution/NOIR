namespace NOIR.Application.Modules.Ecommerce;

public sealed class ReviewsModuleDefinition : IModuleDefinition, ISingletonService
{
    public string Name => ModuleNames.Ecommerce.Reviews;
    public string DisplayNameKey => "modules.ecommerce.reviews";
    public string DescriptionKey => "modules.ecommerce.reviews.description";
    public string Icon => "Star";
    public int SortOrder => 150;
    public bool IsCore => false;
    public bool DefaultEnabled => true;
    public IReadOnlyList<FeatureDefinition> Features => [];
}
