namespace NOIR.Domain.Interfaces;

/// <summary>
/// A code-defined module with its child features.
/// </summary>
public interface IModuleDefinition
{
    /// <summary>Unique key (e.g., "Ecommerce", "Content.Blog")</summary>
    string Name { get; }
    /// <summary>Localization key for display name (e.g., "modules.ecommerce")</summary>
    string DisplayNameKey { get; }
    /// <summary>Localization key for description</summary>
    string DescriptionKey { get; }
    /// <summary>Lucide icon name for UI (e.g., "ShoppingCart", "FileText")</summary>
    string Icon { get; }
    /// <summary>Sort order in admin UI</summary>
    int SortOrder { get; }
    /// <summary>Core modules cannot be disabled (Auth, Users, Dashboard, etc.)</summary>
    bool IsCore { get; }
    /// <summary>Default state when no DB override exists</summary>
    bool DefaultEnabled { get; }
    /// <summary>Child features within this module</summary>
    IReadOnlyList<FeatureDefinition> Features { get; }
}
