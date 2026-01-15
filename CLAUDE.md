# NOIR - Claude Code Instructions

> Specific instructions for Claude Code. For universal AI agent instructions, see [AGENTS.md](AGENTS.md).

---

## SuperClaude Auto-Routing (MUST FOLLOW)

**When the user's intent matches these patterns, AUTOMATICALLY invoke the corresponding skill using the Skill tool BEFORE responding or asking clarifying questions:**

| User Intent Pattern | Auto-Invoke | Description |
|---------------------|-------------|-------------|
| "fix bug", "debug", "error", "not working", "broken", "fails", "exception" | `/sc:troubleshoot` | Systematic diagnosis and root cause analysis |
| "add feature", "implement", "create new", "build", "develop" | `/sc:brainstorm` | Requirements discovery before implementation |
| "plan", "design", "architect", "how should we", "structure", "approach" | `/sc:design` | System architecture and design planning |
| "investigate", "research", "look into", "find out", "what is", "best practice" | `/sc:research` | Autonomous web research with depth control |
| "improve", "enhance", "better", "quality", "optimize performance" | `/sc:improve` | Code quality and performance enhancement |
| "refactor", "clean up", "simplify", "reorganize" | `/sc:cleanup` | Refactoring and code cleanup |
| "estimate", "how long", "complexity", "effort", "time" | `/sc:estimate` | Resource and effort forecasting |
| "test", "write tests", "coverage", "unit test", "integration test" | `/sc:test` | Test suite generation and execution |
| "explain", "what does this do", "understand", "how does", "walk me through" | `/sc:explain` | Code comprehension assistance |
| "document", "add docs", "README", "comments", "docstring" | `/sc:document` | Documentation generation |
| "review requirements", "spec", "requirements", "acceptance criteria" | `/sc:spec-panel` | Multi-expert requirements analysis |
| "analyze", "inspect", "metrics", "code review", "audit" | `/sc:analyze` | Code inspection and quality metrics |
| "commit", "git commit", "save changes" | `/sc:git` | Git operations with smart commit messages |
| "workflow", "process", "steps to", "guide me through" | `/sc:workflow` | Structured workflow guidance |

**Priority Order for Ambiguous Requests:**
1. **Process skills first** (`/sc:brainstorm`, `/sc:troubleshoot`, `/sc:research`) - determines HOW to approach
2. **Implementation skills second** (`/sc:implement`, `/sc:design`, `/sc:cleanup`) - guides execution

**Chained Workflows (invoke in sequence):**
- **Feature Development:** `/sc:brainstorm` → `/sc:design` → `/sc:implement` → `/sc:test`
- **Bug Fixing:** `/sc:troubleshoot` → `/sc:test` → `/sc:document`
- **Investigation:** `/sc:research` → `/sc:analyze` → `/sc:document`

**Example Auto-Triggers:**
- User: "The login is broken" → Auto-invoke `/sc:troubleshoot`
- User: "Add multi-tenant support" → Auto-invoke `/sc:brainstorm`
- User: "How should we structure the notification system?" → Auto-invoke `/sc:design`
- User: "What's the best practice for vertical slice architecture?" → Auto-invoke `/sc:research`
- User: "Clean up the repository layer" → Auto-invoke `/sc:cleanup`
- User: "How long would it take to add SSO?" → Auto-invoke `/sc:estimate`

**Quick Command Reference:**
| Category | Commands |
|----------|----------|
| Planning | `/sc:brainstorm`, `/sc:design`, `/sc:estimate`, `/sc:spec-panel`, `/sc:workflow` |
| Development | `/sc:implement`, `/sc:build`, `/sc:improve`, `/sc:cleanup` |
| Testing | `/sc:test`, `/sc:analyze`, `/sc:troubleshoot` |
| Documentation | `/sc:document`, `/sc:explain`, `/sc:index` |
| Git/Project | `/sc:git`, `/sc:pm`, `/sc:task` |
| Research | `/sc:research`, `/sc:business-panel` |
| Utilities | `/sc:recommend`, `/sc:help`, `/sc:load`, `/sc:save` |

