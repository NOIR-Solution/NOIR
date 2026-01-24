# NOIR Technical Checklist (All Features)

**Created:** 2026-01-16
**Status:** Active Reference

Use this checklist when implementing new features or reviewing existing code.

---

## Backend - Command/Handler Structure

| # | Requirement | Details |
|---|-------------|---------|
| ☐ | **Co-locate Command + Handler + Validator** | Files go in `Application/Features/{Feature}/Commands/{Action}/` folder |
| ☐ | **Command naming** | `[Action][Entity]Command` (e.g., `CreateUserCommand`) |
| ☐ | **Handler naming** | `[Command]Handler` (e.g., `CreateUserCommandHandler`) |
| ☐ | **Validator naming** | `[Command]Validator` (e.g., `CreateUserCommandValidator`) |
| ☐ | **Use `sealed record`** for Commands | `public sealed record MyCommand(...) : IAuditableCommand` |
| ☐ | **Validator extends AbstractValidator** | `public sealed class MyCommandValidator : AbstractValidator<MyCommand>` |
| ☐ | **Use localization for validation messages** | `localization["validation.email.required"]` |

---

## Backend - Specifications (Database Queries)

| # | Requirement | Details |
|---|-------------|---------|
| ☐ | **Use Specifications for ALL queries** | Never raw `DbSet` queries in services |
| ☐ | **Specification naming** | `[Entity][Filter]Spec` (e.g., `ActiveCustomersSpec`) |
| ☐ | **Add `TagWith()` to EVERY spec** | `.TagWith("MethodName")` for SQL debugging |
| ☐ | **Add `.AsTracking()` for mutations** | Required when spec retrieves entities for modification |
| ☐ | **Add `.AsSplitQuery()` for multiple collections** | Performance optimization |
| ☐ | **Default is `AsNoTracking`** | Read-only queries don't need tracking |

**Example:**
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

---

## Backend - Persistence

| # | Requirement | Details |
|---|-------------|---------|
| ☐ | **Inject `IUnitOfWork`** | Repository methods do NOT auto-save |
| ☐ | **Call `SaveChangesAsync()` after mutations** | Always after Add/Update/Remove operations |
| ☐ | **Never inject `ApplicationDbContext` directly** | Use `IRepository<TEntity, TId>` instead |
| ☐ | **Soft delete only** | Use `Remove()` for soft delete (default) |
| ☐ | **Hard delete only for GDPR** | Only when explicitly required |

**Example:**
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

---

## Backend - Dependency Injection

| # | Requirement | Details |
|---|-------------|---------|
| ☐ | **Add marker interface to services** | `IScopedService`, `ITransientService`, or `ISingletonService` |
| ☐ | **No `using` statements in files** | Add to `GlobalUsings.cs` in each project |
| ☐ | **EF configurations auto-discovered** | Just implement `IEntityTypeConfiguration<T>` |

---

## Backend - Audit Logging (Activity Timeline)

Commands that create, update, or delete data via frontend MUST implement `IAuditableCommand`.

| # | Requirement | Details |
|---|-------------|---------|
| ☐ | **Command implements `IAuditableCommand<TResult>`** | Or `IAuditableCommand` for non-typed |
| ☐ | **Add `UserId` property with `[JsonIgnore]`** | See example below |
| ☐ | **Implement `GetTargetId()`** | Returns the entity/user identifier |
| ☐ | **Implement `OperationType`** | `Create`, `Update`, or `Delete` |
| ☐ | **Implement `GetTargetDisplayName()`** | Human-readable name (email, name, etc.) |
| ☐ | **Implement `GetActionDescription()`** | e.g., `"Created user '{DisplayName}'"` |
| ☐ | **Endpoint sets `UserId`** | `command with { UserId = currentUser.UserId }` |
| ☐ | **Frontend page calls `usePageContext()`** | e.g., `usePageContext('Users')` |

**Command Example:**
```csharp
public sealed record CreateUserCommand(
    string Email,
    string Password,
    string? DisplayName) : IAuditableCommand
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Create;
    public object? GetTargetId() => Email;
    public string? GetTargetDisplayName() => DisplayName ?? Email;
    public string? GetActionDescription() => $"Created user '{GetTargetDisplayName()}'";
}
```

**Endpoint Example:**
```csharp
group.MapPost("/users", async (
    CreateUserCommand command,
    [FromServices] ICurrentUser currentUser,
    IMessageBus bus) =>
{
    var auditableCommand = command with { UserId = currentUser.UserId };
    var result = await bus.InvokeAsync<Result<UserDto>>(auditableCommand);
    return result.ToHttpResult();
});
```

