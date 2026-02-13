# NOIR - Technology Stack Reference

> **Complete reference of technologies, libraries, and frameworks used in NOIR.**

**Last Updated:** 2026-01-23

---

## Table of Contents

- [Overview](#overview)
- [Backend Stack (.NET 10)](#backend-stack-net-10)
- [Frontend Stack (React 19)](#frontend-stack-react-19)
- [Infrastructure & DevOps](#infrastructure--devops)
- [Testing Framework](#testing-framework)
- [Development Tools](#development-tools)
- [Why These Choices?](#why-these-choices)

---

## Overview

NOIR is built on modern, production-ready technologies selected for:
- **Performance** - High throughput, low latency
- **Developer Experience** - Fast feedback loops, great tooling
- **Maintainability** - Clear patterns, strong typing
- **Scalability** - Horizontal scaling, multi-tenancy
- **Security** - Built-in protections, audit trails

---

## Backend Stack (.NET 10)

### Core Framework

| Technology | Version | Purpose |
|------------|---------|---------|
| **.NET** | 10.0 | Runtime and SDK |
| **C#** | 14.0 | Primary language |
| **ASP.NET Core** | 10.0 | Web framework |
| **Minimal APIs** | 10.0 | Endpoint routing |

**Why .NET 10?**
- Latest LTS with 3 years of support
- Native AOT compilation support
- Improved performance (JSON serialization, HTTP/3)
- Enhanced cloud-native features

---

### Data Access & Persistence

| Technology | Version | Purpose | Docs |
|------------|---------|---------|------|
| **Entity Framework Core** | 10.0 | ORM | [EF Core Docs](https://learn.microsoft.com/ef/core/) |
| **SQL Server** | 2022 | Database | [SQL Server](https://www.microsoft.com/sql-server) |
| **Ardalis.Specification** | 8.0 | Query pattern | [Specification](backend/patterns/repository-specification.md) |
| **EFCore.BulkExtensions** | 10.0 | Bulk operations | [Bulk Ops](backend/patterns/bulk-operations.md) |

**Key Features:**
- **Specifications** - Reusable query logic
- **Interceptors** - Audit logging, soft delete, multi-tenancy
- **Migrations** - Code-first schema management
- **Performance** - Split queries, no-tracking, compiled queries

**Configuration Files:**
- `src/NOIR.Infrastructure/Persistence/ApplicationDbContext.cs`
- `src/NOIR.Infrastructure/Persistence/Configurations/`

---

### CQRS & Messaging

| Technology | Version | Purpose | Docs |
|------------|---------|---------|------|
| **Wolverine** | 4.0 | CQRS messaging | [Wolverine](https://wolverinefx.net/) |
| **MediatR** | - | Replaced by Wolverine | - |

**Why Wolverine?**
- **Zero reflection** - Source generators for performance
- **Async-first** - Native async/await support
- **Middleware pipeline** - Validation, logging, performance
- **Vertical slices** - Co-located commands/handlers

**Pattern:**
```csharp
// Command
public record CreateUserCommand(string Email, string Password) : IAuditableCommand<UserDto>;

// Handler (auto-discovered)
public class CreateUserCommandHandler
{
    public async Task<Result<UserDto>> Handle(CreateUserCommand cmd, CancellationToken ct)
    {
        // Business logic
    }
}
```

**Configuration:**
- `src/NOIR.Web/Program.cs` - Wolverine registration
- `src/NOIR.Application/Behaviors/` - Pipeline behaviors

---

### Validation

| Technology | Version | Purpose | Docs |
|------------|---------|---------|------|
| **FluentValidation** | 11.0 | Command validation | [FluentValidation](https://docs.fluentvalidation.net/) |

**Features:**
- **Declarative** - Fluent API for rules
- **Async validation** - Database checks
- **Localization** - Multi-language error messages
- **Testing** - Easy to unit test validators

**Pattern:**
```csharp
public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).MinimumLength(6);
    }
}
```

**Integration:** `ValidationBehavior<TRequest, TResponse>` in Wolverine pipeline.

---

### Mapping

| Technology | Version | Purpose | Docs |
|------------|---------|---------|------|
| **Mapperly** | 3.0 | Object mapping | [Mapperly](https://mapperly.riok.app/) |
| **AutoMapper** | - | Replaced by Mapperly | - |

**Why Mapperly?**
- **Zero runtime cost** - Source generators (compile-time)
- **Type-safe** - Compile-time validation
- **Performance** - No reflection
- **Queryable projections** - `IQueryable<T>` support

**Pattern:**
```csharp
[Mapper]
public static partial class UserMapper
{
    public static partial UserDto ToDto(this ApplicationUser user);
    public static partial IQueryable<UserDto> ProjectToDto(this IQueryable<ApplicationUser> query);
}
```

---

### Authentication & Authorization

| Technology | Version | Purpose | Docs |
|------------|---------|---------|------|
| **ASP.NET Core Identity** | 10.0 | User management | [Identity](https://learn.microsoft.com/aspnet/core/security/authentication/identity) |
| **JWT Bearer** | 10.0 | Token authentication | [JWT](backend/patterns/jwt-refresh-token.md) |
| **BCrypt.Net** | 0.1.0 | Password hashing | - |

**Features:**
- **JWT + Refresh Tokens** - Token rotation for security
- **Permission-based** - `resource:action` granular permissions
- **Resource-based** - Owner-based access control
- **Claims** - Role and permission claims in JWT

**Configuration:**
- `src/NOIR.Infrastructure/Identity/` - Identity services
- `src/NOIR.Infrastructure/Identity/Authorization/` - Authorization handlers

---

### Multi-Tenancy

| Technology | Version | Purpose | Docs |
|------------|---------|---------|------|
| **Finbuckle.MultiTenant** | 10.0.2 | Multi-tenancy | [Finbuckle](https://www.finbuckle.com/MultiTenant/) |

**Features:**
- **Strategy-based** - Header, claim, host resolution
- **EF Core store** - Tenant storage in database
- **Automatic filtering** - Query-level isolation
- **Per-tenant services** - Scoped services per tenant

**Architecture:**
- **Tenant Resolution** - `TenantResolutionMiddleware`
- **Query Filtering** - `TenantIdSetterInterceptor`
- **Data Isolation** - Automatic `TenantId` filtering

**Configuration:**
- `src/NOIR.Infrastructure/DependencyInjection.cs` - Multi-tenant setup
- `src/NOIR.Infrastructure/Persistence/Interceptors/TenantIdSetterInterceptor.cs`

**Docs:** [Tenant ID Interceptor](backend/architecture/tenant-id-interceptor.md)

---

### Background Jobs

| Technology | Version | Purpose | Docs |
|------------|---------|---------|------|
| **Hangfire** | 1.8 | Background jobs | [Hangfire](https://www.hangfire.io/) |
| **SQL Server Storage** | 1.8 | Job persistence | - |

**Features:**
- **Dashboard** - `/hangfire` (requires `system:hangfire` permission)
- **Recurring jobs** - Cron-based scheduling
- **Fire-and-forget** - One-time background tasks
- **Retries** - Automatic retry with backoff
- **Job failure notifications** - SignalR alerts

**Jobs:**
- `EmailCleanupJob` - Delete old emails (daily)
- Custom jobs via `IBackgroundJobClient`

**Configuration:**
- `src/NOIR.Infrastructure/BackgroundJobs/`
- `src/NOIR.Infrastructure/DependencyInjection.cs`

---

### Logging

| Technology | Version | Purpose | Docs |
|------------|---------|---------|------|
| **Serilog** | 4.0 | Structured logging | [Serilog](https://serilog.net/) |
| **Serilog.Sinks.File** | 6.0 | File logging | - |
| **Serilog.Sinks.Console** | 6.0 | Console logging | - |
| **Custom SignalR Sink** | - | Real-time log streaming | - |

**Features:**
- **Structured** - JSON-formatted logs
- **Dynamic level** - Change at runtime via `LoggingLevelSwitch`
- **SignalR streaming** - Real-time logs to frontend
- **Enrichment** - Request ID, user ID, tenant ID

**Configuration:**
- `src/NOIR.Web/Program.cs` - Serilog setup
- `src/NOIR.Infrastructure/Logging/DeferredSignalRLogSink.cs`

---

### Caching

| Technology | Version | Purpose | Docs |
|------------|---------|---------|------|
| **FusionCache** | 1.0 | Hybrid caching | [FusionCache](https://github.com/ZiggyCreatures/FusionCache) |
| **In-Memory Cache** | Built-in | L1 cache | - |
| **Redis** | Optional | L2 distributed cache | - |

**Features:**
- **Hybrid L1/L2** - In-memory + distributed
- **Stampede protection** - Prevents cache stampede
- **Fail-safe** - Stale data on failures
- **Auto-refresh** - Background refresh

**Configuration:**
- `src/NOIR.Infrastructure/Caching/FusionCacheExtensions.cs`

---

### Email

| Technology | Version | Purpose | Docs |
|------------|---------|---------|------|
| **FluentEmail** | 3.0 | Email sending | [FluentEmail](https://github.com/lukencode/FluentEmail) |
| **MailKit** | 4.0 | SMTP client | - |
| **Razor Templates** | 10.0 | Email templates | - |

**Features:**
- **Database-driven templates** - `EmailTemplate` entity
- **Multi-tenant** - Copy-on-write templates
- **Variables** - Mustache-style `{{variable}}`
- **Async sending** - Non-blocking email

**Configuration:**
- `src/NOIR.Infrastructure/Email/EmailService.cs`
- `src/NOIR.Application/Common/Settings/EmailSettings.cs`

---

### Storage

| Technology | Version | Purpose | Docs |
|------------|---------|---------|------|
| **FluentStorage** | 5.0 | Blob storage | [FluentStorage](https://github.com/robinrodricks/FluentStorage) |
| **Local File System** | Built-in | Local storage | - |
| **Azure Blob Storage** | Optional | Cloud storage | - |
| **AWS S3** | Optional | Cloud storage | - |

**Configuration:**
- `src/NOIR.Infrastructure/Storage/StorageSettings.cs`
- `src/NOIR.Infrastructure/DependencyInjection.cs`

---

### Real-Time Communication

| Technology | Version | Purpose | Docs |
|------------|---------|---------|------|
| **SignalR** | 10.0 | WebSocket messaging | [SignalR](https://learn.microsoft.com/aspnet/core/signalr/) |

**Hubs:**
- `NotificationHub` - Push notifications
- `DeveloperLogHub` - Real-time log streaming

**Configuration:**
- `src/NOIR.Infrastructure/Hubs/`
- `src/NOIR.Web/Program.cs` - Hub mapping

---

### Image Processing

| Technology | Version | Purpose | Docs |
|------------|---------|---------|------|
| **SixLabors.ImageSharp** | 3.0 | Image manipulation | [ImageSharp](https://sixlabors.com/products/imagesharp/) |

**Features:**
- **Resize** - Avatar thumbnails (200x200px)
- **Format conversion** - PNG, JPEG, WebP
- **Optimization** - Compression for web

**Configuration:**
- `src/NOIR.Infrastructure/Media/ImageProcessingService.cs`

---

### Dependency Injection

| Technology | Version | Purpose | Docs |
|------------|---------|---------|------|
| **Scrutor** | 4.0 | Auto-registration | [Scrutor](https://github.com/khellang/Scrutor) |

**Pattern:**
```csharp
// Just add marker interface - auto-registered!
public class CustomerService : ICustomerService, IScopedService { }
```

**Marker Interfaces:**
- `IScopedService` - Scoped lifetime
- `ITransientService` - Transient lifetime
- `ISingletonService` - Singleton lifetime

**Docs:** [DI Auto-Registration](backend/patterns/di-auto-registration.md)

---

## Frontend Stack (React 19)

### Core Framework

| Technology | Version | Purpose | Docs |
|------------|---------|---------|------|
| **React** | 19.0 | UI library | [React](https://react.dev/) |
| **TypeScript** | 5.7 | Type safety | [TypeScript](https://www.typescriptlang.org/) |
| **Vite** | 6.0 | Build tool | [Vite](https://vite.dev/) |

**Why React 19?**
- **React Compiler** - Automatic optimization
- **Actions** - Built-in async state management
- **Suspense** - Improved loading states
- **Performance** - Faster reconciliation

---

### UI Components

| Technology | Version | Purpose | Docs |
|------------|---------|---------|------|
| **shadcn/ui** | Latest | Component library | [shadcn/ui](https://ui.shadcn.com/) |
| **Radix UI** | 1.0 | Headless components | [Radix](https://www.radix-ui.com/) |
| **Tailwind CSS** | 4.0 | Utility-first CSS | [Tailwind](https://tailwindcss.com/) |
| **Lucide React** | 0.460 | Icons | [Lucide](https://lucide.dev/) |

**Why shadcn/ui?**
- **Copy-paste** - Own the code, not npm package
- **Customizable** - Full control over components
- **Accessible** - Radix UI primitives (WCAG compliant)
- **Type-safe** - TypeScript + Tailwind IntelliSense

**Components Used:**
- `Button`, `Input`, `Select`, `Dialog`, `Sheet`
- `Table`, `Card`, `Badge`, `Avatar`
- `DropdownMenu`, `Tabs`, `Tooltip`
- Custom: `PageSkeleton`, `TippyTooltip`

**Configuration:**
- `src/NOIR.Web/frontend/src/components/ui/` - Component source
- `src/NOIR.Web/frontend/src/uikit/` - Storybook stories (56 components)
- `.storybook/main.ts` - Storybook config (Vite + Tailwind CSS 4)

---

### Routing

| Technology | Version | Purpose | Docs |
|------------|---------|---------|------|
| **React Router** | 7.0 | Client-side routing | [React Router](https://reactrouter.com/) |

**Features:**
- **Lazy loading** - Code splitting per route
- **Suspense** - Loading fallbacks
- **Protected routes** - Authentication guards
- **Nested layouts** - Portal layout with sidebar

**Configuration:**
- `src/NOIR.Web/frontend/src/App.tsx`

---

### State Management

| Technology | Version | Purpose | Docs |
|------------|---------|---------|------|
| **React Context** | 19.0 | Global state | [Context](https://react.dev/reference/react/useContext) |
| **Zustand** | - | Optional | - |

**Contexts:**
- `AuthContext` - Authentication state
- `ThemeContext` - Dark/light mode
- `NotificationContext` - SignalR notifications

**Why Context over Redux?**
- **Simpler** - Less boilerplate
- **Type-safe** - TypeScript integration
- **Performance** - Selective re-renders
- **Server state** - TanStack Query handles API

---

### API Client

| Technology | Version | Purpose | Docs |
|------------|---------|---------|------|
| **Axios** | 1.7 | HTTP client | [Axios](https://axios-http.com/) |
| **TanStack Query** | - | Server state (optional) | - |

**Features:**
- **Interceptors** - JWT injection, error handling
- **Type generation** - Swagger → TypeScript
- **Retry logic** - Automatic retries on failure

**Configuration:**
- `src/NOIR.Web/frontend/src/services/api.ts`

---

### Forms & Validation

| Technology | Version | Purpose | Docs |
|------------|---------|---------|------|
| **React Hook Form** | 7.0 | Form management | [RHF](https://react-hook-form.com/) |
| **Zod** | 3.0 | Schema validation | [Zod](https://zod.dev/) |

**Features:**
- **Performance** - Minimal re-renders
- **Type-safe** - Zod + TypeScript
- **Validation** - Sync + async validation
- **Integration** - `@hookform/resolvers/zod`

**Pattern:**
```typescript
const schema = z.object({
  email: z.string().email(),
  password: z.string().min(6),
})

const form = useForm<FormData>({
  resolver: zodResolver(schema),
})
```

---

### Real-Time Communication

| Technology | Version | Purpose | Docs |
|------------|---------|---------|------|
| **@microsoft/signalr** | 8.0 | SignalR client | [SignalR](https://learn.microsoft.com/aspnet/core/signalr/javascript-client) |

**Hubs:**
- `NotificationHub` - Push notifications
- `DeveloperLogHub` - Log streaming

**Configuration:**
- `src/NOIR.Web/frontend/src/contexts/NotificationContext.tsx`

---

### Internationalization

| Technology | Version | Purpose | Docs |
|------------|---------|---------|------|
| **i18next** | 23.0 | i18n framework | [i18next](https://www.i18next.com/) |
| **react-i18next** | 15.0 | React bindings | - |

**Features:**
- **Multiple languages** - English, Vietnamese (extensible)
- **Lazy loading** - Load translations on demand
- **Namespaces** - `common`, `features`, `errors`
- **Fallback** - English as fallback language

**Configuration:**
- `src/NOIR.Web/frontend/src/i18n/`
- `src/NOIR.Web/frontend/public/locales/`

**Docs:** [Localization Guide](frontend/localization-guide.md)

---

### Animations

| Technology | Version | Purpose | Docs |
|------------|---------|---------|------|
| **Framer Motion** | 11.0 | Animations | [Framer Motion](https://www.framer.com/motion/) |

**Use Cases:**
- Page transitions
- Modal animations
- List animations (notifications)
- Loading spinners

---

### Utilities

| Technology | Version | Purpose |
|------------|---------|---------|
| **clsx** | 2.0 | Conditional classes |
| **date-fns** | 4.0 | Date formatting |
| **sonner** | 1.0 | Toast notifications |
| **@tippyjs/react** | 4.0 | Tooltips |

---

## Infrastructure & DevOps

### Containerization

| Technology | Version | Purpose | Docs |
|------------|---------|---------|------|
| **Docker** | 27.0 | Containerization | [Docker](https://www.docker.com/) |
| **Docker Compose** | 2.0 | Multi-container | - |

**Services:**
- SQL Server (LocalDB on Windows, Docker on macOS/Linux)
- Redis (optional for distributed cache)

---

### CI/CD

| Technology | Purpose |
|------------|---------|
| **GitHub Actions** | Continuous integration |
| **Azure DevOps** | Alternative CI/CD |

**Workflows:**
- Build and test on PR
- Deploy to staging/production
- Database migrations

---

### Hosting

| Platform | Purpose |
|----------|---------|
| **Azure App Service** | Web hosting |
| **Azure SQL Database** | Database hosting |
| **Azure Blob Storage** | File storage |
| **Cloudflare** | CDN and DNS |

---

## Testing Framework

### Unit Testing

| Technology | Version | Purpose | Docs |
|------------|---------|---------|------|
| **xUnit** | 2.9 | Test framework | [xUnit](https://xunit.net/) |
| **FluentAssertions** | 7.0 | Assertions | [FluentAssertions](https://fluentassertions.com/) |
| **NSubstitute** | 5.0 | Mocking | [NSubstitute](https://nsubstitute.github.io/) |

**Test Projects:**
- `NOIR.Domain.UnitTests` - 842 tests
- `NOIR.Application.UnitTests` - 5,231 tests

---

### Integration Testing

| Technology | Version | Purpose | Docs |
|------------|---------|---------|------|
| **WebApplicationFactory** | 10.0 | In-memory API | [WAF](https://learn.microsoft.com/aspnet/core/test/integration-tests) |
| **Testcontainers** | - | Docker containers | - |

**Features:**
- **In-memory SQL Server** - Fast database tests
- **Seeded data** - Test users and roles
- **Cleanup** - Automatic after each test

**Test Project:**
- `NOIR.IntegrationTests` - 654 tests

---

### Architecture Testing

| Technology | Version | Purpose | Docs |
|------------|---------|---------|------|
| **NetArchTest.Rules** | 1.3 | Architecture validation | [NetArchTest](https://github.com/BenMorris/NetArchTest) |

**Rules:**
- Layer dependencies
- Naming conventions
- Circular references

**Test Project:**
- `NOIR.ArchitectureTests` - 25 tests

---

## Development Tools

### Code Quality

| Tool | Purpose |
|------|---------|
| **EditorConfig** | Code style |
| **Roslyn Analyzers** | Code analysis |
| **ESLint** | JavaScript/TypeScript linting |
| **Prettier** | Code formatting |

### IDE Support

| IDE | Extensions |
|-----|-----------|
| **Visual Studio 2025** | ReSharper, CodeMaid |
| **VS Code** | C# Dev Kit, ESLint, Tailwind IntelliSense |
| **Rider** | Built-in support |

### Package Management

| Tool | Purpose |
|------|---------|
| **NuGet** | .NET packages |
| **pnpm** | JavaScript packages |

---

## Why These Choices?

### Backend

**Clean Architecture + Vertical Slices**
- ✅ Clear boundaries between layers
- ✅ Testable in isolation
- ✅ Feature-focused organization
- ❌ More ceremony than needed sometimes

**Entity Framework Core**
- ✅ Mature ORM with great tooling
- ✅ LINQ support for type-safe queries
- ✅ Interceptors for cross-cutting concerns
- ❌ Performance overhead for simple queries

**Wolverine over MediatR**
- ✅ Source generators (zero reflection)
- ✅ Better performance
- ✅ Native async support
- ❌ Smaller ecosystem

**Mapperly over AutoMapper**
- ✅ Compile-time mapping (no reflection)
- ✅ Better performance
- ✅ Type-safe
- ❌ Less flexible for complex mappings

### Frontend

**React 19 over Angular/Vue**
- ✅ Largest ecosystem
- ✅ Great TypeScript support
- ✅ Flexible architecture
- ❌ Requires more decisions

**Tailwind CSS over Bootstrap/Material UI**
- ✅ Utility-first, highly customizable
- ✅ Smaller bundle size
- ✅ Better DX with JIT compiler
- ❌ Steeper learning curve

**shadcn/ui over MUI/Ant Design**
- ✅ Copy-paste components (own the code)
- ✅ Radix UI primitives (accessible)
- ✅ Tailwind-based styling
- ❌ Requires manual updates

### Infrastructure

**Finbuckle.MultiTenant**
- ✅ Battle-tested multi-tenancy
- ✅ EF Core integration
- ✅ Flexible strategies
- ❌ Learning curve

**Hangfire over Quartz.NET**
- ✅ Built-in dashboard
- ✅ SQL Server persistence
- ✅ Simple API
- ❌ Not as flexible as Quartz

**FusionCache over IMemoryCache**
- ✅ Hybrid L1/L2 caching
- ✅ Stampede protection
- ✅ Fail-safe
- ❌ Additional dependency

---

## See Also

- [Architecture Decisions](decisions/README.md) - ADRs for tech choices
- [PROJECT_INDEX.md](PROJECT_INDEX.md) - Project structure
- [KNOWLEDGE_BASE.md](KNOWLEDGE_BASE.md) - Codebase reference

---

**Last Updated:** 2026-01-23
**Maintainer:** NOIR Team
