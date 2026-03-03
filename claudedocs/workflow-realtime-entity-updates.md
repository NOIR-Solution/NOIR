# Workflow: Real-Time Entity Update Signals

**Feature:** Multi-user real-time awareness when entity data changes
**Spec:** Brainstorm session 2026-03-02
**Status:** Ready for implementation

---

## Overview

Add SignalR-based real-time signals so users are immediately aware when another user updates data they're currently viewing.

| Scenario | Behavior |
|----------|----------|
| List page — signal received | Silent TanStack Query invalidation → auto-refresh |
| Edit page — signal received, `!isDirty` | Toast "This record was just updated" + auto-reload |
| Edit page — signal received, `isDirty` | Credenza conflict dialog → Continue or Reload |
| Edit page — deleted signal received | Non-dismissible deleted dialog → navigate back to list |
| SignalR reconnect | Auto-refresh + auto-dismissing sticky banner |

---

## Architecture Decisions

| Decision | Choice | Reason |
|----------|--------|--------|
| Transport | SignalR — extend `NotificationHub` | SSE is `SingleReader`, can't fan-out |
| Group names | `entity_list_{Type}_{tenantId}` / `entity_{Type}_{id}_{tenantId}` | Instance-level precision |
| Payload | Minimal: `entityType`, `entityId`, `operation`, `updatedAt` | No username, no diff |
| Conflict detection | `formState.isDirty` only | No optimistic locking needed |
| Hub method pattern | Follow `INotificationHubContext` → `NotificationHubContext` | Consistent abstraction |
| Signal publication | Opt-in per handler (explicit call) | Bulk/import/system handlers stay silent |
| Bulk operations | Never publish | Suppress all `Bulk*`, `Import*`, background jobs |
| Reconnect | Auto-refresh + auto-dismiss banner after 2s | Per spec decision B |
| Delete on edit page | Non-dismissible dialog → navigate away | Per spec decision A |
| `entityType` format | PascalCase (`"Product"`, `"CrmLead"`) | Matches C# enum convention |
| Frontend connection | Dedicated connection per hook instance | Keeps implementation self-contained |

---

## Signal Payload

```typescript
interface EntityUpdateSignal {
  entityType: string                              // "Product" | "CrmLead" | "Employee" | ...
  entityId: string                                // UUID string
  operation: 'Created' | 'Updated' | 'Deleted'  // PascalCase from C# enum
  updatedAt: string                               // ISO 8601
}
```

---

## Implementation Phases

```
Phase 1 (Backend Core) ─────────────────────────────────────┐
         │                                                   │
         ▼                                                   ▼
Phase 2 (Command Handlers)                  Phase 3 (Frontend Core + i18n)
         │                                                   │
         └──────────────────┬────────────────────────────────┘
                       Quality Gate
                   Phase 4 (List Pages) ──── parallel ──── Phase 5 (Edit Pages)
                            │
                       Quality Gate
                            │
                       Phase 6 (Tests)
                            │
                      Final Quality Gate
```

---

## Phase 1 — Backend Core Infrastructure

**Effort:** 4 new files, 2 modified files
**Dependencies:** None
**Risk:** Low — new files only (except minor hub/interface extensions)

---

### Step 1.1 — Create `EntityUpdateSignal.cs`

**File:** `src/NOIR.Application/Common/Models/EntityUpdateSignal.cs`

```csharp
namespace NOIR.Application.Common.Models;

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

### Step 1.2 — Create `IEntityUpdateHubContext.cs`

**File:** `src/NOIR.Application/Common/Interfaces/IEntityUpdateHubContext.cs`

```csharp
namespace NOIR.Application.Common.Interfaces;

public interface IEntityUpdateHubContext
{
    Task PublishEntityUpdatedAsync(
        string entityType,
        Guid entityId,
        EntityOperation operation,
        string tenantId,
        CancellationToken ct = default);
}
```

---

### Step 1.3 — Extend `INotificationClient.cs`

**File:** `src/NOIR.Application/Common/Interfaces/INotificationClient.cs`

Add two methods to the existing interface:

```csharp
// Instance watchers (edit pages)
Task EntityUpdated(EntityUpdateSignal signal);

// Collection watchers (list pages)
Task EntityCollectionUpdated(EntityUpdateSignal signal);
```

---

### Step 1.4 — Create `EntityUpdateHubContext.cs`

**File:** `src/NOIR.Infrastructure/Hubs/EntityUpdateHubContext.cs`

```csharp
namespace NOIR.Infrastructure.Hubs;

public class EntityUpdateHubContext : IEntityUpdateHubContext, IScopedService
{
    private readonly IHubContext<NotificationHub, INotificationClient> _hubContext;
    private readonly ILogger<EntityUpdateHubContext> _logger;

    public EntityUpdateHubContext(
        IHubContext<NotificationHub, INotificationClient> hubContext,
        ILogger<EntityUpdateHubContext> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task PublishEntityUpdatedAsync(
        string entityType,
        Guid entityId,
        EntityOperation operation,
        string tenantId,
        CancellationToken ct = default)
    {
        var signal = new EntityUpdateSignal(
            entityType,
            entityId.ToString(),
            operation,
            DateTimeOffset.UtcNow);

        try
        {
            // Notify collection watchers (list pages)
            await _hubContext.Clients
                .Group($"entity_list_{entityType}_{tenantId}")
                .EntityCollectionUpdated(signal);

            // Notify instance watchers (edit pages)
            await _hubContext.Clients
                .Group($"entity_{entityType}_{entityId}_{tenantId}")
                .EntityUpdated(signal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to publish entity update signal for {EntityType} {EntityId}",
                entityType, entityId);
        }
    }
}
```

**Note:** DI registration is automatic via Scrutor (`IScopedService` marker).

---

### Step 1.5 — Extend `NotificationHub.cs`

**File:** `src/NOIR.Infrastructure/Hubs/NotificationHub.cs`

Add 4 client-callable hub methods (after the existing `LeaveRoleGroup` method):

```csharp
public async Task JoinEntityList(string entityType, string tenantId)
{
    await Groups.AddToGroupAsync(Context.ConnectionId, $"entity_list_{entityType}_{tenantId}");
    _logger.LogDebug("Connection {ConnectionId} joined list group for {EntityType}",
        Context.ConnectionId, entityType);
}

public async Task LeaveEntityList(string entityType, string tenantId)
{
    await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"entity_list_{entityType}_{tenantId}");
}

