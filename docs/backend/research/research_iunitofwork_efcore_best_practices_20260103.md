# Deep Research: IUnitOfWork & EF Core Best Practices

## Executive Summary

This research evaluates the NOIR project's implementation of the Repository and Unit of Work patterns against industry best practices. The analysis covers:

1. **Unit of Work Pattern Implementation**
2. **Change Tracking (AsTracking vs AsNoTracking)**
3. **Update Patterns (DbContext.Update vs tracked modification)**
4. **Specification Pattern Integration**

**Key Finding**: NOIR's implementation follows EF Core best practices correctly. The pattern of using `AsTracking()` for mutations and `AsNoTracking` (default) for reads, combined with direct property modification on tracked entities followed by `SaveChangesAsync()`, aligns with Microsoft's recommended approach.

---

## 1. Unit of Work Pattern Analysis

### Industry Best Practice

The Unit of Work pattern coordinates multiple repository operations into a single transaction, ensuring data consistency. Key principles:

| Principle | Description |
|-----------|-------------|
| **Single Transaction** | Group multiple operations to succeed/fail together |
| **DbContext IS the Unit of Work** | EF Core's `DbContext` inherently implements UoW pattern |
| **Scoped Lifetime** | One DbContext per request (AddScoped) |
| **Repository Separation** | Repositories should NOT call `SaveChanges()` |
| **Explicit Commit** | Call `SaveChangesAsync()` once at the end of the operation |

### NOIR Implementation ✅ CORRECT

```csharp
// IUnitOfWork.cs - Domain layer interface
public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    bool HasActiveTransaction { get; }
}

// ApplicationDbContext implements IUnitOfWork
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext, IUnitOfWork
```

**Strengths**:
1. **DbContext implements IUnitOfWork** - Follows "DbContext IS the UoW" principle
2. **Repository methods don't save** - `AddAsync()`, `Update()`, `Remove()` do NOT call `SaveChanges()`
3. **Explicit transaction support** - `BeginTransactionAsync()`, `CommitTransactionAsync()`, `RollbackTransactionAsync()`
4. **Scoped registration** - Both `IUnitOfWork` and repositories registered as scoped

**Usage Example (RefreshTokenService)**:
```csharp
// Correct pattern: repository operation + UoW save
await _repository.AddAsync(token, cancellationToken);
await _unitOfWork.SaveChangesAsync(cancellationToken);  // ✅ Explicit save

// Multiple operations, single save
foreach (var token in tokens)
{
    token.Revoke(ipAddress, reason ?? "Token family revoked");
}
await _unitOfWork.SaveChangesAsync(cancellationToken);  // ✅ Single save for all
```

---

## 2. Change Tracking Analysis (AsTracking vs AsNoTracking)

### Industry Best Practice

| Scenario | Tracking Mode | Rationale |
|----------|---------------|-----------|
| **Read-only queries** | `AsNoTracking()` | 10-30% faster, 50% less memory |
| **Entities to be modified** | Tracking (default) | Required for change detection |
| **Complex object graphs (read-only)** | `AsNoTrackingWithIdentityResolution()` | Identity consistency without tracking overhead |
| **Large datasets** | `AsNoTracking()` | Critical for performance |

**Microsoft Recommendation**:
> "If the entities retrieved from the database don't need to be updated, then a no-tracking query should be used."

### NOIR Implementation ✅ CORRECT

**1. Default to NoTracking for Reads** (Specification.cs:39)
```csharp
public bool AsNoTracking { get; internal set; } = true; // Default to no tracking for reads
```

**2. Explicit AsTracking for Mutations** (RefreshTokenByValueSpec.cs)
```csharp
public sealed class RefreshTokenByValueSpec : Specification<RefreshToken>
{
    public RefreshTokenByValueSpec(string token)
    {
        Query.Where(t => t.Token == token)
             .AsTracking()  // Required for entity modification (rotation, revocation)
             .TagWith("RefreshTokenByValue");
    }
}
```

**3. SpecificationEvaluator respects tracking settings** (SpecificationEvaluator.cs:178-194)
```csharp
private static IQueryable<T> ApplyTrackingBehavior<T>(IQueryable<T> query, ISpecification<T> specification)
{
    if (specification.AsNoTrackingWithIdentityResolution)
        query = query.AsNoTrackingWithIdentityResolution();
    else if (specification.AsNoTracking)
        query = query.AsNoTracking();
    // If neither is set, default EF tracking behavior is used
    return query;
}
```

