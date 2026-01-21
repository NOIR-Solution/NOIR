# Project Index: NOIR

**Generated:** 2026-01-21 09:45 GMT+7
**Version:** 2.1
**Architecture:** Clean Architecture + CQRS + Multi-Tenancy

> **Token Efficiency:** Reading this index (~3,000 tokens) vs. reading all files (~58,000 tokens) = **94% reduction**

---

## 📋 Quick Reference

| Metric | Value |
|--------|-------|
| **Backend** | .NET 10, EF Core 10, SQL Server |
| **Frontend** | React 19, TypeScript, Vite, Tailwind CSS 4 |
| **Tests** | 2,100+ tests (Unit + Integration + Architecture) |
| **Source Files** | ~600 files (Backend: 400, Frontend: 162) |
| **Admin Login** | `admin@noir.local` / `123qwe` |

---

## 🚀 Entry Points

### CLI Commands

```bash
# Build & Run
dotnet build src/NOIR.sln
dotnet run --project src/NOIR.Web
dotnet watch --project src/NOIR.Web

# Tests
dotnet test src/NOIR.sln

# Frontend
cd src/NOIR.Web/frontend
npm install && npm run dev

# Start Scripts (RECOMMENDED)
./start-dev.sh           # macOS/Linux
start-dev.bat            # Windows
```

### Application Entry

- **Backend:** `src/NOIR.Web/Program.cs` - ASP.NET Core application startup
- **Frontend:** `src/NOIR.Web/frontend/src/main.tsx` - React application entry
- **Database:** `src/NOIR.Infrastructure/Persistence/ApplicationDbContext.cs` - EF Core context

### URLs

- **Frontend:** http://localhost:3000
- **Backend API:** http://localhost:4000
- **API Docs:** http://localhost:3000/api/docs (Scalar)
- **Hangfire Dashboard:** http://localhost:4000/hangfire

---

## 📁 Project Structure

```
NOIR/
├── src/                          # Source code (4 projects)
│   ├── NOIR.Domain/              # 60 files - Core business entities
│   ├── NOIR.Application/         # 280 files - Business logic (CQRS)
│   ├── NOIR.Infrastructure/      # 180 files - External integrations
│   └── NOIR.Web/                 # 47 files - API + Frontend host
│       └── frontend/             # 162 files - React SPA
├── tests/                        # 2,100+ tests (4 projects)
│   ├── NOIR.Domain.UnitTests/
│   ├── NOIR.Application.UnitTests/
│   ├── NOIR.IntegrationTests/
│   └── NOIR.ArchitectureTests/
├── docs/                         # Documentation
│   ├── backend/                  # Backend patterns & guides
│   ├── frontend/                 # Frontend architecture
│   ├── decisions/                # Architecture Decision Records
│   ├── ARCHITECTURE.md           # System architecture overview
│   └── KNOWLEDGE_BASE.md         # Comprehensive codebase guide
├── .serena/                      # Serena MCP memories
├── CLAUDE.md                     # AI assistant instructions
├── AGENTS.md                     # Universal AI guidelines
└── start-dev.sh                  # Development startup script
```

---

## 📦 Core Modules

### NOIR.Domain (60 files)

**Purpose:** Core business entities and domain logic
**Target Framework:** .NET 10
**Dependencies:** Finbuckle.MultiTenant.Abstractions only

**Key Exports:**

```
Common/
├── Entity<TId>                   # Base entity with ID
├── AggregateRoot<TId>            # Base for domain events
├── AuditableEntity<TId>          # Entity with audit fields
├── ITenantEntity                 # Multi-tenant marker
├── Result<T>                     # Error handling pattern
└── ValueObject                   # Immutable value objects

Entities/
├── User                          # Platform user
├── Tenant                        # Multi-tenant organization
├── Permission                    # Granular permission
├── Role                          # User role with hierarchy
├── EntityAuditLog                # Entity change tracking
├── HttpRequestAuditLog           # HTTP request tracking
├── HandlerAuditLog               # Command/Query handler tracking
├── RefreshToken                  # JWT refresh token
└── EmailTemplate                 # Database-driven email templates

Interfaces/
├── IRepository<TEntity, TId>     # Repository pattern
├── IUnitOfWork                   # Transaction management
└── ISpecification<T>             # Query specification
```

