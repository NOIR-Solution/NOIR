<div align="center">

# 🤖 NOIR - Claude Code Instructions

**Your AI-Powered Development Companion**

*Specific instructions for Claude Code. For universal AI agent instructions, see [AGENTS.md](AGENTS.md).*

**Last Updated:** 2026-02-09 | **Version:** 2.3

</div>

---

## Table of Contents

- [SuperClaude Framework](#superclaude-framework)
- [Critical Rules](#-critical-rules) (23 rules organized by category)
- [Quick Reference](#-quick-reference) (Build, Test, Database, Credentials)
- [Running the Website](#-running-the-website-important)
- [Project Structure](#-project-structure)
- [Code Patterns](#-code-patterns) (Backend patterns with examples)
- [Naming Conventions](#-naming-conventions)
- [Performance Rules](#-performance-rules)
- [Frontend Rules](#-frontend-rules-reacttypescript) (UI, Validation, Forms)
- [E-commerce Patterns](#-e-commerce-patterns) (Products, Cart, Checkout, Orders)
- [Task Management](#-task-management)
- [Documentation](#-documentation)
- [File Boundaries](#-file-boundaries)

---

## SuperClaude Framework

**Just say what you need in natural language** — auto-routing is handled by `.claude/rules/superclaude-routing.md`.

Quick reference: `/sc:help` | `/sc:recommend "your task"`

---

## ⚠️ Critical Rules

### Core Principles

1. ✅ **Check existing patterns first** - Look at similar files before writing new code
2. ✅ **Use Specifications** for all database queries - Never raw `DbSet` queries in services
3. ✅ **Tag all specifications** with `TagWith("MethodName")` for SQL debugging
4. ✅ **Run `dotnet build src/NOIR.sln`** after code changes
5. ✅ **Soft delete only** - Never hard delete unless explicitly requested for GDPR

### Dependency Injection

6. ✅ **No using statements in files** - Add to `GlobalUsings.cs` in each project
7. ✅ **Use marker interfaces** for DI - Add `IScopedService`, `ITransientService`, or `ISingletonService`

### Data Access

8. ✅ **Use IUnitOfWork for persistence** - Repository methods do NOT auto-save. Always inject `IUnitOfWork` and call `SaveChangesAsync()` after mutations. Never inject `ApplicationDbContext` directly into services.
9. ✅ **Use AsTracking for mutations** - Specifications default to `AsNoTracking`. For specs that retrieve entities for modification, add `.AsTracking()` to enable change detection.

### Architecture

10. ✅ **Co-locate Command + Handler + Validator** - All CQRS components live in the same folder under `Application/Features/{Feature}/Commands/{Action}/` or `Application/Features/{Feature}/Queries/{Action}/`

### Audit & Activity Timeline

11. ✅ **Audit logging for user actions** - Commands that create, update, or delete data via frontend MUST implement `IAuditableCommand`. See `docs/backend/patterns/hierarchical-audit-logging.md` for the checklist and pattern. Requires: (a) Command implements `IAuditableCommand<TResult>`, (b) Endpoint sets `UserId` on command, (c) Frontend page calls `usePageContext('PageName')`.
12. ✅ **Register before-state resolvers for Update commands** - Commands implementing `IAuditableCommand<TDto>` with `OperationType.Update` MUST have a before-state resolver registered in `DependencyInjection.cs`. Without this, the Activity Timeline's Handler tab shows "No handler diff available". See `docs/backend/patterns/before-state-resolver.md`. Add: `services.AddBeforeStateResolver<YourDto, GetYourEntityQuery>(targetId => new GetYourEntityQuery(...));`

### Serialization

13. ✅ **Enums serialize as strings** - All C# enums are serialized as strings (not integers) for JavaScript compatibility. This is configured in HTTP JSON, SignalR, and Source Generator. See `docs/backend/patterns/json-enum-serialization.md`.

### Security Patterns

14. ✅ **OTP flow consistency** - All OTP-based features (Password Reset, Email Change, etc.) MUST follow these patterns to prevent bypass attacks and ensure consistent UX:
    - **Backend bypass prevention**: When user requests OTP again with same target (email/userId), if an active OTP exists:
      - If cooldown still active → Return existing session (no new OTP, no email)
      - If cooldown passed but same target → Use `ResendOtpInternalAsync` (keeps same sessionToken, generates new OTP)
      - If cooldown passed but different target → Mark old OTP as used, create new session
    - **Frontend error handling**: Always clear OTP input on verification error (use `useEffect` to watch `serverError`)
    - **Session token stability**: Use refs (`sessionTokenRef`) to avoid stale closure issues in callbacks
    - Reference: `PasswordResetService.cs` is the canonical implementation pattern

### Error Handling

15. ✅ **Error factory method parameter order** - `Error.Validation(propertyName, message, code?)` - The first parameter is the property/field name, second is the human-readable message, third is the optional error code. WRONG: `Error.Validation("message", errorCode)` causes error codes to display instead of messages! CORRECT: `Error.Validation("fieldName", "Message to user", ErrorCodes.SomeCode)`. See `docs/KNOWLEDGE_BASE.md#error-factory-methods` for all factory methods.

### Email System

16. ✅ **Email templates are database-driven** - Email templates are loaded from the `EmailTemplate` table, NOT from .cshtml files. Templates are seeded by `ApplicationDbContextSeeder.cs` and customized via Admin UI. NEVER create .cshtml files in `src/NOIR.Web/EmailTemplates/` - they are not used by `EmailService.SendTemplateAsync()`. To update email template HTML, edit the database seeder methods (`GetPasswordResetOtpHtmlBody()`, etc.) or use the Admin UI. Multi-tenant architecture: platform defaults (TenantId=null) with tenant-specific overrides (copy-on-write pattern).

### Multi-Tenancy

17. ✅ **System users must have TenantId = null** - Platform admins and system processes MUST have `IsSystemUser = true` and `TenantId = null` for cross-tenant access. The `TenantIdSetterInterceptor` protects system users from accidental tenant assignment by checking `IsSystemUser` BEFORE any entity state checks. NEVER manually set `TenantId` on system users. The database seeder automatically creates platform admin with correct values and fixes any drift on startup. Verification: Check logs for "Created platform admin user: {Email} (TenantId = null)". See `docs/backend/architecture/tenant-id-interceptor.md`.

18. ✅ **Unique constraints MUST include TenantId** - For multi-tenant entities (`TenantAggregateRoot`, `TenantEntity`, `PlatformTenantAggregateRoot`, `PlatformTenantEntity`), unique constraints MUST include TenantId to allow the same value in different tenants. Pattern: `builder.HasIndex(e => new { e.Slug, e.TenantId }).IsUnique()`. **Exceptions:** (1) Security tokens (RefreshToken.Token, SessionToken) must be globally unique; (2) Correlation IDs for distributed tracing must be globally unique; (3) System-level entities (Permission) that are not tenant-scoped; (4) Junction tables referencing tenant-scoped FKs (the FK implicitly enforces tenant scope). **For performance**, frequently-queried lookup indexes SHOULD include TenantId as a leading column when the tenant filter is always applied (via Finbuckle): `builder.HasIndex(e => new { e.TenantId, e.UserId })`.

### Testing Requirements

19. ✅ **All code changes must pass existing tests** - Run `dotnet test src/NOIR.sln` after any code change. All tests MUST pass before considering a task complete. Never leave failing tests.
20. ✅ **New features MUST have test coverage** - Every new feature (Commands, Queries, Handlers, Validators, Services) MUST have corresponding unit tests. Integration tests are required for endpoint-level verification. Test projects: `tests/NOIR.Application.UnitTests` for handlers/validators, `tests/NOIR.Domain.UnitTests` for domain logic, `tests/NOIR.IntegrationTests` for API endpoints.
21. ✅ **Repository implementations need DI verification** - When creating a new entity with a Repository, always create the corresponding `{Entity}Repository.cs` in `Infrastructure/Persistence/Repositories/` AND add a test verifying the DI registration resolves correctly.

### Database Migrations

22. ✅ **EF Core migrations MUST specify --context** - ALWAYS use `--context ApplicationDbContext` or `--context TenantStoreDbContext` when running `dotnet ef migrations` commands. This project has multiple DbContexts and omitting `--context` will cause errors. Specify `--output-dir Migrations/App` for ApplicationDbContext or `--output-dir Migrations/Tenant` for TenantStoreDbContext. See Quick Reference for examples.

### Pre-Push Validation

23. ✅ **ALWAYS run frontend build before pushing** - Run `cd src/NOIR.Web/frontend && npm run build` before `git push`. The local dev server (`npm run dev`) allows TypeScript warnings, but CI runs strict mode and will fail. A pre-push hook is installed at `.git/hooks/pre-push` to automatically validate builds. If you bypass the hook with `--no-verify`, you MUST manually verify the build passes.

---

## ⚡ Quick Reference

### Build & Run

```bash
# Build
dotnet build src/NOIR.sln

# Run (production mode - serves frontend)
dotnet run --project src/NOIR.Web

# Development mode with hot reload
dotnet watch --project src/NOIR.Web
```

### Testing

```bash
# All tests (6,750+ tests)
dotnet test src/NOIR.sln

# Specific project
dotnet test tests/NOIR.IntegrationTests

# With coverage
dotnet test src/NOIR.sln --collect:"XPlat Code Coverage"
```

### Database Migrations

**CRITICAL: Always specify `--context`**

```bash
# ApplicationDbContext (main database) -> Migrations/App/
dotnet ef migrations add NAME \
  --project src/NOIR.Infrastructure \
  --startup-project src/NOIR.Web \
  --context ApplicationDbContext \
  --output-dir Migrations/App

# TenantStoreDbContext (tenant store) -> Migrations/Tenant/
dotnet ef migrations add NAME \
  --project src/NOIR.Infrastructure \
  --startup-project src/NOIR.Web \
  --context TenantStoreDbContext \
  --output-dir Migrations/Tenant

# Update database (apply both contexts)
dotnet ef database update \
  --project src/NOIR.Infrastructure \
  --startup-project src/NOIR.Web \
  --context TenantStoreDbContext

dotnet ef database update \
  --project src/NOIR.Infrastructure \
  --startup-project src/NOIR.Web \
  --context ApplicationDbContext

# Drop database and reset migrations to single InitialCreate
dotnet ef database drop \
  --project src/NOIR.Infrastructure \
  --startup-project src/NOIR.Web \
  --context ApplicationDbContext \
  --force

rm -rf src/NOIR.Infrastructure/Migrations/App/*.cs
rm -rf src/NOIR.Infrastructure/Migrations/Tenant/*.cs

dotnet ef migrations add InitialCreate \
  --project src/NOIR.Infrastructure \
  --startup-project src/NOIR.Web \
  --context TenantStoreDbContext \
  --output-dir Migrations/Tenant

dotnet ef migrations add InitialCreate \
  --project src/NOIR.Infrastructure \
  --startup-project src/NOIR.Web \
  --context ApplicationDbContext \
  --output-dir Migrations/App
```

### Admin Credentials

| Account | Email | Password |
|---------|-------|----------|
| **Platform Admin** | `platform@noir.local` | `123qwe` |
| **Tenant Admin** | `admin@noir.local` | `123qwe` |

---

## 🌐 Running the Website (IMPORTANT)

**When user says "run website" or "start the app", use the unified startup script:**

```bash
# All platforms (macOS, Linux, Windows via Git Bash/MSYS2/WSL)
./start-dev.sh
```

### What the Script Does

1. Detects OS (macOS, Linux, Windows, WSL)
2. Checks prerequisites (.NET SDK, Node.js, npm)
3. Frees ports 3000 and 4000 (kills existing processes)
4. Installs frontend dependencies
5. Builds and starts backend on port 4000
6. Starts frontend on port 3000
7. Opens browser automatically
8. Handles graceful shutdown with Ctrl+C

### Logs

The script creates log files in the project root:
- `.backend.log` - Backend output
- `.frontend.log` - Frontend output

View logs with: `tail -f .backend.log` or `tail -f .frontend.log`

### Manual Startup (if script fails)

```bash
# Terminal 1 - Backend
cd src/NOIR.Web && dotnet run

# Terminal 2 - Frontend (MUST run npm install first!)
cd src/NOIR.Web/frontend && npm install && npm run dev
```

### Claude Code on Windows (CRITICAL)

When running commands directly in Claude Code on Windows (not via start-dev.sh):

```bash
# Backend - works with run_in_background
dotnet run --project src/NOIR.Web

# Frontend - MUST use PowerShell Start-Process to spawn detached process
powershell -Command "Start-Process cmd -ArgumentList '/c cd /d D:\GIT\TOP\NOIR\src\NOIR.Web\frontend && npm run dev'"
```

### Access Points

| Service | URL |
|---------|-----|
| **Frontend** | http://localhost:3000 |
| **Backend API** | http://localhost:4000 |
| **API Docs** | http://localhost:4000/api/docs |

---

## 📂 Project Structure

```
src/NOIR.Domain/          # 🎯 Entities, IRepository, ISpecification
src/NOIR.Application/     # 📋 Features (Command + Handler + Validator co-located), DTOs
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
src/NOIR.Infrastructure/  # 🔧 EF Core, Repositories, Service implementations
src/NOIR.Web/             # 🌐 Endpoints, Middleware, Program.cs
    └── frontend/         # ⚛️ React SPA
```

---

## 💻 Code Patterns

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
        await _unitOfWork.SaveChangesAsync(ct);  // REQUIRED - repos don't auto-save
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

---

## 📛 Naming Conventions

| Type | Pattern | Example |
|------|---------|---------|
| Specification | `[Entity][Filter]Spec` | `ActiveCustomersSpec` |
| Command | `[Action][Entity]Command` | `CreateOrderCommand` |
| Handler | `[Command]Handler` | `CreateOrderHandler` |
| Configuration | `[Entity]Configuration` | `CustomerConfiguration` |

---

## ⚡ Performance Rules

| Scenario | Use |
|----------|-----|
| Read-only queries | `AsNoTracking` (default) |
| Multiple collections | `.AsSplitQuery()` |

---

## ⚛️ Frontend Rules (React/TypeScript)

### 🎨 UI Component Building (MANDATORY)

**✅ We use `/ui-ux-pro-max` skill for ALL frontend UI/UX work.**

```typescript
// ✅ CORRECT: Use /ui-ux-pro-max skill for both research AND implementation
// The skill handles:
//    - Design research (color palettes, typography, style guidelines)
//    - UX best practices and design patterns
//    - Component generation (React/TypeScript with shadcn/ui)
//    - Component refinement and improvements
//    - Accessibility and responsive design
```

**Workflow:**
1. For any UI/UX work, invoke the `/ui-ux-pro-max` skill via the Skill tool
2. Provide clear requirements (research question or component specs)
3. The skill will handle both design guidance AND code implementation

**When to Use:**
- **Research**: "What color palette for e-commerce?", "UX best practices for forms"
- **Implementation**: "Build a product card component", "Create a checkout page"
- **Refinement**: "Improve this modal dialog", "Add accessibility to navbar"
- **Review**: "Review my component for UX issues"

**Benefits:**
- Unified workflow for all UI/UX tasks
- Production-ready React/TypeScript components
- Built-in shadcn/ui integration
- Proper accessibility (ARIA labels, keyboard navigation)
- Responsive design patterns
- Consistent with project design system

### 🖱️ Interactive Elements Must Have cursor-pointer

**All clickable/interactive elements MUST have `cursor-pointer` class.** This includes:
- Tabs (`TabsTrigger`)
- Checkboxes (`Checkbox`)
- Select dropdowns (`SelectTrigger`, `SelectItem`)
- Dropdown menu items (`DropdownMenuItem`, `DropdownMenuCheckboxItem`)
- Switches (`Switch`)
- Any custom clickable elements

When creating or modifying UI components in `src/components/ui/`, always verify `cursor-pointer` is included in the className for interactive elements.

### 🎯 UI/UX Standardization Patterns (CRITICAL)

All UI components MUST follow standardized patterns. See [docs/frontend/architecture.md#uiux-standardization-patterns](docs/frontend/architecture.md#uiux-standardization-patterns) for complete patterns.

**Quick Reference:**
- **aria-label**: ALL icon-only buttons must have contextual aria-labels (e.g., `aria-label={`Delete ${item.name}`}`)
- **AlertDialog destructive pattern**: Use `border-destructive/30`, icon container with `p-2 rounded-xl bg-destructive/10 border border-destructive/20`, and `cursor-pointer` on all buttons
- **Confirmation dialogs**: Required for ALL destructive actions (delete, remove)
- **Card shadows**: `shadow-sm hover:shadow-lg transition-all duration-300`
- **Gradient text**: MUST include `text-transparent` with `bg-clip-text`

### 📝 Multi-Select Dropdowns Must Stay Open

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

### ✅ Validation Consistency (CRITICAL)

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

### 🔍 Zod Validation

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

### 📋 Form Validation Mode: `mode: 'onBlur'` (REQUIRED)

All forms MUST use `mode: 'onBlur'` for consistent real-time validation:

```typescript
const form = useForm<FormData>({
  resolver: zodResolver(schema),
  mode: 'onBlur',  // REQUIRED - validates when field loses focus
  defaultValues: { ... },
})
```

**Why `onBlur`:**
- Validates after user finishes typing (not during)
- Shows errors before submit
- Consistent behavior across all forms

**See:** [docs/frontend/architecture.md#form-validation-standards](docs/frontend/architecture.md#form-validation-standards) for complete patterns.

### 🔧 Dynamic Schema Factories with i18n (CRITICAL)

When using Zod schema factories that accept translation functions for i18n validation messages, TypeScript cannot infer compatible resolver types. Use the standardized type assertion pattern:

```typescript
import { useForm, type Resolver } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'

// Schema factory with translation function
const createFormSchema = (t: (key: string, options?: Record<string, unknown>) => string) =>
  z.object({
    name: z.string().min(1, t('validation.required')),
    sortOrder: z.number().default(0),  // .default() causes type mismatch
  })

type FormData = z.infer<ReturnType<typeof createFormSchema>>

const form = useForm<FormData>({
  // TypeScript cannot infer resolver types from dynamic schema factories
  // Using 'as unknown as Resolver<T>' for type-safe assertion
  resolver: zodResolver(createFormSchema(t)) as unknown as Resolver<FormData>,
  mode: 'onBlur',
})
```

**Why this is needed:**
- `z.default()` makes TypeScript infer fields as optional (`field?: type`)
- But form types expect them as required (`field: type`)
- The type assertion bridges this mismatch
- Runtime validation works correctly; this is only for compile-time type checking

**Pattern:**
- ✅ **Use:** `as unknown as Resolver<FormDataType>` (type-safe, explicit)
- ❌ **Avoid:** `as any` (unsafe, loses all type information)
- ✅ **Always include comment** explaining the TypeScript limitation

**Affected files:** All forms using i18n validation messages (13+ files standardized)

### 💬 Dialog Form Layout (Focus Ring Clipping)

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

### 🎭 Multi-Select Role Pattern

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

### 🔄 TanStack Query Hooks Pattern

```typescript
// All API calls use TanStack Query hooks for caching, refetching, and state management
// Hooks are in: src/NOIR.Web/frontend/src/hooks/

// Query hook pattern (GET)
export function useProducts(params?: ProductsParams) {
  return useQuery({
    queryKey: ['products', params],
    queryFn: () => productsApi.getProducts(params),
  })
}

// Mutation hook pattern (POST/PUT/DELETE)
export function useCreateProduct() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: productsApi.createProduct,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['products'] })
    },
  })
}

// Usage in components:
const { data: products, isLoading } = useProducts({ status: 'Active' })
const createProduct = useCreateProduct()
await createProduct.mutateAsync(productData)
```

---

## 🛒 E-commerce Patterns

> **Phase 8 Complete:** Product Catalog, Shopping Cart, Checkout, Orders, Product Attributes.

### Product Attributes

```typescript
// Dynamic attribute system with 13 types
// Location: src/NOIR.Application/Features/ProductAttributes/

// AttributeType: Select, MultiSelect, Text, TextArea, Number, Decimal,
//               Boolean, Date, DateTime, Color, Range, Url, File

// ProductAttribute → ProductAttributeValue (1:N) - predefined options
// Category → CategoryAttribute (M:N) - attributes assigned to categories
// Product → ProductAttributeAssignment (1:N) - actual values on products

// ProductFilterIndex - Denormalized for fast faceted filtering
// FilterAnalyticsEvent - Track filter usage for analytics
```

### Product Management

```typescript
// Products use variant pattern with SKU-level inventory
// Location: src/NOIR.Application/Features/Products/

// Product → ProductVariant (1:N) with SKU, price, inventory
// Product → ProductImage (1:N) with display order
// Product → ProductCategory (N:1) hierarchical categories

// ProductStatus workflow: Draft → Active → Archived
// Use PublishProductCommand to activate, ArchiveProductCommand to archive
```

### Shopping Cart

```typescript
// Cart supports both authenticated users and guests
// Location: src/NOIR.Application/Features/Cart/

// Guest carts use SessionId (cookie/header)
// On login, use MergeCartCommand to combine guest + user carts

// CartStatus: Active → Converted (on checkout) or Abandoned (cleanup)
```

### Checkout Flow

```typescript
// Hybrid accordion pattern: Address → Shipping → Payment → Complete
// Location: src/NOIR.Application/Features/Checkout/

// 1. InitiateCheckoutCommand - Creates session from cart
// 2. SetCheckoutAddressCommand - Shipping/billing address
// 3. SelectShippingCommand - Shipping method
// 4. SelectPaymentCommand - Payment gateway + method
// 5. CompleteCheckoutCommand - Creates Order, reserves inventory

// Session expires after 30 minutes (configurable)
```

### Order Lifecycle

```typescript
// OrderStatus workflow:
// Pending → Confirmed → Processing → Shipped → Delivered → Completed
//    ↓
// Cancelled (with inventory release)

// Location: src/NOIR.Application/Features/Orders/
// OrderItem captures product snapshot (name, price, image) at order time
```

### Inventory Management

```typescript
// Inventory tracked at ProductVariant level
// Reservation on checkout initiation
// Deduction on order ship
// Release on order cancel

// InventoryMovementType: StockIn, StockOut, Adjustment, Return, Reserved, Released
```

---

## 📊 Task Management

This project uses **Vibe Kanban** for task tracking and sprint management. Check the kanban board for current tasks, priorities, and sprint goals before starting work.

---

## 📚 Documentation

For detailed documentation, see the `docs/` folder:

| Topic | Location |
|-------|----------|
| **Core Documentation** | `docs/DOCUMENTATION_INDEX.md` |
| **Knowledge Base** | `docs/KNOWLEDGE_BASE.md` |
| **Project Navigation** | `docs/PROJECT_INDEX.md` |
| **Feature Catalog** | `docs/FEATURE_CATALOG.md` |
| **Tech Stack** | `docs/TECH_STACK.md` |
| **Backend Patterns** | `docs/backend/patterns/` |
| **Backend Research** | `docs/backend/research/` |
| **Frontend Guide** | `docs/frontend/` |
| **Architecture Decisions** | `docs/decisions/` |

### When Creating Documentation

- Research reports go to `docs/backend/research/` or `docs/frontend/research/`
- Do NOT use `claudedocs/` - that folder is deprecated

---

## 🔒 File Boundaries

### Read Freely

✅ `src/`, `tests/`, `docs/`, `.claude/`

### Avoid Modifying

⚠️ `*.Designer.cs`, `Migrations/` (auto-generated)

---

<div align="center">

**🎯 Pro Tip:** Just describe what you need in natural language - Claude auto-routes to the right command!

---

**Built with ❤️ by the NOIR Team**

[📚 Documentation](docs/) • [🤖 AGENTS.md](AGENTS.md) • [🌟 Star on GitHub](https://github.com/NOIR-Solution/NOIR)

</div>

---

## 📝 Changelog

### Version 2.4 (2026-02-10)
- **BREAKING:** Removed entire E2E testing infrastructure (Playwright, 490+ tests, 100 files)
- **Removed:** GitHub Actions workflows (accessibility.yml, visual-regression.yml)
- **Removed:** E2E testing documentation (4 guides, 3 test docs)
- **Updated:** All documentation to reflect backend-only testing focus
- **Focus:** Backend testing only (6,750+ xUnit tests: domain, application, integration, architecture)
- **Reason:** Simplified project maintenance, focus on core backend functionality

### Version 2.3 (2026-02-09)
- **Standardized:** Form resolver pattern across 13 files - migrated from `as any` to safer `as unknown as Resolver<T>`
- **Added:** Documentation for dynamic schema factories with i18n validation messages
- **Fixed:** Type safety improvements for react-hook-form with Zod schema factories
- **Investigated:** Root cause of type assertions (`.default()` in Zod makes fields optional in type inference)
- **Added:** Explanatory comments to all form resolver usages explaining TypeScript limitation

### Version 2.2 (2026-02-08)
- **Updated:** Test count to 6,750+ (verified 2026-02-08: 842 domain + 5,231 application + 654 integration + 25 architecture)
- **Updated:** Documentation audit - removed outdated PRODUCT_E2E_TESTS.md, updated ADR-002 to reflect shadcn/ui

### Version 2.1 (2026-01-29)
- **Added:** Product Attribute System patterns (13 attribute types)
- **Added:** ProductFilterIndex for faceted filtering
- **Added:** FilterAnalyticsEvent for usage tracking
- **Cleaned:** Documentation structure (42 files, removed obsolete plans)

### Version 2.0 (2026-01-26)
- **Fixed:** Rule numbering now sequential (1-22)
- **Added:** Table of Contents for navigation
- **Added:** E-commerce Patterns section (Products, Cart, Checkout, Orders)
- **Added:** TanStack Query hooks pattern for frontend data fetching
- **Added:** Version tracking and changelog

### Version 1.0 (Initial)
- Original CLAUDE.md with 22 critical rules
- Backend patterns, frontend rules, quick reference
