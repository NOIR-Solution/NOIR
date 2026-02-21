namespace NOIR.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when a feature is not enabled for the current tenant.
/// </summary>
public class FeatureNotAvailableException : Exception
{
    public string FeatureName { get; }

    public FeatureNotAvailableException(string featureName)
        : base($"The feature '{featureName}' is not enabled for your organization.")
    {
        FeatureName = featureName;
    }
}