**Frontend Example:**
```tsx
import { usePageContext } from '@/hooks/usePageContext'

export default function UsersPage() {
  usePageContext('Users')  // Required for Activity Timeline
  // ...
}
```

---

## Backend - JSON/Enums

| # | Requirement | Details |
|---|-------------|---------|
| ☐ | **Enums serialize as strings** | Configured globally (not integers) |
| ☐ | **Works in HTTP JSON, SignalR, Source Generator** | No per-property configuration needed |

---

## Backend - Entity Patterns

| # | Requirement | Details |
|---|-------------|---------|
| ☐ | **Use factory methods** | `public static Customer Create(...)` |
| ☐ | **Private setters** | `public string Name { get; private set; }` |
| ☐ | **Private constructor for EF** | `private Customer() { }` |
| ☐ | **Inherit from `Entity<TId>` or `AggregateRoot<TId>`** | Base classes handle audit fields |

---

## Frontend - React/TypeScript

| # | Requirement | Details |
|---|-------------|---------|
| ☐ | **Use 21st.dev for UI components** | MCP tool: `mcp__magic__21st_magic_component_builder` |
| ☐ | **Interactive elements have `cursor-pointer`** | Tabs, checkboxes, dropdowns, switches |
| ☐ | **Multi-select dropdowns stay open** | Add `onSelect={(e) => e.preventDefault()}` |
| ☐ | **Use react-hook-form + Zod + FormField** | Not manual error state management |
| ☐ | **Validation mode `onBlur`** | Real-time validation on blur |
| ☐ | **Zod uses `.issues` not `.errors`** | `result.error.issues.forEach(...)` |
| ☐ | **Dialog forms: no overflow containers** | Focus rings get clipped otherwise |
| ☐ | **Use `noValidate` on forms** | Disable browser validation tooltips |
| ☐ | **Use `type="text"` for email inputs** | Avoid browser popup validation |

**Form Example:**
```tsx
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Form, FormField, FormItem, FormLabel, FormControl, FormMessage } from '@/components/ui/form'

const form = useForm<FormData>({
  resolver: zodResolver(schema),
  mode: 'onBlur',
})

// In JSX:
<Form {...form}>
  <FormField
    control={form.control}
    name="email"
    render={({ field }) => (
      <FormItem>
        <FormLabel>Email</FormLabel>
        <FormControl>
          <Input {...field} />
        </FormControl>
        <FormMessage />
      </FormItem>
    )}
  />
</Form>
```

---

## Frontend - Validation Consistency

| # | Requirement | Details |
|---|-------------|---------|
| ☐ | **Backend: FluentValidation** | For all Commands/Queries |
| ☐ | **Frontend: Zod schemas** | Match backend validation rules |
| ☐ | **Validation rules MUST match** | Same max length, patterns, etc. |

---

## Build & Verification

| # | Requirement | Details |
|---|-------------|---------|
| ☐ | **Run `dotnet build src/NOIR.sln`** | After ALL code changes |
| ☐ | **Run `dotnet test src/NOIR.sln`** | Before committing (5,370+ tests) |
| ☐ | **Add tests for new handlers** | Unit tests with xUnit + Moq |
| ☐ | **Add tests for new specifications** | Test filter logic |

---

## Files to NEVER Modify

| Files | Reason |
|-------|--------|
| `*.Designer.cs` | Auto-generated |
| `Migrations/` | Auto-generated by EF |

---

## Quick Reference: File Locations

```
src/NOIR.Domain/              # Entities, IRepository, ISpecification
src/NOIR.Application/
    └── Features/{Feature}/
        ├── Commands/{Action}/
        │   ├── {Action}Command.cs
        │   ├── {Action}CommandHandler.cs
        │   └── {Action}CommandValidator.cs
        └── Queries/{Action}/
    └── Specifications/       # All specification classes
    └── Common/Interfaces/    # IAuditableCommand, etc.
src/NOIR.Infrastructure/      # EF Core, Repositories, Services
src/NOIR.Web/
    └── Endpoints/            # Minimal API endpoints
    └── frontend/             # React SPA
tests/                        # All test projects
```

---

## Related Documentation

- [Hierarchical Audit Logging](./hierarchical-audit-logging.md)
- [Repository & Specification Pattern](./repository-specification.md)
- [DI Auto-Registration](./di-auto-registration.md)
- [Entity Configuration](./entity-configuration.md)
- [JSON Enum Serialization](./json-enum-serialization.md)