public async Task JoinEntity(string entityType, string entityId, string tenantId)
{
    await Groups.AddToGroupAsync(Context.ConnectionId, $"entity_{entityType}_{entityId}_{tenantId}");
    _logger.LogDebug("Connection {ConnectionId} joined instance group for {EntityType} {EntityId}",
        Context.ConnectionId, entityType, entityId);
}

public async Task LeaveEntity(string entityType, string entityId, string tenantId)
{
    await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"entity_{entityType}_{entityId}_{tenantId}");
}
```

**Note:** `tenantId` passed from client (not extracted from claims) — consistent with `PaymentHub` pattern.

---

### Phase 1 Quality Gate

```bash
dotnet build src/NOIR.sln   # must be 0 errors
```

---

## Phase 2 — Command Handler Integration

**Effort:** ~120 handlers across 7 domain groups
**Dependencies:** Phase 1 complete
**Risk:** Medium — many files, but each change is 3 lines and uniform

### Injection Pattern (same for every handler)

```csharp
// 1. Add to constructor
private readonly IEntityUpdateHubContext _entityUpdateHub;

// 2. After await _unitOfWork.SaveChangesAsync(ct) — last line of Handle()
await _entityUpdateHub.PublishEntityUpdatedAsync(
    entityType: "Product",
    entityId: product.Id,
    operation: EntityOperation.Updated,
    tenantId: _currentUser.TenantId!,
    ct);
