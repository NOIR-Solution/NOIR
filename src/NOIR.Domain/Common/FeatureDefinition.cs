namespace NOIR.Domain.Common;

/// <summary>
/// A single toggleable feature within a module.
/// </summary>
public sealed record FeatureDefinition(
    /// <summary>Unique key, format: "{ModuleName}.{FeatureName}" (e.g., "Ecommerce.Products.Variants")</summary>
    string Name,
    /// <summary>Localization key for display name</summary>
    string DisplayNameKey,
    /// <summary>Localization key for description</summary>
    string DescriptionKey,
    /// <summary>Default state when no DB override exists</summary>
    bool DefaultEnabled = true
);
