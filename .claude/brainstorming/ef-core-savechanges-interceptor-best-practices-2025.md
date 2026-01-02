# EF Core SaveChangesInterceptor Best Practices (2024-2025)

**Research Date:** 2025-12-30
**Focus:** Sync vs Async, Domain Event Dispatching, Performance, Code Patterns
**Status:** ✅ IMPLEMENTED - See `src/NOIR.Infrastructure/Persistence/Interceptors/` for implementation

---

## Executive Summary

Based on current best practices from Microsoft, Ardalis Clean Architecture, and industry experts (Jimmy Bogard, Milan Jovanovic), here are the key findings:

### Quick Recommendations

1. **Implement BOTH sync and async methods** - Override both to ensure consistent behavior
2. **Dispatch domain events AFTER SaveChanges** - Use `SavedChangesAsync` for eventual consistency with Outbox pattern
3. **Keep interceptors lightweight** - Avoid expensive operations; performance overhead exists but isn't quantified
4. **Consider Outbox pattern** - For reliable, eventually consistent domain event processing
5. **Enforce async-only** - Throw from sync methods to force `SaveChangesAsync` usage

---

## 1. Should Sync SavedChanges Be Implemented or Only Async?

### Best Practice: Implement BOTH

**Official Microsoft Guidance:**
> "The methods on each interceptor type come in pairs, with the first being called before the database operation is started, and the second after the operation has completed. Each pair of methods have both sync and async variations."

**Why Implement Both:**

1. **Prevent accidental sync calls** - If you don't override sync methods, sync database calls will silently succeed
2. **Enforce async-only pattern** - Throw from sync methods to force proper async usage
3. **Consistent behavior** - Ensures auditing/events work regardless of SaveChanges vs SaveChangesAsync

### Code Pattern: Enforce Async-Only

```csharp
public class AsyncOnlyInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        throw new InvalidOperationException(
            "Synchronous SaveChanges is not allowed. Use SaveChangesAsync instead.");
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        // Allow async operations to proceed
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
```

**Microsoft's Recommendation:**
> "You may wish to throw from the sync `SavingChanges` method to ensure that all database I/O is async. This then requires that the application always calls `SaveChangesAsync` and never `SaveChanges`."

### Code Pattern: Implement Both with Shared Logic

```csharp
public class AuditingInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        UpdateAuditableEntities(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateAuditableEntities(eventData.Context);
        return new ValueTask<InterceptionResult<int>>(result);
    }

    private void UpdateAuditableEntities(DbContext? context)
    {
        if (context == null) return;

        var entries = context.ChangeTracker
            .Entries<IAuditableEntity>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
            }
            entry.Entity.UpdatedAt = DateTime.UtcNow;
        }
    }
}
```

---

## 2. Domain Event Dispatching Patterns - Before or After SaveChanges?

### Best Practice: AFTER SaveChanges (SavedChangesAsync) with Outbox Pattern

### Two Main Approaches

#### Approach A: Immediate Dispatching (SavedChangesAsync)

**Pros:**
- Simpler implementation
- Immediate consistency

**Cons:**
- If event handler fails, main transaction is already committed (inconsistency)
- No retry mechanism
- Not transactionally safe

#### Approach B: Outbox Pattern (SavingChangesAsync) ✅ RECOMMENDED

**Pros:**
- Transactionally safe (atomic operation)
- Reliable message delivery
- Built-in retry mechanism
- Eventual consistency with guarantees

**Cons:**
- Slightly more complex
- Requires background processor
- Eventual consistency (not immediate)

### Microsoft's Guidance

> "If you dispatch the domain events right before committing the original transaction, it is because you want the side effects of those events to be included in the same transaction. The initial deferred approach—raising the events before committing, so you use a single transaction—is the simplest approach when using EF Core and a relational database."

However, for production systems:

> "Some developers prefer publishing domain events after saving changes to the database, using the Outbox pattern to add transactional guarantees. This approach introduces eventual consistency, but it's also more reliable."

### Decision Matrix

| Scenario | Use Immediate (SavedChangesAsync) | Use Outbox (SavingChangesAsync) |
|----------|----------------------------------|--------------------------------|
| Event handlers must be in same transaction | ✅ | ❌ |
| Need guaranteed delivery | ❌ | ✅ |
| Critical domain events (payment, orders) | ❌ | ✅ |
| Simple notifications (email, logging) | ✅ | ❌ |
| High-reliability requirements | ❌ | ✅ |

---

## 3. Complete Code Examples

### Pattern 1: Immediate Domain Events (PublishDomainEventsInterceptor)

