namespace NOIR.Application.Behaviors;

/// <summary>
/// Wolverine middleware that gates command/query execution based on the [RequiresFeature] attribute.
/// If any required feature is not enabled for the current tenant, throws <see cref="FeatureNotAvailableException"/>.
/// </summary>
public class FeatureCheckMiddleware
{
    private static readonly ConcurrentDictionary<Type, RequiresFeatureAttribute?> AttributeCache = new();

    /// <summary>
    /// Called before the handler executes.
    /// Reads [RequiresFeature] from the message type and checks each feature via IFeatureChecker.
    /// </summary>
    public async Task BeforeAsync(
        Envelope envelope,
        IFeatureChecker featureChecker,
        ILogger<FeatureCheckMiddleware> logger)
    {
        var messageType = envelope.Message?.GetType();
        if (messageType is null)
            return;

        var attribute = AttributeCache.GetOrAdd(
            messageType,
            static t => t.GetCustomAttribute<RequiresFeatureAttribute>());
        if (attribute is null)
            return;

        logger.LogDebug(
            "Checking features {Features} for {MessageType}",
            string.Join(", ", attribute.Features),
            messageType.Name);

        foreach (var feature in attribute.Features)
        {
            if (!await featureChecker.IsEnabledAsync(feature))
            {
                throw new FeatureNotAvailableException(feature);
            }
        }

        logger.LogDebug(
            "All features enabled for {MessageType}",
            messageType.Name);
    }
}
