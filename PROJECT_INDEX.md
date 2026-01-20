# Project Index: NOIR

**Generated:** 2026-01-20
**Type:** Enterprise .NET 10 + React 19 SaaS Foundation
**Architecture:** Clean Architecture, CQRS, DDD, Multi-Tenant
**Tests:** 4,317 passing ✅ (833 Domain + 2,944 Application + 25 Architecture + 515 Integration)

---

## 📊 Codebase Metrics

| Metric | Count | Notes |
|--------|-------|-------|
| **Backend Files** | 562 | C# source files (excluding generated) |
| **Frontend Files** | 163 | TypeScript/TSX files |
| **Test Files** | 246 | Comprehensive test coverage |
| **Total Tests** | 4,317 | 100% passing |
| **Documentation** | 38 | Markdown files in `docs/` |
| **AI Memories** | 13 | Serena context in `.serena/memories/` |

---

## 🚀 Quick Start

### Development Mode (Hot Reload)

```bash
# Option 1: Use startup scripts (recommended)
./start-dev.sh          # macOS/Linux
start-dev.bat           # Windows

# Option 2: Manual startup
# Terminal 1 - Backend
dotnet run --project src/NOIR.Web

# Terminal 2 - Frontend
cd src/NOIR.Web/frontend && npm install && npm run dev
```

### Production Build

```bash
dotnet build -c Release src/NOIR.sln
dotnet run --project src/NOIR.Web --configuration Release
```

### Run Tests

```bash
dotnet test src/NOIR.sln                    # All 4,317 tests
dotnet test --filter "Category=Unit"        # Unit tests only
dotnet test --filter "Category=Integration" # Integration tests only
```

### Admin Login
- **Email:** `admin@noir.local`
- **Password:** `123qwe` (6 chars, configurable in appsettings.json)

---

## 🌐 URLs

| URL | Purpose | Environment |
|-----|---------|-------------|
| `http://localhost:3000` | Frontend SPA + API proxy | Development |
| `http://localhost:4000` | Backend API (direct) | Development |
| `http://localhost:3000/api/docs` | OpenAPI/Scalar documentation | Development |
| `http://localhost:3000/hangfire` | Background jobs dashboard | Development |

---

## 📁 Project Structure

```
NOIR/
├── src/
│   ├── NOIR.Domain/           # Core entities, value objects, domain events
│   │   ├── Entities/          # 15+ domain entities
│   │   ├── Common/            # Base classes, interfaces
│   │   ├── Interfaces/        # IRepository, IUnitOfWork
│   │   ├── Enums/             # Domain enums
│   │   └── Specifications/    # ISpecification base
│   │
│   ├── NOIR.Application/      # Use cases, DTOs, specifications
│   │   ├── Features/          # Vertical slices (Command + Handler + Validator)
│   │   │   ├── Auth/          # Login, Register, RefreshToken, etc.
│   │   │   ├── Users/         # User management
│   │   │   ├── Roles/         # Role management
│   │   │   ├── Permissions/   # Permission management
│   │   │   ├── Audit/         # Audit trail queries
│   │   │   ├── EmailTemplates/# Email template management
│   │   │   ├── Tenants/       # Multi-tenant management
│   │   │   ├── Notifications/ # User notifications
│   │   │   └── Blog/          # Blog posts, categories, tags
│   │   ├── Common/            # Shared interfaces, exceptions
│   │   ├── Specifications/    # Query specifications
│   │   └── Behaviors/         # Wolverine middleware
│   │
│   ├── NOIR.Infrastructure/   # External concerns, persistence
│   │   ├── Persistence/       # EF Core, DbContext, configurations
│   │   ├── Identity/          # ASP.NET Core Identity, authorization
│   │   ├── Services/          # Email, background jobs, device fingerprint
│   │   ├── Email/             # FluentEmail setup
│   │   └── Audit/             # Audit retention, before-state provider
│   │
│   └── NOIR.Web/              # API endpoints, middleware, frontend
│       ├── Endpoints/         # Minimal API endpoints
│       ├── Middleware/        # Exception handling, security headers
│       ├── Filters/           # Hangfire authorization
│       ├── Hubs/              # SignalR hubs
│       ├── Resources/         # Localization (en, vi)
│       ├── Program.cs         # Entry point
│       ├── appsettings.json   # Configuration
│       └── frontend/          # React 19 SPA
│           ├── src/
│           │   ├── pages/     # Page components (React Router 7)
│           │   ├── components/# Reusable UI components
│           │   ├── lib/       # Utilities, API client
│           │   ├── hooks/     # Custom React hooks
│           │   └── types/     # TypeScript types
│           ├── vite.config.ts # Vite configuration
│           └── package.json   # Frontend dependencies
│
├── tests/
│   ├── NOIR.Domain.UnitTests/       # 833 tests - Domain logic
│   ├── NOIR.Application.UnitTests/  # 2,944 tests - Application logic
│   ├── NOIR.ArchitectureTests/      # 25 tests - Architecture rules
│   └── NOIR.IntegrationTests/       # 515 tests - End-to-end flows
│
├── docs/
│   ├── backend/
│   │   ├── patterns/          # 8 pattern documents
│   │   └── research/          # 5 research documents
│   ├── frontend/              # 9 frontend guides
│   ├── decisions/             # 3 Architecture Decision Records
│   ├── plans/                 # 4 feature implementation plans
│   ├── KNOWLEDGE_BASE.md      # Centralized knowledge
│   └── PROJECT_INDEX.md       # This file
│
├── .serena/memories/          # 13 AI context memories
├── .github/                   # GitHub templates, workflows
├── CLAUDE.md                  # Claude Code instructions
├── AGENTS.md                  # Universal AI agent instructions
├── README.md                  # Project overview
├── SETUP.md                   # Detailed setup guide
└── start-dev.{sh,bat}         # Development startup scripts
```