```csharp
// Domain Event Interface
using MediatR;

public interface IDomainEvent : INotification
{
}

// Entity Base Class
public abstract class Entity
{
    private readonly List<IDomainEvent> _domainEvents = new();

    [NotMapped]
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}

// Interceptor Implementation
internal sealed class PublishDomainEventsInterceptor : SaveChangesInterceptor
{
    private readonly IPublisher _publisher;

    public PublishDomainEventsInterceptor(IPublisher publisher)
    {
        _publisher = publisher;
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            await PublishDomainEventsAsync(eventData.Context, cancellationToken);
        }

        return result;
    }

    private async Task PublishDomainEventsAsync(DbContext context, CancellationToken cancellationToken)
    {
        var domainEvents = context
            .ChangeTracker
            .Entries<Entity>()
            .Select(entry => entry.Entity)
            .SelectMany(entity =>
            {
                List<IDomainEvent> domainEvents = entity.DomainEvents.ToList();
                entity.ClearDomainEvents();
                return domainEvents;
            })
            .ToList();

        foreach (IDomainEvent domainEvent in domainEvents)
        {
            await _publisher.Publish(domainEvent, cancellationToken);
        }
    }
}
```

**When to Use:**
- Simple applications
- Events are non-critical (logging, notifications)
- You can tolerate event handler failures after main transaction commits

**Issues:**
- Event handlers run AFTER database commit
- If handler fails, no rollback possible
- Can lead to inconsistent state

---

### Pattern 2: Outbox Pattern (InsertOutboxMessagesInterceptor) ✅ RECOMMENDED

```csharp
// Domain Event Base Class
public abstract record DomainEvent(DateTime OccurredOnUtc);

// Interface for Entities with Domain Events
public interface IHasDomainEvents
{
    IReadOnlyCollection<DomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}

// Entity Example
public sealed class Order : IHasDomainEvents
{
    private readonly List<DomainEvent> _domainEvents = new();
    public Guid Id { get; private set; } = Guid.NewGuid();
    public decimal Total { get; private set; }

    [NotMapped]
    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents;

    public Order(decimal total)
    {
        Total = total;
        AddEvent(new OrderPlacedDomainEvent(Id, Total));
    }

    private void AddEvent(DomainEvent @event) => _domainEvents.Add(@event);
    public void ClearDomainEvents() => _domainEvents.Clear();
}

// Outbox Message Entity
public class OutboxMessage
{
    public int Id { get; set; }
    public DateTime OccurredOnUtc { get; set; }
    public required string Type { get; set; }
    public required string Payload { get; set; }
    public DateTime? ProcessedOnUtc { get; set; }
    public string? Error { get; set; }
}

// Event Serializer
public interface IEventSerializer
{
    string Serialize(DomainEvent domainEvent);
    DomainEvent Deserialize(string payload, string type);
}

// Outbox Interceptor - Runs BEFORE SaveChanges
public sealed class OutboxSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly IEventSerializer _serializer;

    public OutboxSaveChangesInterceptor(IEventSerializer serializer)
        => _serializer = serializer;

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context == null) return result;

        var aggregates = context.ChangeTracker
            .Entries()
            .Where(e => e.Entity is IHasDomainEvents hasEvents
                && hasEvents.DomainEvents.Count > 0)
            .Select(e => (IHasDomainEvents)e.Entity)
            .ToList();

        if (aggregates.Count == 0)
            return result;

        var outbox = context.Set<OutboxMessage>();

        foreach (var aggregate in aggregates)
        {
            foreach (var @event in aggregate.DomainEvents)
            {
                outbox.Add(new OutboxMessage
                {
                    OccurredOnUtc = @event.OccurredOnUtc,
                    Type = @event.GetType().AssemblyQualifiedName!,
                    Payload = _serializer.Serialize(@event)
                });
            }

            // Clear events after converting to outbox messages
            aggregate.ClearDomainEvents();
        }

        return result;
    }
}

// Background Processor - Dispatches Outbox Messages
public sealed class OutboxDispatcher : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxDispatcher> _logger;
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(2);

    public OutboxDispatcher(
        IServiceProvider serviceProvider,
        ILogger<OutboxDispatcher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox dispatcher started...");

        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

            var pendingMessages = await dbContext.OutboxMessages
                .Where(x => x.ProcessedOnUtc == null)
                .OrderBy(x => x.Id)
                .Take(50)
                .ToListAsync(stoppingToken);

            foreach (var message in pendingMessages)
            {
                try
                {
                    var domainEvent = _serializer.Deserialize(message.Payload, message.Type);
                    await publisher.Publish(domainEvent, stoppingToken);
                    message.ProcessedOnUtc = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    message.Error = ex.Message;
                    _logger.LogError(ex, "Error processing outbox message {Id}", message.Id);
                }
            }

            if (pendingMessages.Any())
                await dbContext.SaveChangesAsync(stoppingToken);

            await Task.Delay(_pollInterval, stoppingToken);
        }
    }
}
```