**Strengths**:
1. **Safe default** - `AsNoTracking = true` by default prevents accidental tracking overhead
2. **Opt-in tracking** - `.AsTracking()` must be explicitly called for mutations
3. **Clear documentation** - Specs document why tracking is needed

---

## 3. Update Patterns Analysis

### Industry Best Practice

There are two primary update approaches:

#### Approach 1: Tracked Entity Modification (RECOMMENDED)
```csharp
// Load entity WITH tracking
var entity = await context.Entities.FirstAsync(e => e.Id == id);

// Modify properties directly
entity.Name = "Updated Name";

// SaveChanges detects and persists only changed properties
await context.SaveChangesAsync();
```

**Advantages**:
- EF Core detects only modified properties
- Generates optimal SQL (UPDATE only changed columns)
- Preserves shadow properties and concurrency tokens
- No need to call `.Update()` explicitly

#### Approach 2: Disconnected Entity Update (AVOID for tracked scenarios)
```csharp
// Create or receive entity without loading
var entity = new Entity { Id = 1, Name = "Updated" };

// Force all properties as modified
context.Update(entity);  // Marks ALL properties modified

await context.SaveChanges();  // Updates ALL columns
```

**When to use `.Update()`**:
- **Disconnected scenarios only** - Entity wasn't loaded from current DbContext
- **DTOs** - Converting a DTO to entity without loading original
- **Batch operations** - When loading would be too expensive

### NOIR Implementation ✅ CORRECT

**Pattern Used: Tracked Entity Modification**

```csharp
// RefreshTokenService.cs - Correct pattern
var spec = new RefreshTokenByValueSpec(currentToken);  // Uses .AsTracking()
var existingToken = await _repository.FirstOrDefaultAsync(spec, cancellationToken);

// Direct property modification on tracked entity
existingToken.Revoke(ipAddress, "Rotated", newToken.Token);

// SaveChanges detects changes automatically
await _unitOfWork.SaveChangesAsync(cancellationToken);
```

**Repository.Update() Implementation** (Repository.cs:183-186)
```csharp
public virtual void Update(TEntity entity)
{
    DbSet.Update(entity);  // For disconnected scenarios only
}
```

**Note**: The `Update()` method exists for disconnected scenarios but the primary pattern in NOIR is tracked modification.

---

## 4. Specification Pattern Integration

### Industry Best Practice (Ardalis.Specification)

The Specification pattern encapsulates query logic:

| Feature | Purpose |
|---------|---------|
| **Reusable queries** | Define once, use everywhere |
| **Testable** | Unit test specifications independently |
| **Composable** | Combine multiple criteria |
| **Tracking control** | Specify tracking per specification |
| **Query tags** | Debug and profile queries |

### NOIR Implementation ✅ CORRECT

**Custom Implementation Based on Ardalis Patterns**:

```csharp
// Specification.cs - Fluent API
public abstract class Specification<T> : ISpecification<T>
{
    public bool AsNoTracking { get; internal set; } = true;  // Default

    protected SpecificationBuilder<T> Query { get; }

    // Builder methods
    Query.Where(...)
         .Include(...)
         .OrderBy(...)
         .AsTracking()     // Override for mutations
         .AsSplitQuery()   // For multiple includes
         .TagWith("...")   // For debugging
}
```

**Strengths**:
1. **Query tags required** - `TagWith()` for SQL debugging (per CLAUDE.md)
2. **Tracking explicit** - Must call `.AsTracking()` for mutations
3. **Split query support** - `.AsSplitQuery()` for cartesian prevention
4. **Identity resolution** - `.AsNoTrackingWithIdentityResolution()` available

---

## 5. Comparison: NOIR vs Best Practices

| Aspect | Best Practice | NOIR Implementation | Status |
|--------|---------------|---------------------|--------|
| **UoW Pattern** | DbContext IS UoW | `ApplicationDbContext : IUnitOfWork` | ✅ |
| **Repository Save** | NO SaveChanges in repos | Repos don't save, UoW does | ✅ |
| **DI Lifetime** | Scoped | `AddScoped<IUnitOfWork>` | ✅ |
| **Default Tracking** | NoTracking for reads | `AsNoTracking = true` default | ✅ |
| **Mutation Tracking** | Explicit tracking | `.AsTracking()` in specs | ✅ |
| **Update Method** | Tracked modification | Direct property changes | ✅ |
| **Query Tags** | For debugging | `TagWith()` required | ✅ |
| **Transaction Support** | Explicit transactions | `BeginTransactionAsync` etc. | ✅ |