---

## Critical Rules

1. **Check existing patterns first** - Look at similar files before writing new code
2. **Use Specifications** for all database queries - Never raw `DbSet` queries in services
3. **Tag all specifications** with `TagWith("MethodName")` for SQL debugging
4. **Run `dotnet build src/NOIR.sln`** after code changes
5. **Soft delete only** - Never hard delete unless explicitly requested for GDPR
6. **No using statements in files** - Add to `GlobalUsings.cs` in each project
7. **Use marker interfaces** for DI - Add `IScopedService`, `ITransientService`, or `ISingletonService`
8. **Use IUnitOfWork for persistence** - Repository methods do NOT auto-save. Always inject `IUnitOfWork` and call `SaveChangesAsync()` after mutations. Never inject `ApplicationDbContext` directly into services.
9. **Use AsTracking for mutations** - Specifications default to `AsNoTracking`. For specs that retrieve entities for modification, add `.AsTracking()` to enable change detection.
10. **Co-locate Command + Handler + Validator** - All CQRS components live in the same folder under `Application/Features/{Feature}/Commands/{Action}/` or `Application/Features/{Feature}/Queries/{Action}/`
11. **Audit logging for user actions** - Commands that create, update, or delete data via frontend MUST implement `IAuditableCommand`. See `docs/backend/patterns/hierarchical-audit-logging.md` for the checklist and pattern. Requires: (a) Command implements `IAuditableCommand<TResult>`, (b) Endpoint sets `UserId` on command, (c) Frontend page calls `usePageContext('PageName')`.

## Quick Reference

```bash
# Build & Run
dotnet build src/NOIR.sln
dotnet run --project src/NOIR.Web
dotnet watch --project src/NOIR.Web

# Tests (1,800+ tests)
dotnet test src/NOIR.sln

# Migrations
dotnet ef migrations add NAME --project src/NOIR.Infrastructure --startup-project src/NOIR.Web
```

**Admin Login:** `admin@noir.local` / `123qwe`

## Project Structure

```
src/NOIR.Domain/          # Entities, IRepository, ISpecification
src/NOIR.Application/     # Features (Command + Handler + Validator co-located), DTOs
    └── Features/
        └── {Feature}/
            ├── Commands/{Action}/
            │   ├── {Action}Command.cs
            │   ├── {Action}CommandHandler.cs
            │   └── {Action}CommandValidator.cs
            └── Queries/{Action}/
                ├── {Action}Query.cs
                └── {Action}QueryHandler.cs
    └── Common/Interfaces/  # Service abstractions (IUserIdentityService, etc.)
src/NOIR.Infrastructure/  # EF Core, Repositories, Service implementations
src/NOIR.Web/             # Endpoints, Middleware, Program.cs
    └── frontend/         # React SPA
```

## Code Patterns

### Service Registration
```csharp
// Just add marker interface - auto-registered!
public class CustomerService : ICustomerService, IScopedService { }
```

### Specifications (Required for queries)
```csharp
public class ActiveCustomersSpec : Specification<Customer>
{
    public ActiveCustomersSpec(string? search = null)
    {
        Query.Where(c => c.IsActive)
             .TagWith("GetActiveCustomers");  // REQUIRED
    }
}
```

### Handlers (Wolverine - Vertical Slice)
```csharp
// Handler co-located with Command in Application/Features/{Feature}/Commands/{Action}/
public class CreateOrderCommandHandler
{
    private readonly IRepository<Order, Guid> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateOrderCommandHandler(
        IRepository<Order, Guid> repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<OrderDto>> Handle(
        CreateOrderCommand cmd,
        CancellationToken ct)
    {
        // Validation and business logic
        var order = Order.Create(cmd.CustomerId, cmd.Items);
        await _repository.AddAsync(order, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result.Success(order.ToDto());
    }
}
```

### Entity Configuration
```csharp
// Auto-discovered via ApplyConfigurationsFromAssembly
public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        builder.HasKey(e => e.Id);
    }
}
```

