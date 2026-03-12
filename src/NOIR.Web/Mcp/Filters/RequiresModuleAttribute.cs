namespace NOIR.Web.Mcp.Filters;

/// <summary>
/// Declares that an MCP tool class requires a specific module to be enabled.
/// Used by FeatureGatedToolFilter to check module availability at runtime.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class RequiresModuleAttribute : Attribute
{
    public string ModuleName { get; }

    public RequiresModuleAttribute(string moduleName)
    {
        ModuleName = moduleName;
    }
}
