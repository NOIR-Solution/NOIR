# NOIR Architecture Patterns

## Repository & Specification Pattern

### Repository Interfaces
- `IReadRepository<TEntity, TId>` - Read-only queries
- `IRepository<TEntity, TId>` - Full CRUD with soft delete

### Specification Pattern
All database queries MUST use specifications:
```csharp
public class ActiveCustomersSpec : Specification<Customer>
{
    public ActiveCustomersSpec(string? search = null)
    {
        Query.Where(c => c.IsActive)
             .TagWith("GetActiveCustomers");  // REQUIRED for SQL debugging
    }
}
```

**Critical**: Never use raw `DbSet` queries in services.

## CQRS with Wolverine

### Handler Convention
Handlers are static classes with `Handle` method:
```csharp
public static class CreateOrderHandler
{
    public static async Task<OrderResponse> Handle(
        CreateOrderCommand cmd,
        IRepository<Order, Guid> repo,
        CancellationToken ct)
    {
        // Implementation
    }
}
```

### Naming Conventions
| Type | Pattern | Example |
|------|---------|---------|
| Command | `[Action][Entity]Command` | `CreateOrderCommand` |
| Query | `Get[Entity][Filter]Query` | `GetActiveUsersQuery` |
| Handler | `[Command]Handler` | `CreateOrderHandler` |
| Specification | `[Entity][Filter]Spec` | `ActiveCustomersSpec` |

## DI Auto-Registration

### Marker Interfaces
Services are auto-registered via Scrutor using marker interfaces:
```csharp
public class CustomerService : ICustomerService, IScopedService { }
public class CacheService : ICacheService, ISingletonService { }
public class EmailSender : IEmailSender, ITransientService { }
```

### GlobalUsings
No `using` statements in files - add to `GlobalUsings.cs` in each project.

## Entity Configuration

### Base Classes
- `Entity<TId>` - Base entity with Id, CreatedAt, ModifiedAt
- `AggregateRoot<TId>` - Aggregate root (Entity + IAuditableEntity)

### IAuditableEntity
```csharp
public interface IAuditableEntity
{
    DateTimeOffset CreatedAt { get; }
    string? CreatedBy { get; }
    DateTimeOffset? ModifiedAt { get; }
    string? ModifiedBy { get; }
    bool IsDeleted { get; }
    DateTimeOffset? DeletedAt { get; }
    string? DeletedBy { get; }
}
```

### EF Configuration
Configurations are auto-discovered:
```csharp
public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        builder.HasKey(e => e.Id);
    }
}
```

## Soft Delete

- Default behavior: `Remove()` triggers soft delete
- Global query filters exclude soft-deleted entities
- Use `IgnoreQueryFilters()` in specs to include deleted
- Hard delete only for GDPR "right to be forgotten"
