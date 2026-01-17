# NOIR Coding Standards

## Critical Rules

### 1. Specifications Required
All database queries MUST use specifications:
```csharp
// ✅ Correct
var users = await repo.ListAsync(new ActiveUsersSpec());

// ❌ Wrong - Never do this
var users = await context.Users.Where(u => u.IsActive).ToListAsync();
```

### 2. TagWith Required
Every specification MUST include `TagWith()` for SQL debugging:
```csharp
public ActiveUsersSpec()
{
    Query.Where(u => u.IsActive)
         .TagWith("GetActiveUsers");  // REQUIRED
}
```

### 3. Soft Delete Only
Never hard delete unless explicitly required for GDPR:
```csharp
// ✅ Default - soft delete
repo.Remove(entity);

// ⚠️ Only for GDPR "right to be forgotten"
await repo.HardDeleteAsync(entity);
```

### 4. No Using Statements
Add namespaces to `GlobalUsings.cs` in each project:
```csharp
// In GlobalUsings.cs
global using NOIR.Domain.Entities;
global using NOIR.Domain.Common;
```

### 5. IUnitOfWork for Persistence
Repository methods do NOT auto-save. Always inject `IUnitOfWork` and call `SaveChangesAsync()`:
```csharp
public class CustomerService : ICustomerService, IScopedService
{
    private readonly IRepository<Customer, Guid> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public async Task UpdateAsync(Customer customer, CancellationToken ct)
    {
        customer.UpdateName("New Name");
        await _unitOfWork.SaveChangesAsync(ct);  // REQUIRED
    }
}
```

### 6. AsTracking for Mutations
Specifications default to `AsNoTracking`. For specs that retrieve entities for modification, add `.AsTracking()`:
```csharp
public class CustomerByIdForUpdateSpec : Specification<Customer>
{
    public CustomerByIdForUpdateSpec(Guid id)
    {
        Query.Where(c => c.Id == id)
             .AsTracking()  // REQUIRED for modification!
             .TagWith("CustomerByIdForUpdate");
    }
}
```

### 7. Marker Interfaces for DI
Services MUST implement marker interface:
```csharp
public class CustomerService : ICustomerService, IScopedService { }
public class CacheService : ICacheService, ISingletonService { }
public class EmailSender : IEmailSender, ITransientService { }
```

## Naming Conventions

| Type | Pattern | Example |
|------|---------|---------|
| Entity | PascalCase | `Customer` |
| Specification | `[Entity][Filter]Spec` | `ActiveCustomersSpec` |
| Command | `[Action][Entity]Command` | `CreateOrderCommand` |
| Query | `Get[Entity][Filter]Query` | `GetActiveUsersQuery` |
| Handler | `[Command]Handler` | `CreateOrderHandler` |
| Configuration | `[Entity]Configuration` | `CustomerConfiguration` |
| DTO | `[Entity][Purpose]Dto` | `UserProfileDto` |

## Performance Rules

| Scenario | Pattern |
|----------|---------|
| Read-only queries | `AsNoTracking` (default) |
| Multiple collections | `.AsSplitQuery()` |
| Bulk operations (1000+) | Bulk extension methods |
| Complex joins | Specification with includes |

## Entity Patterns

### Factory Methods
```csharp
public static Customer Create(string name, string email)
{
    return new Customer
    {
        Id = Guid.NewGuid(),
        Name = name,
        Email = email.ToLowerInvariant()
    };
}
```

### Private Setters
```csharp
public string Name { get; private set; } = default!;
```

### Private Constructor
```csharp
private Customer() { }  // For EF Core
```

## Console Logging Standards

### Backend
- Use `ILogger<T>` for all logging - never `Console.WriteLine`
- Log levels: Debug for verbose, Information for flow, Warning for recoverable issues, Error for failures

### Frontend
- **Do NOT use `console.error()` in production code**
- Errors are visible in browser Network tab - redundant logging clutters console
- Use toast notifications to inform users of failures
- Only exception: `console.warn()` for developer warnings (e.g., unsupported language)

**Pattern for error handling:**
```typescript
// CORRECT: Silent catch with user feedback
try {
  await someAction()
} catch {
  toast.error('Operation failed')
}

// WRONG: Redundant logging
try {
  await someAction()
} catch (error) {
  console.error('Failed:', error)  // ❌ Remove this
  toast.error('Operation failed')
}
```

## File Boundaries

### Read Freely
- `src/`
- `tests/`
- `docs/`
- `.claude/`

### Avoid Modifying
- `*.Designer.cs` (auto-generated)
- `Migrations/` (auto-generated)