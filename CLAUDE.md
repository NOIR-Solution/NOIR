# NOIR - Claude Code Instructions

> Specific instructions for Claude Code. For universal AI agent instructions, see [AGENTS.md](AGENTS.md).

---

## SuperClaude Framework - Quick Guide

**Don't remember commands? Just say what you want in natural language!**

| You Say... | Claude Uses |
|------------|-------------|
| "fix this bug" / "why isn't this working" | `/sc:troubleshoot` |
| "add a new feature" / "implement X" | `/sc:brainstorm` → `/sc:implement` |
| "how should we design this" | `/sc:design` |
| "clean up this code" / "refactor" | `/sc:cleanup` |
| "write tests for this" | `/sc:test` |
| "explain this code" | `/sc:explain` |
| "commit my changes" | `/sc:git` |
| "how long will this take" | `/sc:estimate` |
| "research best practices for X" | `/sc:research` |

**Helpful Commands:**
```
/sc:help      - Show all available commands
/sc:recommend - Get command suggestions for your task
```

**Pro Tips:**
1. **Natural language works** - Just describe what you need, Claude auto-routes to the right command
2. **Use `/sc:help`** - When you forget, this shows everything
3. **Use `/sc:recommend`** - Describes your task, gets the right command suggested

---

## SuperClaude Auto-Routing (MUST FOLLOW)

**CRITICAL: When user's message matches ANY pattern below, IMMEDIATELY invoke the skill using the Skill tool BEFORE responding. Do NOT ask clarifying questions first - let the skill handle that.**

### Routing Rules by Priority (Check in Order)

#### 🔴 PRIORITY 1: Problem/Error Detection → `/sc:troubleshoot`
**Trigger immediately when user mentions ANY of these:**
- Error words: `error`, `exception`, `fail`, `crash`, `bug`, `issue`, `problem`
- Broken state: `not working`, `doesn't work`, `broken`, `stuck`, `hang`, `freeze`
- Unexpected behavior: `wrong`, `incorrect`, `unexpected`, `weird`, `strange`
- Questions about failures: `why is`, `why does`, `why isn't`, `what's wrong`

**Examples:**
- "The API returns 500 error" → `/sc:troubleshoot`
- "Login doesn't work anymore" → `/sc:troubleshoot`
- "Why is this test failing?" → `/sc:troubleshoot`
- "Something's wrong with the database" → `/sc:troubleshoot`

#### 🟠 PRIORITY 2: New Feature/Implementation → `/sc:brainstorm`
**Trigger when user wants to ADD something new:**
- Creation words: `add`, `create`, `new`, `implement`, `build`, `develop`, `make`
- Feature requests: `feature`, `functionality`, `capability`, `support for`
- Integration: `integrate`, `connect`, `hook up`, `add support`

**Examples:**
- "Add user notifications" → `/sc:brainstorm`
- "I want to implement SSO" → `/sc:brainstorm`
- "Create a new dashboard" → `/sc:brainstorm`
- "Build export functionality" → `/sc:brainstorm`

#### 🟡 PRIORITY 3: Architecture/Design → `/sc:design`
**Trigger when user asks HOW to structure something:**
- Design words: `design`, `architect`, `structure`, `organize`, `pattern`
- Planning: `how should`, `what's the best way`, `approach`, `strategy`
- System design: `system`, `architecture`, `layer`, `module`, `component`

**Examples:**
- "How should we structure the API?" → `/sc:design`
- "What's the best pattern for this?" → `/sc:design`
- "Design the notification system" → `/sc:design`

#### 🟢 PRIORITY 4: Code Quality → `/sc:cleanup` or `/sc:improve`
**Use `/sc:cleanup` for:**
- Refactoring: `refactor`, `clean up`, `simplify`, `reorganize`, `restructure`
- Code smell: `messy`, `ugly`, `duplicate`, `DRY`, `dead code`

**Use `/sc:improve` for:**
- Optimization: `optimize`, `faster`, `performance`, `efficient`, `speed up`
- Quality: `improve`, `enhance`, `better`, `quality`, `maintainable`

**Examples:**
- "Clean up this service class" → `/sc:cleanup`
- "Refactor the repository layer" → `/sc:cleanup`
- "Make this query faster" → `/sc:improve`
- "Improve the code quality" → `/sc:improve`

#### 🔵 PRIORITY 5: Testing → `/sc:test`
**Trigger for test-related requests:**
- Test words: `test`, `spec`, `coverage`, `TDD`, `unit test`, `integration test`
- Verification: `verify`, `validate`, `check`, `ensure`

**Examples:**
- "Write tests for this handler" → `/sc:test`
- "Add unit tests" → `/sc:test`
- "Increase test coverage" → `/sc:test`

#### 🟣 PRIORITY 6: Research/Learning → `/sc:research` or `/sc:explain`
**Use `/sc:research` for:**
- External knowledge: `best practice`, `how do others`, `industry standard`
- Technology research: `compare`, `alternatives`, `which library`, `latest`

**Use `/sc:explain` for:**
- Code understanding: `explain`, `what does`, `how does`, `walk me through`
- Internal code: asking about THIS codebase

**Examples:**
- "What's the best practice for CQRS?" → `/sc:research`
- "Explain how the auth flow works" → `/sc:explain`
- "What does this specification do?" → `/sc:explain`