```

### Operation Mapping Rules

| Command Prefix | Operation |
|---------------|-----------|
| `Create*`, `Add*`, `Duplicate*`, `Record*`, `Request*` | `Created` |
| `Update*`, `Change*`, `Confirm*`, `Ship*`, `Deliver*`, `Complete*`, `Win*`, `Lose*`, `Reopen*`, `Move*`, `Assign*`, `Approve*`, `Reject*`, `Publish*`, `Archive*`, `Activate*`, `Deactivate*`, `Lock*`, `Link*`, `Set*`, `Vote*` | `Updated` |
| `Delete*`, `Remove*`, `Cancel*` | `Deleted` |

### Child Entity Strategy

Fire signals on the **aggregate root**, not the child. Example: `AddProductVariant` fires `EntityOperation.Updated` on `"Product"` — the product list page then refreshes.

---

### Step 2.1 — Products Domain

**Entity: `"Product"`**

| Handler | File | Operation |
|---------|------|-----------|
| CreateProductCommandHandler | `Features/Products/Commands/CreateProduct/` | `Created` |
| UpdateProductCommandHandler | `Features/Products/Commands/UpdateProduct/` | `Updated` |
| DeleteProductCommandHandler | `Features/Products/Commands/DeleteProduct/` | `Deleted` |
| PublishProductCommandHandler | `Features/Products/Commands/PublishProduct/` | `Updated` |
| ArchiveProductCommandHandler | `Features/Products/Commands/ArchiveProduct/` | `Updated` |
| DuplicateProductCommandHandler | `Features/Products/Commands/DuplicateProduct/` | `Created` |
| AddProductVariantCommandHandler | `Features/Products/Commands/AddProductVariant/` | `Updated` (on Product) |
| UpdateProductVariantCommandHandler | `Features/Products/Commands/UpdateProductVariant/` | `Updated` (on Product) |
| DeleteProductVariantCommandHandler | `Features/Products/Commands/DeleteProductVariant/` | `Updated` (on Product) |
| AddProductImageCommandHandler | `Features/Products/Commands/AddProductImage/` | `Updated` (on Product) |
| UpdateProductImageCommandHandler | `Features/Products/Commands/UpdateProductImage/` | `Updated` (on Product) |
| SetPrimaryProductImageCommandHandler | `Features/Products/Commands/SetPrimaryProductImage/` | `Updated` (on Product) |
| DeleteProductImageCommandHandler | `Features/Products/Commands/DeleteProductImage/` | `Updated` (on Product) |
| AddProductOptionCommandHandler | `Features/Products/Commands/AddProductOption/` | `Updated` (on Product) |
| UpdateProductOptionCommandHandler | `Features/Products/Commands/UpdateProductOption/` | `Updated` (on Product) |
| DeleteProductOptionCommandHandler | `Features/Products/Commands/DeleteProductOption/` | `Updated` (on Product) |
| AddProductOptionValueCommandHandler | `Features/Products/Commands/AddProductOptionValue/` | `Updated` (on Product) |
| UpdateProductOptionValueCommandHandler | `Features/Products/Commands/UpdateProductOptionValue/` | `Updated` (on Product) |
| DeleteProductOptionValueCommandHandler | `Features/Products/Commands/DeleteProductOptionValue/` | `Updated` (on Product) |

**Entity: `"ProductCategory"`**

| Handler | File | Operation |
|---------|------|-----------|
| CreateProductCategoryCommandHandler | `Features/Products/Commands/CreateProductCategory/` | `Created` |
| UpdateProductCategoryCommandHandler | `Features/Products/Commands/UpdateProductCategory/` | `Updated` |
| DeleteProductCategoryCommandHandler | `Features/Products/Commands/DeleteProductCategory/` | `Deleted` |

**Entity: `"ProductAttribute"`**

| Handler | File | Operation |
|---------|------|-----------|
| CreateProductAttributeCommandHandler | `Features/ProductAttributes/Commands/CreateProductAttribute/` | `Created` |
| UpdateProductAttributeCommandHandler | `Features/ProductAttributes/Commands/UpdateProductAttribute/` | `Updated` |
| DeleteProductAttributeCommandHandler | `Features/ProductAttributes/Commands/DeleteProductAttribute/` | `Deleted` |
| AddProductAttributeValueCommandHandler | `Features/ProductAttributes/Commands/AddProductAttributeValue/` | `Updated` (on ProductAttribute) |
| UpdateProductAttributeValueCommandHandler | `Features/ProductAttributes/Commands/UpdateProductAttributeValue/` | `Updated` (on ProductAttribute) |
| RemoveProductAttributeValueCommandHandler | `Features/ProductAttributes/Commands/RemoveProductAttributeValue/` | `Updated` (on ProductAttribute) |

---

### Step 2.2 — E-Commerce Domain

**Entity: `"Order"`**

| Handler | File | Operation |
|---------|------|-----------|
| CreateOrderCommandHandler | `Features/Orders/Commands/CreateOrder/` | `Created` |
| ManualCreateOrderCommandHandler | `Features/Orders/Commands/ManualCreateOrder/` | `Created` |
| ConfirmOrderCommandHandler | `Features/Orders/Commands/ConfirmOrder/` | `Updated` |
| ShipOrderCommandHandler | `Features/Orders/Commands/ShipOrder/` | `Updated` |
| DeliverOrderCommandHandler | `Features/Orders/Commands/DeliverOrder/` | `Updated` |
| CompleteOrderCommandHandler | `Features/Orders/Commands/CompleteOrder/` | `Updated` |
| ReturnOrderCommandHandler | `Features/Orders/Commands/ReturnOrder/` | `Updated` |
| CancelOrderCommandHandler | `Features/Orders/Commands/CancelOrder/` | `Deleted` |
| AddOrderNoteCommandHandler | `Features/Orders/Commands/AddOrderNote/` | `Updated` (on Order) |
| DeleteOrderNoteCommandHandler | `Features/Orders/Commands/DeleteOrderNote/` | `Updated` (on Order) |

**Entity: `"Customer"`**

| Handler | File | Operation |
|---------|------|-----------|
| CreateCustomerCommandHandler | `Features/Customers/Commands/CreateCustomer/` | `Created` |
| UpdateCustomerCommandHandler | `Features/Customers/Commands/UpdateCustomer/` | `Updated` |
| UpdateCustomerSegmentCommandHandler | `Features/Customers/Commands/UpdateCustomerSegment/` | `Updated` |
| DeleteCustomerCommandHandler | `Features/Customers/Commands/DeleteCustomer/` | `Deleted` |
| AddCustomerAddressCommandHandler | `Features/Customers/Commands/AddCustomerAddress/` | `Updated` (on Customer) |
| UpdateCustomerAddressCommandHandler | `Features/Customers/Commands/UpdateCustomerAddress/` | `Updated` (on Customer) |
| DeleteCustomerAddressCommandHandler | `Features/Customers/Commands/DeleteCustomerAddress/` | `Updated` (on Customer) |

**Entity: `"CustomerGroup"`**

| Handler | File | Operation |
|---------|------|-----------|
| CreateCustomerGroupCommandHandler | `Features/CustomerGroups/Commands/CreateCustomerGroup/` | `Created` |
| UpdateCustomerGroupCommandHandler | `Features/CustomerGroups/Commands/UpdateCustomerGroup/` | `Updated` |
| DeleteCustomerGroupCommandHandler | `Features/CustomerGroups/Commands/DeleteCustomerGroup/` | `Deleted` |

**Entity: `"Promotion"`**

| Handler | File | Operation |
|---------|------|-----------|
| CreatePromotionCommandHandler | `Features/Promotions/Commands/CreatePromotion/` | `Created` |
| UpdatePromotionCommandHandler | `Features/Promotions/Commands/UpdatePromotion/` | `Updated` |
| ActivatePromotionCommandHandler | `Features/Promotions/Commands/ActivatePromotion/` | `Updated` |
| DeactivatePromotionCommandHandler | `Features/Promotions/Commands/DeactivatePromotion/` | `Updated` |
| DeletePromotionCommandHandler | `Features/Promotions/Commands/DeletePromotion/` | `Deleted` |

**Entity: `"Review"`**

| Handler | File | Operation |
|---------|------|-----------|
| CreateReviewCommandHandler | `Features/Reviews/Commands/CreateReview/` | `Created` |
| ApproveReviewCommandHandler | `Features/Reviews/Commands/ApproveReview/` | `Updated` |
| RejectReviewCommandHandler | `Features/Reviews/Commands/RejectReview/` | `Updated` |
| VoteReviewCommandHandler | `Features/Reviews/Commands/VoteReview/` | `Updated` |
| AddAdminResponseCommandHandler | `Features/Reviews/Commands/AddAdminResponse/` | `Updated` |

**Entity: `"Brand"`**

| Handler | File | Operation |
|---------|------|-----------|
| CreateBrandCommandHandler | `Features/Brands/Commands/CreateBrand/` | `Created` |
| UpdateBrandCommandHandler | `Features/Brands/Commands/UpdateBrand/` | `Updated` |
| DeleteBrandCommandHandler | `Features/Brands/Commands/DeleteBrand/` | `Deleted` |

**Entity: `"InventoryReceipt"`**

| Handler | File | Operation |
|---------|------|-----------|
| CreateInventoryReceiptCommandHandler | `Features/Inventory/Commands/CreateInventoryReceipt/` | `Created` |
| ConfirmInventoryReceiptCommandHandler | `Features/Inventory/Commands/ConfirmInventoryReceipt/` | `Updated` |
| CancelInventoryReceiptCommandHandler | `Features/Inventory/Commands/CancelInventoryReceipt/` | `Deleted` |

**Entity: `"PaymentTransaction"`**

| Handler | File | Operation |
|---------|------|-----------|
| CreatePaymentCommandHandler | `Features/Payments/Commands/CreatePayment/` | `Created` |
| RecordManualPaymentCommandHandler | `Features/Payments/Commands/RecordManualPayment/` | `Created` |
| RequestRefundCommandHandler | `Features/Payments/Commands/RequestRefund/` | `Created` |
| ApproveRefundCommandHandler | `Features/Payments/Commands/ApproveRefund/` | `Updated` |
| RejectRefundCommandHandler | `Features/Payments/Commands/RejectRefund/` | `Updated` |
| ConfirmCodCollectionCommandHandler | `Features/Payments/Commands/ConfirmCodCollection/` | `Updated` |
| CancelPaymentCommandHandler | `Features/Payments/Commands/CancelPayment/` | `Deleted` |

**Entity: `"WebhookSubscription"`**

| Handler | File | Operation |
|---------|------|-----------|
| CreateWebhookSubscriptionCommandHandler | `Features/Webhooks/Commands/CreateWebhookSubscription/` | `Created` |
| UpdateWebhookSubscriptionCommandHandler | `Features/Webhooks/Commands/UpdateWebhookSubscription/` | `Updated` |
| ActivateWebhookSubscriptionCommandHandler | `Features/Webhooks/Commands/ActivateWebhookSubscription/` | `Updated` |
| DeactivateWebhookSubscriptionCommandHandler | `Features/Webhooks/Commands/DeactivateWebhookSubscription/` | `Updated` |
| DeleteWebhookSubscriptionCommandHandler | `Features/Webhooks/Commands/DeleteWebhookSubscription/` | `Deleted` |

---

### Step 2.3 — Blog Domain

**Entity: `"BlogPost"`**

| Handler | File | Operation |
|---------|------|-----------|
| CreatePostCommandHandler | `Features/Blog/Commands/CreatePost/` | `Created` |
| UpdatePostCommandHandler | `Features/Blog/Commands/UpdatePost/` | `Updated` |
| DeletePostCommandHandler | `Features/Blog/Commands/DeletePost/` | `Deleted` |

**Entity: `"BlogCategory"`**

| Handler | File | Operation |
|---------|------|-----------|
| CreateCategoryCommandHandler | `Features/Blog/Commands/CreateCategory/` | `Created` |
| UpdateCategoryCommandHandler | `Features/Blog/Commands/UpdateCategory/` | `Updated` |
| DeleteCategoryCommandHandler | `Features/Blog/Commands/DeleteCategory/` | `Deleted` |

**Entity: `"BlogTag"`**

| Handler | File | Operation |
|---------|------|-----------|
| CreateTagCommandHandler | `Features/Blog/Commands/CreateTag/` | `Created` |
| UpdateTagCommandHandler | `Features/Blog/Commands/UpdateTag/` | `Updated` |
| DeleteTagCommandHandler | `Features/Blog/Commands/DeleteTag/` | `Deleted` |

---

### Step 2.4 — User Access Domain

**Entity: `"User"`**

| Handler | File | Operation |
|---------|------|-----------|
| CreateUserCommandHandler | `Features/Users/Commands/CreateUser/` | `Created` |
| UpdateUserCommandHandler | `Features/Users/Commands/UpdateUser/` | `Updated` |
| AssignRolesToUserCommandHandler | `Features/Users/Commands/AssignRoles/` | `Updated` |
| LockUserCommandHandler | `Features/Users/Commands/LockUser/` | `Updated` |
| DeleteUserCommandHandler | `Features/Users/Commands/DeleteUser/` | `Deleted` |

**Entity: `"Role"`**

| Handler | File | Operation |
|---------|------|-----------|
| CreateRoleCommandHandler | `Features/Roles/Commands/CreateRole/` | `Created` |
| UpdateRoleCommandHandler | `Features/Roles/Commands/UpdateRole/` | `Updated` |
| DeleteRoleCommandHandler | `Features/Roles/Commands/DeleteRole/` | `Deleted` |

**Entity: `"Tenant"`**

| Handler | File | Operation |
|---------|------|-----------|
| CreateTenantCommandHandler | `Features/Tenants/Commands/CreateTenant/` | `Created` |
| UpdateTenantCommandHandler | `Features/Tenants/Commands/UpdateTenant/` | `Updated` |
| DeleteTenantCommandHandler | `Features/Tenants/Commands/DeleteTenant/` | `Deleted` |

---

### Step 2.5 — HR Domain

**Entity: `"Employee"`**

| Handler | File | Operation |
|---------|------|-----------|
| CreateEmployeeCommandHandler | `Features/Hr/Commands/CreateEmployee/` | `Created` |
| UpdateEmployeeCommandHandler | `Features/Hr/Commands/UpdateEmployee/` | `Updated` |
| DeactivateEmployeeCommandHandler | `Features/Hr/Commands/DeactivateEmployee/` | `Updated` |
| ReactivateEmployeeCommandHandler | `Features/Hr/Commands/ReactivateEmployee/` | `Updated` |
| LinkEmployeeToUserCommandHandler | `Features/Hr/Commands/LinkEmployeeToUser/` | `Updated` |

**Entity: `"Department"`**

| Handler | File | Operation |
|---------|------|-----------|
| CreateDepartmentCommandHandler | `Features/Hr/Commands/CreateDepartment/` | `Created` |
| UpdateDepartmentCommandHandler | `Features/Hr/Commands/UpdateDepartment/` | `Updated` |
| DeleteDepartmentCommandHandler | `Features/Hr/Commands/DeleteDepartment/` | `Deleted` |

**Entity: `"EmployeeTag"`**

| Handler | File | Operation |
|---------|------|-----------|
| CreateTagCommandHandler (Hr) | `Features/Hr/Commands/CreateTag/` | `Created` |
| UpdateTagCommandHandler (Hr) | `Features/Hr/Commands/UpdateTag/` | `Updated` |
| DeleteTagCommandHandler (Hr) | `Features/Hr/Commands/DeleteTag/` | `Deleted` |

---

### Step 2.6 — CRM Domain

**Entity: `"CrmContact"`**

| Handler | File | Operation |
|---------|------|-----------|
| CreateContactCommandHandler | `Features/Crm/Commands/CreateContact/` | `Created` |
| UpdateContactCommandHandler | `Features/Crm/Commands/UpdateContact/` | `Updated` |
| DeleteContactCommandHandler | `Features/Crm/Commands/DeleteContact/` | `Deleted` |

**Entity: `"CrmCompany"`**

| Handler | File | Operation |
|---------|------|-----------|
| CreateCompanyCommandHandler | `Features/Crm/Commands/CreateCompany/` | `Created` |
| UpdateCompanyCommandHandler | `Features/Crm/Commands/UpdateCompany/` | `Updated` |
| DeleteCompanyCommandHandler | `Features/Crm/Commands/DeleteCompany/` | `Deleted` |

**Entity: `"CrmLead"`**

| Handler | File | Operation |
|---------|------|-----------|
| CreateLeadCommandHandler | `Features/Crm/Commands/CreateLead/` | `Created` |
| UpdateLeadCommandHandler | `Features/Crm/Commands/UpdateLead/` | `Updated` |
| WinLeadCommandHandler | `Features/Crm/Commands/WinLead/` | `Updated` |
| LoseLeadCommandHandler | `Features/Crm/Commands/LoseLead/` | `Updated` |
| ReopenLeadCommandHandler | `Features/Crm/Commands/ReopenLead/` | `Updated` |
| MoveLeadStageCommandHandler | `Features/Crm/Commands/MoveLeadStage/` | `Updated` |

**Entity: `"CrmActivity"`**

| Handler | File | Operation |
|---------|------|-----------|
| CreateActivityCommandHandler | `Features/Crm/Commands/CreateActivity/` | `Created` |
| UpdateActivityCommandHandler | `Features/Crm/Commands/UpdateActivity/` | `Updated` |
| DeleteActivityCommandHandler | `Features/Crm/Commands/DeleteActivity/` | `Deleted` |

**Entity: `"Pipeline"`**

| Handler | File | Operation |
|---------|------|-----------|
| CreatePipelineCommandHandler | `Features/Crm/Commands/CreatePipeline/` | `Created` |
| UpdatePipelineCommandHandler | `Features/Crm/Commands/UpdatePipeline/` | `Updated` |
| DeletePipelineCommandHandler | `Features/Crm/Commands/DeletePipeline/` | `Deleted` |

---

### Step 2.7 — PM Domain

**Entity: `"Project"`**

| Handler | File | Operation |
|---------|------|-----------|
| CreateProjectCommandHandler | `Features/Pm/Commands/CreateProject/` | `Created` |
| UpdateProjectCommandHandler | `Features/Pm/Commands/UpdateProject/` | `Updated` |
| ChangeProjectStatusCommandHandler | `Features/Pm/Commands/ChangeProjectStatus/` | `Updated` |
| ArchiveProjectCommandHandler | `Features/Pm/Commands/ArchiveProject/` | `Updated` |
| DeleteProjectCommandHandler | `Features/Pm/Commands/DeleteProject/` | `Deleted` |

**Entity: `"ProjectTask"`**

| Handler | File | Operation |
|---------|------|-----------|
| CreateTaskCommandHandler | `Features/Pm/Commands/CreateTask/` | `Created` |
| AddSubtaskCommandHandler | `Features/Pm/Commands/AddSubtask/` | `Created` |
| UpdateTaskCommandHandler | `Features/Pm/Commands/UpdateTask/` | `Updated` |
| ChangeTaskStatusCommandHandler | `Features/Pm/Commands/ChangeTaskStatus/` | `Updated` |
| MoveTaskCommandHandler | `Features/Pm/Commands/MoveTask/` | `Updated` |
| DeleteTaskCommandHandler | `Features/Pm/Commands/DeleteTask/` | `Deleted` |
| AddTaskCommentCommandHandler | `Features/Pm/Commands/AddTaskComment/` | `Updated` (on ProjectTask) |
| UpdateTaskCommentCommandHandler | `Features/Pm/Commands/UpdateTaskComment/` | `Updated` (on ProjectTask) |
| DeleteTaskCommentCommandHandler | `Features/Pm/Commands/DeleteTaskComment/` | `Updated` (on ProjectTask) |

**Entity: `"ProjectColumn"`**

| Handler | File | Operation |
|---------|------|-----------|
| CreateColumnCommandHandler | `Features/Pm/Commands/CreateColumn/` | `Created` |
| UpdateColumnCommandHandler | `Features/Pm/Commands/UpdateColumn/` | `Updated` |
| DeleteColumnCommandHandler | `Features/Pm/Commands/DeleteColumn/` | `Deleted` |

**Entity: `"TaskLabel"`**

| Handler | File | Operation |
|---------|------|-----------|
| CreateTaskLabelCommandHandler | `Features/Pm/Commands/CreateTaskLabel/` | `Created` |
| UpdateTaskLabelCommandHandler | `Features/Pm/Commands/UpdateTaskLabel/` | `Updated` |
| DeleteTaskLabelCommandHandler | `Features/Pm/Commands/DeleteTaskLabel/` | `Deleted` |

---

### Phase 2 Quality Gate

```bash
dotnet build src/NOIR.sln   # 0 errors
dotnet test src/NOIR.sln     # all pass
```

---

## Phase 3 — Frontend Core Components + i18n

**Effort:** 4 new files + 2 modified locale files
**Dependencies:** Phase 1 complete (Phase 2 can run in parallel)

---

### Step 3.1 — `useEntityUpdateSignal.ts`

**File:** `src/NOIR.Web/frontend/src/hooks/useEntityUpdateSignal.ts`

**Signature:**

```typescript
import type { EntityUpdateSignal } from '@/types/signals'