---

## 🎯 Entry Points

| Entry Point | Path | Purpose |
|-------------|------|---------|
| **Backend API** | `src/NOIR.Web/Program.cs` | .NET 10 Minimal API with Wolverine |
| **Frontend SPA** | `src/NOIR.Web/frontend/src/main.tsx` | React 19 application entry |
| **Solution File** | `src/NOIR.sln` | All C# projects |
| **Frontend Config** | `src/NOIR.Web/frontend/vite.config.ts` | Vite bundler configuration |

---

## 📦 Core Modules

### Domain Layer (`src/NOIR.Domain/`)

| Module | Count | Key Types |
|--------|-------|-----------|
| **Entities** | 15 | `EntityAuditLog`, `HandlerAuditLog`, `HttpRequestAuditLog`, `Permission`, `RefreshToken`, `ResourceShare`, `EmailTemplate`, `AuditRetentionPolicy`, `PasswordResetOtp`, `EmailChangeOtp`, `Post`, `PostCategory`, `PostTag`, `Notification`, `TenantSetting` |
| **Common** | 18 | `Entity<TId>`, `AuditableEntity`, `AggregateRoot`, `ValueObject`, `Result<T>`, `Error`, `IAuditableEntity`, `ITenantEntity`, `IResource` |
| **Interfaces** | 2 | `IRepository<TEntity, TId>`, `IUnitOfWork` |
| **Specifications** | 1 | `ISpecification<T>` (base interface) |
| **Enums** | 5 | `PostStatus`, `NotificationType`, `NotificationCategory`, `TenantRole`, `AuditOperationType` |

**Key Patterns:**
- Entity base classes with typed IDs (Guid, int)
- Soft delete support (`IsDeleted`, `DeletedAt`, `DeletedBy`)
- Multi-tenant support (`ITenantEntity`)
- Domain events (`IDomainEvent`, `AggregateRoot`)
- Resource-based permissions (`IResource`)

### Application Layer (`src/NOIR.Application/`)

| Feature Area | Commands | Queries | Total Handlers |
|--------------|----------|---------|----------------|
| **Auth** | 10 | 3 | 13 |
| **Users** | 4 | 3 | 7 |
| **Roles** | 3 | 2 | 5 |
| **Permissions** | 2 | 2 | 4 |
| **Audit** | 0 | 5 | 5 |
| **EmailTemplates** | 3 | 2 | 5 |
| **Tenants** | 3 | 2 | 5 |
| **Notifications** | 4 | 3 | 7 |
| **Blog** | 10 | 4 | 14 |
| **TOTAL** | **39** | **26** | **65** |

**Auth Features:**
- Login, Logout, Register
- RefreshToken (cookie + JWT)
- ChangePassword, ResetPassword (OTP-based)
- ChangeEmail (OTP-based with cooldown)
- UpdateProfile, UpdateAvatar
- GetActiveSessions, RevokeSession

**Supporting Infrastructure:**
- `Specifications/` - 25+ query specifications for EF Core
- `Behaviors/` - Wolverine middleware (Logging, Performance, Validation, Audit)
- `Common/Interfaces/` - Service abstractions (Email, DateTime, DeviceFingerprint, etc.)
- `Common/Exceptions/` - Custom exceptions (Validation, NotFound, ForbiddenAccess)

### Infrastructure Layer (`src/NOIR.Infrastructure/`)

| Module | Files | Purpose |
|--------|-------|---------|
| **Persistence** | 40+ | EF Core DbContext, configurations, interceptors, repositories |
| **Identity** | 15+ | ASP.NET Core Identity, JWT, authorization, permissions |
| **Services** | 10+ | Email, background jobs, device fingerprint, localization |
| **Email** | 3 | FluentEmail, SMTP, template rendering |
| **Audit** | 4 | Audit retention job, before-state provider |

