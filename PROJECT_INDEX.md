# NOIR Project Index

**Generated**: 2026-01-20 20:52:00
**Version**: 2.1 (Multi-Tenant Filter & React 19 Migration)

---

## 📁 Project Structure

```
NOIR/
├── src/
│   ├── NOIR.Domain/              # Core domain entities, value objects
│   ├── NOIR.Application/         # CQRS commands/queries, specifications
│   ├── NOIR.Infrastructure/      # EF Core, Identity, services
│   └── NOIR.Web/                 # Minimal API + React frontend
│       └── frontend/             # React TypeScript SPA
├── tests/
│   ├── NOIR.Domain.UnitTests/    # 838 domain tests
│   ├── NOIR.Application.UnitTests/ # 2,944 application tests
│   ├── NOIR.IntegrationTests/    # 515 integration tests
│   └── NOIR.ArchitectureTests/   # 25 architecture tests
└── docs/
    ├── backend/patterns/         # Backend patterns documentation
    ├── backend/research/         # Research and analysis
    ├── frontend/                 # Frontend guide
    └── KNOWLEDGE_BASE.md         # Comprehensive codebase guide
```

---

## 🚀 Entry Points

**Backend**:
- Main API: `src/NOIR.Web/Program.cs` - Minimal API with Wolverine
- DbContext: `src/NOIR.Infrastructure/Persistence/ApplicationDbContext.cs`
- Seeder: `src/NOIR.Infrastructure/Persistence/ApplicationDbContextSeeder.cs`

**Frontend**:
- Entry: `src/NOIR.Web/frontend/src/main.tsx` - React 19 + TypeScript
- Routing: `src/NOIR.Web/frontend/src/App.tsx` - React Router v7

**Tests**:
- Run all: `dotnet test src/NOIR.sln`
- Integration: `tests/NOIR.IntegrationTests/`

---

## 📦 Core Domain Patterns

### Platform/Tenant Pattern (v2.0)
**Base Classes**:
- `PlatformTenantEntity<TId>` - Entities without domain events
- `PlatformTenantAggregateRoot<TId>` - Aggregates with domain events
- `ISeedableEntity` - Version-based seed updates

**Key Entities**:
- `EmailTemplate` - Platform email templates with tenant overrides
- `TenantSetting` - Platform settings with tenant customization
- `PermissionTemplate` - Platform permission presets

**Pattern Features**:
- TenantId = null → Platform default (shared)
- TenantId = value → Tenant override (copy-on-edit)
- Filtered indexes for 2-3x faster platform queries
- Smart seed updates (Version tracking)

### Database Constants
**Location**: `src/NOIR.Domain/Common/DatabaseConstants.cs`
- `TenantIdMaxLength = 64` - Standard across all entities
- `UserIdMaxLength = 450` - ASP.NET Identity compatibility

**Adoption**: 100% of configuration files (66 usages)

---

## 🔧 Architecture Layers

### Domain Layer (`NOIR.Domain`)
**Purpose**: Core business logic, entities, value objects

**Key Components**:
- `Entity<TId>` - Base entity (Id, timestamps)
- `AggregateRoot<TId>` - DDD aggregate with domain events
- `Result<T>` - Railway-oriented error handling
- `IAuditableEntity` - Soft delete + audit fields

**Entities**: 30+ domain entities including Tenant, User, Permission, Post, Media, Notification

### Application Layer (`NOIR.Application`)
**Purpose**: CQRS commands/queries, specifications, DTOs

**Pattern**: Vertical slice architecture
- Commands: `Features/{Feature}/Commands/{Action}/`
- Queries: `Features/{Feature}/Queries/{Action}/`
- Co-located: Handler + Validator + Command in same folder

**Key Features**:
- Specifications for all queries (no raw DbSet access)
- FluentValidation for all commands
- MediatR pipeline behaviors (logging, validation, audit)

### Infrastructure Layer (`NOIR.Infrastructure`)
**Purpose**: EF Core, ASP.NET Identity, external services

**Key Services**:
- `ApplicationDbContext` - EF Core DbContext
- `UserIdentityService` - Identity operations
- `EmailService` - Email with templating
- `Repository<T>` - Generic repository pattern

### Web Layer (`NOIR.Web`)
**Purpose**: Minimal API endpoints, middleware, React SPA

**Backend**:
- Wolverine message bus for CQRS
- Minimal API endpoints
- JWT authentication
- Multi-tenancy (Finbuckle)

**Frontend**:
- React 19 + TypeScript
- TanStack Query for data fetching
- shadcn/ui + Radix UI components
- Tailwind CSS v4

---

## 🧪 Test Coverage

**Total Tests**: 4,322 (100% passing)

| Test Suite | Count | Purpose |
|------------|-------|---------|
| Domain.UnitTests | 838 | Entity behavior, business rules |
| Application.UnitTests | 2,944 | Command/query handlers, specs |
| IntegrationTests | 515 | API endpoints, database |
| ArchitectureTests | 25 | Dependency rules, naming |

**Run Tests**:
```bash
dotnet test src/NOIR.sln
```

---

## 🔗 Key Dependencies

**Backend**:
- .NET 10.0 - Runtime
- EF Core 10.0 - ORM
- Wolverine 3.x - CQRS message bus
- Finbuckle.MultiTenant - Multi-tenancy
- FluentValidation - Input validation
- Serilog - Structured logging

