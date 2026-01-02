namespace NOIR.Infrastructure.Audit;

/// <summary>
/// Hosted service that applies IBeforeStateResolverRegistration instances to the WolverineBeforeStateProvider.
/// Runs once at application startup to wire up all DTO before-state resolvers.
/// </summary>
public class BeforeStateRegistrationHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BeforeStateRegistrationHostedService> _logger;

    public BeforeStateRegistrationHostedService(
        IServiceProvider serviceProvider,
        ILogger<BeforeStateRegistrationHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Get the provider instance (singleton)
        var provider = _serviceProvider.GetService<IBeforeStateProvider>() as WolverineBeforeStateProvider;
        if (provider is null)
        {
            _logger.LogWarning("WolverineBeforeStateProvider not found, skipping resolver registration");
            return Task.CompletedTask;
        }

        // Get all registrations and apply them
        var registrations = _serviceProvider.GetServices<IBeforeStateResolverRegistration>();
        var count = 0;

        foreach (var registration in registrations)
        {
            try
            {
                registration.Register(provider);
                count++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register before-state resolver: {Type}", registration.GetType().Name);
            }
        }

        _logger.LogInformation("Registered {Count} before-state resolvers", count);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