### NOIR.Application (280 files)

**Purpose:** Business logic with Vertical Slice CQRS
**Target Framework:** .NET 10
**Dependencies:** Domain, FluentValidation, Mapperly

**Key Features:**

```
Features/                         # Vertical slices by feature
├── Auth/
│   ├── Commands/
│   │   ├── Login/
│   │   │   ├── LoginCommand.cs
│   │   │   ├── LoginCommandHandler.cs
│   │   │   └── LoginCommandValidator.cs
│   │   ├── RefreshToken/
│   │   └── ChangePassword/
│   └── Queries/
│       ├── GetActiveSession/
│       └── GetCurrentUser/
├── Users/
│   ├── Commands/CreateUser/
│   ├── Commands/UpdateUser/
│   ├── Commands/DeleteUser/
│   └── Queries/GetUsers/
├── Roles/
├── Permissions/
├── Tenants/
├── Audit/
└── Notifications/

Specifications/                    # Query specifications
├── Users/ActiveUsersSpec.cs
├── Roles/RoleByIdSpec.cs
└── RefreshTokens/ExpiredRefreshTokensSpec.cs

Behaviors/                         # Pipeline middleware
├── PerformanceMiddleware.cs      # Request timing
├── LoggingMiddleware.cs          # Request/response logging
└── HandlerAuditMiddleware.cs     # Command/Query audit tracking
```

**Pattern:** Vertical Slice Architecture - Each feature is self-contained in one folder

### NOIR.Infrastructure (180 files)

**Purpose:** External integrations (Database, Identity, Email, Cache)
**Target Framework:** .NET 10
**Dependencies:** Application, EF Core, ASP.NET Identity, Hangfire

**Key Components:**

```
Persistence/
├── ApplicationDbContext.cs       # EF Core DbContext
├── Configurations/               # Entity configurations (IEntityTypeConfiguration)
├── Interceptors/
│   ├── AuditableEntityInterceptor.cs   # Auto-set audit fields
│   ├── DomainEventInterceptor.cs       # Dispatch domain events
│   └── TenantIdSetterInterceptor.cs    # Auto-set TenantId
├── Repositories/Repository.cs    # Generic repository implementation
└── SpecificationEvaluator.cs     # Execute specifications

Identity/
├── ApplicationUser.cs            # ASP.NET Identity user
├── TokenService.cs               # JWT token generation
├── RefreshTokenService.cs        # Refresh token management
├── UserIdentityService.cs        # User CRUD operations
└── Authorization/
    ├── PermissionPolicyProvider.cs        # Dynamic permissions
    ├── PermissionAuthorizationHandler.cs  # Permission checks
    └── ResourceAuthorizationHandler.cs    # Resource-level auth

Services/
├── EmailService.cs               # FluentEmail wrapper
├── FileStorageService.cs         # File upload/download
├── ImageProcessorService.cs      # Image resizing
├── LocalizationService.cs        # i18n support
└── BackgroundJobsService.cs      # Hangfire wrapper

Audit/
├── HttpRequestAuditMiddleware.cs        # HTTP audit logging
├── HandlerAuditMiddleware.cs            # Command/Query audit
└── WolverineBeforeStateProvider.cs      # Before-state capture
```

### NOIR.Web (47 files + frontend)

**Purpose:** ASP.NET Core API host + React SPA
**Target Framework:** .NET 10
**Dependencies:** Infrastructure, Wolverine

**Key Files:**

