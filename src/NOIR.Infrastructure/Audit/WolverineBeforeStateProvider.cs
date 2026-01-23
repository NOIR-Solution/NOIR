using System.Collections.Concurrent;

namespace NOIR.Infrastructure.Audit;

/// <summary>
/// Wolverine-based implementation of IBeforeStateProvider.
/// Uses registered resolvers to fetch DTO state before handler execution.
/// </summary>
/// <remarks>
/// This provider enables DTO-level diff tracking for update operations.
/// Register resolvers for each auditable DTO type to enable before/after comparison.
/// </remarks>
public class WolverineBeforeStateProvider : IBeforeStateProvider, ISingletonService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WolverineBeforeStateProvider> _logger;
    private readonly ConcurrentDictionary<Type, Func<IServiceProvider, object, CancellationToken, Task<object?>>> _resolvers = new();

    // Lazy initialization state
    private volatile bool _initialized;
    private readonly object _initLock = new();

    public WolverineBeforeStateProvider(
        IServiceProvider serviceProvider,
        ILogger<WolverineBeforeStateProvider> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Ensures all resolver registrations are applied. Thread-safe and idempotent.
    /// Called automatically on first use - no need for hosted service.
    /// </summary>
    public void EnsureInitialized()
    {
        if (_initialized) return;

        lock (_initLock)
        {
            if (_initialized) return;

            try
            {
                // Get all resolver registrations from DI and apply them
                var registrations = _serviceProvider.GetServices<IBeforeStateResolverRegistration>();
                var count = 0;

                foreach (var registration in registrations)
                {
                    try
                    {
                        registration.Register(this);
                        count++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to register before-state resolver: {Type}", registration.GetType().Name);
                    }
                }

                _logger.LogDebug("Lazily registered {Count} before-state resolvers", count);
            }
            finally
            {
                _initialized = true;
            }
        }
    }

    /// <summary>
    /// Registers a resolver function for a DTO type.
    /// Thread-safe: ConcurrentDictionary ensures atomic registration.
    /// </summary>
    /// <typeparam name="TDto">The DTO type to register.</typeparam>
    /// <param name="resolver">Function to fetch the DTO given an ID.</param>
    public void Register<TDto>(Func<IServiceProvider, object, CancellationToken, Task<TDto?>> resolver)
        where TDto : class
    {
        ArgumentNullException.ThrowIfNull(resolver);

        // Store the resolver with type-safe wrapper
        // AddOrUpdate is atomic and handles race conditions
        _resolvers.AddOrUpdate(
            typeof(TDto),
            _ => async (sp, id, ct) => await resolver(sp, id, ct).ConfigureAwait(false),
            (_, __) => async (sp, id, ct) => await resolver(sp, id, ct).ConfigureAwait(false));

        _logger.LogDebug("Registered before-state resolver for {DtoType}", typeof(TDto).Name);
    }

    /// <summary>
    /// Registers a resolver that uses Wolverine IMessageBus to fetch the DTO.
    /// Thread-safe: Uses scoped service provider to avoid cross-thread issues.
    /// </summary>
    /// <typeparam name="TDto">The DTO type to register.</typeparam>
    /// <typeparam name="TQuery">The query type that returns the DTO.</typeparam>
    /// <param name="queryFactory">Factory to create query from target ID.</param>
    public void RegisterWithQuery<TDto, TQuery>(Func<object, TQuery> queryFactory)
        where TDto : class
        where TQuery : class
    {
        ArgumentNullException.ThrowIfNull(queryFactory);

        // Create resolver that properly scopes the IMessageBus
        async Task<object?> ScopedResolver(IServiceProvider sp, object id, CancellationToken ct)
        {
            // Create new scope to ensure thread-safety and proper lifecycle
            await using var scope = sp.CreateAsyncScope();
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            var query = queryFactory(id);

            try
            {
                var result = await bus.InvokeAsync<Result<TDto>>(query, ct).ConfigureAwait(false);
                return result.IsSuccess ? result.Value : null;
            }
            catch (OperationCanceledException)
            {
                // Cancellation is expected, don't log as error
                return null;
            }
            catch
            {
                // If the query fails (e.g., entity not found), return null
                return null;
            }
        }

        // AddOrUpdate is atomic and handles race conditions
        _resolvers.AddOrUpdate(
            typeof(TDto),
            _ => ScopedResolver,
            (_, __) => ScopedResolver);

        _logger.LogDebug("Registered Wolverine query resolver for {DtoType}", typeof(TDto).Name);
    }

    /// <inheritdoc />
    public async Task<object?> GetBeforeStateAsync(Type dtoType, object targetId, CancellationToken cancellationToken)
    {
        // Defensive null checks to prevent NullReferenceException
        if (dtoType is null)
        {
            _logger.LogWarning("GetBeforeStateAsync called with null dtoType");
            return null;
        }

        if (targetId is null)
        {
            _logger.LogWarning("GetBeforeStateAsync called with null targetId for {DtoType}", dtoType.Name);
            return null;
        }

        // Lazy initialization - apply all registrations on first use
        EnsureInitialized();

        // TryGetValue is thread-safe with ConcurrentDictionary
        if (!_resolvers.TryGetValue(dtoType, out var resolver))
        {
            _logger.LogDebug("No before-state resolver registered for {DtoType}", dtoType.Name);
            return null;
        }

        try
        {
            // Use ConfigureAwait(false) since we don't need synchronization context
            var result = await resolver(_serviceProvider, targetId, cancellationToken).ConfigureAwait(false);
            _logger.LogDebug(
                "Fetched before-state for {DtoType} with ID {TargetId}: {Found}",
                dtoType.Name, targetId, result is not null);
            return result;
        }
        catch (OperationCanceledException)
        {
            // Cancellation is expected, don't log as error
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch before-state for {DtoType} with ID {TargetId}", dtoType.Name, targetId);
            return null;
        }
    }
}