**Key Services:**
- `UserIdentityService` - ASP.NET Core Identity wrapper
- `TokenService` - JWT generation/validation
- `CookieAuthService` - Cookie-based authentication
- `RefreshTokenService` - Refresh token management
- `EmailService` - Template-based email sending
- `BackgroundJobsService` - Hangfire job scheduling
- `DeviceFingerprintService` - Device tracking
- `JsonLocalizationService` - i18n support (en, vi)
- `ResourceAuthorizationService` - Resource-level permissions

### Web Layer (`src/NOIR.Web/`)

| Module | Files | Purpose |
|--------|-------|---------|
| **Endpoints** | 15+ | Minimal API endpoints (Auth, Users, Roles, Audit, etc.) |
| **Middleware** | 5 | Exception handling, security headers, audit logging |
| **Hubs** | 2 | SignalR hubs (Notifications, DeveloperLogs) |
| **Filters** | 1 | Hangfire authorization |
| **Resources/Localization** | 2 | JSON localization files (en, vi) |

**Frontend (React 19 SPA):**
- **Pages:** 25+ (Auth, Dashboard, Users, Roles, Audit, Blog, etc.)
- **Components:** 50+ UI components (shadcn/ui + custom)
- **Hooks:** 15+ custom hooks (auth, API, form, etc.)
- **API Client:** Type-safe generated from OpenAPI spec
- **Routing:** React Router 7 with nested layouts
- **State:** React hooks + Zustand (minimal global state)
- **Styling:** Tailwind CSS 4 + CSS variables (dark/light mode)

---

## 🔧 Configuration

| File | Purpose |
|------|---------|
| `appsettings.json` | **Production** settings (DB, Identity, JWT, Email, Hangfire) |
| `appsettings.Development.json` | **Development** overrides (weak passwords, local SMTP) |
| `launchSettings.json` | Development server profiles |
| `vite.config.ts` | Frontend bundler config (proxy, plugins) |
| `tsconfig.json` | TypeScript compiler options |
| `tailwind.config.js` | Tailwind CSS configuration |
| `eslint.config.js` | ESLint rules for frontend |

**Key Settings:**
- **Password Policy:** 6 chars minimum (dev), configurable for production
- **JWT:** 60-minute access tokens, 7-day refresh tokens
- **Multi-Tenancy:** Database-per-tenant isolation via Finbuckle
- **Audit Retention:** Configurable per category (default: 90 days)

---

## 📚 Documentation

### Backend Documentation (`docs/backend/`)

**Patterns:**
1. `bulk-operations.md` - EF Core BulkExtensions patterns
2. `di-auto-registration.md` - Auto-registration via marker interfaces
3. `entity-configuration.md` - EF Core entity configurations
4. `jwt-refresh-token.md` - Token-based authentication
5. `repository-specification.md` - Repository + Specification pattern
6. `hierarchical-audit-logging.md` - 3-level audit system
7. `json-enum-serialization.md` - String enum serialization
8. `before-state-resolver.md` - Audit before-state tracking

**Research:**
1. `hierarchical-audit-logging-comparison-2025.md`
2. `role-permission-best-practices-2025.md`
3. `cache-busting-best-practices.md`
4. `developer-log-system-research.md`
5. `validation-unification-plan.md`

### Frontend Documentation (`docs/frontend/`)

1. `README.md` - Overview and conventions
2. `architecture.md` - Frontend structure
3. `api-types.md` - Type generation from OpenAPI
4. `theme.md` - Dark/light mode implementation
5. `localization-guide.md` - i18n setup
6. `vibe-kanban-integration.md` - Task management UI
7. `COLOR_SCHEMA_GUIDE.md` - Color system
8. `designs/notification-dropdown-ui-design.md`
9. `designs/developer-log-ui-ux-design.md`

### Architecture Decision Records (`docs/decisions/`)

1. `001-tech-stack.md` - Technology choices
2. `002-frontend-ui-stack.md` - UI library selection
3. `003-vertical-slice-cqrs.md` - CQRS approach

### Knowledge Base

- `docs/KNOWLEDGE_BASE.md` - Centralized patterns, gotchas, best practices

---

## 🧪 Test Coverage

| Test Suite | Files | Tests | Coverage |
|------------|-------|-------|----------|
| **Domain.UnitTests** | 15 | 833 | Entity logic, value objects, domain events |
| **Application.UnitTests** | 110+ | 2,944 | Handlers, validators, specifications, services |
| **ArchitectureTests** | 3 | 25 | Dependency rules, naming conventions |
| **IntegrationTests** | 118+ | 515 | End-to-end API flows with real DB |
| **TOTAL** | **246** | **4,317** | **100% passing** ✅ |

