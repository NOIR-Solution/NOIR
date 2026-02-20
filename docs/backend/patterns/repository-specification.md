# Repository & Specification Pattern Best Practices

**Created:** 2025-12-30
**Based on:** Ardalis.Specification, eShopOnWeb, Microsoft EF Core Guidelines

---

## Overview

This project implements the **Repository Pattern** with **Specification Pattern** for data access, providing:
- Encapsulated query logic
- Reusable, composable queries
- Performance optimizations
- Testability without exposing `IQueryable`

---

## Architecture

```
Domain Layer (Interfaces)
├── ISpecification<T>           # Query specification contract
├── IRepository<T, TId>         # Full CRUD repository
├── IReadRepository<T, TId>     # Read-only repository
└── ISoftDeleteRepository<T>    # Soft delete extensions

Application Layer (Implementation)
├── Specification<T>            # Base specification with fluent API
├── Specification<T, TResult>   # Projection specification
└── And/Or/Not Specifications   # Combining operators

Infrastructure Layer
├── SpecificationEvaluator      # Converts spec to IQueryable
└── Repository<T, TId>          # Abstract base repository implementation
```

---

## Creating Specifications

### Basic Specification

```csharp
public class ActiveCustomersSpec : Specification<Customer>
{
    public ActiveCustomersSpec()
    {
        Query.Where(c => c.IsActive)
             .OrderBy(c => c.Name);
    }
}
```

### Parameterized Specification

```csharp
public class CustomersByNameSpec : Specification<Customer>
{
    public CustomersByNameSpec(string nameFilter, bool includeOrders = false)
    {
        Query.Where(c => c.Name.Contains(nameFilter))
             .Where(c => c.IsActive)  // Multiple WHERE = AND
             .OrderBy(c => c.Name)
             .TagWith($"CustomersByName:{nameFilter}");  // For debugging

        if (includeOrders)
        {
            Query.Include(c => c.Orders)
                 .AsSplitQuery();  // Prevent cartesian explosion
        }
    }
}
```

### Projection Specification (DTOs)

```csharp
public class CustomerSummarySpec : Specification<Customer, CustomerSummaryDto>
{
    public CustomerSummarySpec()
    {
        Query.Where(c => c.IsActive)
             .Select(c => new CustomerSummaryDto
             {
                 Id = c.Id,
                 Name = c.Name,
                 OrderCount = c.Orders.Count
             });
    }
}
```

### Paginated Specification

```csharp
public class PagedCustomersSpec : Specification<Customer>
{
    public PagedCustomersSpec(int pageIndex, int pageSize)
    {
        Query.Where(c => c.IsActive)
             .OrderBy(c => c.CreatedAt)
             .Paginate(pageIndex, pageSize);
    }
}
```

---

## Fluent API Reference

### Filtering
| Method | Description |
|--------|-------------|
| `.Where(predicate)` | Add filter criteria (multiple = AND) |

### Includes (Eager Loading)
| Method | Description |
|--------|-------------|
| `.Include(x => x.Nav)` | Expression-based include |
| `.Include("Orders.Items")` | String-based nested include |

### Ordering
| Method | Description |
|--------|-------------|
| `.OrderBy(x => x.Prop)` | Primary ascending |
| `.OrderByDescending(x => x.Prop)` | Primary descending |
| `.ThenBy(x => x.Prop)` | Secondary ascending |
| `.ThenByDescending(x => x.Prop)` | Secondary descending |

### Paging
| Method | Description |
|--------|-------------|
| `.Paginate(pageIndex, pageSize)` | Apply pagination |
| `.Skip(count)` | Skip N items |
| `.Take(count)` | Take N items |

### Performance
| Method | Description |
|--------|-------------|
| `.AsTracking()` | Enable change tracking |
| `.AsNoTrackingWithIdentityResolution()` | No tracking + identity |
| `.AsSplitQuery()` | Prevent cartesian explosion |
| `.IgnoreQueryFilters()` | Bypass global filters |
| `.IgnoreAutoIncludes()` | Skip auto-includes |

### Debugging
| Method | Description |
|--------|-------------|
| `.TagWith("tag")` | Add SQL comment for tracing |

---

## Performance Best Practices

### 1. Tracking Behavior (Fastest → Slowest)

```csharp
// 1. FASTEST: No tracking, no identity resolution
Query.Where(...)  // AsNoTracking is default

// 2. FAST: No tracking, maintains identity
Query.AsNoTrackingWithIdentityResolution();

// 3. SLOW: Full change tracking
Query.AsTracking();
```