#### ⚫ PRIORITY 7: Documentation → `/sc:document`
**Trigger for documentation requests:**
- Doc words: `document`, `docs`, `README`, `comment`, `docstring`, `JSDoc`
- Writing: `write docs`, `add documentation`, `update README`

#### ⚪ PRIORITY 8: Estimation → `/sc:estimate`
**Trigger for time/effort questions:**
- Time: `how long`, `time`, `duration`, `deadline`
- Effort: `effort`, `complexity`, `estimate`, `scope`

#### 🔘 PRIORITY 9: Git Operations → `/sc:git`
**Trigger for version control:**
- Git words: `commit`, `push`, `branch`, `merge`, `PR`, `pull request`
- Save: `save changes`, `check in`

### Chained Workflows (Auto-Sequence)

When a task requires multiple steps, chain these commands:

| Workflow | Sequence | When to Use |
|----------|----------|-------------|
| **Feature Dev** | `/sc:brainstorm` → `/sc:design` → `/sc:implement` → `/sc:test` | Building new features |
| **Bug Fix** | `/sc:troubleshoot` → fix → `/sc:test` | Fixing issues |
| **Refactor** | `/sc:analyze` → `/sc:cleanup` → `/sc:test` | Code improvement |
| **Research** | `/sc:research` → `/sc:design` → `/sc:document` | Learning + planning |

### DO NOT Auto-Route When:

1. **User explicitly names a command** - Just run what they asked
2. **Simple file operations** - "read file X", "show me Y" - just do it
3. **Direct questions about this conversation** - "what did we do?"
4. **Run/start commands** - "run tests", "start website", "build" - execute directly

### Disambiguation Rules

When multiple patterns match:
1. **Error/problem keywords ALWAYS win** → `/sc:troubleshoot`
2. **"Add/create/new" + feature description** → `/sc:brainstorm`
3. **"How should/what's best" + architecture** → `/sc:design`
4. **"Clean/refactor" existing code** → `/sc:cleanup`
5. **"Make better/faster"** → `/sc:improve`

### Quick Command Reference

| Category | Commands | Use For |
|----------|----------|---------|
| **Discovery** | `/sc:brainstorm`, `/sc:research` | Understanding requirements, exploring options |
| **Planning** | `/sc:design`, `/sc:estimate`, `/sc:workflow` | Architecture, estimates, process |
| **Development** | `/sc:implement`, `/sc:build`, `/sc:improve`, `/sc:cleanup` | Writing and improving code |
| **Quality** | `/sc:test`, `/sc:analyze`, `/sc:troubleshoot` | Testing, analysis, debugging |
| **Documentation** | `/sc:document`, `/sc:explain`, `/sc:index` | Docs, explanations, indexing |
| **Project** | `/sc:git`, `/sc:pm`, `/sc:task`, `/sc:spec-panel` | Git, project management, specs |
| **Utilities** | `/sc:recommend`, `/sc:help`, `/sc:load`, `/sc:save` | Help, session management |

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
12. **Enums serialize as strings** - All C# enums are serialized as strings (not integers) for JavaScript compatibility. This is configured in HTTP JSON, SignalR, and Source Generator. See `docs/backend/patterns/json-enum-serialization.md`.

## Quick Reference

```bash
# Build & Run
dotnet build src/NOIR.sln
dotnet run --project src/NOIR.Web
dotnet watch --project src/NOIR.Web

# Tests (2,000+ tests)
dotnet test src/NOIR.sln

# Migrations
dotnet ef migrations add NAME --project src/NOIR.Infrastructure --startup-project src/NOIR.Web
```

**Admin Login:** `admin@noir.local` / `123qwe`

## Running the Website (IMPORTANT)

**When user says "run website" or "start the app", use the startup scripts:**

```bash
# macOS/Linux - Run from project root:
./start-dev.sh

# Windows - Run from project root:
start-dev.bat
```

**What the scripts do:**
1. Kill any processes on ports 3000 and 4000
2. Install frontend npm dependencies (prevents missing package errors)
3. Start backend on port 4000
4. Start frontend on port 3000
5. Display URLs and login credentials

**Manual startup (if scripts fail):**
```bash
# Terminal 1 - Backend
cd src/NOIR.Web && dotnet run

# Terminal 2 - Frontend (MUST run npm install first!)
cd src/NOIR.Web/frontend && npm install && npm run dev
```

**URLs:**
- Frontend: http://localhost:3000
- Backend API: http://localhost:4000

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

### Interactive Elements Must Have cursor-pointer

**All clickable/interactive elements MUST have `cursor-pointer` class.** This includes:
- Tabs (`TabsTrigger`)
- Checkboxes (`Checkbox`)
- Select dropdowns (`SelectTrigger`, `SelectItem`)
- Dropdown menu items (`DropdownMenuItem`, `DropdownMenuCheckboxItem`)
- Switches (`Switch`)
- Any custom clickable elements

When creating or modifying UI components in `src/components/ui/`, always verify `cursor-pointer` is included in the className for interactive elements.

### Multi-Select Dropdowns Must Stay Open

For dropdown menus that allow multi-selection (checkboxes), add `onSelect={(e) => e.preventDefault()}` to prevent the dropdown from closing on each click:

```typescript
<DropdownMenuCheckboxItem
  checked={selected}
  onSelect={(e) => e.preventDefault()}  // Keeps dropdown open
  onCheckedChange={handleChange}
>
  {label}
</DropdownMenuCheckboxItem>
```

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