**Test Frameworks:**
- xUnit for all test suites
- FluentAssertions for assertions
- NSubstitute for mocking
- In-memory database for unit tests
- LocalDB for integration tests

---

## 🔗 Key Dependencies

### Backend

| Package | Version | Purpose |
|---------|---------|---------|
| **.NET** | 10.0 | Runtime framework |
| **EF Core** | 10.0 | ORM, database access |
| **Wolverine** | 5.9+ | Mediator, message bus |
| **FluentValidation** | 11.x | Input validation |
| **Mapperly** | 4.3+ | Compile-time mapping |
| **Finbuckle.MultiTenant** | 9.0+ | Multi-tenancy |
| **Hangfire** | 1.8+ | Background jobs |
| **Serilog** | 4.x | Structured logging |
| **FluentEmail** | 3.x | Email sending |
| **Scalar** | Latest | OpenAPI UI |

### Frontend

| Package | Version | Purpose |
|---------|---------|---------|
| **React** | 19 | UI framework |
| **TypeScript** | 5.x | Type safety |
| **Vite** | 6.x | Build tool |
| **React Router** | 7.x | Routing |
| **Tailwind CSS** | 4.x | Styling |
| **shadcn/ui** | Latest | UI components |
| **TanStack Query** | 5.x | Data fetching |
| **Zod** | 3.x | Schema validation |
| **React Hook Form** | 7.x | Form management |

---

## 🎨 Code Patterns

### Backend

**Vertical Slice CQRS:**
```
Features/
  └── Users/
      ├── Commands/
      │   └── CreateUser/
      │       ├── CreateUserCommand.cs       (DTO)
      │       ├── CreateUserCommandHandler.cs (Logic)
      │       └── CreateUserCommandValidator.cs (Validation)
      └── Queries/
          └── GetUsers/
              ├── GetUsersQuery.cs           (DTO)
              └── GetUsersQueryHandler.cs    (Logic)
```

**Auto-Registration via Marker Interfaces:**
```csharp
public class CustomerService : ICustomerService, IScopedService { }
// Automatically registered as Scoped
```

**Specification Pattern:**
```csharp
public class ActiveCustomersSpec : Specification<Customer>
{
    public ActiveCustomersSpec(string? search = null)
    {
        Query.Where(c => c.IsActive)
             .TagWith("GetActiveCustomers");  // SQL debugging
    }
}
```

**Unit of Work:**
```csharp
await _repository.AddAsync(customer, ct);
await _unitOfWork.SaveChangesAsync(ct);  // Required!
```

### Frontend

**Page Component Pattern:**
```tsx
export default function UsersPage() {
  usePageContext('Users')  // For Activity Timeline
  const { data, isLoading } = useQuery(...)

  return <PageLayout>...</PageLayout>
}
```

**Form Validation:**
```tsx
const form = useForm({
  resolver: zodResolver(schema),
  mode: 'onBlur'  // Real-time validation
})
```

---

## 🚦 Quick Commands

```bash
# Build
dotnet build src/NOIR.sln
dotnet build -c Release

# Run
dotnet run --project src/NOIR.Web
dotnet watch --project src/NOIR.Web  # Hot reload

# Tests
dotnet test
dotnet test --filter "Category=Unit"
dotnet test --logger "console;verbosity=detailed"

# Migrations
dotnet ef migrations add NAME --project src/NOIR.Infrastructure --startup-project src/NOIR.Web
dotnet ef database update --project src/NOIR.Infrastructure --startup-project src/NOIR.Web
dotnet ef database drop --force --project src/NOIR.Infrastructure --startup-project src/NOIR.Web

# Frontend
cd src/NOIR.Web/frontend
npm install
npm run dev           # Development
npm run build         # Production build
npm run type-check    # TypeScript check
npm run lint          # ESLint
```

---

## 📊 Token Efficiency

**Before Index:**
- Reading all files: ~58,000 tokens per session
- Context pollution from unnecessary files

**After Index:**
- Reading this index: ~5,000 tokens
- **Savings: 91%** (53,000 tokens per session)
- **ROI**: Break-even after 1 session
- **100 sessions**: 5,300,000 tokens saved

---

## 🎯 Development Workflow

1. **Start Services:** Run `./start-dev.sh` or `start-dev.bat`
2. **Check Logs:** Backend logs in terminal, frontend at `http://localhost:3000`
3. **Run Tests:** `dotnet test` before committing
4. **Create Migration:** After entity changes
5. **Update Types:** `npm run generate:api` after backend API changes
6. **Commit:** Follow conventional commits (feat:, fix:, docs:, etc.)

---

**Last Updated:** 2026-01-20
**Maintained By:** Claude Code + Human Developer
**Index Version:** 2.0