**Use `AsNoTrackingWithIdentityResolution` when:**
- Loading related entities that may appear multiple times
- Need identity consistency without modification

### 2. Prevent Cartesian Explosion

When loading **multiple collections at the same level**, use `AsSplitQuery()`:

```csharp
// BAD: Cartesian explosion (100 orders × 100 items = 10,000 rows)
Query.Include(o => o.Orders)
     .Include(o => o.Items);

// GOOD: Split into multiple queries
Query.Include(o => o.Orders)
     .Include(o => o.Items)
     .AsSplitQuery();  // 99% row reduction possible
```

**Trade-off:** Multiple roundtrips vs. data explosion.

### 3. Count/Any Optimization

`GetQueryForCount()` automatically skips ordering, includes, and paging:

```csharp
// Efficient - only applies WHERE criteria
var count = await repo.CountAsync(specification);
var exists = await repo.AnyAsync(specification);
```

### 4. Query Tagging for Debugging

```csharp
Query.Where(...)
     .TagWith("GetActiveCustomers")
     .TagWith($"RequestId:{requestId}");

// Generated SQL includes:
-- GetActiveCustomers
-- RequestId:abc123
SELECT * FROM Customers WHERE ...
```

---

## Combining Specifications

```csharp
var activeSpec = new ActiveCustomersSpec();
var premiumSpec = new PremiumCustomersSpec();

// AND combination
var combined = activeSpec.And(premiumSpec);

// OR combination
var either = activeSpec.Or(premiumSpec);

// NOT
var inactive = activeSpec.Not();
```

---

## In-Memory Evaluation

```csharp
var spec = new ActiveCustomersSpec();

// Validate single entity
if (spec.IsSatisfiedBy(customer))
{
    // Customer matches criteria
}

// Filter collection
var filtered = spec.Evaluate(customers);
```

---

## Repository Usage

```csharp
public class CustomerService
{
    private readonly IRepository<Customer, Guid> _repository;

    // Specification-based queries
    public async Task<IReadOnlyList<Customer>> GetActiveAsync()
    {
        var spec = new ActiveCustomersSpec();
        return await _repository.ListAsync(spec);
    }

    // Projection
    public async Task<IReadOnlyList<CustomerSummaryDto>> GetSummariesAsync()
    {
        var spec = new CustomerSummarySpec();
        return await _repository.ListAsync(spec);
    }

    // Predicate-based (simple cases)
    public async Task<Customer?> GetByEmailAsync(string email)
    {
        return await _repository.FirstOrDefaultAsync(c => c.Email == email);
    }
}
```

---

## Anti-Patterns to Avoid

### ❌ Exposing IQueryable

```csharp
// BAD: Leaks EF Core to upper layers
public IQueryable<Customer> GetCustomers() => _dbSet;
```

### ❌ Generic GetAll + Filter

```csharp
// BAD: Loads all data, filters in memory
var all = await repo.GetAllAsync();
var active = all.Where(c => c.IsActive);
```

### ❌ Missing Specification for Complex Queries

```csharp
// BAD: Duplicated query logic
var result1 = await _dbSet.Where(c => c.IsActive && c.IsPremium)...
var result2 = await _dbSet.Where(c => c.IsActive && c.IsPremium)...
```

### ✅ Use Specification Instead

```csharp
// GOOD: Reusable, testable, discoverable
var spec = new ActivePremiumCustomersSpec();
var result = await _repository.ListAsync(spec);
```

---

## File Locations

```
src/
├── NOIR.Domain/
│   └── Specifications/
│       └── ISpecification.cs          # Interface
├── NOIR.Application/
│   └── Specifications/
│       ├── Specification.cs           # Base class
│       └── [Entity]Specifications/    # Per-entity specs
└── NOIR.Infrastructure/
    └── Persistence/
        ├── SpecificationEvaluator.cs  # Query builder
        └── Repositories/
            └── Repository.cs          # Abstract base implementation
```

---

## References

- [Ardalis.Specification](https://github.com/ardalis/Specification)
- [eShopOnWeb](https://github.com/NimblePros/eShopOnWeb)
- [EF Core Performance](https://learn.microsoft.com/en-us/ef/core/performance/)
- [Split Queries](https://learn.microsoft.com/en-us/ef/core/querying/single-split-queries)
- [Query Tags](https://learn.microsoft.com/en-us/ef/core/querying/tags)