**Key Advantages:**

1. **Atomic Operation** - Either domain event AND outbox message are saved, or neither
2. **Guaranteed Delivery** - Messages persisted in database, won't be lost
3. **Retry Mechanism** - Background processor retries failed messages
4. **Transactional Safety** - If SaveChanges fails, outbox messages aren't saved either

**Registration:**

```csharp
// Add interceptor
services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(connectionString)
           .AddInterceptors(
               sp.GetRequiredService<OutboxSaveChangesInterceptor>()
           );
});

// Register interceptor
services.AddScoped<OutboxSaveChangesInterceptor>();
services.AddSingleton<IEventSerializer, SystemTextJsonEventSerializer>();

// Add background processor
services.AddHostedService<OutboxDispatcher>();
```

---

### Pattern 3: Jimmy Bogard's Override SaveChanges Pattern

```csharp
// Entity Base with Domain Events
public abstract class Entity : IEntity
{
    [NotMapped]
    private readonly ConcurrentQueue<IDomainEvent> _domainEvents =
        new ConcurrentQueue<IDomainEvent>();

    [NotMapped]
    public IProducerConsumerCollection<IDomainEvent> DomainEvents => _domainEvents;

    protected void PublishEvent(IDomainEvent @event)
    {
        _domainEvents.Enqueue(@event);
    }
}

// DbContext with SaveChanges Override
public class AppDbContext : DbContext
{
    private readonly IDomainEventDispatcher _dispatcher;

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        IDomainEventDispatcher dispatcher)
        : base(options)
    {
        _dispatcher = dispatcher;
    }

    public override int SaveChanges()
    {
        // Throw to enforce async-only
        throw new InvalidOperationException(
            "Use SaveChangesAsync instead.");
    }

    public override async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await DispatchDomainEventsAsync();
        var result = await base.SaveChangesAsync(cancellationToken);
        return result;
    }

    private async Task DispatchDomainEventsAsync()
    {
        var domainEventEntities = ChangeTracker.Entries<IEntity>()
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Any())
            .ToArray();

        foreach (var entity in domainEventEntities)
        {
            while (entity.DomainEvents.TryTake(out var domainEvent))
            {
                await _dispatcher.Dispatch(domainEvent);
            }
        }
    }
}
```

**When to Use:**
- Simpler than interceptors for basic scenarios
- Full control over DbContext behavior
- Legacy codebases already using this pattern

**Limitations:**
- Less flexible than interceptors
- Can't be applied across multiple DbContext types easily
- Tightly couples DbContext to domain event infrastructure

---

## 4. Performance Implications of Interceptors

### General Guidance (No Specific Benchmarks Found)

**Performance Warnings from Multiple Sources:**

1. **Don't Perform Expensive Operations**
   > "The TL;DR is: don't perform expensive operations in interceptors."

2. **Performance Overhead Exists**
   > "Performance Overhead: Excessive processing within interceptors can degrade application performance."

3. **Minimize Impact**
   > "Minimize Performance Impact: Log only necessary fields and changes. Use Async Operations: To avoid blocking operations."

### Best Practices to Minimize Overhead

1. **Log Only Essential Data**
   ```csharp
   // BAD - Serializing entire entity graphs
   var json = JsonSerializer.Serialize(entry.Entity);

   // GOOD - Log only key fields
   var auditLog = new AuditLog
   {
       EntityId = entry.Entity.Id,
       Action = entry.State.ToString(),
       Timestamp = DateTime.UtcNow
   };
   ```

2. **Use Async Operations**
   - Avoid blocking calls in interceptors
   - Use `async`/`await` properly
   - Don't use `.Result` or `.Wait()`

3. **Avoid External Service Calls**
   ```csharp
   // BAD - Calling external API in interceptor
   public override async ValueTask<int> SavedChangesAsync(...)
   {
       await _httpClient.PostAsync("https://external-api.com/audit", ...);
       return result;
   }

   // GOOD - Queue for background processing
   public override async ValueTask<int> SavedChangesAsync(...)
   {
       _backgroundQueue.Enqueue(new AuditMessage { ... });
       return result;
   }
   ```