```
Program.cs                        # Application entry point
Endpoints/
├── AuthEndpoints.cs
├── UserEndpoints.cs
├── RoleEndpoints.cs
├── TenantEndpoints.cs
└── AuditEndpoints.cs

Middleware/
├── CurrentUserLoaderMiddleware.cs     # Load user from DB
├── ExceptionHandlingMiddleware.cs    # Global error handler
└── SecurityHeadersMiddleware.cs      # Security headers

frontend/                         # React 19 SPA (162 files)
├── src/
│   ├── pages/                    # Route components
│   │   ├── Landing.tsx
│   │   ├── Login.tsx
│   │   └── portal/
│   │       ├── Dashboard.tsx
│   │       └── admin/
│   │           ├── users/
│   │           ├── roles/
│   │           ├── tenants/
│   │           └── audit/
│   ├── components/               # Reusable components
│   │   ├── ui/                   # shadcn/ui + 21st.dev
│   │   ├── PermissionGate.tsx
│   │   └── ProtectedRoute.tsx
│   ├── services/                 # API client
│   │   ├── apiClient.ts          # Axios with interceptors
│   │   ├── auth.ts
│   │   └── users.ts
│   ├── hooks/                    # Custom React hooks
│   │   ├── usePermissions.ts
│   │   └── usePageContext.ts
│   ├── contexts/                 # React contexts
│   │   ├── AuthContext.tsx
│   │   └── ThemeContext.tsx
│   ├── types/                    # TypeScript types
│   │   ├── generated.ts          # Auto-generated from backend
│   │   └── auth.ts
│   └── lib/                      # Utilities
│       └── utils.ts
├── package.json                  # NPM dependencies
├── vite.config.ts                # Vite build config
└── tailwind.config.js            # Tailwind CSS 4 config
```

---

## 🧪 Test Coverage

### Test Projects (2,100+ tests)

| Project | Tests | Purpose |
|---------|-------|---------|
| **NOIR.Domain.UnitTests** | 150+ | Entity behavior, value objects, domain events |
| **NOIR.Application.UnitTests** | 1,200+ | Command/Query handlers, specifications, validators |
| **NOIR.IntegrationTests** | 700+ | End-to-end API tests with LocalDB |
| **NOIR.ArchitectureTests** | 50+ | NetArchTest rules (dependency violations) |

**Key Test Files:**

```
tests/
├── NOIR.Domain.UnitTests/
│   ├── Common/EntityTests.cs
│   ├── Common/ResultTests.cs
│   └── Entities/PermissionTests.cs
├── NOIR.Application.UnitTests/
│   ├── Features/Auth/LoginCommandHandlerTests.cs
│   ├── Features/Users/CreateUserCommandHandlerTests.cs
│   └── Specifications/ProjectionSpecificationTests.cs
├── NOIR.IntegrationTests/
│   ├── AuthEndpointsTests.cs
│   ├── UserEndpointsTests.cs
│   └── Persistence/RepositoryTests.cs
└── NOIR.ArchitectureTests/
    ├── DependencyTests.cs
    └── LayerTests.cs
```

**Run Tests:**

```bash
dotnet test src/NOIR.sln          # All tests
dotnet test --filter "FullyQualifiedName~Auth"  # Auth tests only
```

---

## 🔧 Configuration

### Backend Configuration

| File | Purpose |
|------|---------|
| `appsettings.json` | Default configuration |
| `appsettings.Development.json` | Dev overrides |
| `appsettings.Production.json` | Prod overrides |
| User Secrets | Sensitive dev config (`dotnet user-secrets`) |

**Key Settings:**

- **ConnectionStrings:DefaultConnection** - SQL Server connection
- **JwtSettings** - JWT signing key, expiration
- **EmailSettings** - SMTP configuration
- **HangfireSettings** - Background job config

### Frontend Configuration

| File | Purpose |
|------|---------|
| `vite.config.ts` | Vite build configuration |
| `tailwind.config.js` | Tailwind CSS 4 configuration |
| `tsconfig.json` | TypeScript compiler options |
| `.env.development` | Dev environment variables |
| `.env.production` | Prod environment variables |

---

## 🔗 Key Dependencies

### Backend

