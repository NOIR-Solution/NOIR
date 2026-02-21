namespace NOIR.Domain.Common;

/// <summary>
/// Marks a command/query as requiring one or more features to be enabled.
/// Checked by FeatureCheckMiddleware in the Wolverine pipeline.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class RequiresFeatureAttribute : Attribute
{
    public string[] Features { get; }

    public RequiresFeatureAttribute(params string[] features)
    {
        Features = features;
    }
}
