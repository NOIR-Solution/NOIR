namespace NOIR.Application.Features.FeatureManagement.DTOs;

public sealed record ModuleCatalogDto(
    IReadOnlyList<ModuleDto> Modules
);

public sealed record ModuleDto(
    string Name,
    string DisplayNameKey,
    string DescriptionKey,
    string Icon,
    int SortOrder,
    bool IsCore,
    bool DefaultEnabled,
    IReadOnlyList<FeatureDto> Features,
    bool? IsAvailable = null,
    bool? IsEnabled = null,
    bool? IsEffective = null
);

public sealed record FeatureDto(
    string Name,
    string DisplayNameKey,
    string DescriptionKey,
    bool DefaultEnabled,
    bool? IsAvailable = null,
    bool? IsEnabled = null,
    bool? IsEffective = null
);

public sealed record TenantFeatureStateDto(
    string FeatureName,
    bool IsAvailable,
    bool IsEnabled
);
