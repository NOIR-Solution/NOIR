# Bulk Operations Pattern

## Overview

NOIR provides high-performance bulk operations for scenarios requiring insertion, update, or deletion of large datasets (1000+ records). These operations use [EFCore.BulkExtensions.MIT](https://www.nuget.org/packages/EFCore.BulkExtensions.MIT) under the hood, which leverages SQL Server's `SqlBulkCopy` for maximum performance.

> **Package Note:** This project uses `EFCore.BulkExtensions.MIT` (v10.22.0), the MIT-licensed fork of [EFCore.BulkExtensions](https://github.com/borisdj/EFCore.BulkExtensions) with EF Core 10 support. The original package uses a dual-license model and does not yet support EF Core 10.

## Performance Comparison

| Operation | AddRange/UpdateRange | Bulk Operations | Improvement |
|-----------|---------------------|-----------------|-------------|
| Insert 10K records | ~200ms | ~15ms | **13x faster** |
| Insert 100K records | ~2,100ms | ~150ms | **14x faster** |
| Insert 1M records | ~21,000ms | ~1,400ms | **15x faster** |
| Update 10K records | ~250ms | ~60ms | **4x faster** |
| Delete 10K records | ~300ms | ~50ms | **6x faster** |

## When to Use

| Dataset Size | Recommendation |
|--------------|----------------|
| < 100 records | Use standard `AddAsync`/`AddRangeAsync` |
| 100-1,000 records | Either approach works |
| 1,000-10,000 records | Consider bulk operations |
| > 10,000 records | **Always use bulk operations** |

## Available Methods

### IRepository<TEntity, TId> Bulk Methods

```csharp
// Insert - SqlBulkCopy for maximum performance
Task BulkInsertAsync(IEnumerable<TEntity> entities, BulkOperationConfig? config = null, CancellationToken ct = default);

// Update - MERGE statement for efficient updates
Task BulkUpdateAsync(IEnumerable<TEntity> entities, BulkOperationConfig? config = null, CancellationToken ct = default);

// Upsert - Insert or Update based on key match
Task BulkInsertOrUpdateAsync(IEnumerable<TEntity> entities, BulkOperationConfig? config = null, CancellationToken ct = default);

// Delete - Bulk delete provided entities
Task BulkDeleteAsync(IEnumerable<TEntity> entities, BulkOperationConfig? config = null, CancellationToken ct = default);

// Sync - Full table sync (insert new, update existing, delete missing)
Task BulkSyncAsync(IEnumerable<TEntity> entities, BulkOperationConfig? config = null, CancellationToken ct = default);

// Read - Efficient bulk lookup by keys
Task<IReadOnlyList<TEntity>> BulkReadAsync(IEnumerable<TEntity> entities, BulkOperationConfig? config = null, CancellationToken ct = default);
```

## Configuration Options

```csharp
public class BulkOperationConfig
{
    // Batch size (default: 2000)
    public int BatchSize { get; set; } = 2000;

    // Timeout in seconds (null = 30s default, 0 = no timeout)
    public int? BulkCopyTimeout { get; set; }

    // Return generated IDs (performance cost)
    public bool SetOutputIdentity { get; set; }

    // Maintain entity order (required when SetOutputIdentity = true)
    public bool PreserveInsertOrder { get; set; } = true;

    // Properties to include (null = all)
    public IList<string>? PropertiesToInclude { get; set; }

    // Properties to exclude
    public IList<string>? PropertiesToExclude { get; set; }

    // Custom matching properties for update/upsert
    public IList<string>? UpdateByProperties { get; set; }

    // Enable operation statistics
    public bool CalculateStats { get; set; }

    // HOLDLOCK for MERGE (default: true)
    public bool WithHoldlock { get; set; } = true;

    // Safety confirmations for BulkSyncAsync (REQUIRED)
    public bool ConfirmSyncWillDeleteMissingRecords { get; set; }
    public bool ConfirmSyncWithEmptyCollection { get; set; }
}
```

### Pre-built Configurations

```csharp
// Default performance configuration
var config = BulkOperationConfig.Default;

// When you need generated IDs back
var config = BulkOperationConfig.WithOutputIdentity;

// For very large datasets
var config = BulkOperationConfig.LargeBatch; // BatchSize = 5000, Timeout = 120s
```

### Fluent API

```csharp
// Fluent configuration for complex scenarios
var config = new BulkOperationConfig()
    .WithBatchSize(5000)                      // Custom batch size
    .WithTimeout(120)                          // 120 second timeout
    .WithIdentityOutput()                      // Return generated IDs
    .UpdateBy("Email", "TenantId")            // Match by business key
    .ExcludeProperties("CreatedAt", "CreatedBy") // Don't overwrite audit fields
    .WithStats()                               // Enable statistics
    .WithoutHoldlock()                         // Reduce deadlock risk
    .ConfirmSyncDeletion();                    // Required for BulkSyncAsync
```

## Usage Examples

### Basic Bulk Insert

```csharp
public async Task ImportProducts(List<Product> products, CancellationToken ct)
{
    await _productRepository.BulkInsertAsync(products, cancellationToken: ct);
}
```

### Bulk Insert with Generated IDs

```csharp
public async Task CreateOrdersWithItems(List<Order> orders, CancellationToken ct)
{
    // Insert orders and get generated IDs
    await _orderRepository.BulkInsertAsync(orders, BulkOperationConfig.WithOutputIdentity, ct);

    // Now orders have their IDs populated - assign to items
    var allItems = orders.SelectMany(o => o.Items).ToList();
    foreach (var order in orders)
    {
        foreach (var item in order.Items)
        {
            item.OrderId = order.Id;
        }
    }

    // Insert all items
    await _orderItemRepository.BulkInsertAsync(allItems, cancellationToken: ct);
}
```

### Bulk Upsert (Import/Sync Scenario)

```csharp
public async Task SyncProductCatalog(List<Product> externalProducts, CancellationToken ct)
{
    var config = new BulkOperationConfig
    {
        UpdateByProperties = ["ExternalId", "TenantId"], // Match by business key
        PropertiesToExclude = ["CreatedAt", "CreatedBy"] // Don't overwrite audit fields
    };

    await _productRepository.BulkInsertOrUpdateAsync(externalProducts, config, ct);
}
```

### Bulk Operations with Transaction

```csharp
public async Task ImportDataset(ImportData data, CancellationToken ct)
{
    await using var transaction = await _unitOfWork.BeginTransactionAsync(ct);

    try
    {
        // Multiple bulk operations in single transaction
        await _customerRepository.BulkInsertAsync(data.Customers, cancellationToken: ct);
        await _orderRepository.BulkInsertAsync(data.Orders, cancellationToken: ct);
        await _productRepository.BulkUpdateAsync(data.UpdatedProducts, cancellationToken: ct);

        await _unitOfWork.CommitTransactionAsync(ct);
    }
    catch
    {
        await _unitOfWork.RollbackTransactionAsync(ct);
        throw;
    }
}
```

### Full Table Sync (Dangerous - Requires Explicit Confirmation!)

```csharp
// WARNING: This DELETES entities not in the collection!
// You MUST explicitly confirm this destructive behavior.
public async Task FullProductSync(List<Product> masterList, CancellationToken ct)
{
    var config = new BulkOperationConfig()
        .UpdateBy("Sku")                  // Match by SKU
        .WithStats()                       // Enable statistics
        .ConfirmSyncDeletion();           // REQUIRED: Confirms deletion of missing records

    await _productRepository.BulkSyncAsync(masterList, config, ct);

    // Check what happened
    var stats = config.Stats;
    _logger.LogInformation(
        "Sync complete: {Inserted} inserted, {Updated} updated, {Deleted} deleted",
        stats?.RowsInserted, stats?.RowsUpdated, stats?.RowsDeleted);
}

// For syncing with an empty collection (deletes ALL records)
public async Task DeleteAllProducts(CancellationToken ct)
{
    var config = new BulkOperationConfig
    {
        ConfirmSyncWillDeleteMissingRecords = true,
        ConfirmSyncWithEmptyCollection = true  // REQUIRED when passing empty collection
    };

    await _productRepository.BulkSyncAsync([], config, ct);
}
```

### Bulk Read for Efficient Lookups

```csharp
public async Task<List<Product>> GetProductsBySkus(List<string> skus, CancellationToken ct)
{
    // Create entities with just the lookup keys
    var lookups = skus.Select(sku => new Product { Sku = sku }).ToList();

    var config = new BulkOperationConfig
    {
        UpdateByProperties = ["Sku"] // Match by SKU instead of PK
    };

    // BulkRead populates all properties from database
    return (await _productRepository.BulkReadAsync(lookups, config, ct)).ToList();
}
```

## Important Considerations

### Bypasses Change Tracking and Audit Logging

Bulk operations bypass EF Core's change tracking for performance. This means:

- **No automatic audit logging** via `AuditableEntityInterceptor`
- **No domain events** triggered
- **No cascade operations** (must handle manually)
- **CreatedBy, ModifiedBy, etc. will NOT be set automatically**

**Recommended Approaches for Audit-Critical Bulk Operations:**

```csharp
// Option 1: For smaller datasets (< 1000 records), use standard methods
// This preserves full audit trail via interceptors
await _repo.AddRangeAsync(products);
await _unitOfWork.SaveChangesAsync(ct);

// Option 2: For larger datasets, create a bulk operation audit log entry
public async Task ImportProductsWithAuditLog(List<Product> products, CancellationToken ct)
{
    var config = new BulkOperationConfig().WithStats();
    await _productRepo.BulkInsertAsync(products, config, ct);

    // Log the bulk operation for compliance
    var auditLog = new EntityAuditLog
    {
        EntityType = nameof(Product),
        Operation = "BulkInsert",
        Changes = $"Imported {config.Stats?.RowsInserted} products",
        UserId = _currentUser.UserId,
        TenantId = _currentUser.TenantId,
        Timestamp = _dateTime.UtcNow
    };
    await _auditLogRepo.AddAsync(auditLog, ct);
    await _unitOfWork.SaveChangesAsync(ct);
}

// Option 3: Implement database triggers for CreatedBy/ModifiedBy
// CREATE TRIGGER trg_Products_BulkAudit ON Products
// AFTER INSERT AS ...
```

**When to Accept No Audit Trail:**
- Data imports from trusted external systems
- Batch processing / background jobs with their own logging
- Performance-critical operations where audit is tracked elsewhere

### Bypasses Soft Delete

`BulkDeleteEntitiesAsync(IEnumerable<TEntity>)` performs **hard delete**. For soft delete in bulk:

```csharp
// Use specification-based bulk soft delete
var spec = new ActiveProductsSpec(categoryId);
await _productRepository.BulkSoftDeleteAsync(spec, ct);
```

### Multi-Tenancy Protection

Bulk operations include **automatic tenant validation**. Before executing, the repository validates that all entities in the collection belong to the current tenant:

```csharp
// This will throw InvalidOperationException if any entity has wrong TenantId
var products = GetProductsFromExternalSystem(); // Some have TenantId = "other-tenant"
await _productRepo.BulkInsertAsync(products);  // THROWS: Cross-tenant data manipulation not allowed

// Correct approach: ensure TenantId matches current context
var currentTenantId = _currentUser.TenantId;
var validProducts = products.Where(p => p.TenantId == currentTenantId).ToList();
await _productRepo.BulkInsertAsync(validProducts);
```

This validation prevents:
- Accidental cross-tenant data leaks
- Malicious attempts to manipulate other tenants' data
- Data import errors where TenantId wasn't set correctly

### Testing Limitations

`EFCore.BulkExtensions` requires a real relational database - it **does not work with InMemory provider**. For testing:

- Use LocalDB or SQLite for integration tests
- Mock the repository interface for unit tests

## Files Changed

| File | Purpose |
|------|---------|
| `src/NOIR.Domain/Common/BulkOperationConfig.cs` | Configuration class |
| `src/NOIR.Domain/Common/BulkOperationStats.cs` | Statistics class |
| `src/NOIR.Domain/Interfaces/IRepository.cs` | Bulk method interfaces |
| `src/NOIR.Domain/Interfaces/IUnitOfWork.cs` | Transaction support |
| `src/NOIR.Infrastructure/Persistence/Repositories/Repository.cs` | Implementation |
| `src/NOIR.Infrastructure/Persistence/BulkConfigMapper.cs` | Config mapping |
| `src/NOIR.Infrastructure/Persistence/DbTransactionWrapper.cs` | Transaction wrapper |
| `src/NOIR.Infrastructure/Persistence/ApplicationDbContext.cs` | Transaction methods |

## References

- [EFCore.BulkExtensions.MIT on NuGet](https://www.nuget.org/packages/EFCore.BulkExtensions.MIT)
- [EFCore.BulkExtensions GitHub](https://github.com/borisdj/EFCore.BulkExtensions)
- [EF Core ExecuteUpdate/ExecuteDelete](https://learn.microsoft.com/en-us/ef/core/saving/execute-insert-update-delete)
- [SqlBulkCopy Performance](https://www.milanjovanovic.tech/blog/fast-sql-bulk-inserts-with-csharp-and-ef-core)