interface UseEntityUpdateSignalOptions {
  entityType: string
  entityId?: string              // undefined = collection-only mode
  isDirty?: boolean              // from formState.isDirty
  onCollectionUpdate?: () => void  // list pages: trigger refetch
  onAutoReload?: () => void        // edit pages, !isDirty: refetch + reset form
  onNavigateAway?: () => void      // edit pages, deleted: navigate to list
}

interface UseEntityUpdateSignalResult {
  conflictSignal: EntityUpdateSignal | null   // Updated + isDirty
  deletedSignal: EntityUpdateSignal | null    // Deleted
  dismissConflict: () => void                 // "Continue Editing"
  reloadAndRestart: () => void                // "Reload & Start Over"
  isReconnecting: boolean                     // for offline banner
}

export const useEntityUpdateSignal = (
  options: UseEntityUpdateSignalOptions
): UseEntityUpdateSignalResult
```

**Implementation notes:**

1. Uses `@microsoft/signalr` `HubConnectionBuilder` directly (own dedicated connection to `/hubs/notifications`)
2. Auth: `accessTokenFactory` from the auth store (same pattern as `useSignalR`)
3. Group management:
   - `useEffect([entityType, tenantId])` → `JoinEntityList` / `LeaveEntityList` on change/unmount
   - `useEffect([entityId, tenantId])` → `JoinEntity` / `LeaveEntity` when entityId present, on change/unmount
4. Event routing:
   - `EntityUpdated` with `operation === 'Deleted'` → set `deletedSignal` (regardless of isDirty)
   - `EntityUpdated` with `operation !== 'Deleted'` and `!isDirty` → call `onAutoReload()` + show toast
   - `EntityUpdated` with `operation !== 'Deleted'` and `isDirty` → set `conflictSignal`
   - `EntityCollectionUpdated` → call `onCollectionUpdate()`
5. Reconnect detection: watch `ConnectionState` transitions — `Reconnecting → Connected` → set `isReconnecting = true`, trigger refetch, auto-clear after 2000ms
6. `reloadAndRestart`: calls `onAutoReload()` + clears `conflictSignal`
7. `dismissConflict`: clears `conflictSignal` only (keeps form state)
8. `tenantId`: extracted from JWT via the auth store (same source as existing hooks)

---

### Step 3.2 — `EntityConflictDialog.tsx`

**File:** `src/NOIR.Web/frontend/src/components/EntityConflictDialog.tsx`

```tsx
interface EntityConflictDialogProps {
  open: boolean
  onContinue: () => void      // "Continue Editing" — dismiss only
  onReload: () => void        // "Reload & Start Over" — refetch + reset
}
```

UI:
- `Credenza` component (standard dialog per design language)
- No destructive styling — this is a warning, not a destructive action
- Buttons: `[Continue Editing]` (outline) / `[Reload & Start Over]` (default)
- i18n: `t('entityUpdate.conflict.title')`, `t('entityUpdate.conflict.description')`, etc.

---

### Step 3.3 — `EntityDeletedDialog.tsx`

**File:** `src/NOIR.Web/frontend/src/components/EntityDeletedDialog.tsx`

```tsx
interface EntityDeletedDialogProps {
  open: boolean
  onNavigateAway: () => void   // "Go Back" — navigate to list
}
```

UI:
- `Credenza` with **no close button** (`showCloseButton={false}`)
- Backdrop click disabled (`onPointerDownOutside={(e) => e.preventDefault()}`)
- Escape key disabled (`onEscapeKeyDown={(e) => e.preventDefault()}`)
- Single button: `[Go Back]` (default variant)
- i18n: `t('entityUpdate.deleted.title')`, `t('entityUpdate.deleted.description')`, `t('entityUpdate.deleted.goBack')`

---

### Step 3.4 — `OfflineBanner.tsx`

**File:** `src/NOIR.Web/frontend/src/components/OfflineBanner.tsx`

```tsx
interface OfflineBannerProps {
  visible: boolean   // pass isReconnecting from useEntityUpdateSignal
}
```

UI:
- Sticky top banner (not fixed-position — flows in document)
- Amber/warning color from design token
- Text: `t('entityUpdate.offline.banner')`
- Animated slide-in/out with `transition-all`
- Auto-dismisses (controlled by `isReconnecting` from hook, clears after 2s)

---

### Step 3.5 — i18n Keys

**Files:**
- `src/NOIR.Web/frontend/public/locales/en/common.json`
- `src/NOIR.Web/frontend/public/locales/vi/common.json`

Add under `"entityUpdate"` namespace key:

**English (en/common.json):**

```json
{
  "entityUpdate": {
    "conflict": {
      "title": "Record Updated",
      "description": "This record was updated while you were editing. Your unsaved changes may no longer be accurate.",
      "continueEditing": "Continue Editing",
      "reloadAndRestart": "Reload & Start Over"
    },
    "deleted": {
      "title": "Record Deleted",
      "description": "This record no longer exists. You will be returned to the list.",
      "goBack": "Go Back"
    },
    "offline": {
      "banner": "You were offline — this page may show outdated data"
    },
    "autoReloaded": "This record was just updated"
  }
}
```

**Vietnamese (vi/common.json):**

```json
{
  "entityUpdate": {
    "conflict": {
      "title": "Bản ghi đã được cập nhật",
      "description": "Bản ghi này đã được cập nhật trong khi bạn đang chỉnh sửa. Các thay đổi chưa lưu của bạn có thể không còn chính xác.",
      "continueEditing": "Tiếp tục chỉnh sửa",
      "reloadAndRestart": "Tải lại & Bắt đầu lại"
    },
    "deleted": {
      "title": "Bản ghi đã bị xóa",
      "description": "Bản ghi này không còn tồn tại. Bạn sẽ được đưa trở lại danh sách.",
      "goBack": "Quay lại"
    },
    "offline": {
      "banner": "Bạn đã mất kết nối — trang này có thể hiển thị dữ liệu cũ"
    },
    "autoReloaded": "Bản ghi này vừa được cập nhật"
  }
}
```

---

### Phase 3 Quality Gate

```bash
cd src/NOIR.Web/frontend
pnpm run build               # 0 errors, 0 warnings (strict TypeScript)
pnpm build-storybook         # 0 errors
```

---

## Phase 4 — List Page Integration

**Effort:** 23 pages
**Dependencies:** Phase 3 complete
**Can run in parallel with:** Phase 5

### Universal Pattern

```tsx
// Simple list page (no bulk selection)
const { data, refetch } = useQuery({ queryKey: ['products'], ... })