**Frontend**:
- React 19.2 - UI library
- TypeScript 5.9 - Type safety
- TanStack Query - Server state
- React Router 7 - Routing
- shadcn/ui + Radix UI - Component libraries
- Tailwind CSS 4.1 - Styling

---

## 📚 Key Documentation

| Document | Purpose |
|----------|---------|
| `CLAUDE.md` | Claude Code instructions |
| `docs/KNOWLEDGE_BASE.md` | Comprehensive codebase guide (v2.0) |
| `docs/backend/patterns/` | Backend architectural patterns |
| `docs/backend/patterns/hierarchical-audit-logging.md` | Activity timeline audit pattern |
| `docs/backend/patterns/jwt-refresh-token.md` | JWT + refresh token flow |
| `docs/frontend/README.md` | Frontend development guide |

---

## 📝 Recent Changes

### v2.1 - Multi-Tenant Filter & React 19 Migration (2026-01-20)

**Multi-Tenant Filter & Notification Fix**:
1. RefreshToken Filter Exclusion: Excluded RefreshToken from multi-tenant query filter (user-scoped, not tenant-scoped)
2. Notification Platform Admin Skip: Added check in NotificationService to skip notification creation for platform admins (TenantId = null)
3. Fixed NullReferenceException for platform admins accessing notifications and login endpoints
4. Architectural Decision: Notifications ARE tenant-scoped, so they remain subject to tenant filtering
5. Migrations: `20260120124230_ExcludeRefreshTokenFromMultiTenantFilter`, `20260120135707_RevertNotificationTenantIdToNotNull`

**React 19 Tooltip Migration** (Commit: cc3d713):
1. Migrated TippyTooltip and RichTooltip from Tippy.js to Radix UI
2. Fixed React 19 deprecation warnings ("Accessing element.ref was removed")
3. Maintained backward-compatible API (no consumer changes required)
4. Benefits: No warnings, better a11y, smaller bundle, consistent with Radix UI usage

**Impact**:
- ✅ Platform admins can login successfully (RefreshToken fix)
- ✅ Platform admins won't receive notifications (architecturally correct - they have no tenant)
- ✅ Tenant isolation security maintained for notifications
- ✅ No React 19 console warnings
- ✅ 100% test pass rate maintained (4,322 tests)

### v2.0 - Platform/Tenant Pattern Optimization (2026-01-20)

**Platform/Tenant Pattern Optimization**:
1. Created `DatabaseConstants.cs` for schema consistency
2. Added `PlatformTenantEntity<TId>` and `PlatformTenantAggregateRoot<TId>` base classes
3. Refactored EmailTemplate, TenantSetting, PermissionTemplate to use new bases
4. Added filtered indexes for 2-3x faster platform default queries
5. Updated 20+ configuration files to use DatabaseConstants (100% adoption)
6. Updated KNOWLEDGE_BASE.md with comprehensive platform/tenant documentation

**Schema Changes**:
- TenantSettings.TenantId: 36→64 characters
- PermissionTemplates.TenantId: 500→64 characters (type changed Guid?→string?)
- Added filtered indexes: `IX_EmailTemplates_Platform_Lookup`, `IX_TenantSettings_Platform_Lookup`, `IX_PermissionTemplates_Platform_Lookup`

---

## 🚀 Quick Start

**Prerequisites**:
- .NET 10 SDK
- Node.js 20+
- SQL Server (LocalDB or full)

**Setup**:
```bash
# Clone repository
git clone https://github.com/your-org/NOIR.git
cd NOIR

# Run startup script
./start-dev.sh  # macOS/Linux
# or
start-dev.bat   # Windows

# Access application
# Frontend: http://localhost:3000
# Backend: http://localhost:4000
# Admin: admin@noir.local / 123qwe
```

**Manual Setup**:
```bash
# Backend
cd src/NOIR.Web
dotnet run

# Frontend (separate terminal)
cd src/NOIR.Web/frontend
npm install
npm run dev
```

---

## 📊 Repository Statistics

- **Languages**: C# (Backend), TypeScript (Frontend)
- **Total Tests**: 4,322 tests (100% passing)
- **Build Time**: ~30 seconds
- **Test Time**: ~2.5 minutes (all tests)
- **Configuration Files**: 20+ EF Core configurations
- **Database Migrations**: 5 migrations (latest: RevertNotificationTenantIdToNotNull)
- **Documentation Pages**: 15+ markdown files

---

## 🔐 Authentication & Authorization

**Authentication**: JWT + Refresh Token pattern
- Access tokens (15 min expiry)
- Refresh tokens (7 day expiry)
- HttpOnly cookies for security

**Authorization**: RBAC with hierarchical roles
- Permission-based authorization
- Tenant-scoped permissions
- System vs tenant roles

**Multi-Tenancy**: Finbuckle.MultiTenant
- String-based tenant IDs (max 64 chars)
- Host-based tenant resolution
- Query filter for tenant isolation
- Excluded entities: Audit logs (system-level operations), RefreshToken (user-scoped sessions)

---

**Index Version**: 2.1 (Multi-Tenant Filter & React 19 Migration)
**Last Updated**: 2026-01-20 20:52:00
