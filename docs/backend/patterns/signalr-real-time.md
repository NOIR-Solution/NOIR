# SignalR Real-Time Updates Pattern

**Created:** 2026-03-03
**Updated:** 2026-03-08
**Status:** Implemented
**Based on:** ASP.NET Core SignalR, Strongly-Typed Hubs

---

## Overview

NOIR uses SignalR for real-time server-to-client communication across three domains: entity CRUD updates, user notifications, and payment/log streaming. All hubs require JWT authentication and use strongly-typed client interfaces to avoid magic strings.

**Key design decisions:**
- Entity updates use SignalR (not SSE) because SSE channels are `SingleReader=true` and cannot fan-out to multiple watchers
- Client-side filtering for log streaming to avoid per-connection server state
- Tenant-scoped groups for multi-tenancy isolation
- Fire-and-forget publishing — hub errors never fail the command

---

## Hub Architecture

```
SignalR Hubs
├── NotificationHub         # Notifications + entity updates
│   ├── INotificationClient # ReceiveNotification, EntityUpdated, EntityCollectionUpdated
│   └── [Authorize]
├── PaymentHub              # Payment status, COD, refunds, webhooks
│   ├── IPaymentClient      # PaymentStatusChanged, CodCollected, RefundStatusChanged
│   └── [Authorize]
└── LogStreamHub            # Developer log streaming
    ├── ILogStreamClient    # ReceiveLogEntry, ReceiveLogBatch, ReceiveLevelChanged
    └── [Authorize("system:admin")]
```

**Hub URLs** (mapped in `Program.cs`):
- `/hubs/notifications` — NotificationHub
- `/hubs/payments` — PaymentHub
- `/hubs/logs` — LogStreamHub

**Context wrappers** (registered as `IScopedService`):
- `EntityUpdateHubContext` implements `IEntityUpdateHubContext` (Application layer)
- `NotificationHubContext` implements `INotificationHubContext` (Application layer)
- `PaymentHubContext` implements `IPaymentHubContext` (Application layer)

These wrappers let Application-layer handlers publish signals without referencing `Microsoft.AspNetCore.SignalR`.

---

## Group Naming Convention

All groups include tenant ID for multi-tenancy isolation.

| Group Pattern | Purpose | Example |
|---|---|---|
| `entity_list_{EntityType}_{tenantId}` | List page watchers — notified on any CRUD | `entity_list_Product_tenant-1` |
| `entity_{EntityType}_{id}_{tenantId}` | Detail/edit page — notified for specific entity | `entity_Product_abc123_tenant-1` |
| `user_{userId}` | Personal notifications | `user_550e8400-...` |
| `role_{roleName}` | Role-based broadcast | `role_Admin` |
| `payment_{transactionId}` | Payment transaction tracking | `payment_abc123` |
| `order_{orderId}` | Order payment tracking | `order_def456` |
| `cod_updates_{tenantId}` | Tenant-wide COD collection feed | `cod_updates_tenant-1` |
| `webhooks_{tenantId}` | Webhook processing monitoring | `webhooks_tenant-1` |
| `log_stream` | Developer log broadcast (no tenant scope) | `log_stream` |

Hub methods for joining/leaving groups:

```csharp
// NotificationHub
Task JoinEntityList(string entityType, string tenantId)
Task LeaveEntityList(string entityType, string tenantId)
Task JoinEntity(string entityType, string entityId, string tenantId)
Task LeaveEntity(string entityType, string entityId, string tenantId)
Task JoinRoleGroup(string roleName)
Task LeaveRoleGroup(string roleName)

// PaymentHub
Task JoinPaymentGroup(string transactionId)
Task LeavePaymentGroup(string transactionId)
Task JoinOrderPaymentsGroup(string orderId)
Task LeaveOrderPaymentsGroup(string orderId)
Task JoinCodUpdatesGroup()   // tenantId from claims
Task LeaveCodUpdatesGroup()
```

---

## Signal Model

```csharp
// Application/Common/Models/EntityUpdateSignal.cs
public record EntityUpdateSignal(
    string EntityType,
    string EntityId,
    EntityOperation Operation,
    DateTimeOffset UpdatedAt);

public enum EntityOperation
{
    Created,
    Updated,
    Deleted
}
```

---

## Backend Integration

### Step 1: Inject IEntityUpdateHubContext

Add as the last constructor parameter (convention):

```csharp
public class CreateProductCategoryCommandHandler
{
    private readonly IRepository<ProductCategory, Guid> _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IEntityUpdateHubContext _entityUpdateHub;  // Last param

    public CreateProductCategoryCommandHandler(
        IRepository<ProductCategory, Guid> categoryRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IEntityUpdateHubContext entityUpdateHub) { ... }
```

### Step 2: Publish after SaveChangesAsync

Always publish **after** persistence succeeds, never before:

```csharp
await _categoryRepository.AddAsync(category, cancellationToken);
await _unitOfWork.SaveChangesAsync(cancellationToken);

await _entityUpdateHub.PublishEntityUpdatedAsync(
    entityType: "ProductCategory",
    entityId: category.Id,
    operation: EntityOperation.Created,
    tenantId: _currentUser.TenantId!,
    cancellationToken);
```

### Error handling

`EntityUpdateHubContext.PublishEntityUpdatedAsync` wraps all SignalR calls in try/catch and logs errors. Hub failures never propagate to the caller — commands succeed even if no clients are connected.

---

## Client Interfaces

### INotificationClient

```csharp
Task ReceiveNotification(NotificationDto notification);
Task UpdateUnreadCount(int count);
Task ReceiveServerShutdown(string reason);
Task ReceiveServerRecovery();
Task EntityUpdated(EntityUpdateSignal signal);
Task EntityCollectionUpdated(EntityUpdateSignal signal);
```