/// <summary>
/// Extension methods for registering IBeforeStateProvider.
/// </summary>
public static class BeforeStateProviderExtensions
{
    /// <summary>
    /// Registers a resolver for a DTO type using a simple fetch function.
    /// </summary>
    public static IServiceCollection AddBeforeStateResolver<TDto>(
        this IServiceCollection services,
        Func<IServiceProvider, object, CancellationToken, Task<TDto?>> resolver)
        where TDto : class
    {
        services.AddSingleton<IBeforeStateResolverRegistration>(new BeforeStateResolverRegistration<TDto>(resolver));
        return services;
    }

    /// <summary>
    /// Registers a resolver for a DTO type using a Wolverine query.
    /// </summary>
    public static IServiceCollection AddBeforeStateResolver<TDto, TQuery>(
        this IServiceCollection services,
        Func<object, TQuery> queryFactory)
        where TDto : class
        where TQuery : class
    {
        services.AddSingleton<IBeforeStateResolverRegistration>(
            new BeforeStateQueryResolverRegistration<TDto, TQuery>(queryFactory));
        return services;
    }

    /// <summary>
    /// Registers a resolver for a settings DTO type using a parameterless Wolverine query.
    /// Used for tenant-scoped singleton settings that don't require an ID.
    /// </summary>
    /// <typeparam name="TSettingsDto">The DTO type for the settings</typeparam>
    /// <typeparam name="TSettingsQuery">The query type (must have parameterless constructor)</typeparam>
    public static IServiceCollection AddSettingsBeforeStateResolver<TSettingsDto, TSettingsQuery>(
        this IServiceCollection services)
        where TSettingsDto : class
        where TSettingsQuery : class, new()
    {
        services.AddBeforeStateResolver<TSettingsDto>(
            async (sp, _, ct) =>
            {
                await using var scope = sp.CreateAsyncScope();
                var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
                var result = await bus.InvokeAsync<Result<TSettingsDto>>(new TSettingsQuery(), ct);
                return result.IsSuccess ? result.Value : null;
            });
        return services;
    }
}

/// <summary>
/// Marker interface for before-state resolver registrations.
/// </summary>
public interface IBeforeStateResolverRegistration
{
    void Register(WolverineBeforeStateProvider provider);
}

/// <summary>
/// Registration for a direct resolver function.
/// </summary>
internal class BeforeStateResolverRegistration<TDto> : IBeforeStateResolverRegistration
    where TDto : class
{
    private readonly Func<IServiceProvider, object, CancellationToken, Task<TDto?>> _resolver;

    public BeforeStateResolverRegistration(Func<IServiceProvider, object, CancellationToken, Task<TDto?>> resolver)
    {
        _resolver = resolver;
    }

    public void Register(WolverineBeforeStateProvider provider)
    {
        provider.Register(_resolver);
    }
}

/// <summary>
/// Registration for a Wolverine query resolver.
/// </summary>
internal class BeforeStateQueryResolverRegistration<TDto, TQuery> : IBeforeStateResolverRegistration
    where TDto : class
    where TQuery : class
{
    private readonly Func<object, TQuery> _queryFactory;

    public BeforeStateQueryResolverRegistration(Func<object, TQuery> queryFactory)
    {
        _queryFactory = queryFactory;
    }

    public void Register(WolverineBeforeStateProvider provider)
    {
        provider.RegisterWithQuery<TDto, TQuery>(_queryFactory);
    }
}