4. **Consider Alternatives**
   > "However, in many cases it may be easier to override the SaveChanges method or use the .NET events for SaveChanges exposed on DbContext."

### When NOT to Use Interceptors

- High-throughput scenarios where every millisecond counts
- Complex business logic that should be in domain services
- Operations requiring multiple external service calls
- Heavy data transformations

### Benchmarking Recommendation

**No official benchmarks found** in Microsoft docs or community articles. Recommendation:

```csharp
// Use BenchmarkDotNet to measure your specific use case
[MemoryDiagnoser]
public class InterceptorBenchmarks
{
    [Benchmark(Baseline = true)]
    public async Task SaveChanges_NoInterceptor()
    {
        // Baseline measurement
    }

    [Benchmark]
    public async Task SaveChanges_WithAuditInterceptor()
    {
        // Measure with your interceptor
    }
}
```

---

## 5. Comparison Table: When to Use Each Pattern

| Pattern | Use Case | Complexity | Reliability | Performance |
|---------|----------|-----------|-------------|-------------|
| **PublishDomainEventsInterceptor** | Non-critical events, simple apps | Low | Medium | Good |
| **OutboxSaveChangesInterceptor** | Production systems, critical events | Medium | High | Good |
| **SaveChanges Override** | Legacy apps, simple scenarios | Low | Medium | Best |
| **No Events** | CRUD apps, no domain logic | Lowest | N/A | Best |

---

## 6. Key Takeaways from Industry Experts

### Microsoft Official Docs
- Implement both sync and async methods for consistency
- Consider throwing from sync methods to enforce async-only
- Interceptors are powerful but often SaveChanges override is simpler

### Jimmy Bogard (MediatR Creator)
- "A better domain events pattern" - Decouple raising from dispatching
- Use entity base class with events collection
- Dispatch before commit for single transaction OR use Outbox for reliability

### Milan Jovanovic
- Recommends `SavedChangesAsync` for domain events (after commit)
- Prefers Outbox pattern for production systems
- Emphasizes transactional safety

### Ardalis (Steve Smith)
- Uses `IDomainEventDispatcher` in Clean Architecture template
- Entities inherit from base class with domain events
- AppDbContext receives dispatcher in constructor

---

## 7. Implementation Checklist for NOIR Project

Based on your current implementation (Finbuckle multi-tenancy, Wolverine, Clean Architecture):

### Current State Analysis
- ✅ You have `AuditableEntityInterceptor` and `DomainEventInterceptor`
- ✅ Using `SavingChangesAsync` (before save)
- ❓ Need to verify if domain events are dispatched reliably

### Recommended Changes

1. **Enforce Async-Only Pattern**
   ```csharp
   public override InterceptionResult<int> SavingChanges(
       DbContextEventData eventData,
       InterceptionResult<int> result)
   {
       throw new InvalidOperationException(
           "Use SaveChangesAsync instead of SaveChanges.");
   }
   ```

2. **Implement Outbox Pattern (Optional but Recommended)**
   - Add `OutboxMessage` entity
   - Create `OutboxSaveChangesInterceptor`
   - Add background `OutboxDispatcher` service
   - Convert domain events to outbox messages in `SavingChangesAsync`

3. **Keep AuditableEntityInterceptor as-is**
   - This is fine in `SavingChangesAsync`
   - Auditing should be in same transaction

4. **Performance Monitoring**
   - Add logging to measure interceptor execution time
   - Monitor in production
   - Add BenchmarkDotNet tests if needed

### Code Structure

```
NOIR.Infrastructure/
├── Persistence/
│   ├── Interceptors/
│   │   ├── AuditableEntityInterceptor.cs         # Keep as-is
│   │   ├── DomainEventInterceptor.cs             # Consider replacing with Outbox
│   │   ├── OutboxSaveChangesInterceptor.cs       # NEW - Recommended
│   │   └── EnforceAsyncInterceptor.cs            # NEW - Optional
│   ├── BackgroundServices/
│   │   └── OutboxDispatcher.cs                   # NEW - If using Outbox
│   └── Entities/
│       └── OutboxMessage.cs                      # NEW - If using Outbox
```

---

## 8. Additional Resources