| Package | Version | Purpose |
|---------|---------|---------|
| .NET | 10 LTS | Framework (support until 2028) |
| EF Core | 10 | ORM |
| SQL Server | 2022 | Database |
| Wolverine | 3.x | CQRS messaging |
| FluentValidation | 11.x | Request validation |
| Mapperly | 3.x | DTO mapping (source gen) |
| Finbuckle.MultiTenant | 10.x | Multi-tenancy |
| ASP.NET Identity | 10 | Authentication |
| Hangfire | 1.8.x | Background jobs |
| Serilog | 10.x | Structured logging |

### Frontend

| Package | Version | Purpose |
|---------|---------|---------|
| React | 19 | UI library |
| TypeScript | 5.x | Type safety |
| Vite | Latest | Build tool & dev server |
| Tailwind CSS | 4 | Styling |
| React Router | 7 | Client-side routing |
| shadcn/ui | Latest | UI component primitives |
| i18next | Latest | Internationalization |
| axios | Latest | HTTP client |
| zod | 4.x | Schema validation |

---

## 📚 Documentation

### Quick Links

| Document | Purpose |
|----------|---------|
| [README.md](README.md) | Project overview and quick start |
| [CLAUDE.md](CLAUDE.md) | AI assistant instructions (Claude Code) |
| [AGENTS.md](AGENTS.md) | Universal AI agent guidelines |
| [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) | System architecture overview |
| [docs/KNOWLEDGE_BASE.md](docs/KNOWLEDGE_BASE.md) | Comprehensive codebase guide |
| [docs/backend/README.md](docs/backend/README.md) | Backend patterns & guides |
| [docs/frontend/README.md](docs/frontend/README.md) | Frontend architecture |
| [docs/decisions/](docs/decisions/) | Architecture Decision Records (ADRs) |

### Documentation Structure

```
docs/
├── ARCHITECTURE.md                    # System architecture (v1.1)
├── KNOWLEDGE_BASE.md                  # Complete codebase reference
├── API_INDEX.md                       # API endpoint documentation
├── backend/
│   ├── README.md
│   ├── patterns/
│   │   ├── repository-specification.md
│   │   ├── hierarchical-audit-logging.md
│   │   ├── bulk-operations.md
│   │   └── jwt-refresh-token.md
│   └── research/
│       ├── role-permission-best-practices-2025.md
│       └── hierarchical-audit-logging-comparison-2025.md
├── frontend/
│   ├── README.md
│   ├── api-types.md
│   └── theme.md
└── decisions/
    ├── 001-tech-stack.md
    ├── 002-frontend-ui-stack.md
    └── 003-vertical-slice-cqrs.md
```

---

## 🏗️ Architecture Overview

### Clean Architecture Layers

```
┌──────────────────────────────────────────────────────────┐
│                    Web Layer                             │
│  • API Endpoints (Minimal API)                           │
│  • Middleware (Auth, Audit, Exception)                   │
│  • Frontend SPA (React)                                  │
└─────────────────────┬────────────────────────────────────┘
                      │ depends on
┌─────────────────────┴────────────────────────────────────┐
│               Infrastructure Layer                       │
│  • EF Core DbContext & Repositories                      │
│  • ASP.NET Identity (UserManager, RoleManager)           │
│  • External Services (Email, Storage, Cache)             │
│  • Audit Interceptors                                    │
└─────────────────────┬────────────────────────────────────┘
                      │ implements
┌─────────────────────┴────────────────────────────────────┐
│                Application Layer                         │
│  • Features (Commands, Queries, Handlers)                │
│  • Specifications (Query criteria)                       │
│  • DTOs (Data Transfer Objects)                          │
│  • Validators (FluentValidation)                         │
│  • Behaviors (Logging, Performance)                      │
└─────────────────────┬────────────────────────────────────┘
                      │ depends on
┌─────────────────────┴────────────────────────────────────┐
│                  Domain Layer                            │
│  • Entities (User, Tenant, Permission)                   │
│  • Value Objects (Email, PhoneNumber)                    │
│  • Interfaces (IRepository, ISpecification)              │
│  • Domain Events                                         │
│  • Business Rules                                        │
└──────────────────────────────────────────────────────────┘
```