### Unit of Work Pattern (CRITICAL)
```csharp
// Repository methods do NOT auto-save! Always use IUnitOfWork.
// For tracked entities (from spec with AsTracking), just modify and save.
// For new entities, call AddAsync then save.
public class CustomerService : ICustomerService, IScopedService
{
    private readonly IRepository<Customer, Guid> _repository;
    private readonly IUnitOfWork _unitOfWork; // REQUIRED for mutations

    public async Task UpdateCustomerAsync(Customer customer, CancellationToken ct)
    {
        // Entity already tracked from spec query - just modify and save
        customer.UpdateName("New Name");
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
```

### Specification Tracking (CRITICAL)
```csharp
// Specifications default to AsNoTracking (read-only).
// For entities that WILL BE MODIFIED, use .AsTracking()!
public class CustomerByIdSpec : Specification<Customer>
{
    public CustomerByIdSpec(Guid id)
    {
        Query.Where(c => c.Id == id)
             .AsTracking()  // REQUIRED for modification!
             .TagWith("CustomerById");
    }
}
```

### Auditable Commands (CRITICAL for Activity Timeline)
```csharp
// Commands that mutate data via frontend MUST implement IAuditableCommand
public sealed record UpdateCustomerCommand(
    Guid Id,
    string Name) : IAuditableCommand<CustomerDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }  // Set by endpoint

    public object? GetTargetId() => UserId;
    public AuditOperationType OperationType => AuditOperationType.Update;
    public string? GetTargetDisplayName() => Name;
    public string? GetActionDescription() => $"Updated customer '{Name}'";
}

// Endpoint must set UserId:
group.MapPut("/customers/{id}", async (
    Guid id,
    UpdateCustomerCommand command,
    [FromServices] ICurrentUser currentUser,
    IMessageBus bus) =>
{
    var auditableCommand = command with { UserId = currentUser.UserId };
    var result = await bus.InvokeAsync<Result<CustomerDto>>(auditableCommand);
    return result.ToHttpResult();
});

// Frontend page must set context:
usePageContext('Customers')  // Required for Activity Timeline
```

## Naming Conventions

| Type | Pattern | Example |
|------|---------|---------|
| Specification | `[Entity][Filter]Spec` | `ActiveCustomersSpec` |
| Command | `[Action][Entity]Command` | `CreateOrderCommand` |
| Handler | `[Command]Handler` | `CreateOrderHandler` |
| Configuration | `[Entity]Configuration` | `CustomerConfiguration` |

## Performance Rules

| Scenario | Use |
|----------|-----|
| Read-only queries | `AsNoTracking` (default) |
| Multiple collections | `.AsSplitQuery()` |

## Frontend Rules (React/TypeScript)

### 21st.dev Component Standard (MANDATORY)
**All frontend UI components and pages MUST use 21st.dev for consistency and best UI/UX.**

```typescript
// When building new UI components, use 21st.dev MCP tool:
// Claude Code: Use the mcp__magic__21st_magic_component_builder tool
// This ensures consistent, production-quality UI with:
// - Modern design patterns (glassmorphism, animations, micro-interactions)
// - Accessible components (WCAG compliant)
// - Responsive layouts (mobile-first)
// - Consistent spacing, typography, and color schemes
```

**Do NOT:**
- Hand-build pagination, page headers, empty states, or other common UI patterns
- Create custom form validation state management (use react-hook-form + FormField)
- Write inline gradient/focus styling (extract to design tokens)

**Components requiring 21st.dev rebuild (existing debt):**
- Pagination in TenantsPage, RolesPage
- Page headers across admin pages
- CreateUserDialog form validation
- Empty state components in tables

### Validation Consistency (CRITICAL)

**Backend:** FluentValidation for all Commands/Queries
**Frontend:** Real-time validation with smooth UI/UX

```typescript
// CORRECT: Use react-hook-form + Zod + FormField pattern (like CreateRoleDialog)
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Form, FormField, FormItem, FormLabel, FormControl, FormMessage } from '@/components/ui/form'

const form = useForm<FormData>({
  resolver: zodResolver(schema),
  mode: 'onBlur', // Real-time validation on blur
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
        <FormMessage /> {/* Auto-displays validation errors */}
      </FormItem>
    )}
  />
</Form>

// WRONG: Manual error state management (like CreateUserDialog)
const [errors, setErrors] = useState({})  // ❌ Don't do this
const [touched, setTouched] = useState({}) // ❌ Don't do this
```