### Official Documentation
- [Interceptors - EF Core | Microsoft Learn](https://learn.microsoft.com/en-us/ef/core/logging-events-diagnostics/interceptors)
- [Domain events: Design and implementation - .NET | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-events-design-implementation)

### Community Articles
- [How To Use EF Core Interceptors - Milan Jovanovic](https://www.milanjovanovic.tech/blog/how-to-use-ef-core-interceptors)
- [How To Use Domain Events To Build Loosely Coupled Systems - Milan Jovanovic](https://www.milanjovanovic.tech/blog/how-to-use-domain-events-to-build-loosely-coupled-systems)
- [A better domain events pattern - Jimmy Bogard](https://lostechies.com/jimmybogard/2014/05/13/a-better-domain-events-pattern/)
- [Reliable Messaging in .NET: Domain Events and the Outbox Pattern](https://dev.to/stevsharp/reliable-messaging-in-net-domain-events-and-the-outbox-pattern-with-ef-core-interceptors-pjp)
- [Simple Domain Events with EFCore and MediatR](https://cfrenzel.com/domain-events-efcore-mediatr/)

### Code Examples
- [Ardalis Clean Architecture](https://github.com/ardalis/CleanArchitecture)
- [Using Domain Events within a .NET Core Microservice - Cesar de la Torre](https://devblogs.microsoft.com/cesardelatorre/using-domain-events-within-a-net-core-microservice/)

---

## 9. Final Recommendations for NOIR

### Priority 1: Implement Immediately
1. ✅ **Enforce async-only** - Throw from sync `SavingChanges` methods
2. ✅ **Validate current domain event dispatching** - Ensure Wolverine handlers work correctly

### Priority 2: Consider for Production
1. ⚠️ **Outbox Pattern** - For critical domain events (user registration, payments, etc.)
2. ⚠️ **Performance monitoring** - Add logging to track interceptor overhead

### Priority 3: Future Enhancements
1. 📋 **Idempotency** - Ensure event handlers are idempotent
2. 📋 **Dead letter queue** - Handle permanently failed outbox messages
3. 📋 **Monitoring dashboard** - Track outbox processing metrics

### Code Template for NOIR

```csharp
// NOIR.Infrastructure/Persistence/Interceptors/EnforceAsyncInterceptor.cs
public sealed class EnforceAsyncInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        throw new InvalidOperationException(
            "Synchronous database operations are not allowed. " +
            "Use SaveChangesAsync instead of SaveChanges.");
    }

    public override int SavedChanges(
        SaveChangesCompletedEventData eventData,
        int result)
    {
        throw new InvalidOperationException(
            "Synchronous database operations are not allowed. " +
            "Use SaveChangesAsync instead of SaveChanges.");
    }

    public override void SaveChangesFailed(
        DbContextErrorEventData eventData)
    {
        throw new InvalidOperationException(
            "Synchronous database operations are not allowed. " +
            "Use SaveChangesAsync instead of SaveChanges.");
    }
}

// Register in DependencyInjection.cs
services.AddDbContext<ApplicationDbContext>((sp, options) =>
{
    options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
           .AddInterceptors(
               sp.GetRequiredService<AuditableEntityInterceptor>(),
               sp.GetRequiredService<DomainEventInterceptor>(),
               sp.GetRequiredService<EnforceAsyncInterceptor>()  // Add this
           );
});

services.AddScoped<AuditableEntityInterceptor>();
services.AddScoped<DomainEventInterceptor>();
services.AddScoped<EnforceAsyncInterceptor>();  // Add this
```

---

## Conclusion

The consensus from Microsoft, industry experts, and production systems points to:

1. **Always implement both sync and async methods** (throw from sync to enforce async)
2. **Use Outbox pattern for production systems** with critical domain events
3. **Keep interceptors lightweight** to minimize performance impact
4. **Dispatch domain events AFTER SaveChanges** (SavedChangesAsync) OR use Outbox pattern for transactional safety

For the NOIR project, the recommended approach is:
- Keep `AuditableEntityInterceptor` in `SavingChangesAsync` (auditing is transactional)
- Consider migrating `DomainEventInterceptor` to Outbox pattern for reliability
- Add `EnforceAsyncInterceptor` to prevent accidental sync database calls

---

**Research Sources:**
- [Interceptors - EF Core | Microsoft Learn](https://learn.microsoft.com/en-us/ef/core/logging-events-diagnostics/interceptors)
- [How To Use EF Core Interceptors - Milan Jovanovic](https://www.milanjovanovic.tech/blog/how-to-use-ef-core-interceptors)
- [Domain Events and Outbox Pattern - DEV Community](https://dev.to/stevsharp/reliable-messaging-in-net-domain-events-and-the-outbox-pattern-with-ef-core-interceptors-pjp)
- [Domain events: Design and implementation - Microsoft](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-events-design-implementation)
- [A better domain events pattern - Jimmy Bogard](https://lostechies.com/jimmybogard/2014/05/13/a-better-domain-events-pattern/)