**Dependency Rule:** ↑ Layers can only depend on layers below them ↑

### Key Patterns

| Pattern | Where | Purpose |
|---------|-------|---------|
| **Clean Architecture** | Solution structure | Separation of concerns |
| **CQRS** | Application layer | Command/Query separation |
| **Vertical Slice** | Feature organization | Co-locate related code |
| **Repository** | Data access | Abstract persistence |
| **Specification** | Queries | Encapsulate query logic |
| **Unit of Work** | Transactions | Manage DB transactions |
| **Factory Method** | Entity creation | Controlled instantiation |
| **Domain Events** | Business logic | Decouple side effects |
| **Result Pattern** | Error handling | Railway-oriented programming |

---

## 🚦 Development Workflow

### 1. Build the Solution

```bash
dotnet build src/NOIR.sln
```

### 2. Run Backend

```bash
dotnet run --project src/NOIR.Web
# Or with hot reload:
dotnet watch --project src/NOIR.Web
```

### 3. Run Frontend

```bash
cd src/NOIR.Web/frontend
npm install && npm run dev
```

### 4. Run Tests

```bash
dotnet test src/NOIR.sln
```

### 5. Generate API Types (Frontend)

```bash
cd src/NOIR.Web/frontend
npm run generate:api
```

### 6. Migrations

```bash
# CRITICAL: Always specify --context!
dotnet ef migrations add NAME \
  --project src/NOIR.Infrastructure \
  --startup-project src/NOIR.Web \
  --context ApplicationDbContext \
  --output-dir Migrations/ApplicationDbContext
```

---

## 🔒 Security Architecture

### Authentication Flow

```
User Login → JWT + Refresh Token → HTTP-only Cookie
          ↓
     Verify JWT
          ↓
   Check Permissions
          ↓
    Access Granted
```

### Permission Format

`{resource}:{action}:{scope}`

**Examples:**
- `users:read:all` - View all users
- `users:write:own` - Create/update own tenant's users
- `audit:view:all` - View all audit logs

---

## 📊 Project Metrics

| Metric | Count |
|--------|-------|
| **Total Source Files** | ~600 |
| **Backend Files** | ~400 |
| **Frontend Files** | ~162 |
| **Test Files** | ~140 |
| **Total Tests** | 2,100+ |
| **C# Projects** | 8 (4 src + 4 tests) |
| **Database Tables** | 25+ |
| **API Endpoints** | 80+ |
| **React Components** | 50+ |

---

## 🎯 Quick Start for Contributors

### First-Time Setup

1. **Clone Repository**
   ```bash
   git clone https://github.com/NOIR-Solution/NOIR.git
   cd NOIR
   ```

2. **Install Dependencies**
   ```bash
   # Backend: .NET 10 SDK
   # Frontend: Node.js 20+
   # Database: SQL Server (LocalDB/Docker)
   ```

3. **Database Setup**
   ```bash
   dotnet ef database update \
     --project src/NOIR.Infrastructure \
     --startup-project src/NOIR.Web \
     --context ApplicationDbContext
   ```

4. **Run Application**
   ```bash
   ./start-dev.sh  # macOS/Linux
   start-dev.bat   # Windows
   ```

5. **Login**
   - URL: http://localhost:3000
   - Email: `admin@noir.local`
   - Password: `123qwe`

### Development Tips

- **Read [CLAUDE.md](CLAUDE.md)** for coding patterns and rules
- **Use Vertical Slice CQRS** for all new features
- **Always use Specifications** for database queries (never raw DbSet)
- **Tag all specs** with `TagWith("MethodName")` for SQL debugging
- **Implement IAuditableCommand** for user actions
- **Run tests** before committing: `dotnet test src/NOIR.sln`

---

## 📝 Naming Conventions

