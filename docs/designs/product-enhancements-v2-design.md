# Product Enhancements V2 - Technical Design Document

**Version:** 2.0
**Date:** 2026-01-29
**Status:** Ready for Implementation
**Scope:** Attribute Enhancement + Inventory Management + Variant Images

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [Phase 1: Attribute System Enhancement](#2-phase-1-attribute-system-enhancement)
3. [Phase 2: Inventory Management System](#3-phase-2-inventory-management-system)
4. [Phase 3: Variant Image Gallery](#4-phase-3-variant-image-gallery)
5. [Phase 4: Frontend Implementation](#5-phase-4-frontend-implementation)
6. [Phase 5: Testing Strategy](#6-phase-5-testing-strategy)
7. [Implementation Order](#7-implementation-order)
8. [Migration Strategy](#8-migration-strategy)

---

## 1. Executive Summary

### 1.1 Goals

| Goal | Description |
|------|-------------|
| **Flexible Attributes** | Move hardcoded shipping fields to dynamic attribute system with auto-assign |
| **Multi-Warehouse Inventory** | Full inventory tracking across multiple locations with movement history |
| **Variant Image Management** | Shopee-style shared gallery with variant-to-image assignments |

### 1.2 Scope

- **Backend:** 7 new entities, 25+ new endpoints, database migration
- **Frontend:** 6 new pages, 15+ new components, enhanced ProductFormPage
- **Testing:** 50+ unit tests, 20+ integration tests, 10+ Playwright E2E tests

---

## 2. Phase 1: Attribute System Enhancement

### 2.1 Entity Changes

#### 2.1.1 ProductAttribute (Modified)

**File:** `src/NOIR.Domain/Entities/Product/ProductAttribute.cs`

```csharp
// Add new properties
public bool IsGlobal { get; private set; } = false;

// Methods
public void SetGlobal(bool isGlobal)
{
    IsGlobal = isGlobal;
}
```

#### 2.1.2 Product (Modified)

**File:** `src/NOIR.Domain/Entities/Product/Product.cs`

```csharp
// REMOVE these properties:
// - public decimal? Weight { get; private set; }
// - public void SetWeight(decimal? weight)
```

### 2.2 Database Migration

**Migration Name:** `RemoveWeightAddGlobalAttribute`

```sql
-- Remove Weight column from Products
ALTER TABLE Products DROP COLUMN Weight;

-- Add IsGlobal to ProductAttributes
ALTER TABLE ProductAttributes ADD IsGlobal BIT NOT NULL DEFAULT 0;

-- Seed global shipping attributes
INSERT INTO ProductAttributes (Id, TenantId, Code, Name, Type, IsGlobal, IsActive, ...)
VALUES
  (NEWID(), NULL, 'weight', 'Weight', 'Decimal', 1, 1, ...),
  (NEWID(), NULL, 'height', 'Height', 'Decimal', 1, 1, ...),
  (NEWID(), NULL, 'width', 'Width', 'Decimal', 1, 1, ...),
  (NEWID(), NULL, 'length', 'Length', 'Decimal', 1, 1, ...);
```

### 2.3 Backend Implementation

#### Commands

| Command | Location | Description |
|---------|----------|-------------|
| `UpdateProductAttributeCommand` | Modify existing | Add `IsGlobal` parameter |
| `SyncGlobalAttributesCommand` | New | Sync global attributes to all categories |

#### Event Handler

**File:** `src/NOIR.Application/Features/ProductAttributes/EventHandlers/GlobalAttributeSyncHandler.cs`

```csharp
// When attribute marked as global, auto-assign to all categories
// When category created, auto-assign all global attributes
```

### 2.4 API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| `PUT` | `/api/product-attributes/{id}/global` | Toggle global status |
| `POST` | `/api/product-attributes/sync-global` | Sync all global to categories |

### 2.5 Unit Tests

**File:** `tests/NOIR.Application.UnitTests/Features/ProductAttributes/`

| Test Class | Tests |
|------------|-------|
| `SetGlobalAttributeCommandHandlerTests` | Should mark as global, Should sync to categories |
| `GlobalAttributeSyncHandlerTests` | Should add to new category, Should skip existing |

---

## 3. Phase 2: Inventory Management System

### 3.1 Domain Entities

#### 3.1.1 Warehouse

**File:** `src/NOIR.Domain/Entities/Inventory/Warehouse.cs`

```csharp
public class Warehouse : TenantAggregateRoot<Guid>
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Address { get; private set; }
    public string? City { get; private set; }
    public string? Province { get; private set; }
    public string? PostalCode { get; private set; }
    public string? ContactName { get; private set; }
    public string? ContactPhone { get; private set; }
    public string? ContactEmail { get; private set; }
    public bool IsDefault { get; private set; }
    public bool IsActive { get; private set; } = true;
    public int SortOrder { get; private set; }

    // Navigation
    public virtual ICollection<InventoryLevel> InventoryLevels { get; private set; } = new List<InventoryLevel>();

    // Factory
    public static Warehouse Create(string code, string name, string? tenantId)
    {
        return new Warehouse
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Code = code,
            Name = name,
            IsDefault = false,
            IsActive = true
        };
    }

    // Methods
    public void Update(string code, string name, string? address, string? city,
                       string? province, string? postalCode, string? contactName,
                       string? contactPhone, string? contactEmail, int sortOrder)
    {
        Code = code;
        Name = name;
        Address = address;
        City = city;
        Province = province;
        PostalCode = postalCode;
        ContactName = contactName;
        ContactPhone = contactPhone;
        ContactEmail = contactEmail;
        SortOrder = sortOrder;
    }

    public void SetAsDefault()
    {
        IsDefault = true;
    }

    public void UnsetDefault()
    {
        IsDefault = false;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
```

#### 3.1.2 InventoryLevel

**File:** `src/NOIR.Domain/Entities/Inventory/InventoryLevel.cs`

```csharp
public class InventoryLevel : TenantEntity<Guid>
{
    public Guid WarehouseId { get; private set; }
    public Guid ProductVariantId { get; private set; }

    [ConcurrencyCheck]
    public int Quantity { get; private set; }

    [ConcurrencyCheck]
    public int ReservedQuantity { get; private set; }

    public int? ReorderPoint { get; private set; }
    public int? ReorderQuantity { get; private set; }
    public DateTime? LastStockCheckAt { get; private set; }

    // Computed
    public int AvailableQuantity => Quantity - ReservedQuantity;
    public bool IsLowStock => ReorderPoint.HasValue && AvailableQuantity <= ReorderPoint;
    public bool IsOutOfStock => AvailableQuantity <= 0;

    // Navigation
    public virtual Warehouse Warehouse { get; private set; } = null!;
    public virtual ProductVariant ProductVariant { get; private set; } = null!;

    // Factory
    public static InventoryLevel Create(Guid warehouseId, Guid productVariantId, string? tenantId)
    {
        return new InventoryLevel
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            WarehouseId = warehouseId,
            ProductVariantId = productVariantId,
            Quantity = 0,
            ReservedQuantity = 0
        };
    }

    // Methods
    public void SetReorderSettings(int? reorderPoint, int? reorderQuantity)
    {
        ReorderPoint = reorderPoint;
        ReorderQuantity = reorderQuantity;
    }

    public void AddStock(int quantity)
    {
        if (quantity < 0) throw new InvalidOperationException("Quantity must be positive");
        Quantity += quantity;
    }

    public void RemoveStock(int quantity)
    {
        if (quantity < 0) throw new InvalidOperationException("Quantity must be positive");
        if (AvailableQuantity < quantity)
            throw new InvalidOperationException($"Insufficient stock. Available: {AvailableQuantity}");
        Quantity -= quantity;
    }

    public void Reserve(int quantity)
    {
        if (AvailableQuantity < quantity)
            throw new InvalidOperationException($"Insufficient stock to reserve. Available: {AvailableQuantity}");
        ReservedQuantity += quantity;
    }

    public void ReleaseReservation(int quantity)
    {
        ReservedQuantity = Math.Max(0, ReservedQuantity - quantity);
    }

    public void ConfirmReservation(int quantity)
    {
        // Convert reservation to actual stock reduction
        ReservedQuantity -= quantity;
        Quantity -= quantity;
    }

    public void SetQuantity(int quantity, int? reservedQuantity = null)
    {
        if (quantity < 0) throw new InvalidOperationException("Quantity cannot be negative");
        Quantity = quantity;
        if (reservedQuantity.HasValue)
            ReservedQuantity = Math.Max(0, reservedQuantity.Value);
    }

    public void RecordStockCheck()
    {
        LastStockCheckAt = DateTime.UtcNow;
    }
}
```

#### 3.1.3 InventoryMovement

**File:** `src/NOIR.Domain/Entities/Inventory/InventoryMovement.cs`

```csharp
public class InventoryMovement : TenantEntity<Guid>
{
    public Guid WarehouseId { get; private set; }
    public Guid ProductVariantId { get; private set; }
    public InventoryMovementType MovementType { get; private set; }
    public int Quantity { get; private set; }
    public int PreviousQuantity { get; private set; }
    public int NewQuantity { get; private set; }
    public string? ReferenceType { get; private set; }
    public Guid? ReferenceId { get; private set; }
    public string? Notes { get; private set; }
    public string CreatedBy { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    // Navigation
    public virtual Warehouse Warehouse { get; private set; } = null!;
    public virtual ProductVariant ProductVariant { get; private set; } = null!;

    // Factory
    public static InventoryMovement Create(
        Guid warehouseId,
        Guid productVariantId,
        InventoryMovementType movementType,
        int quantity,
        int previousQuantity,
        int newQuantity,
        string createdBy,
        string? tenantId,
        string? referenceType = null,
        Guid? referenceId = null,
        string? notes = null)
    {
        return new InventoryMovement
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            WarehouseId = warehouseId,
            ProductVariantId = productVariantId,
            MovementType = movementType,
            Quantity = quantity,
            PreviousQuantity = previousQuantity,
            NewQuantity = newQuantity,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            Notes = notes,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };
    }
}
```

#### 3.1.4 InventoryTransfer

**File:** `src/NOIR.Domain/Entities/Inventory/InventoryTransfer.cs`

```csharp
public class InventoryTransfer : TenantAggregateRoot<Guid>
{
    public string TransferNumber { get; private set; } = string.Empty;
    public Guid FromWarehouseId { get; private set; }
    public Guid ToWarehouseId { get; private set; }
    public TransferStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public string RequestedBy { get; private set; } = string.Empty;
    public DateTime RequestedAt { get; private set; }
    public string? ApprovedBy { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public string? CompletedBy { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? CancelledBy { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public string? CancellationReason { get; private set; }

    // Navigation
    public virtual Warehouse FromWarehouse { get; private set; } = null!;
    public virtual Warehouse ToWarehouse { get; private set; } = null!;
    public virtual ICollection<InventoryTransferItem> Items { get; private set; } = new List<InventoryTransferItem>();

    // Factory
    public static InventoryTransfer Create(
        Guid fromWarehouseId,
        Guid toWarehouseId,
        string requestedBy,
        string? tenantId,
        string? notes = null)
    {
        return new InventoryTransfer
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TransferNumber = GenerateTransferNumber(),
            FromWarehouseId = fromWarehouseId,
            ToWarehouseId = toWarehouseId,
            Status = TransferStatus.Pending,
            Notes = notes,
            RequestedBy = requestedBy,
            RequestedAt = DateTime.UtcNow
        };
    }

    private static string GenerateTransferNumber()
    {
        return $"TRF-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
    }

    // Methods
    public InventoryTransferItem AddItem(Guid productVariantId, int quantity)
    {
        if (Status != TransferStatus.Pending)
            throw new InvalidOperationException("Cannot modify transfer after approval");

        var item = InventoryTransferItem.Create(Id, productVariantId, quantity, TenantId);
        Items.Add(item);
        return item;
    }

    public void RemoveItem(Guid itemId)
    {
        if (Status != TransferStatus.Pending)
            throw new InvalidOperationException("Cannot modify transfer after approval");

        var item = Items.FirstOrDefault(i => i.Id == itemId);
        if (item != null) Items.Remove(item);
    }

    public void Approve(string approvedBy)
    {
        if (Status != TransferStatus.Pending)
            throw new InvalidOperationException("Transfer is not pending");

        Status = TransferStatus.Approved;
        ApprovedBy = approvedBy;
        ApprovedAt = DateTime.UtcNow;
    }

    public void StartTransit(string userId)
    {
        if (Status != TransferStatus.Approved)
            throw new InvalidOperationException("Transfer must be approved first");

        Status = TransferStatus.InTransit;
    }

    public void Complete(string completedBy)
    {
        if (Status != TransferStatus.InTransit && Status != TransferStatus.Approved)
            throw new InvalidOperationException("Transfer must be in transit or approved");

        Status = TransferStatus.Completed;
        CompletedBy = completedBy;
        CompletedAt = DateTime.UtcNow;
    }

    public void Cancel(string cancelledBy, string reason)
    {
        if (Status == TransferStatus.Completed)
            throw new InvalidOperationException("Cannot cancel completed transfer");

        Status = TransferStatus.Cancelled;
        CancelledBy = cancelledBy;
        CancelledAt = DateTime.UtcNow;
        CancellationReason = reason;
    }
}

public class InventoryTransferItem : TenantEntity<Guid>
{
    public Guid TransferId { get; private set; }
    public Guid ProductVariantId { get; private set; }
    public int RequestedQuantity { get; private set; }
    public int? ReceivedQuantity { get; private set; }
    public string? Notes { get; private set; }

    // Navigation
    public virtual InventoryTransfer Transfer { get; private set; } = null!;
    public virtual ProductVariant ProductVariant { get; private set; } = null!;

    public static InventoryTransferItem Create(Guid transferId, Guid productVariantId, int quantity, string? tenantId)
    {
        return new InventoryTransferItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TransferId = transferId,
            ProductVariantId = productVariantId,
            RequestedQuantity = quantity
        };
    }

    public void SetReceivedQuantity(int quantity, string? notes = null)
    {
        ReceivedQuantity = quantity;
        Notes = notes;
    }
}
```

#### 3.1.5 InventoryAlert

**File:** `src/NOIR.Domain/Entities/Inventory/InventoryAlert.cs`

```csharp
public class InventoryAlert : TenantEntity<Guid>
{
    public Guid WarehouseId { get; private set; }
    public Guid ProductVariantId { get; private set; }
    public AlertType AlertType { get; private set; }
    public int CurrentQuantity { get; private set; }
    public int ThresholdQuantity { get; private set; }
    public bool IsRead { get; private set; }
    public bool IsResolved { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ReadAt { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public string? ResolvedBy { get; private set; }
    public string? ResolutionNotes { get; private set; }

    // Navigation
    public virtual Warehouse Warehouse { get; private set; } = null!;
    public virtual ProductVariant ProductVariant { get; private set; } = null!;

    public static InventoryAlert Create(
        Guid warehouseId,
        Guid productVariantId,
        AlertType alertType,
        int currentQuantity,
        int thresholdQuantity,
        string? tenantId)
    {
        return new InventoryAlert
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            WarehouseId = warehouseId,
            ProductVariantId = productVariantId,
            AlertType = alertType,
            CurrentQuantity = currentQuantity,
            ThresholdQuantity = thresholdQuantity,
            IsRead = false,
            IsResolved = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkAsRead()
    {
        if (!IsRead)
        {
            IsRead = true;
            ReadAt = DateTime.UtcNow;
        }
    }

    public void Resolve(string resolvedBy, string? notes = null)
    {
        IsResolved = true;
        ResolvedAt = DateTime.UtcNow;
        ResolvedBy = resolvedBy;
        ResolutionNotes = notes;
    }
}
```

### 3.2 Enums

**File:** `src/NOIR.Domain/Enums/TransferStatus.cs`

```csharp
public enum TransferStatus
{
    Pending = 0,
    Approved = 1,
    InTransit = 2,
    Completed = 3,
    Cancelled = 4
}
```

**File:** `src/NOIR.Domain/Enums/AlertType.cs`

```csharp
public enum AlertType
{
    LowStock = 0,
    OutOfStock = 1,
    Overstock = 2
}
```

### 3.3 EF Configurations

**File:** `src/NOIR.Infrastructure/Persistence/Configurations/WarehouseConfiguration.cs`

```csharp
public class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.ToTable("Warehouses");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Code).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Address).HasMaxLength(500);
        builder.Property(e => e.City).HasMaxLength(100);
        builder.Property(e => e.Province).HasMaxLength(100);
        builder.Property(e => e.PostalCode).HasMaxLength(20);
        builder.Property(e => e.ContactName).HasMaxLength(200);
        builder.Property(e => e.ContactPhone).HasMaxLength(50);
        builder.Property(e => e.ContactEmail).HasMaxLength(200);

        // Unique: Code per tenant
        builder.HasIndex(e => new { e.TenantId, e.Code }).IsUnique();

        // Only one default per tenant
        builder.HasIndex(e => new { e.TenantId, e.IsDefault })
            .HasFilter("[IsDefault] = 1")
            .IsUnique();

        builder.HasMany(e => e.InventoryLevels)
            .WithOne(e => e.Warehouse)
            .HasForeignKey(e => e.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

**File:** `src/NOIR.Infrastructure/Persistence/Configurations/InventoryLevelConfiguration.cs`

```csharp
public class InventoryLevelConfiguration : IEntityTypeConfiguration<InventoryLevel>
{
    public void Configure(EntityTypeBuilder<InventoryLevel> builder)
    {
        builder.ToTable("InventoryLevels");
        builder.HasKey(e => e.Id);

        // Unique: One level per warehouse per variant
        builder.HasIndex(e => new { e.WarehouseId, e.ProductVariantId }).IsUnique();

        // Performance: Query by warehouse
        builder.HasIndex(e => new { e.TenantId, e.WarehouseId });

        // Performance: Query by variant
        builder.HasIndex(e => new { e.TenantId, e.ProductVariantId });

        builder.HasOne(e => e.Warehouse)
            .WithMany(e => e.InventoryLevels)
            .HasForeignKey(e => e.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ProductVariant)
            .WithMany()
            .HasForeignKey(e => e.ProductVariantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

**File:** `src/NOIR.Infrastructure/Persistence/Configurations/InventoryMovementConfiguration.cs`

```csharp
public class InventoryMovementConfiguration : IEntityTypeConfiguration<InventoryMovement>
{
    public void Configure(EntityTypeBuilder<InventoryMovement> builder)
    {
        builder.ToTable("InventoryMovements");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.ReferenceType).HasMaxLength(50);
        builder.Property(e => e.Notes).HasMaxLength(1000);
        builder.Property(e => e.CreatedBy).HasMaxLength(100).IsRequired();

        // Performance: Query by warehouse and date
        builder.HasIndex(e => new { e.TenantId, e.WarehouseId, e.CreatedAt });

        // Performance: Query by variant
        builder.HasIndex(e => new { e.TenantId, e.ProductVariantId, e.CreatedAt });

        // Performance: Query by reference
        builder.HasIndex(e => new { e.ReferenceType, e.ReferenceId });

        builder.HasOne(e => e.Warehouse)
            .WithMany()
            .HasForeignKey(e => e.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ProductVariant)
            .WithMany()
            .HasForeignKey(e => e.ProductVariantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

**File:** `src/NOIR.Infrastructure/Persistence/Configurations/InventoryTransferConfiguration.cs`

```csharp
public class InventoryTransferConfiguration : IEntityTypeConfiguration<InventoryTransfer>
{
    public void Configure(EntityTypeBuilder<InventoryTransfer> builder)
    {
        builder.ToTable("InventoryTransfers");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.TransferNumber).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Notes).HasMaxLength(1000);
        builder.Property(e => e.RequestedBy).HasMaxLength(100).IsRequired();
        builder.Property(e => e.ApprovedBy).HasMaxLength(100);
        builder.Property(e => e.CompletedBy).HasMaxLength(100);
        builder.Property(e => e.CancelledBy).HasMaxLength(100);
        builder.Property(e => e.CancellationReason).HasMaxLength(500);

        // Unique transfer number per tenant
        builder.HasIndex(e => new { e.TenantId, e.TransferNumber }).IsUnique();

        // Performance: Query by status
        builder.HasIndex(e => new { e.TenantId, e.Status, e.RequestedAt });

        builder.HasOne(e => e.FromWarehouse)
            .WithMany()
            .HasForeignKey(e => e.FromWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ToWarehouse)
            .WithMany()
            .HasForeignKey(e => e.ToWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Items)
            .WithOne(e => e.Transfer)
            .HasForeignKey(e => e.TransferId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

**File:** `src/NOIR.Infrastructure/Persistence/Configurations/InventoryAlertConfiguration.cs`

```csharp
public class InventoryAlertConfiguration : IEntityTypeConfiguration<InventoryAlert>
{
    public void Configure(EntityTypeBuilder<InventoryAlert> builder)
    {
        builder.ToTable("InventoryAlerts");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.ResolvedBy).HasMaxLength(100);
        builder.Property(e => e.ResolutionNotes).HasMaxLength(500);

        // Performance: Query unread/unresolved alerts
        builder.HasIndex(e => new { e.TenantId, e.IsRead, e.IsResolved, e.CreatedAt });

        // Performance: Query by warehouse
        builder.HasIndex(e => new { e.TenantId, e.WarehouseId, e.CreatedAt });

        builder.HasOne(e => e.Warehouse)
            .WithMany()
            .HasForeignKey(e => e.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ProductVariant)
            .WithMany()
            .HasForeignKey(e => e.ProductVariantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

### 3.4 API Endpoints

#### Warehouse Endpoints

**File:** `src/NOIR.Web/Endpoints/WarehouseEndpoints.cs`

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/warehouses` | Get all warehouses (paged) |
| `GET` | `/api/warehouses/active` | Get active warehouses (for dropdowns) |
| `GET` | `/api/warehouses/{id}` | Get warehouse by ID |
| `POST` | `/api/warehouses` | Create warehouse |
| `PUT` | `/api/warehouses/{id}` | Update warehouse |
| `DELETE` | `/api/warehouses/{id}` | Soft delete warehouse |
| `POST` | `/api/warehouses/{id}/set-default` | Set as default warehouse |

#### Inventory Level Endpoints

**File:** `src/NOIR.Web/Endpoints/InventoryEndpoints.cs`

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/inventory/levels` | Get inventory levels (filterable) |
| `GET` | `/api/inventory/levels/{warehouseId}/{variantId}` | Get specific level |
| `PUT` | `/api/inventory/levels/{warehouseId}/{variantId}` | Update level settings |
| `POST` | `/api/inventory/adjust` | Manual stock adjustment |
| `POST` | `/api/inventory/import` | Bulk stock import (StockIn) |
| `POST` | `/api/inventory/reserve` | Reserve stock for order |
| `POST` | `/api/inventory/release` | Release reservation |
| `POST` | `/api/inventory/confirm` | Confirm reservation (ship) |

#### Movement Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/inventory/movements` | Get movement history (paged) |
| `GET` | `/api/inventory/movements/by-variant/{variantId}` | Get by variant |
| `GET` | `/api/inventory/movements/by-warehouse/{warehouseId}` | Get by warehouse |

#### Transfer Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/inventory/transfers` | Get transfers (paged) |
| `GET` | `/api/inventory/transfers/{id}` | Get transfer by ID |
| `POST` | `/api/inventory/transfers` | Create transfer |
| `PUT` | `/api/inventory/transfers/{id}` | Update transfer |
| `POST` | `/api/inventory/transfers/{id}/approve` | Approve transfer |
| `POST` | `/api/inventory/transfers/{id}/start-transit` | Start transit |
| `POST` | `/api/inventory/transfers/{id}/complete` | Complete transfer |
| `POST` | `/api/inventory/transfers/{id}/cancel` | Cancel transfer |

#### Alert Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/inventory/alerts` | Get alerts (filterable) |
| `GET` | `/api/inventory/alerts/unread-count` | Get unread count |
| `POST` | `/api/inventory/alerts/{id}/read` | Mark as read |
| `POST` | `/api/inventory/alerts/{id}/resolve` | Resolve alert |
| `POST` | `/api/inventory/alerts/mark-all-read` | Mark all as read |

---

## 4. Phase 3: Variant Image Gallery

### 4.1 Entity Changes

#### 4.1.1 ProductVariantImage (New)

**File:** `src/NOIR.Domain/Entities/Product/ProductVariantImage.cs`

```csharp
public class ProductVariantImage : TenantEntity<Guid>
{
    public Guid ProductVariantId { get; private set; }
    public Guid ProductImageId { get; private set; }
    public bool IsPrimary { get; private set; }
    public int SortOrder { get; private set; }

    // Navigation
    public virtual ProductVariant ProductVariant { get; private set; } = null!;
    public virtual ProductImage ProductImage { get; private set; } = null!;

    // Factory
    public static ProductVariantImage Create(
        Guid productVariantId,
        Guid productImageId,
        bool isPrimary,
        int sortOrder,
        string? tenantId)
    {
        return new ProductVariantImage
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ProductVariantId = productVariantId,
            ProductImageId = productImageId,
            IsPrimary = isPrimary,
            SortOrder = sortOrder
        };
    }

    public void SetAsPrimary() => IsPrimary = true;
    public void UnsetPrimary() => IsPrimary = false;
    public void SetSortOrder(int order) => SortOrder = order;
}
```

#### 4.1.2 ProductVariant (Modified)

**File:** `src/NOIR.Domain/Entities/Product/ProductVariant.cs`

```csharp
// REMOVE:
// public Guid? ImageId { get; private set; }
// public virtual ProductImage? Image { get; private set; }
// public void SetImage(Guid? imageId) { ... }

// ADD:
public virtual ICollection<ProductVariantImage> VariantImages { get; private set; } = new List<ProductVariantImage>();

public ProductImage? PrimaryImage => VariantImages
    .Where(vi => vi.IsPrimary)
    .Select(vi => vi.ProductImage)
    .FirstOrDefault() ?? VariantImages
    .OrderBy(vi => vi.SortOrder)
    .Select(vi => vi.ProductImage)
    .FirstOrDefault();

public void AssignImage(Guid imageId, bool isPrimary, int sortOrder, string? tenantId)
{
    if (isPrimary)
    {
        foreach (var vi in VariantImages.Where(v => v.IsPrimary))
            vi.UnsetPrimary();
    }

    var variantImage = ProductVariantImage.Create(Id, imageId, isPrimary, sortOrder, tenantId);
    VariantImages.Add(variantImage);
}

public void RemoveImage(Guid imageId)
{
    var variantImage = VariantImages.FirstOrDefault(vi => vi.ProductImageId == imageId);
    if (variantImage != null)
        VariantImages.Remove(variantImage);
}

public void SetPrimaryImage(Guid imageId)
{
    foreach (var vi in VariantImages)
    {
        if (vi.ProductImageId == imageId)
            vi.SetAsPrimary();
        else
            vi.UnsetPrimary();
    }
}
```

### 4.2 EF Configuration

**File:** `src/NOIR.Infrastructure/Persistence/Configurations/ProductVariantImageConfiguration.cs`

```csharp
public class ProductVariantImageConfiguration : IEntityTypeConfiguration<ProductVariantImage>
{
    public void Configure(EntityTypeBuilder<ProductVariantImage> builder)
    {
        builder.ToTable("ProductVariantImages");
        builder.HasKey(e => e.Id);

        // Unique: Each image can only be assigned once per variant
        builder.HasIndex(e => new { e.ProductVariantId, e.ProductImageId }).IsUnique();

        // Performance: Query by variant
        builder.HasIndex(e => e.ProductVariantId);

        builder.HasOne(e => e.ProductVariant)
            .WithMany(e => e.VariantImages)
            .HasForeignKey(e => e.ProductVariantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.ProductImage)
            .WithMany()
            .HasForeignKey(e => e.ProductImageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

### 4.3 API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/products/{productId}/variants/{variantId}/images` | Get variant images |
| `PUT` | `/api/products/{productId}/variants/{variantId}/images` | Assign images to variant |
| `DELETE` | `/api/products/{productId}/variants/{variantId}/images/{imageId}` | Remove image from variant |
| `POST` | `/api/products/{productId}/variants/{variantId}/images/{imageId}/set-primary` | Set primary |

---

## 5. Phase 4: Frontend Implementation

### 5.1 New Pages

#### Warehouse Management

**File:** `src/NOIR.Web/frontend/src/pages/portal/inventory/warehouses/WarehousesPage.tsx`

```
Features:
- List all warehouses with status badges
- Create/Edit dialog with full address form
- Set default warehouse action
- Activate/Deactivate toggle
- Delete with confirmation
```

#### Inventory Levels

**File:** `src/NOIR.Web/frontend/src/pages/portal/inventory/levels/InventoryLevelsPage.tsx`

```
Features:
- Table with filters (warehouse, product, low stock only)
- Inline edit for reorder settings
- Bulk adjust modal
- Low stock highlighting
- Export to CSV
```

#### Inventory Movements

**File:** `src/NOIR.Web/frontend/src/pages/portal/inventory/movements/InventoryMovementsPage.tsx`

```
Features:
- Timeline view of movements
- Filters (warehouse, variant, type, date range)
- Movement type badges with colors
- Reference links (to order, transfer, etc.)
- Export to CSV
```

#### Inventory Transfers

**File:** `src/NOIR.Web/frontend/src/pages/portal/inventory/transfers/InventoryTransfersPage.tsx`

```
Features:
- Kanban-style view by status OR table view
- Create transfer wizard
- Status progression actions
- Transfer detail page with items
- Print transfer slip
```

#### Inventory Alerts

**File:** `src/NOIR.Web/frontend/src/pages/portal/inventory/alerts/InventoryAlertsPage.tsx`

```
Features:
- Alert list with severity badges
- Mark as read/resolve actions
- Bulk actions
- Alert settings configuration
```

#### Stock Import

**File:** `src/NOIR.Web/frontend/src/pages/portal/inventory/import/StockImportPage.tsx`

```
Features:
- CSV upload with template download
- Column mapping
- Preview and validation
- Import progress
- Error report
```

### 5.2 ProductFormPage Enhancements

**File:** `src/NOIR.Web/frontend/src/pages/portal/ecommerce/products/ProductFormPage.tsx`

#### Changes:

1. **Remove Weight field** from Inventory card
2. **Add Variant Image Picker** to variant edit dialog

```typescript
// New component in variant edit form
<VariantImagePicker
  productImages={images}
  assignedImages={variant.images}
  onAssign={(imageIds, primaryId) => handleAssignVariantImages(variant.id, imageIds, primaryId)}
/>
```

### 5.3 New Components

#### VariantImagePicker

**File:** `src/NOIR.Web/frontend/src/components/products/VariantImagePicker.tsx`

```typescript
interface VariantImagePickerProps {
  productImages: ProductImage[]
  assignedImages: ProductVariantImage[]
  onAssign: (imageIds: string[], primaryId: string | null) => void
  disabled?: boolean
}

// UI: Grid of product images with checkboxes
// Selected images show with primary badge option
// Drag to reorder selected images
```

#### InventoryLevelBadge

**File:** `src/NOIR.Web/frontend/src/components/inventory/InventoryLevelBadge.tsx`

```typescript
// Shows stock status with color coding
// Red: Out of stock
// Yellow: Low stock
// Green: In stock
// Includes available vs reserved breakdown
```

#### TransferStatusBadge

**File:** `src/NOIR.Web/frontend/src/components/inventory/TransferStatusBadge.tsx`

#### MovementTypeBadge

**File:** `src/NOIR.Web/frontend/src/components/inventory/MovementTypeBadge.tsx`

#### StockAdjustDialog

**File:** `src/NOIR.Web/frontend/src/components/inventory/StockAdjustDialog.tsx`

```typescript
// Modal for manual stock adjustment
// Fields: Warehouse, Variant, Adjustment Type (+/-), Quantity, Notes
// Shows current stock and preview of new stock
```

#### CreateTransferDialog

**File:** `src/NOIR.Web/frontend/src/components/inventory/CreateTransferDialog.tsx`

```typescript
// Multi-step wizard:
// 1. Select From/To warehouses
// 2. Add items with quantities
// 3. Review and submit
```

### 5.4 Sidebar Navigation Update

**File:** `src/NOIR.Web/frontend/src/components/portal/Sidebar.tsx`

```typescript
// Add new section under E-commerce:
{
  title: t('nav.inventory'),
  icon: Package,
  items: [
    { title: t('nav.warehouses'), href: '/portal/inventory/warehouses' },
    { title: t('nav.stockLevels'), href: '/portal/inventory/levels' },
    { title: t('nav.movements'), href: '/portal/inventory/movements' },
    { title: t('nav.transfers'), href: '/portal/inventory/transfers' },
    { title: t('nav.alerts'), href: '/portal/inventory/alerts', badge: alertCount },
    { title: t('nav.import'), href: '/portal/inventory/import' },
  ]
}
```

### 5.5 Hooks

| Hook | File | Purpose |
|------|------|---------|
| `useWarehouses` | `hooks/useWarehouses.ts` | CRUD for warehouses |
| `useInventoryLevels` | `hooks/useInventoryLevels.ts` | Get/update levels |
| `useInventoryMovements` | `hooks/useInventoryMovements.ts` | Movement history |
| `useInventoryTransfers` | `hooks/useInventoryTransfers.ts` | Transfer management |
| `useInventoryAlerts` | `hooks/useInventoryAlerts.ts` | Alert management |
| `useVariantImages` | `hooks/useVariantImages.ts` | Variant image assignment |

### 5.6 Services

| Service | File | Purpose |
|---------|------|---------|
| `warehouses.ts` | `services/warehouses.ts` | Warehouse API calls |
| `inventory.ts` | `services/inventory.ts` | Inventory API calls |
| `transfers.ts` | `services/transfers.ts` | Transfer API calls |
| `inventoryAlerts.ts` | `services/inventoryAlerts.ts` | Alert API calls |

### 5.7 Types

**File:** `src/NOIR.Web/frontend/src/types/inventory.ts`

```typescript
export interface Warehouse {
  id: string
  code: string
  name: string
  address?: string
  city?: string
  province?: string
  postalCode?: string
  contactName?: string
  contactPhone?: string
  contactEmail?: string
  isDefault: boolean
  isActive: boolean
  sortOrder: number
}

export interface InventoryLevel {
  id: string
  warehouseId: string
  warehouseName: string
  productVariantId: string
  variantName: string
  productName: string
  sku?: string
  quantity: number
  reservedQuantity: number
  availableQuantity: number
  reorderPoint?: number
  reorderQuantity?: number
  lastStockCheckAt?: string
  isLowStock: boolean
  isOutOfStock: boolean
}

export interface InventoryMovement {
  id: string
  warehouseId: string
  warehouseName: string
  productVariantId: string
  variantName: string
  productName: string
  movementType: InventoryMovementType
  quantity: number
  previousQuantity: number
  newQuantity: number
  referenceType?: string
  referenceId?: string
  notes?: string
  createdBy: string
  createdByName: string
  createdAt: string
}

export type InventoryMovementType =
  | 'StockIn' | 'StockOut' | 'Adjustment' | 'Return'
  | 'Reservation' | 'ReservationRelease' | 'Damaged' | 'Expired'
  | 'TransferOut' | 'TransferIn'

export interface InventoryTransfer {
  id: string
  transferNumber: string
  fromWarehouseId: string
  fromWarehouseName: string
  toWarehouseId: string
  toWarehouseName: string
  status: TransferStatus
  notes?: string
  requestedBy: string
  requestedByName: string
  requestedAt: string
  approvedBy?: string
  approvedAt?: string
  completedBy?: string
  completedAt?: string
  cancelledBy?: string
  cancelledAt?: string
  cancellationReason?: string
  items: InventoryTransferItem[]
}

export type TransferStatus = 'Pending' | 'Approved' | 'InTransit' | 'Completed' | 'Cancelled'

export interface InventoryTransferItem {
  id: string
  productVariantId: string
  variantName: string
  productName: string
  sku?: string
  requestedQuantity: number
  receivedQuantity?: number
  notes?: string
}

export interface InventoryAlert {
  id: string
  warehouseId: string
  warehouseName: string
  productVariantId: string
  variantName: string
  productName: string
  alertType: AlertType
  currentQuantity: number
  thresholdQuantity: number
  isRead: boolean
  isResolved: boolean
  createdAt: string
  readAt?: string
  resolvedAt?: string
  resolvedBy?: string
  resolutionNotes?: string
}

export type AlertType = 'LowStock' | 'OutOfStock' | 'Overstock'
```

### 5.8 Localization

**Files:**
- `src/NOIR.Web/frontend/public/locales/en/common.json`
- `src/NOIR.Web/frontend/public/locales/vi/common.json`

```json
{
  "inventory": {
    "title": "Inventory Management",
    "warehouses": "Warehouses",
    "levels": "Stock Levels",
    "movements": "Stock Movements",
    "transfers": "Stock Transfers",
    "alerts": "Alerts",
    "import": "Import Stock",
    "createWarehouse": "Create Warehouse",
    "editWarehouse": "Edit Warehouse",
    "warehouseCode": "Warehouse Code",
    "warehouseName": "Warehouse Name",
    "setAsDefault": "Set as Default",
    "currentStock": "Current Stock",
    "reservedStock": "Reserved",
    "availableStock": "Available",
    "reorderPoint": "Reorder Point",
    "reorderQuantity": "Reorder Quantity",
    "adjustStock": "Adjust Stock",
    "addStock": "Add Stock",
    "removeStock": "Remove Stock",
    "adjustmentNotes": "Adjustment Notes",
    "movementType": "Movement Type",
    "createTransfer": "Create Transfer",
    "fromWarehouse": "From Warehouse",
    "toWarehouse": "To Warehouse",
    "transferNumber": "Transfer #",
    "requestedQuantity": "Requested Qty",
    "receivedQuantity": "Received Qty",
    "approveTransfer": "Approve",
    "startTransit": "Start Transit",
    "completeTransfer": "Complete",
    "cancelTransfer": "Cancel",
    "lowStockAlert": "Low Stock",
    "outOfStockAlert": "Out of Stock",
    "markAsRead": "Mark as Read",
    "resolveAlert": "Resolve",
    "downloadTemplate": "Download Template",
    "uploadFile": "Upload File",
    "importPreview": "Import Preview",
    "startImport": "Start Import"
  },
  "variantImages": {
    "title": "Variant Images",
    "selectImages": "Select images for this variant",
    "setPrimary": "Set as Primary",
    "noImagesAssigned": "No images assigned to this variant"
  }
}
```

---

## 6. Phase 5: Testing Strategy

### 6.1 Unit Tests

#### Domain Entity Tests

**File:** `tests/NOIR.Domain.UnitTests/Entities/Inventory/`

| Test Class | Tests |
|------------|-------|
| `WarehouseTests` | Create, Update, SetDefault, Activate/Deactivate |
| `InventoryLevelTests` | Create, AddStock, RemoveStock, Reserve, Release, Confirm |
| `InventoryMovementTests` | Create with all movement types |
| `InventoryTransferTests` | Create, AddItem, Approve, StartTransit, Complete, Cancel |
| `InventoryAlertTests` | Create, MarkAsRead, Resolve |

#### Command Handler Tests

**File:** `tests/NOIR.Application.UnitTests/Features/Inventory/`

| Test Class | Tests |
|------------|-------|
| `CreateWarehouseCommandHandlerTests` | Happy path, Duplicate code, Validation |
| `UpdateWarehouseCommandHandlerTests` | Happy path, Not found, Duplicate code |
| `DeleteWarehouseCommandHandlerTests` | Happy path, Not found, Has inventory |
| `SetDefaultWarehouseCommandHandlerTests` | Happy path, Unset previous default |
| `AdjustInventoryCommandHandlerTests` | Add stock, Remove stock, Insufficient stock |
| `CreateTransferCommandHandlerTests` | Happy path, Same warehouse, Empty items |
| `ApproveTransferCommandHandlerTests` | Happy path, Not pending, Insufficient stock |
| `CompleteTransferCommandHandlerTests` | Happy path, Not approved, Partial receipt |

#### Query Handler Tests

| Test Class | Tests |
|------------|-------|
| `GetWarehousesQueryHandlerTests` | List, Filter by active, Pagination |
| `GetInventoryLevelsQueryHandlerTests` | List, Filter by warehouse, Low stock filter |
| `GetInventoryMovementsQueryHandlerTests` | List, Filter by type, Date range |
| `GetInventoryTransfersQueryHandlerTests` | List, Filter by status |
| `GetInventoryAlertsQueryHandlerTests` | List, Filter by read/resolved |

### 6.2 Integration Tests

**File:** `tests/NOIR.IntegrationTests/Endpoints/`

#### Warehouse Integration Tests

```csharp
public class WarehouseEndpointsTests : IntegrationTestBase
{
    [Fact]
    public async Task CreateWarehouse_WithValidData_ReturnsCreated()

    [Fact]
    public async Task CreateWarehouse_DuplicateCode_ReturnsConflict()

    [Fact]
    public async Task GetWarehouses_ReturnsPagedResult()

    [Fact]
    public async Task SetDefaultWarehouse_UnsetsOther()

    [Fact]
    public async Task DeleteWarehouse_WithInventory_ReturnsBadRequest()
}
```

#### Inventory Integration Tests

```csharp
public class InventoryEndpointsTests : IntegrationTestBase
{
    [Fact]
    public async Task AdjustStock_Positive_IncreasesQuantity()

    [Fact]
    public async Task AdjustStock_Negative_DecreasesQuantity()

    [Fact]
    public async Task AdjustStock_InsufficientStock_ReturnsBadRequest()

    [Fact]
    public async Task AdjustStock_CreatesMovementRecord()

    [Fact]
    public async Task LowStock_CreatesAlert()
}
```

#### Transfer Integration Tests

```csharp
public class TransferEndpointsTests : IntegrationTestBase
{
    [Fact]
    public async Task CreateTransfer_WithItems_ReturnsPending()

    [Fact]
    public async Task ApproveTransfer_ReservesStock()

    [Fact]
    public async Task CompleteTransfer_MovesStock()

    [Fact]
    public async Task CancelTransfer_ReleasesReservation()

    [Fact]
    public async Task CompleteTransfer_CreatesMovementRecords()
}
```

#### Variant Image Integration Tests

```csharp
public class VariantImageEndpointsTests : IntegrationTestBase
{
    [Fact]
    public async Task AssignImages_ToVariant_ReturnsSuccess()

    [Fact]
    public async Task AssignImages_SetsPrimary()

    [Fact]
    public async Task RemoveImage_FromVariant_ReturnsSuccess()

    [Fact]
    public async Task DeleteProductImage_CascadesToVariants()
}
```

### 6.3 Playwright E2E Tests

**File:** `tests/NOIR.E2E/specs/inventory/`

#### Warehouse Management E2E

```typescript
// warehouse.spec.ts
import { test, expect } from '@playwright/test'

test.describe('Warehouse Management', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/portal/inventory/warehouses')
  })

  test('should create a new warehouse', async ({ page }) => {
    await page.click('button:has-text("Create Warehouse")')
    await page.fill('input[name="code"]', 'KHO-TEST')
    await page.fill('input[name="name"]', 'Test Warehouse')
    await page.fill('input[name="address"]', '123 Test Street')
    await page.click('button:has-text("Create")')

    await expect(page.locator('text=KHO-TEST')).toBeVisible()
    await expect(page.locator('text=Test Warehouse')).toBeVisible()
  })

  test('should set warehouse as default', async ({ page }) => {
    await page.click('[data-testid="warehouse-menu-KHO-HN"]')
    await page.click('text=Set as Default')

    await expect(page.locator('[data-testid="default-badge-KHO-HN"]')).toBeVisible()
  })

  test('should prevent deleting warehouse with inventory', async ({ page }) => {
    // Warehouse with stock
    await page.click('[data-testid="warehouse-menu-KHO-HN"]')
    await page.click('text=Delete')
    await page.click('button:has-text("Confirm")')

    await expect(page.locator('text=Cannot delete warehouse with inventory')).toBeVisible()
  })
})
```

#### Inventory Levels E2E

```typescript
// inventory-levels.spec.ts
test.describe('Inventory Levels', () => {
  test('should display stock levels', async ({ page }) => {
    await page.goto('/portal/inventory/levels')

    await expect(page.locator('table')).toBeVisible()
    await expect(page.locator('th:has-text("Product")')).toBeVisible()
    await expect(page.locator('th:has-text("Warehouse")')).toBeVisible()
    await expect(page.locator('th:has-text("Available")')).toBeVisible()
  })

  test('should filter by low stock', async ({ page }) => {
    await page.goto('/portal/inventory/levels')
    await page.click('label:has-text("Low Stock Only")')

    // All rows should have low stock badge
    const rows = page.locator('tr[data-low-stock="true"]')
    await expect(rows).toHaveCount(await page.locator('tbody tr').count())
  })

  test('should adjust stock manually', async ({ page }) => {
    await page.goto('/portal/inventory/levels')
    await page.click('[data-testid="adjust-stock-btn"]')

    // Fill adjustment form
    await page.selectOption('select[name="adjustmentType"]', 'add')
    await page.fill('input[name="quantity"]', '50')
    await page.fill('textarea[name="notes"]', 'Manual stock adjustment')
    await page.click('button:has-text("Confirm Adjustment")')

    await expect(page.locator('text=Stock adjusted successfully')).toBeVisible()
  })
})
```

#### Stock Transfer E2E

```typescript
// transfers.spec.ts
test.describe('Stock Transfers', () => {
  test('should create transfer with items', async ({ page }) => {
    await page.goto('/portal/inventory/transfers')
    await page.click('button:has-text("Create Transfer")')

    // Step 1: Select warehouses
    await page.selectOption('select[name="fromWarehouse"]', 'KHO-HN')
    await page.selectOption('select[name="toWarehouse"]', 'KHO-HCM')
    await page.click('button:has-text("Next")')

    // Step 2: Add items
    await page.click('button:has-text("Add Item")')
    await page.fill('input[name="search"]', 'iPhone')
    await page.click('text=iPhone 15 Pro - 256GB')
    await page.fill('input[name="quantity"]', '10')
    await page.click('button:has-text("Add")')
    await page.click('button:has-text("Next")')

    // Step 3: Review and submit
    await page.fill('textarea[name="notes"]', 'Monthly transfer')
    await page.click('button:has-text("Create Transfer")')

    await expect(page.locator('text=Transfer created successfully')).toBeVisible()
    await expect(page.locator('[data-status="Pending"]')).toBeVisible()
  })

  test('should complete transfer flow', async ({ page }) => {
    // Navigate to pending transfer
    await page.goto('/portal/inventory/transfers')
    await page.click('[data-testid="transfer-TRF-20260129-ABC"]')

    // Approve
    await page.click('button:has-text("Approve")')
    await expect(page.locator('[data-status="Approved"]')).toBeVisible()

    // Start transit
    await page.click('button:has-text("Start Transit")')
    await expect(page.locator('[data-status="InTransit"]')).toBeVisible()

    // Complete with received quantities
    await page.click('button:has-text("Complete")')
    await page.fill('input[name="receivedQty-0"]', '10')
    await page.click('button:has-text("Confirm Completion")')

    await expect(page.locator('[data-status="Completed"]')).toBeVisible()
    await expect(page.locator('text=Transfer completed')).toBeVisible()
  })
})
```

#### Variant Images E2E

```typescript
// variant-images.spec.ts
test.describe('Variant Image Assignment', () => {
  test('should assign images to variant', async ({ page }) => {
    await page.goto('/portal/ecommerce/products/123/edit')

    // Open variant edit dialog
    await page.click('[data-testid="edit-variant-red"]')

    // Select images
    await page.click('[data-testid="image-checkbox-1"]')
    await page.click('[data-testid="image-checkbox-2"]')

    // Set primary
    await page.click('[data-testid="set-primary-1"]')

    // Save
    await page.click('button:has-text("Save Variant")')

    await expect(page.locator('text=Variant updated')).toBeVisible()
  })

  test('should scroll to variant image on storefront', async ({ page }) => {
    await page.goto('/products/test-product')

    // Click Red variant
    await page.click('[data-variant="Red"]')

    // Check gallery scrolled to Red's primary image
    const activeImage = page.locator('[data-gallery-active="true"]')
    await expect(activeImage).toHaveAttribute('data-image-id', 'red-primary-image-id')
  })
})
```

#### Product Form E2E

```typescript
// product-form.spec.ts
test.describe('Product Form - Weight Removed', () => {
  test('should not show weight field in form', async ({ page }) => {
    await page.goto('/portal/ecommerce/products/new')

    // Weight field should not exist
    await expect(page.locator('label:has-text("Weight")')).not.toBeVisible()

    // Inventory card should exist but without weight
    await expect(page.locator('text=Inventory')).toBeVisible()
    await expect(page.locator('text=Track Inventory')).toBeVisible()
  })

  test('should show shipping attributes in attribute section', async ({ page }) => {
    await page.goto('/portal/ecommerce/products/123/edit')

    // Attribute section should show global shipping attributes
    await expect(page.locator('[data-attribute="weight"]')).toBeVisible()
    await expect(page.locator('[data-attribute="height"]')).toBeVisible()
    await expect(page.locator('[data-attribute="width"]')).toBeVisible()
    await expect(page.locator('[data-attribute="length"]')).toBeVisible()
  })
})
```

---

## 7. Implementation Order

### Phase 1: Attribute Enhancement (Week 1)

| Day | Task | Effort |
|-----|------|--------|
| 1 | Add `IsGlobal` to ProductAttribute entity + migration | 2h |
| 1 | Update ProductAttribute commands/queries | 3h |
| 1 | Remove Weight from Product entity + migration | 1h |
| 2 | Seed global shipping attributes | 2h |
| 2 | GlobalAttributeSyncHandler implementation | 3h |
| 2 | Unit tests for attribute changes | 3h |
| 3 | Frontend: Update attribute management page | 4h |
| 3 | Frontend: Remove weight from ProductFormPage | 1h |
| 3 | Integration tests | 3h |

### Phase 2: Inventory Core (Week 2)

| Day | Task | Effort |
|-----|------|--------|
| 1 | Warehouse entity + configuration + migration | 3h |
| 1 | InventoryLevel entity + configuration | 3h |
| 2 | InventoryMovement entity + configuration | 3h |
| 2 | Warehouse CRUD commands/queries | 4h |
| 3 | Inventory level queries + adjust command | 4h |
| 3 | Movement history query | 2h |
| 4 | Unit tests for inventory core | 4h |
| 4 | Integration tests for warehouse/levels | 4h |
| 5 | Frontend: Warehouses page | 6h |

### Phase 3: Inventory Transfers (Week 3)

| Day | Task | Effort |
|-----|------|--------|
| 1 | InventoryTransfer entity + configuration | 4h |
| 1 | Transfer commands (create, approve, complete, cancel) | 6h |
| 2 | Transfer state machine + stock reservation | 4h |
| 2 | Transfer queries | 2h |
| 3 | Unit tests for transfers | 4h |
| 3 | Integration tests for transfers | 4h |
| 4-5 | Frontend: Transfers page + dialogs | 8h |

### Phase 4: Alerts & Import (Week 4)

| Day | Task | Effort |
|-----|------|--------|
| 1 | InventoryAlert entity + configuration | 3h |
| 1 | Alert generation on low stock | 3h |
| 2 | Alert commands/queries | 3h |
| 2 | Email notification service (optional) | 4h |
| 3 | Bulk import command | 4h |
| 3 | CSV parsing + validation | 3h |
| 4 | Frontend: Alerts page | 4h |
| 4 | Frontend: Import page | 4h |
| 5 | Frontend: Inventory levels page | 6h |

### Phase 5: Variant Images (Week 5)

| Day | Task | Effort |
|-----|------|--------|
| 1 | ProductVariantImage entity + configuration + migration | 3h |
| 1 | Migrate existing ImageId data | 2h |
| 2 | Variant image commands (assign, remove, set-primary) | 4h |
| 2 | Variant image queries | 2h |
| 3 | Unit tests | 3h |
| 3 | Integration tests | 3h |
| 4 | Frontend: VariantImagePicker component | 4h |
| 4 | Frontend: Integrate into ProductFormPage | 3h |
| 5 | Frontend: Storefront gallery scroll behavior | 4h |

### Phase 6: E2E Testing & Polish (Week 6)

| Day | Task | Effort |
|-----|------|--------|
| 1-2 | Playwright E2E tests for all features | 8h |
| 3 | Bug fixes from E2E testing | 4h |
| 3 | Performance optimization | 4h |
| 4 | UI/UX polish and accessibility | 4h |
| 4 | Localization (EN/VI) | 4h |
| 5 | Documentation update | 4h |
| 5 | Final review and cleanup | 4h |

---

## 8. Migration Strategy

### 8.1 Database Migrations

```bash
# Step 1: Add new tables (non-breaking)
dotnet ef migrations add AddInventoryEntities \
  --project src/NOIR.Infrastructure \
  --startup-project src/NOIR.Web \
  --context ApplicationDbContext \
  --output-dir Migrations/App

# Step 2: Add ProductVariantImage table
dotnet ef migrations add AddProductVariantImage \
  --project src/NOIR.Infrastructure \
  --startup-project src/NOIR.Web \
  --context ApplicationDbContext \
  --output-dir Migrations/App

# Step 3: Remove Weight from Product (breaking change - do last)
dotnet ef migrations add RemoveWeightFromProduct \
  --project src/NOIR.Infrastructure \
  --startup-project src/NOIR.Web \
  --context ApplicationDbContext \
  --output-dir Migrations/App
```

### 8.2 Data Migration

#### Migrate Existing Variant ImageId to ProductVariantImage

```csharp
// In migration or data seeder
public void MigrateVariantImages()
{
    // Get all variants with ImageId set
    var variants = context.ProductVariants
        .Where(v => v.ImageId != null)
        .ToList();

    foreach (var variant in variants)
    {
        var variantImage = ProductVariantImage.Create(
            variant.Id,
            variant.ImageId!.Value,
            isPrimary: true,
            sortOrder: 0,
            variant.TenantId
        );
        context.ProductVariantImages.Add(variantImage);
    }

    context.SaveChanges();
}
```

#### Migrate Existing Weight to Attribute

```csharp
// In migration or data seeder
public void MigrateWeightToAttribute()
{
    // Get weight attribute
    var weightAttr = context.ProductAttributes
        .FirstOrDefault(a => a.Code == "weight" && a.IsGlobal);

    // Get all products with weight
    var products = context.Products
        .Where(p => p.Weight != null)
        .ToList();

    foreach (var product in products)
    {
        var assignment = ProductAttributeAssignment.Create(
            product.Id,
            weightAttr.Id,
            product.Weight.ToString(),
            product.TenantId
        );
        context.ProductAttributeAssignments.Add(assignment);
    }

    context.SaveChanges();
}
```

### 8.3 Feature Flags (Optional)

If needed, use feature flags for gradual rollout:

```csharp
// appsettings.json
{
  "FeatureFlags": {
    "MultiWarehouseInventory": false,
    "VariantImageGallery": false,
    "GlobalAttributes": true
  }
}
```

---

## Summary

This design document provides a complete implementation plan for:

1. **Attribute System Enhancement** - Global attributes with auto-assign
2. **Full Inventory Management** - Multi-warehouse, movements, transfers, alerts
3. **Variant Image Gallery** - Shared gallery with M:N relationships

**Total Effort Estimate:** 6 weeks

**Key Deliverables:**
- 7 new entities
- 40+ new API endpoints
- 6 new frontend pages
- 15+ new components
- 50+ unit tests
- 20+ integration tests
- 10+ Playwright E2E tests

**Ready for Implementation?** Use `/sc:implement` to start building.