---

## 6. Recommendations

### Current Implementation: No Changes Needed

The NOIR implementation correctly follows EF Core best practices:

1. **Repository methods don't save** ✅
2. **IUnitOfWork.SaveChangesAsync() for persistence** ✅
3. **AsNoTracking default for reads** ✅
4. **AsTracking explicit for mutations** ✅
5. **Direct property modification on tracked entities** ✅

### Documentation Clarification (CLAUDE.md is correct)

The existing CLAUDE.md instructions are accurate:
```markdown
## Critical Rules
8. **Use IUnitOfWork for persistence** - Repository methods do NOT auto-save.
   Always inject `IUnitOfWork` and call `SaveChangesAsync()` after mutations.
9. **Use AsTracking for mutations** - Specifications default to `AsNoTracking`.
   For specs that retrieve entities for modification, add `.AsTracking()`.
```

### When to Use Each Pattern

| Scenario | Pattern | Example |
|----------|---------|---------|
| **Read-only list** | Spec (default) | `new ActiveCustomersSpec()` |
| **Read for display** | Spec (default) | `new CustomerDetailsSpec(id)` |
| **Modify entity** | Spec + `.AsTracking()` | `new RefreshTokenByValueSpec(token)` |
| **Batch update** | Load tracked + modify + save | `foreach (t in tokens) t.Revoke(); await _unitOfWork.SaveChangesAsync();` |
| **Bulk update (performance)** | `ExecuteUpdateAsync` | `await _repository.BulkSoftDeleteAsync(spec, ct)` |
| **Disconnected entity** | `.Update()` + save | Rare, for DTOs not loaded from DB |

---

## 7. Anti-Patterns to Avoid

### ❌ DON'T: Call Update() on tracked entities
```csharp
// WRONG - unnecessary, marks ALL properties modified
var entity = await _repository.FirstOrDefaultAsync(spec, ct);  // Already tracked
entity.Name = "New Name";
_repository.Update(entity);  // ❌ Unnecessary!
await _unitOfWork.SaveChangesAsync(ct);
```

### ✅ DO: Just modify and save
```csharp
// CORRECT - EF Core auto-detects changes
var entity = await _repository.FirstOrDefaultAsync(spec, ct);  // spec uses .AsTracking()
entity.Name = "New Name";
await _unitOfWork.SaveChangesAsync(ct);  // ✅ Detects and saves only changed properties
```

### ❌ DON'T: Forget AsTracking for mutations
```csharp
// WRONG - NoTracking is default, changes won't persist
public class GetEntitySpec : Specification<Entity>
{
    Query.Where(e => e.Id == id);  // ❌ Missing .AsTracking()
}
var entity = await _repo.FirstOrDefaultAsync(spec, ct);
entity.Name = "Updated";  // Change lost - entity not tracked!
await _unitOfWork.SaveChangesAsync(ct);  // Nothing happens
```

### ✅ DO: Add AsTracking for mutation specs
```csharp
// CORRECT - Explicitly enable tracking
public class GetEntityForUpdateSpec : Specification<Entity>
{
    Query.Where(e => e.Id == id)
         .AsTracking()  // ✅ Enable change tracking
         .TagWith("GetEntityForUpdate");
}
```

---

## 8. Sources

- [Microsoft: Change Tracking in EF Core](https://learn.microsoft.com/en-us/ef/core/change-tracking/)
- [Microsoft: Tracking vs No-Tracking Queries](https://learn.microsoft.com/en-us/ef/core/querying/tracking)
- [Ardalis.Specification Documentation](https://specification.ardalis.com/)
- [Anton Martyniuk: Unit of Work Pattern](https://antondevtips.com/blog/implementing-unit-of-work-pattern-in-ef-core)
- [DEV.to: No Need for Repositories with EF Core](https://dev.to/gpeipman/no-need-for-repositories-and-unit-of-work-with-entity-framework-core-48mh)
- [Stack Overflow: When to Use Update()](https://stackoverflow.com/questions/73714355/bettercleaner-way-to-update-record-using-entity-framework-core)

---

## Confidence Level: HIGH

The NOIR implementation follows established best practices from Microsoft documentation, Ardalis patterns, and community consensus. No architectural changes are recommended.
