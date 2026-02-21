namespace NOIR.Web.Filters;

/// <summary>
/// Endpoint filter that checks if a feature/module is enabled for the current tenant.
/// Returns 403 Forbidden if the feature is disabled.
/// </summary>
public sealed class RequireFeatureEndpointFilter : IEndpointFilter
{
    private readonly string _featureName;

    public RequireFeatureEndpointFilter(string featureName)
    {
        _featureName = featureName;
    }

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var featureChecker = context.HttpContext.RequestServices
            .GetRequiredService<IFeatureChecker>();

        if (!await featureChecker.IsEnabledAsync(_featureName))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Feature Not Available",
                detail: $"The module '{_featureName}' is not available for this tenant.",
                type: "https://tools.ietf.org/html/rfc7231#section-6.5.3");
        }

        return await next(context);
    }
}

/// <summary>
/// Extension methods for adding feature requirement filters to endpoint routes.
/// </summary>
public static class FeatureFilterExtensions
{
    /// <summary>
    /// Adds a feature availability check to the endpoint or group.
    /// Returns 403 Forbidden if the specified feature/module is not enabled for the current tenant.
    /// </summary>
    public static TBuilder RequireFeature<TBuilder>(
        this TBuilder builder,
        string featureName) where TBuilder : IEndpointConventionBuilder
    {
        builder.AddEndpointFilter(new RequireFeatureEndpointFilter(featureName));
        return builder;
    }
}
