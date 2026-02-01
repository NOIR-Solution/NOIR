namespace NOIR.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Interceptor that dispatches domain events after saving changes.
/// Enforces async-only database operations per EF Core best practices.
/// </summary>
public class DomainEventInterceptor : SaveChangesInterceptor
{
    private readonly IMessageBus _messageBus;
    private readonly ILogger<DomainEventInterceptor> _logger;

    public DomainEventInterceptor(IMessageBus messageBus, ILogger<DomainEventInterceptor> logger)
    {
        _messageBus = messageBus;
        _logger = logger;
    }

    /// <summary>
    /// Throws exception to enforce async-only database operations.
    /// ASP.NET Core should always use SaveChangesAsync to avoid thread pool starvation.
    /// </summary>
    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        throw new InvalidOperationException(
            "Synchronous SaveChanges is not supported. Use SaveChangesAsync instead. " +
            "This prevents thread pool starvation and potential deadlocks in ASP.NET Core.");
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        await DispatchDomainEventsAsync(eventData.Context);
        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    private async Task DispatchDomainEventsAsync(DbContext? context)
    {
        if (context is null) return;

        var aggregateRoots = context.ChangeTracker
            .Entries<IAggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Count != 0)
            .Select(e => e.Entity)
            .ToList();

        var domainEvents = aggregateRoots
            .SelectMany(e => e.DomainEvents)
            .ToList();

        aggregateRoots.ForEach(e => e.ClearDomainEvents());

        // Skip event publishing if no events or if Wolverine hasn't started yet
        // (occurs during database seeding before host is fully started)
        if (domainEvents.Count == 0) return;

        try
        {
            foreach (var domainEvent in domainEvents)
            {
                await _messageBus.PublishAsync(domainEvent);
            }
        }
        catch (Wolverine.WolverineHasNotStartedException)
        {
            // Wolverine hasn't started yet (occurs during database seeding in integration tests)
            // Log and skip - events during seeding are not critical for application startup
            _logger.LogDebug(
                "Skipping {Count} domain event(s) - Wolverine message bus not yet started (likely during database seeding)",
                domainEvents.Count);
        }
    }
}