useEntityUpdateSignal({
  entityType: 'Product',
  onCollectionUpdate: refetch,
})
```

```tsx
// List page with bulk selection (defer during active selection)
const { data, refetch } = useQuery(...)
const { selectedIds } = useSelection(data)

useEntityUpdateSignal({
  entityType: 'Product',
  onCollectionUpdate: () => {
    if (selectedIds.size === 0) {
      refetch()
    }
    // else: signal received mid-selection — skip (next interaction will refetch)
  },
})
```

### Pages

| Page | File | EntityType | Has Bulk Select |
|------|------|------------|-----------------|
| ProductsPage | `portal-app/products/features/product-list/ProductsPage.tsx` | `"Product"` | Yes |
| ProductCategoriesPage | `portal-app/products/features/product-category-list/ProductCategoriesPage.tsx` | `"ProductCategory"` | No |
| ProductAttributesPage | `portal-app/products/features/product-attribute-list/ProductAttributesPage.tsx` | `"ProductAttribute"` | No |
| BrandsPage | `portal-app/brands/features/brand-list/BrandsPage.tsx` | `"Brand"` | No |
| OrdersPage | `portal-app/orders/features/order-list/OrdersPage.tsx` | `"Order"` | Yes |
| PaymentsPage | `portal-app/payments/features/payment-list/PaymentsPage.tsx` | `"PaymentTransaction"` | No |
| CustomersPage | `portal-app/customers/features/customer-list/CustomersPage.tsx` | `"Customer"` | Yes |
| CustomerGroupsPage | `portal-app/customer-groups/features/customer-group-list/CustomerGroupsPage.tsx` | `"CustomerGroup"` | No |
| PromotionsPage | `portal-app/promotions/features/promotion-list/PromotionsPage.tsx` | `"Promotion"` | No |
| ReviewsPage | `portal-app/reviews/features/review-list/ReviewsPage.tsx` | `"Review"` | Yes |
| InventoryReceiptsPage | `portal-app/inventory/features/inventory-receipts/InventoryReceiptsPage.tsx` | `"InventoryReceipt"` | No |
| BlogPostsPage | `portal-app/blogs/features/blog-post-list/BlogPostsPage.tsx` | `"BlogPost"` | Yes |
| BlogCategoriesPage | `portal-app/blogs/features/blog-category-list/BlogCategoriesPage.tsx` | `"BlogCategory"` | No |
| BlogTagsPage | `portal-app/blogs/features/blog-tag-list/BlogTagsPage.tsx` | `"BlogTag"` | No |
| UsersPage | `portal-app/user-access/features/user-list/UsersPage.tsx` | `"User"` | Yes |
| RolesPage | `portal-app/user-access/features/role-list/RolesPage.tsx` | `"Role"` | No |
| TenantsPage | `portal-app/user-access/features/tenant-list/TenantsPage.tsx` | `"Tenant"` | No |
| EmployeesPage | `portal-app/hr/features/employee-list/EmployeesPage.tsx` | `"Employee"` | Yes |
| TagsPage (HR) | `portal-app/hr/features/tag-management/TagsPage.tsx` | `"EmployeeTag"` | No |
| CompaniesPage | `portal-app/crm/features/companies/CompaniesPage.tsx` | `"CrmCompany"` | No |
| ContactsPage | `portal-app/crm/features/contacts/ContactsPage.tsx` | `"CrmContact"` | No |
| ProjectsPage | `portal-app/pm/features/project-list/ProjectsPage.tsx` | `"Project"` | No |
| MediaLibraryPage | `portal-app/media/features/media-library/MediaLibraryPage.tsx` | `"MediaFile"` | No |

**Excluded:** Dashboard, Reports, OrgChart, Kanban views, Settings pages (non-CRUD entity lists), DeveloperLogsPage (system-generated), NotificationsPage (user inbox)

---

### Phase 4 Quality Gate

```bash
cd src/NOIR.Web/frontend && pnpm run build   # 0 errors, 0 warnings
```

---

## Phase 5 — Edit/Detail Page Integration

**Effort:** 13 pages
**Dependencies:** Phase 3 complete
**Can run in parallel with:** Phase 4

### Pattern for Full Edit Pages (with `useForm`)

```tsx
const { id } = useParams()
const form = useForm<ProductFormData>({ mode: 'onBlur' })
const { isDirty } = form.formState
const queryClient = useQueryClient()
const navigate = useNavigate()