### IPaymentClient

```csharp
Task PaymentStatusChanged(PaymentStatusUpdate update);
Task CodCollected(CodCollectionUpdate update);
Task RefundStatusChanged(RefundStatusUpdate update);
Task WebhookProcessed(WebhookProcessedUpdate update);
```

### ILogStreamClient

```csharp
Task ReceiveLogEntry(LogEntryDto entry);
Task ReceiveLogBatch(IEnumerable<LogEntryDto> entries);
Task ReceiveLevelChanged(string newLevel);
Task ReceiveBufferStats(LogBufferStatsDto stats);
Task ReceiveErrorSummary(IEnumerable<ErrorClusterDto> clusters);
```

---

## Frontend Hooks

### useSignalR — Notification Connection

General-purpose hook for the notification hub. Handles connection lifecycle, JWT auth, auto-reconnect with exponential backoff, and tab-visibility disconnect (30s threshold).

```tsx
const { connectionState, isConnected } = useSignalR({
  autoConnect: true,
  onNotification: (n) => addNotification(n),
  onUnreadCountUpdate: (count) => setUnreadCount(count),
  onServerShutdown: (reason) => showBanner(reason),
  onServerRecovery: () => dismissBanner(),
})
```

### useEntityUpdateSignal — Entity CRUD Signals

Specialized hook for entity update subscriptions. Manages group joins/leaves, conflict detection, and reconnect recovery.

```tsx
const {
  conflictSignal,
  deletedSignal,
  dismissConflict,
  reloadAndRestart,
  isReconnecting,
} = useEntityUpdateSignal({
  entityType: "Product",
  entityId: productId,           // Optional — omit for list pages
  isDirty: formState.isDirty,    // Drives conflict vs auto-reload
  onCollectionUpdate: () => queryClient.invalidateQueries({ queryKey: ["products"] }),
  onAutoReload: () => refetch(), // Called when !isDirty and entity updated
  onNavigateAway: () => navigate("/products"),
})
```

**Behavior matrix:**

| Signal | isDirty | Action |
|---|---|---|
| `Updated` | `false` | Auto-reload + info toast |
| `Updated` | `true` | Show `EntityConflictDialog` |
| `Deleted` | any | Show `EntityDeletedDialog` (non-dismissible) |
| Collection change | n/a | Call `onCollectionUpdate` (list refetch) |

**Reconnect behavior:** On reconnect, the hook re-joins all groups and triggers both `onCollectionUpdate` and `onAutoReload` to catch missed signals.

---

## Conflict Detection

No optimistic locking — conflict detection uses `formState.isDirty` only.

### EntityConflictDialog

Shown when another user updates an entity while the current user has unsaved changes. Two actions:
- **Continue Editing** — dismiss dialog, keep local changes
- **Reload and Restart** — discard local changes, refetch from server

### EntityDeletedDialog

Shown when another user deletes the entity being edited. Non-dismissible (blocks escape key and outside click). Single action:
- **Go Back** — navigates away from the edit page

### OfflineBanner

Shown when `isReconnecting` is true. Auto-dismisses after reconnect with a 2-second delay. Triggers data refresh on reconnect.

---

## Suppression Rules

Not all mutations should fire entity update signals. The following are **suppressed**:

| Category | Examples | Reason |
|---|---|---|
| Bulk operations | `BulkDeleteProducts`, `BulkAssignTags` | Would flood clients with N signals |
| Import commands | `ImportEmployees`, `ImportCustomers` | Same as bulk — too many signals |
| Background jobs | `ITenantJobRunner` tasks | No user session to attribute |

**Rule of thumb:** Only user-initiated single-entity CRUD commands publish signals. If a command processes multiple entities in a loop, it should not call `PublishEntityUpdatedAsync` for each one.

---

## Testing Considerations

### Unit tests

When adding `IEntityUpdateHubContext` to a handler constructor, unit tests must mock it:

```csharp
private readonly Mock<IEntityUpdateHubContext> _entityUpdateHubMock = new();

// Pass to handler constructor
var handler = new CreateProductCategoryCommandHandler(
    _categoryRepositoryMock.Object,
    _unitOfWorkMock.Object,
    _currentUserMock.Object,
    _entityUpdateHubMock.Object);  // Add mock
```

If the handler also uses `ICurrentUser`, both mocks are needed:

```csharp
private readonly Mock<ICurrentUser> _currentUserMock = new();
private readonly Mock<IEntityUpdateHubContext> _entityUpdateHubMock = new();
```

### Integration tests

`EntityUpdateHubContext` is registered as `IScopedService` and auto-discovered by Scrutor. No special test configuration needed — the real implementation runs against the test hub context.

### Verifying signal publication

```csharp
_entityUpdateHubMock.Verify(
    x => x.PublishEntityUpdatedAsync(
        "ProductCategory",
        It.IsAny<Guid>(),
        EntityOperation.Created,
        "test-tenant",
        It.IsAny<CancellationToken>()),
    Times.Once);
```

---

## Quick Reference

```
Adding real-time updates to a new entity:
1. Inject IEntityUpdateHubContext in handler (last param)
2. Call PublishEntityUpdatedAsync AFTER SaveChangesAsync
3. Use entityType string matching the entity name (e.g., "Product")
4. Frontend: useEntityUpdateSignal({ entityType: "Product", ... })
5. Add EntityConflictDialog + EntityDeletedDialog to edit pages
6. List pages: onCollectionUpdate -> invalidateQueries
7. Mock IEntityUpdateHubContext in unit tests
8. Skip signals for bulk/import/background operations
```
