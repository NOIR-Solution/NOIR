# SSE & Background Job Patterns

## Overview

NOIR uses **Server-Sent Events (SSE)** for lightweight one-way server-to-client push, primarily for job progress tracking and operation status updates. For bidirectional or fan-out communication (entity updates, notifications, server lifecycle), **SignalR** is used instead.

The deploy recovery system combines both transports: `GracefulShutdownService` writes a shutdown marker and notifies clients via SignalR, while `DeployRecoveryService` detects the marker on startup and broadcasts recovery. Command/query dispatch uses **Wolverine** with auto-discovered handlers.

## SSE Architecture

### Channel Model

SSE channels use `System.Threading.Channels.Channel<SseEvent>` -- bounded, in-memory, one reader per channel.

```
Backend Service              SseEventPublisher (Singleton)                Client
     |                                |                                    |
     |-- PublishForTenantAsync() ---->|                                    |
     |   (tenantId:channelId)        |-- TryWrite --> Channel<T>          |
     |                               |                    |               |
     |                               |   SubscribeAsync() |               |
     |                               |   ReadAllAsync() <-|               |
     |                               |-- yield SseEvent ----------------->|
     |                               |-- heartbeat (20s) ---------------->|
```

**Key design decisions:**

- `BoundedChannelOptions(100)` with `DropOldest` -- prevents memory leaks from slow consumers
- `SingleReader = true` -- one SSE connection per channel (use SignalR for fan-out)
- Channel created on first subscribe, removed on disconnect
- Events silently dropped if no subscriber is listening

### SseEvent Model

```csharp
// Application/Common/Models/SseEvent.cs
public sealed record SseEvent
{
    public required string EventType { get; init; }  // SSE "event:" field
    public required string Data { get; init; }        // JSON payload
    public string? Id { get; init; }                  // Optional, for last-event-id reconnection
}
```

### Tenant Scoping

Channel IDs are tenant-scoped at the endpoint level to prevent cross-tenant access:

```csharp
// Endpoint: GET /api/sse/channels/{channelId}
var tenantId = currentUser.TenantId ?? "system";
var scopedChannelId = $"{tenantId}:{channelId}";
```

Backend services use the extension method:

```csharp
await _ssePublisher.PublishForTenantAsync(tenantId, "job-123", new SseEvent
{
    EventType = "progress",
    Data = JsonSerializer.Serialize(new { progress = 50, status = "processing" })
}, ct);
```

## ISseEventPublisher Interface

```csharp
// Application/Common/Interfaces/ISseEventPublisher.cs
public interface ISseEventPublisher
{
    Task PublishAsync(string channelId, SseEvent sseEvent, CancellationToken ct = default);
    IAsyncEnumerable<SseEvent> SubscribeAsync(string channelId, CancellationToken ct = default);
}
```

Extension method `PublishForTenantAsync` prefixes channel ID with `{tenantId}:` automatically.

**Registration:** `SseEventPublisher` implements `ISingletonService` -- auto-registered via Scrutor. Must be singleton so all scoped endpoints share the same channel dictionary.

## SSE Endpoint

```
GET /api/sse/channels/{channelId}  (requires authorization)
```

- Auth: JWT via query string `?access_token=...` (EventSource cannot set headers)
- Headers: `Content-Type: text/event-stream`, `Cache-Control: no-cache`, `X-Accel-Buffering: no`
- Streams events in standard SSE format: `event:`, `data:`, optional `id:`
- Heartbeat every 20 seconds keeps connection alive

## Job Progress Tracking

### Publishing Progress (Backend)

```csharp
// In a command handler or background job
var sseEvent = new SseEvent
{
    EventType = "progress",
    Data = JsonSerializer.Serialize(new
    {
        progress = (int)(processedCount * 100.0 / totalCount),
        status = "processing",
        message = $"Processed {processedCount} of {totalCount} items"
    })
};

await _ssePublisher.PublishForTenantAsync(tenantId, $"job-{jobId}", sseEvent, ct);
```

### Consuming Progress (Frontend)

```tsx
const { progress, status, message, isConnected } = useJobProgress(importJobId)

return (
  <div>
    <Progress value={progress} />
    <span>{status}: {message}</span>
  </div>
)
```

**Channel URL convention:** `/api/sse/channels/job-{jobId}`

## Frontend Integration

### useSse Hook

Generic SSE hook in `src/hooks/useSse.ts`:

- **Reconnect:** Exponential backoff: 1s, 2s, 4s, ... up to 30s max, 10 retries default
- **Heartbeat filtering:** Server sends heartbeats every 20s to keep connection alive
- **Connection states:** `disconnected` | `connecting` | `connected` | `error`

```tsx
const { isConnected, lastEvent, connectionState } = useSse<MyPayload>(
  enabled ? `/api/sse/channels/${channelId}` : null,
  {
    onMessage: (data, eventType) => handleEvent(data),
    maxRetries: 5,
  }
)
```

Pass `null` as URL to disable the connection (conditional subscription).

### useJobProgress Hook

Convenience wrapper in `src/hooks/useJobProgress.ts`:

```tsx
const { progress, status, message, metadata, isConnected } = useJobProgress(jobId)
```

Returns `JobProgressPayload`: `progress` (0-100), `status`, `message`, optional `metadata`.

## SSE vs SignalR

| Criteria | SSE | SignalR |
|----------|-----|---------|
| **Direction** | Server-to-client only | Bidirectional |
| **Fan-out** | Single reader per channel | Multiple clients per group |
| **Use case** | Job progress, import status | Notifications, entity updates, server lifecycle |
| **Transport** | Native EventSource API | WebSocket with fallbacks |
| **Reconnect** | Manual (useSse hook) | Built-in |
| **State** | In-memory Channel | SignalR groups |

**Rules of thumb:**

- One producer, one consumer, short-lived stream: **SSE**
- Multiple consumers need the same event: **SignalR**
- Need client-to-server calls (join/leave groups): **SignalR**

## Deploy Recovery

### Architecture

```
Shutdown                                    Startup
   |                                          |
   +- GracefulShutdownService.OnStopping()    +- DeployRecoveryService.ExecuteAsync()
   |  1. Write .shutdown-marker.json          |  1. Check for marker file
   |  2. SignalR: ReceiveServerShutdown()     |  2. Read and delete marker
   |  3. Thread.Sleep(5s) drain period        |  3. Wait 5s for client connections
   |                                          |  4. SignalR: ReceiveServerRecovery()
   +- Process exits                           +- Clients invalidate query cache
```

### Shutdown Marker

```csharp
// Infrastructure/Lifecycle/ShutdownMarker.cs
public sealed record ShutdownMarker
{
    public DateTimeOffset Timestamp { get; init; }
    public string Reason { get; init; } = string.Empty;
    public bool IsClean { get; init; }  // true = graceful, false = crash
}
```

Written to `{ContentRootPath}/.shutdown-marker.json`. Clean marker present means graceful restart. No marker after previous run means crash recovery or first start.

### Frontend: ServerRecoveryBanner

`useServerHealth` hook listens for SignalR lifecycle events:

1. `ReceiveServerShutdown(reason)` -- shows amber banner with spinner
2. `ReceiveServerRecovery()` -- shows green banner, invalidates all React Query caches, auto-dismisses after 3s

The `ServerRecoveryBanner` component renders in the root `App.tsx` layout.

## Background Jobs (Wolverine)

NOIR uses **Wolverine** as its CQRS mediator with auto-discovered handlers:

```csharp
builder.Host.UseWolverine(opts =>
{
    opts.Discovery.IncludeAssembly(typeof(Application.DependencyInjection).Assembly);
    opts.Discovery.IncludeAssembly(typeof(Infrastructure.DependencyInjection).Assembly);
    opts.UseFluentValidation();
    opts.Policies.AddMiddleware<FeatureCheckMiddleware>();
    opts.Policies.AddMiddleware<LoggingMiddleware>();
    opts.Policies.AddMiddleware<PerformanceMiddleware>();
    opts.Policies.AddMiddleware<HandlerAuditMiddleware>();
});
```

**Middleware pipeline** (applied globally):

1. `FluentValidation` -- validates commands/queries before handler
2. `FeatureCheckMiddleware` -- gates by `[RequiresFeature]` attribute (cached reflection)
3. `LoggingMiddleware` -- logs before/after handler execution
4. `PerformanceMiddleware` -- warns on slow handlers
5. `HandlerAuditMiddleware` -- captures DTO diffs for audit trail

### Combining SSE with Background Jobs

For long-running operations (imports, bulk processing):

1. Endpoint creates a job ID and returns it immediately
2. Background handler processes work, publishing SSE progress events
3. Frontend subscribes to `job-{jobId}` channel via `useJobProgress`
4. Handler publishes final `completed` or `failed` event
5. SSE channel auto-cleans on client disconnect

## File Reference

| Component | Location |
|-----------|----------|
| `SseEvent` model | `Application/Common/Models/SseEvent.cs` |
| `ISseEventPublisher` | `Application/Common/Interfaces/ISseEventPublisher.cs` |
| `SseEventPublisher` impl | `Infrastructure/Sse/SseEventPublisher.cs` |
| SSE endpoint | `Web/Endpoints/SseEndpoints.cs` |
| `GracefulShutdownService` | `Infrastructure/Lifecycle/GracefulShutdownService.cs` |
| `DeployRecoveryService` | `Infrastructure/Lifecycle/DeployRecoveryService.cs` |
| `ShutdownMarker` | `Infrastructure/Lifecycle/ShutdownMarker.cs` |
| `useSse` hook | `frontend/src/hooks/useSse.ts` |
| `useJobProgress` hook | `frontend/src/hooks/useJobProgress.ts` |
| `useServerHealth` hook | `frontend/src/hooks/useServerHealth.ts` |
| `ServerRecoveryBanner` | `frontend/src/components/ServerRecoveryBanner.tsx` |

## Related Documentation

- [Hierarchical Audit Logging](./hierarchical-audit-logging.md)
- [Bulk Operations](./bulk-operations.md)