const {
  conflictSignal,
  deletedSignal,
  dismissConflict,
  reloadAndRestart,
  isReconnecting,
} = useEntityUpdateSignal({
  entityType: 'Product',
  entityId: id,
  isDirty,
  onAutoReload: () => {
    queryClient.invalidateQueries({ queryKey: ['product', id] })
    // form.reset() called after query resolves via useEffect on data
  },
  onNavigateAway: () => navigate('/products'),
})

return (
  <>
    {isReconnecting && <OfflineBanner visible />}
    <form>...</form>
    <EntityConflictDialog
      open={!!conflictSignal}
      onContinue={dismissConflict}
      onReload={reloadAndRestart}
    />
    <EntityDeletedDialog
      open={!!deletedSignal}
      onNavigateAway={() => navigate('/products')}
    />
  </>
)
```

### Pattern for Detail Pages (read-heavy, no `useForm`)

```tsx
// isDirty = false always → auto-reload on any signal
useEntityUpdateSignal({
  entityType: 'Order',
  entityId: id,
  isDirty: false,
  onAutoReload: () => queryClient.invalidateQueries({ queryKey: ['order', id] }),
  onNavigateAway: () => navigate('/orders'),
})
// Still render EntityDeletedDialog for the deleted case
```

### Pages

| Page | File | EntityType | List Route | Form Type |
|------|------|------------|------------|-----------|
| ProductFormPage | `portal-app/products/features/product-edit/ProductFormPage.tsx` | `"Product"` | `/products` | useForm |
| BlogPostEditPage | `portal-app/blogs/features/blog-post-edit/BlogPostEditPage.tsx` | `"BlogPost"` | `/blogs` | useForm |
| EmailTemplateEditPage | `portal-app/settings/features/email-template-edit/EmailTemplateEditPage.tsx` | `"EmailTemplate"` | `/settings/email-templates` | useForm |
| LegalPageEditPage | `portal-app/settings/features/legal-page-edit/LegalPageEditPage.tsx` | `"LegalPage"` | `/settings/legal` | useForm |
| OrderDetailPage | `portal-app/orders/features/order-detail/OrderDetailPage.tsx` | `"Order"` | `/orders` | Read-only |
| CustomerDetailPage | `portal-app/customers/features/customer-detail/CustomerDetailPage.tsx` | `"Customer"` | `/customers` | Read-only |
| PaymentDetailPage | `portal-app/payments/features/payment-detail/PaymentDetailPage.tsx` | `"PaymentTransaction"` | `/payments` | Read-only |
| ProjectDetailPage | `portal-app/pm/features/project-detail/ProjectDetailPage.tsx` | `"Project"` | `/pm/projects` | Mixed |
| TaskDetailPage | `portal-app/pm/features/task-detail/TaskDetailPage.tsx` | `"ProjectTask"` | `/pm/tasks` | Mixed |
| CompanyDetailPage | `portal-app/crm/features/companies/CompanyDetailPage.tsx` | `"CrmCompany"` | `/crm/companies` | Mixed |
| ContactDetailPage | `portal-app/crm/features/contacts/ContactDetailPage.tsx` | `"CrmContact"` | `/crm/contacts` | Mixed |
| DealDetailPage | `portal-app/crm/features/pipeline/DealDetailPage.tsx` | `"CrmLead"` | `/crm/pipeline` | Mixed |
| EmployeeDetailPage | `portal-app/hr/features/employee-detail/EmployeeDetailPage.tsx` | `"Employee"` | `/hr/employees` | Mixed |

**"Mixed"** = page has tabs, some tabs are read-only, some have inline edit forms. Use `isDirty` from the specific active edit form component's context. If no form is active, pass `isDirty: false`.

---

### Phase 5 Quality Gate

```bash
cd src/NOIR.Web/frontend && pnpm run build   # 0 errors, 0 warnings
```

---

## Phase 6 — Tests

**Effort:** 3 test files
**Dependencies:** All phases complete

---

### Step 6.1 — Backend Unit Tests

**File:** `tests/NOIR.Application.UnitTests/Hubs/EntityUpdateHubContextTests.cs`

Test cases:
- `PublishEntityUpdatedAsync_SendsToCollectionGroup` — verifies `entity_list_{type}_{tenantId}` group is called
- `PublishEntityUpdatedAsync_SendsToInstanceGroup` — verifies `entity_{type}_{id}_{tenantId}` group is called
- `PublishEntityUpdatedAsync_DoesNotThrow_WhenHubFails` — exception in hub call is swallowed, logged
- `PublishEntityUpdatedAsync_SignalHasCorrectPayload` — entityType, entityId, operation, updatedAt set correctly

---

### Step 6.2 — Frontend Hook Unit Tests

**File:** `src/NOIR.Web/frontend/src/hooks/__tests__/useEntityUpdateSignal.test.tsx`

Test cases (mock `HubConnectionBuilder`):
1. `EntityUpdated (Updated) + !isDirty → calls onAutoReload`
2. `EntityUpdated (Updated) + isDirty → sets conflictSignal`
3. `EntityUpdated (Deleted) + !isDirty → sets deletedSignal (not onAutoReload)`
4. `EntityUpdated (Deleted) + isDirty → sets deletedSignal (not conflictSignal)`
5. `EntityCollectionUpdated → calls onCollectionUpdate`
6. `dismissConflict → clears conflictSignal`
7. `reloadAndRestart → calls onAutoReload + clears conflictSignal`
8. `Connection Reconnecting→Connected → sets isReconnecting=true → auto-clears after 2s`
9. `entityId change → leaves old instance group, joins new instance group`
10. `unmount → leaves all groups`

---

### Step 6.3 — E2E Two-Browser Test

**File:** `e2e/specs/realtime-entity-updates.spec.ts`

Uses Playwright multi-context to simulate two concurrent users:

```typescript
test.describe('Real-time entity update signals', () => {
  test('list page auto-refreshes when another user creates', async ({ browser }) => {
    // Context A: on BrandsPage (list)
    // Context B: creates a new brand via API
    // Assert: Context A's list refreshes (new brand appears without manual refresh)
  })

  test('edit page auto-reloads when another user updates (no dirty state)', async ({ browser }) => {
    // Context A: on BrandEditDialog (form not yet touched)
    // Context B: updates the brand name
    // Assert: Context A's form updates to new name, toast shown
  })

  test('edit page shows conflict dialog when another user updates (dirty state)', async ({ browser }) => {
    // Context A: on BrandEditDialog, types into a field (isDirty = true)
    // Context B: updates the brand
    // Assert: Context A sees conflict Credenza dialog
    // Assert: "Continue Editing" closes dialog, form state preserved
  })

  test('edit page shows deleted dialog when another user deletes', async ({ browser }) => {
    // Context A: on BrandEditDialog
    // Context B: deletes the brand
    // Assert: Context A sees deleted Credenza dialog (no X, no backdrop close)
    // Assert: "Go Back" navigates to /brands
  })
})
```

---

### Phase 6 Quality Gate

```bash
# Backend
dotnet test src/NOIR.sln                                    # all pass

