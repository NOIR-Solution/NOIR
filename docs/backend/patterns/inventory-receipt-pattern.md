# Inventory Receipt Pattern

**Created:** 2026-02-18
**Status:** Implemented

---

## Overview

The Inventory Receipt system implements a batch stock movement workflow with approval controls. It models the Vietnamese warehouse concept of "phieu nhap/xuat kho" (stock-in/stock-out receipts) with a Draft-Confirm-Cancel lifecycle.

---

## Domain Model

### InventoryReceipt (Aggregate Root)

```
InventoryReceipt : TenantAggregateRoot<Guid>
├── ReceiptNumber     (string, auto-generated)
├── Type              (InventoryReceiptType: StockIn | StockOut)
├── Status            (InventoryReceiptStatus: Draft | Confirmed | Cancelled)
├── Notes             (string?, optional)
├── ConfirmedBy       (string?, userId)
├── ConfirmedAt       (DateTimeOffset?)
├── CancelledBy       (string?, userId)
├── CancelledAt       (DateTimeOffset?)
├── CancellationReason (string?)
├── Items             (ICollection<InventoryReceiptItem>)
├── TotalQuantity     (computed: sum of item quantities)
└── TotalCost         (computed: sum of item line totals)
```

### InventoryReceiptItem (Child Entity)

```
InventoryReceiptItem : TenantEntity<Guid>
├── InventoryReceiptId  (Guid, FK to parent)
├── ProductVariantId    (Guid, identifies variant)
├── ProductId           (Guid, denormalized for querying)
├── ProductName         (string, snapshot at creation)
├── VariantName         (string, snapshot at creation)
├── Sku                 (string?, snapshot at creation)
├── Quantity            (int)
├── UnitCost            (decimal)
└── LineTotal           (computed: Quantity * UnitCost)
```

**Key Design Decisions:**
- Product name, variant name, and SKU are **snapshots** at creation time, not FK references. This ensures receipt history remains accurate even if products are renamed or deleted.
- `LineTotal` is a computed property (not persisted), ignored in EF configuration via `builder.Ignore(e => e.LineTotal)`.
- `TotalQuantity` and `TotalCost` are also computed from Items collection.

---

## Status Lifecycle

```
  ┌─────────┐
  │  Draft  │ (initial state)
  └────┬────┘
       │
  ┌────┴────┐
  │         │
  ▼         ▼
┌──────────┐ ┌───────────┐
│Confirmed │ │ Cancelled │
└──────────┘ └───────────┘
```

**Transitions:**
- `Draft` -> `Confirmed`: Via `Confirm(userId)`. Requires at least one item.
- `Draft` -> `Cancelled`: Via `Cancel(userId, reason?)`. Optional cancellation reason.
- Guard clauses prevent operations on non-Draft receipts.

---

## Receipt Number Generation

Format: `{PREFIX}-{YYYYMMDD}-{NNNN}`

| Type | Prefix | Example |
|------|--------|---------|
| StockIn | `RCV` | `RCV-20260218-0001` |
| StockOut | `SHP` | `SHP-20260218-0001` |

**Sequence Logic:**
1. Build prefix string (e.g., `RCV-20260218-`)
2. Query `LatestReceiptNumberTodaySpec` to find the highest existing number with this prefix
3. Extract the sequence number from the last segment, increment by 1
4. Format with zero-padding to 4 digits (`D4`)

---

## CQRS Commands

### CreateInventoryReceiptCommand

Creates a draft receipt with items. Auto-generates receipt number.

```csharp
public sealed record CreateInventoryReceiptCommand(
    InventoryReceiptType Type,
    string? Notes,
    List<CreateInventoryReceiptItemDto> Items) : IAuditableCommand<InventoryReceiptDto>
```

**Handler flow:**
1. Generate receipt number using `LatestReceiptNumberTodaySpec`
2. Create `InventoryReceipt` via factory method
3. Add items via `receipt.AddItem(...)` for each DTO
4. Persist via `IRepository.AddAsync` + `IUnitOfWork.SaveChangesAsync`
5. Return mapped DTO

### ConfirmInventoryReceiptCommand

Confirms a draft receipt and adjusts stock for all items.

```csharp
public sealed record ConfirmInventoryReceiptCommand(Guid ReceiptId) : IAuditableCommand<InventoryReceiptDto>
```

**Handler flow:**
1. Load receipt with `InventoryReceiptByIdForUpdateSpec` (uses `AsTracking()`)
2. Call `receipt.Confirm(userId)` (validates Draft status + non-empty items)
3. For each item, load product and find variant:
   - **StockIn**: Call `variant.ReleaseStock(quantity)` to increase stock
   - **StockOut**: Call `variant.ReserveStock(quantity)` to decrease stock (validates sufficient stock)
4. Log each movement via `IInventoryMovementLogger`
5. Save all changes in single `SaveChangesAsync` call

**Error handling:**
- Receipt not found: Returns `Error.NotFound` with code `NOIR-INVENTORY-003`
- Invalid status transition: Returns `Error.Validation` with code `NOIR-INVENTORY-004`
- Insufficient stock (StockOut): Returns `Error.Validation` with code `NOIR-INVENTORY-002`

### CancelInventoryReceiptCommand