| Type | Pattern | Example |
|------|---------|---------|
| Entity | `PascalCase` | `ApplicationUser`, `Tenant` |
| Specification | `[Entity][Filter]Spec` | `ActiveUsersSpec` |
| Command | `[Action][Entity]Command` | `CreateUserCommand` |
| Query | `Get[Entity][Filter]Query` | `GetUsersQuery` |
| Handler | `[Command/Query]Handler` | `CreateUserCommandHandler` |
| DTO | `[Entity]Dto` | `UserDto`, `TenantDto` |
| Validator | `[Command]Validator` | `CreateUserCommandValidator` |

---

## 🔄 Workflow Commands

### Git Workflow

```bash
git checkout -b feature/my-feature
# Make changes
dotnet test src/NOIR.sln
git add .
git commit -m "feat: add my feature"
git push origin feature/my-feature
```

### Database Workflow

```bash
# Create migration
dotnet ef migrations add MyMigration \
  --project src/NOIR.Infrastructure \
  --startup-project src/NOIR.Web \
  --context ApplicationDbContext \
  --output-dir Migrations/ApplicationDbContext

# Apply migration
dotnet ef database update \
  --project src/NOIR.Infrastructure \
  --startup-project src/NOIR.Web \
  --context ApplicationDbContext

# Drop database (dev only)
dotnet ef database drop \
  --project src/NOIR.Infrastructure \
  --startup-project src/NOIR.Web \
  --context ApplicationDbContext \
  --force
```

---

## 🏆 Best Practices

### Backend

1. **Use Specifications** for all queries (never raw `DbSet`)
2. **Tag specifications** with `TagWith("MethodName")`
3. **Inject IUnitOfWork** for all mutations (Repository doesn't auto-save)
4. **Use AsTracking** for entities that will be modified
5. **Implement IAuditableCommand** for user actions
6. **Co-locate** Command + Handler + Validator in same folder
7. **Soft delete** by default (IsDeleted flag)
8. **Use marker interfaces** for DI (IScopedService, ITransientService)

### Frontend

1. **Use 21st.dev** for all new UI components
2. **Implement real-time validation** (react-hook-form + Zod)
3. **Check permissions** before rendering actions
4. **Call usePageContext** on every page for audit tracking
5. **Handle OTP errors** by clearing input on error
6. **Add cursor-pointer** to all interactive elements
7. **Prevent dropdown close** for multi-select (onSelect preventDefault)

---

## 🌐 Deployment

### Production Build

```bash
# Backend + Frontend
dotnet build -c Release src/NOIR.sln
```

Frontend is automatically built and copied to `wwwroot/` during Release build.

### Docker (Future)

```bash
docker build -t noir:latest .
docker run -p 80:80 noir:latest
```

---

## 🔍 Troubleshooting

### Common Issues

| Issue | Solution |
|-------|----------|
| **Build fails** | Run `dotnet clean src/NOIR.sln` then rebuild |
| **Database migration error** | Ensure `--context ApplicationDbContext` is specified |
| **Frontend 404** | Run `npm install` in `src/NOIR.Web/frontend` |
| **Tests failing** | Check if LocalDB/SQL Server is running |
| **JWT validation fails** | Check `appsettings.json` JwtSettings |

### Getting Help

- **Documentation:** [docs/](docs/)
- **Issues:** https://github.com/NOIR-Solution/NOIR/issues
- **Contributing:** [CONTRIBUTING.md](CONTRIBUTING.md)

---

## 📈 Token Economics

**Index Creation:** 2,000 tokens (one-time)
**Index Reading:** 3,000 tokens (every session)
**Full Codebase Read:** 58,000 tokens (every session without index)

**Break-even:** 1 session
**10 sessions savings:** 550,000 tokens
**100 sessions savings:** 5,500,000 tokens

---

*Last Updated: 2026-01-21 | Version: 2.1 | Clean Architecture + CQRS + Multi-Tenancy*
*For detailed architecture, see [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)*
*For code patterns, see [docs/KNOWLEDGE_BASE.md](docs/KNOWLEDGE_BASE.md)*