# Frontend unit
cd src/NOIR.Web/frontend && pnpm test                       # all pass

# E2E
cd e2e && npx playwright test realtime-entity-updates       # all pass
```

---

## Final Quality Gate

```bash
# Full backend
dotnet build src/NOIR.sln                                   # 0 errors
dotnet test src/NOIR.sln                                    # all pass, 0 skipped

# Full frontend
cd src/NOIR.Web/frontend
pnpm run build                                              # 0 errors, 0 warnings
pnpm build-storybook                                        # 0 errors

# Localization
# Manual check: every key in en/common.json exists in vi/common.json
```

Additional checklist:
- [ ] All new interactive elements have `cursor-pointer`
- [ ] All icon-only buttons have `aria-label`
- [ ] Both dialogs have i18n keys in EN + VI
- [ ] OfflineBanner is accessible (role="alert" or aria-live="polite")

---

## File Count Summary

| Category | New Files | Modified Files |
|----------|-----------|----------------|
| Backend DTOs/Interfaces | 2 | 1 |
| Backend Hub/Implementation | 1 | 1 |
| Backend Command Handlers | 0 | ~120 |
| Frontend Hook | 1 | 0 |
| Frontend Components | 3 | 0 |
| Frontend Pages (List) | 0 | 23 |
| Frontend Pages (Edit/Detail) | 0 | 13 |
| i18n | 0 | 2 |
| Tests (Backend) | 1 | 0 |
| Tests (Frontend) | 1 | 0 |
| Tests (E2E) | 1 | 0 |
| **Total** | **9** | **~161** |

---

## Risks & Mitigations

| Risk | Mitigation |
|------|------------|
| Handlers added in future don't publish signals | Add `IEntityUpdateHubContext` to the handler code scaffold/template |
| Signal fires during bulk operation via a direct single-entity handler | Acceptable — bulk commands don't publish; incidental single updates are fine |
| Multiple signals for same entity in quick succession | TanStack Query deduplicates concurrent `invalidateQueries` calls |
| `tenantId` null for platform admin users | Guard in `EntityUpdateHubContext`: skip publish if `tenantId` is null or "system" |
| Frontend creates 2nd SignalR connection per page | Acceptable for v1; optimize later by sharing connection via context if needed |
| Detail pages with tabs — which `isDirty` to use | Pass `isDirty` from the active inline edit form; default `false` when no form active |

---

## Excluded from Scope

- `Cart*` and `Checkout*` handlers — no admin list pages for these entities
- `Media*` handlers — MediaLibraryPage gets collection signal; no instance edit page
- Auth commands — `Login`, `Logout`, `ResetPassword`, OTP flows
- `Shipping*` provider commands — admin config, not frequently viewed concurrently
- `Profile*` commands — user self-service, single-user scope
- Notification management — system-generated, not user-created entities
- Background job outputs — suppressed by design

---

*Workflow generated: 2026-03-02*
*Based on: `claudedocs/workflow-realtime-entity-updates.md`*
*Next step: `/feature-dev` or `/sc:implement` to execute phase by phase*