**Validation rules must match between FluentValidation and Zod schemas.**

### Zod Validation
```typescript
// CORRECT: Zod uses `.issues` not `.errors`
const result = schema.safeParse(data)
if (!result.success) {
  result.error.issues.forEach((issue) => {  // ✅ .issues
    console.log(issue.message)
  })
}

// WRONG: This will throw "Cannot read properties of undefined"
result.error.errors.forEach(...)  // ❌ .errors does not exist
```

### Real-Time Form Validation
```typescript
// Use onBlur + touched state for inline validation (not browser tooltips)
const [errors, setErrors] = useState<Record<string, string>>({})
const [touched, setTouched] = useState<Record<string, boolean>>({})

const handleBlur = (field: string, value: string) => {
  setTouched(prev => ({ ...prev, [field]: true }))
  const error = validateField(field, value)
  setErrors(prev => ({ ...prev, [field]: error }))
}

// In JSX:
<form noValidate>  {/* Disable browser validation */}
  <Input
    type="text"  {/* Use text, not email - avoids browser popup */}
    onBlur={(e) => handleBlur('email', e.target.value)}
    className={touched.email && errors.email ? 'border-destructive' : ''}
  />
  {touched.email && errors.email && (
    <p className="text-sm text-destructive">{errors.email}</p>
  )}
</form>
```

### Dialog Form Layout (Focus Ring Clipping)
```typescript
// CORRECT: Simple DialogContent without scroll containers (like CreateRoleDialog)
<DialogContent className="sm:max-w-[500px]">
  <DialogHeader>...</DialogHeader>
  <form className="space-y-4">
    <div className="grid gap-4">
      {/* Form fields - focus rings won't be clipped */}
    </div>
    <DialogFooter>...</DialogFooter>
  </form>
</DialogContent>

// WRONG: These all clip focus rings
<DialogContent className="... overflow-hidden">     {/* ❌ overflow-hidden */}
<DialogContent className="... max-h-[90vh] flex flex-col">  {/* ❌ flex container */}
<ScrollArea>...</ScrollArea>                         {/* ❌ ScrollArea has overflow-hidden */}
<div className="overflow-y-auto">...</div>           {/* ❌ Any overflow container */}
```

**Key insight:** Never wrap form inputs in any overflow container. Let the dialog grow naturally - the browser handles tall dialogs.

### Multi-Select Role Pattern
```typescript
// Use Set<string> for role selection
const [selectedRoles, setSelectedRoles] = useState<Set<string>>(new Set())

const handleToggleRole = (roleName: string) => {
  setSelectedRoles(prev => {
    const next = new Set(prev)
    if (next.has(roleName)) {
      next.delete(roleName)
    } else {
      next.add(roleName)
    }
    return next
  })
}

// Convert to array for API
roleNames: selectedRoles.size > 0 ? Array.from(selectedRoles) : null
```

## Task Management

This project uses **Vibe Kanban** for task tracking and sprint management. Check the kanban board for current tasks, priorities, and sprint goals before starting work.

## Documentation

For detailed documentation, see the `docs/` folder:

| Topic | Location |
|-------|----------|
| Backend patterns | `docs/backend/patterns/` |
| Backend research | `docs/backend/research/` |
| Frontend guide | `docs/frontend/` |
| Architecture decisions | `docs/decisions/` |
| Knowledge base | `docs/KNOWLEDGE_BASE.md` |

**When creating documentation:**
- Research reports go to `docs/backend/research/` or `docs/frontend/research/`
- Do NOT use `claudedocs/` - that folder is deprecated

## File Boundaries

**Read freely:** `src/`, `tests/`, `docs/`, `.claude/`

**Avoid modifying:** `*.Designer.cs`, `Migrations/` (auto-generated)