Cancels a draft receipt. No stock adjustment.

```csharp
public sealed record CancelInventoryReceiptCommand(
    Guid ReceiptId,
    string? Reason) : IAuditableCommand<InventoryReceiptDto>
```

### CreateStockMovementCommand

Creates an individual manual stock movement (not receipt-based).

```csharp
public sealed record CreateStockMovementCommand(
    Guid ProductId,
    Guid ProductVariantId,
    InventoryMovementType MovementType,
    int Quantity,
    string? Reference,
    string? Notes) : IAuditableCommand<InventoryMovementDto>
```

---

## Specifications

| Specification | Purpose | Tracking |
|---------------|---------|----------|
| `InventoryReceiptByIdSpec` | Load receipt with items (read-only) | No |
| `InventoryReceiptByIdForUpdateSpec` | Load receipt for mutation | Yes (`AsTracking()`) |
| `InventoryReceiptsListSpec` | Paginated list with optional type/status filter | No |
| `InventoryReceiptsCountSpec` | Count matching receipts for pagination | No |
| `LatestReceiptNumberTodaySpec` | Find highest sequence number for receipt number generation | No |

All specifications use `TagWith()` for SQL debugging per project convention.

---

## EF Core Configuration

### InventoryReceipts Table

```
InventoryReceipts
├── Id (PK)
├── ReceiptNumber (nvarchar(50), required)
├── Type (nvarchar(20), string conversion)
├── Status (nvarchar(20), string conversion)
├── Notes (nvarchar(1000))
├── ConfirmedBy, CancelledBy (nvarchar, UserIdMaxLength)
├── ConfirmedAt, CancelledAt (datetimeoffset)
├── CancellationReason (nvarchar(500))
├── TenantId, CreatedBy, ModifiedBy, DeletedBy, IsDeleted (audit fields)
└── Indexes:
    ├── UNIQUE(ReceiptNumber, TenantId)          -- per tenant uniqueness
    ├── IX(TenantId, Status, CreatedAt)           -- status queries
    ├── IX(TenantId, Type, CreatedAt)             -- type queries
    └── IX(TenantId)                              -- tenant filter
```

### InventoryReceiptItems Table

```
InventoryReceiptItems
├── Id (PK)
├── InventoryReceiptId (FK -> InventoryReceipts, CASCADE delete)
├── ProductVariantId, ProductId (Guid)
├── ProductName (nvarchar(200)), VariantName (nvarchar(200))
├── Sku (nvarchar(100))
├── Quantity (int)
├── UnitCost (decimal(18,4))
├── TenantId (nvarchar, TenantIdMaxLength)
└── Indexes:
    ├── IX(InventoryReceiptId)                    -- parent lookup
    └── IX(TenantId)                              -- tenant filter
```

**Note:** `LineTotal` is computed and excluded via `builder.Ignore(e => e.LineTotal)`.

---

## API Endpoints

**Base Path:** `/api/inventory`
**Authorization:** `RequireAuthorization()` on group

| Method | Route | Permission | Description |
|--------|-------|------------|-------------|
| GET | `/products/{productId}/variants/{variantId}/history` | `OrdersRead` | Stock movement history |
| POST | `/movements` | `OrdersManage` | Create manual stock movement |
| GET | `/receipts` | `OrdersRead` | List receipts (paginated, filterable) |
| GET | `/receipts/{id}` | `OrdersRead` | Get receipt by ID with items |
| POST | `/receipts` | `OrdersManage` | Create draft receipt |
| POST | `/receipts/{id}/confirm` | `OrdersManage` | Confirm receipt (adjusts stock) |
| POST | `/receipts/{id}/cancel` | `OrdersManage` | Cancel draft receipt |

---

## DTOs

```csharp
// Full receipt with items (for detail view)
InventoryReceiptDto {
    Id, ReceiptNumber, Type, Status, Notes,
    ConfirmedBy, ConfirmedAt, CancelledBy, CancelledAt, CancellationReason,
    TotalQuantity, TotalCost, Items, CreatedAt, CreatedBy
}

// Summary for list views
InventoryReceiptSummaryDto {
    Id, ReceiptNumber, Type, Status,
    TotalQuantity, TotalCost, ItemCount, CreatedAt, CreatedBy
}

// Line item detail
InventoryReceiptItemDto {
    Id, ProductVariantId, ProductId, ProductName, VariantName,
    Sku, Quantity, UnitCost, LineTotal
}

// Create request item
CreateInventoryReceiptItemDto(
    ProductVariantId, ProductId, ProductName, VariantName,
    Sku, Quantity, UnitCost)
```

---

## Testing

Tests are located at:
- `tests/NOIR.Domain.UnitTests/Entities/InventoryReceiptTests.cs` - Domain logic (status transitions, guard clauses, item management)
- `tests/NOIR.Application.UnitTests/Features/Inventory/Commands/` - Handler tests for all three receipt commands

---

## Related Patterns

- **[Repository & Specification](repository-specification.md)** - Data access patterns used by all specs
- **[Hierarchical Audit Logging](hierarchical-audit-logging.md)** - All commands implement `IAuditableCommand`
- **[JSON Enum Serialization](json-enum-serialization.md)** - `InventoryReceiptType` and `InventoryReceiptStatus` serialize as strings
