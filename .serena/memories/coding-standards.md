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

### 5. Marker Interfaces for DI
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

## File Boundaries

### Read Freely
- `src/`
- `tests/`
- `docs/`
- `.claude/`

### Avoid Modifying
- `*.Designer.cs` (auto-generated)
- `Migrations/` (auto-generated)
