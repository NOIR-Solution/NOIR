# Project Index: NOIR

**Generated:** 2026-01-19
**Type:** Enterprise .NET 10 + React SaaS Foundation
**Architecture:** Clean Architecture, CQRS, DDD

---

## Quick Start

```bash
# Build & Run
dotnet build src/NOIR.sln
dotnet run --project src/NOIR.Web

# Frontend (separate terminal)
cd src/NOIR.Web/frontend && npm install && npm run dev

# Tests (3,500+ tests)
dotnet test src/NOIR.sln

# Admin Login: admin@noir.local / 123qwe
```

| URL | Purpose |
|-----|---------|
| `http://localhost:3000` | Application (frontend + API via proxy) |
| `http://localhost:3000/api/docs` | API documentation (Scalar) |
| `http://localhost:3000/hangfire` | Background jobs dashboard |

---

## Project Structure

```
NOIR/
├── src/
│   ├── NOIR.Domain/           # Core entities, interfaces (no dependencies)
│   ├── NOIR.Application/      # Commands, queries, specifications, DTOs
│   ├── NOIR.Infrastructure/   # EF Core, handlers, external services
│   └── NOIR.Web/              # API endpoints, middleware
│       └── frontend/          # React 19 SPA
├── tests/
│   ├── NOIR.Domain.UnitTests/       # 488 tests
│   ├── NOIR.Application.UnitTests/  # 1,235 tests
│   ├── NOIR.ArchitectureTests/      # 25 tests
│   └── NOIR.IntegrationTests/       # 302 tests
├── docs/
│   ├── backend/patterns/      # 6 pattern docs
│   ├── backend/research/      # 3 research docs
│   ├── frontend/              # 7 frontend docs
│   └── decisions/             # 3 ADRs
└── .serena/memories/          # 11 AI context memories
```

---

## Entry Points

| Entry Point | Path | Purpose |
|-------------|------|---------|
| **Backend API** | `src/NOIR.Web/Program.cs` | .NET 10 minimal API |
| **Frontend** | `src/NOIR.Web/frontend/src/main.tsx` | React 19 SPA |
| **Solution** | `src/NOIR.sln` | All projects |

---

## Core Modules

### Domain Layer (`src/NOIR.Domain/`)

| Module | Files | Purpose |
|--------|-------|---------|
| `Entities/` | 13 | EntityAuditLog, HandlerAuditLog, HttpRequestAuditLog, Permission, RefreshToken, ResourceShare, EmailTemplate, AuditRetentionPolicy, PasswordResetOtp, Post, PostCategory, PostTag, Notification, NotificationPreference |
| `Common/` | 18 | Entity, AuditableEntity, AggregateRoot, ValueObject, Result, Permissions, Roles, ErrorCodes |
| `Interfaces/` | 2 | IRepository, IUnitOfWork |
| `Specifications/` | 1 | ISpecification base |
| `Enums/` | 4 | PostStatus, NotificationType, NotificationCategory, TenantRole |

### Application Layer (`src/NOIR.Application/`)

| Feature | Commands | Queries | Purpose |
|---------|----------|---------|---------|
| `Auth/` | 10 | 3 | Login, Logout, Register, RefreshToken, ChangePassword, ChangeEmail, UpdateProfile, Avatar, RevokeSession, GetActiveSessions |
| `Users/` | 3 | 2 | CRUD, AssignRoles, GetUserRoles |
| `Roles/` | 3 | 2 | CRUD, GetRoleById |
| `Permissions/` | 2 | 2 | Assign/Remove permissions to roles |
| `Audit/` | 0 | 5 | AuditTrail, EntityHistory, Search, Export, RetentionPolicy |
| `EmailTemplates/` | 2 | 2 | Update templates, SendTestEmail, Preview |
| `Tenants/` | 3 | 2 | CRUD, Multi-tenant management |
| `Notifications/` | 4 | 3 | MarkAsRead, Delete, Preferences, List, UnreadCount |
| `Blog/` | 10 | 4 | Posts CRUD/Publish, Categories CRUD, Tags CRUD |
| `DeveloperLogs/` | 0 | 0 | Real-time log streaming via SignalR |

**Totals:** 39 Command Handlers, 25 Query Handlers (64 handlers total)

**Supporting Modules:**
| Module | Purpose |
|--------|---------|
| `Behaviors/` | LoggingMiddleware, PerformanceMiddleware |
| `Specifications/` | RefreshTokens, PasswordResetOtps, ResourceShares |
| `Common/Interfaces/` | ICurrentUser, ITokenService, IEmailService, IFileStorage, +25 more |

### Infrastructure Layer (`src/NOIR.Infrastructure/`)

| Module | Files | Purpose |
|--------|-------|---------|
| `Persistence/` | 15+ | ApplicationDbContext, Repositories, Entity Configurations |
| `Identity/` | 9 | ApplicationUser, TokenService, RefreshTokenService, PasswordResetService |
| `Identity/Authorization/` | 8 | PermissionHandler, PermissionPolicyProvider, ResourceAuthorization |
| `Audit/` | 8 | HandlerAuditMiddleware, HttpRequestAuditMiddleware, AuditSearchService |
| `Email/` | 4 | FluentEmail integration, templating |
| `Storage/` | 2 | FluentStorage file storage |
| `Media/` | 5 | ImageProcessorService, ThumbHashGenerator, ColorAnalyzer, SrcsetGenerator, SlugGenerator |
| `Caching/` | 3 | CacheInvalidationService, CacheKeys, distributed cache utilities |
| `BackgroundJobs/` | 2 | Hangfire job implementations |
| `Localization/` | 2 | i18n service implementations |

### Web Layer (`src/NOIR.Web/`)

| Module | Files | Purpose |
|--------|-------|---------|
| `Endpoints/` | 12 | Auth, Users, Roles, Audit, EmailTemplates, Tenants, Notifications, Blog, DeveloperLogs, Files, Permissions, Media |
| `Middleware/` | 4 | ExceptionHandling, SecurityHeaders, Correlation |
| `Hubs/` | 3 | SignalR for real-time audit and log streaming |
| `Extensions/` | 5 | Service configuration, result extensions |
| `frontend/` | 95+ | React 19 SPA |

---

## API Endpoints

### Authentication (`/api/auth/`)
| Method | Endpoint | Purpose |
|--------|----------|---------|
| POST | `/login` | User login (JWT + optional cookies) |
| POST | `/logout` | User logout |
| POST | `/register` | User registration |
| POST | `/refresh` | Token refresh |
| GET | `/me` | Current user info |
| PUT | `/me` | Update profile |
| POST | `/me/avatar` | Upload avatar |
| DELETE | `/me/avatar` | Delete avatar |
| POST | `/me/change-password` | Change password |
| POST | `/me/change-email/request` | Request email change |
| POST | `/me/change-email/verify` | Verify email change OTP |

### Users (`/api/users/`)
| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/` | List users (paginated) |
| GET | `/{id}` | Get user by ID |
| PUT | `/{id}` | Update user |
| DELETE | `/{id}` | Delete user (soft) |
| GET | `/{id}/roles` | Get user roles |
| POST | `/{id}/roles` | Assign roles |

### Roles (`/api/roles/`)
| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/` | List roles |
| POST | `/` | Create role |
| GET | `/{id}` | Get role by ID |
| PUT | `/{id}` | Update role |
| DELETE | `/{id}` | Delete role |
| GET | `/{id}/permissions` | Get role permissions |
| POST | `/{id}/permissions` | Assign permissions |
| DELETE | `/{id}/permissions/{permissionId}` | Remove permission |

### Audit (`/api/audit/`)
| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/http` | HTTP request logs |
| GET | `/handlers` | Handler execution logs |
| GET | `/entities` | Entity change logs |
| GET | `/entities/{type}/{id}` | Entity history |
| GET | `/search` | Advanced search |
| GET | `/export` | Export logs |
| GET | `/retention` | Get retention policies |
| POST | `/retention` | Create retention policy |
| PUT | `/retention/{id}` | Update retention policy |
| DELETE | `/retention/{id}` | Delete retention policy |

### Email Templates (`/api/email-templates/`)
| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/` | List templates |
| GET | `/{id}` | Get template |
| PUT | `/{id}` | Update template |
| POST | `/{id}/preview` | Preview template |
| POST | `/{id}/test` | Send test email |

### Tenants (`/api/tenants/`)
| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/` | List tenants |
| POST | `/` | Create tenant |
| GET | `/{id}` | Get tenant |
| PUT | `/{id}` | Update tenant |
| DELETE | `/{id}` | Delete tenant |

### Notifications (`/api/notifications/`)
| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/` | List notifications |
| PUT | `/{id}/read` | Mark as read |
| POST | `/mark-all-read` | Mark all as read |
| DELETE | `/{id}` | Delete notification |
| GET | `/preferences` | Get notification preferences |
| PUT | `/preferences` | Update preferences |

### Blog (`/api/blog/`)
| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/posts` | List posts (paginated) |
| GET | `/posts/{id}` | Get post by ID |
| POST | `/posts` | Create post |
| PUT | `/posts/{id}` | Update post |
| DELETE | `/posts/{id}` | Delete post |
| POST | `/posts/{id}/publish` | Publish post |
| GET | `/categories` | List categories |
| POST | `/categories` | Create category |
| PUT | `/categories/{id}` | Update category |
| DELETE | `/categories/{id}` | Delete category |
| GET | `/tags` | List tags |
| POST | `/tags` | Create tag |
| PUT | `/tags/{id}` | Update tag |
| DELETE | `/tags/{id}` | Delete tag |

### Media (`/api/media/`)
| Method | Endpoint | Purpose |
|--------|----------|---------|
| POST | `/upload` | Upload and process image (multi-variant, WebP) |

### Developer Logs (`/api/developer-logs/`)
| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/stream` | SignalR hub for real-time logs |

---

## Frontend Structure

```
frontend/src/
├── components/
│   ├── audit/              # Audit dashboard, retention manager
│   ├── decorative/         # Background animations
│   ├── forgot-password/    # Password reset flow components
│   ├── notifications/      # Real-time notification system
│   ├── portal/             # Sidebar navigation
│   ├── settings/           # Profile, password, email change
│   └── ui/                 # shadcn/ui base components
├── contexts/               # Auth, Notification, Language
├── hooks/                  # useLogin, useImageUpload, useSignalR, useTenants
├── i18n/                   # i18next setup, language switching
├── layouts/                # PortalLayout with sidebar
├── lib/                    # Utilities, validation, gravatar
├── pages/
│   ├── forgot-password/    # Password reset flow
│   ├── portal/             # Dashboard, Settings, Notifications
│   │   ├── admin/tenants/  # Tenant management
│   │   ├── admin/users/    # User management
│   │   ├── admin/roles/    # Role management
│   │   ├── admin/activity-timeline/ # Audit timeline
│   │   ├── admin/developer-logs/    # Real-time log viewer
│   │   ├── blog/posts/     # Blog posts management
│   │   ├── blog/categories/# Blog categories
│   │   ├── blog/tags/      # Blog tags
│   │   └── email-templates/# Email template editor
│   ├── Landing.tsx
│   └── Login.tsx
├── services/               # API clients
└── types/                  # TypeScript types + generated API types
```

---

## Key Technologies

### Backend
| Technology | Version | Purpose |
|------------|---------|---------|
| .NET | 10.0 | Runtime (LTS until Nov 2028) |
| EF Core | 10.0 | ORM |
| SQL Server | Latest | Primary database |
| Wolverine | Latest | CQRS handlers, messaging |
| Hangfire | Latest | Background jobs |
| FluentValidation | Latest | Request validation |
| Mapperly | Latest | Compile-time mapping |
| Serilog | Latest | Structured logging |
| SignalR | Latest | Real-time notifications |
| Finbuckle.MultiTenant | Latest | Multi-tenancy |
| FluentEmail | Latest | Email sending |
| FluentStorage | Latest | File storage abstraction |

### Frontend
| Technology | Version | Purpose |
|------------|---------|---------|
| React | 19.x | UI library |
| TypeScript | 5.9 | Type safety |
| Vite | 7.x | Build tool |
| Tailwind CSS | 4.x | Styling |
| React Router | 7.x | Routing |
| shadcn/ui | Latest | UI components |
| i18next | Latest | Internationalization |

---

## Configuration

| File | Purpose |
|------|---------|
| `src/NOIR.Web/appsettings.json` | Base configuration |
| `src/NOIR.Web/appsettings.Development.json` | Dev overrides |
| `docker-compose.yml` | SQL Server + MailHog |
| `Dockerfile` | Production container |
| `Directory.Build.props` | Shared MSBuild properties |

---

## Critical Patterns

1. **Use Specifications** for all database queries - never raw `DbSet`
2. **Tag all specifications** with `TagWith("MethodName")` for SQL debugging
3. **Use IUnitOfWork** for persistence - repositories don't auto-save
4. **Use AsTracking** for mutation specs - default is `AsNoTracking`
5. **Soft delete only** - never hard delete (except GDPR)
6. **Marker interfaces** for DI: `IScopedService`, `ITransientService`, `ISingletonService`
7. **Co-locate Command + Handler + Validator** in same folder

---

## File Statistics

| Category | Count |
|----------|-------|
| C# Source Files | 377 |
| C# Test Files | 112 |
| Frontend TS/TSX | 87 |
| Documentation (MD) | 24 |
| Projects (csproj) | 8 |

---

## Test Coverage

| Project | Tests | Duration |
|---------|-------|----------|
| Domain.UnitTests | 488 | ~120ms |
| Application.UnitTests | 1,235 | ~1s |
| ArchitectureTests | 25 | ~960ms |
| IntegrationTests | 302 | ~26s |
| **Total** | **2,050** | ~28s |

### Handler Test Coverage (31 test files)

| Feature | Handler Tests |
|---------|---------------|
| Auth | LoginCommand, Logout, Register, RefreshToken, ChangePassword, RevokeSession, UploadAvatar, DeleteAvatar, GetCurrentUser |
| Roles | CreateRole, UpdateRole, DeleteRole |
| Permissions | AssignPermissionToRole, RemovePermissionFromRole |
| Users | UpdateUser, DeleteUser, AssignRolesToUser |
| Tenants | CreateTenant, UpdateTenant, DeleteTenant |
| EmailTemplates | GetEmailTemplate, GetEmailTemplates, UpdateEmailTemplate, SendTestEmail |
| Notifications | MarkAsRead, MarkAllAsRead, DeleteNotification, UpdatePreferences, GetPreferences, GetNotifications, GetUnreadCount |

---

## Documentation

### Backend Patterns
- [Repository & Specification](docs/backend/patterns/repository-specification.md)
- [DI Auto-Registration](docs/backend/patterns/di-auto-registration.md)
- [JWT Refresh Tokens](docs/backend/patterns/jwt-refresh-token.md)
- [Audit Logging](docs/backend/patterns/hierarchical-audit-logging.md)
- [Bulk Operations](docs/backend/patterns/bulk-operations.md)
- [Entity Configuration](docs/backend/patterns/entity-configuration.md)

### Frontend
- [Architecture](docs/frontend/architecture.md)
- [Theme](docs/frontend/theme.md)
- [Color Schema Guide](docs/frontend/COLOR_SCHEMA_GUIDE.md)
- [API Types](docs/frontend/api-types.md)
- [Localization](docs/frontend/localization-guide.md)
- [Vibe Kanban Integration](docs/frontend/vibe-kanban-integration.md)

### Architecture Decisions
- [001 - Tech Stack](docs/decisions/001-tech-stack.md)
- [002 - Frontend UI Stack](docs/decisions/002-frontend-ui-stack.md)
- [003 - Vertical Slice CQRS](docs/decisions/003-vertical-slice-cqrs.md)

### AI Instructions
- [CLAUDE.md](CLAUDE.md) - Claude Code specific
- [AGENTS.md](AGENTS.md) - Universal AI agents

### AI Context (Serena Memories)
Available memories for quick context:
- `project-overview` - High-level project summary
- `architecture-patterns` - Repository, specification, CQRS patterns
- `api-endpoints` - Endpoint patterns and conventions
- `application-features` - Feature module structure
- `domain-entities` - Entity definitions and patterns
- `frontend-architecture` - React architecture and patterns
- `infrastructure-services` - Service implementations
- `authentication-authorization` - JWT, permissions, roles
- `coding-standards` - Coding conventions and rules
- `testing-strategy` - Test patterns and coverage

---

## Default Credentials

- **Email:** `admin@noir.local`
- **Password:** `123qwe`

---

---

## Log Level Philosophy

NOIR uses a deliberate log level strategy for HTTP responses:

| HTTP Status | Log Level | Meaning |
|-------------|-----------|---------|
| 2xx | INFO | Success |
| 4xx | **WARNING** | Business logic failure (system working correctly) |
| 5xx | **ERROR** | System failure (needs investigation) |

**Key insight:** 4xx responses are WARNING, not ERROR, because the system correctly identified and rejected an invalid request. Only 5xx (system failures) should trigger production alerts.

**Examples:**
- User tries to delete protected tenant → 400 → WARNING
- User access denied → 403 → WARNING
- Database connection failed → 500 → ERROR

---

## Recent Changes (2026-01-18)

- **Media Upload System**: Unified image upload with processing pipeline
  - `/api/media/upload` endpoint with image variants (thumb, xl) and WebP output
  - ImageProcessorService with ThumbHash, parallel processing, configurable formats
  - Returns absolute URLs to fix image loading in rich text editors
  - Optimized: 2 variants (Thumb + ExtraLarge), WebP only, ~2-3 seconds per upload
- **Caching Infrastructure**: Added CacheInvalidationService and CacheKeys
- **Blog/CMS Feature**: Full blog system with Posts, Categories, Tags
  - 10 commands: CreatePost, UpdatePost, DeletePost, PublishPost, Create/Update/Delete Category/Tag
  - 4 queries: GetPosts (paginated), GetPost, GetCategories, GetTags
  - Frontend pages: BlogPostsPage, BlogCategoriesPage, BlogTagsPage
  - Permissions: blog-posts:read/create/update/delete/publish, blog-categories:*, blog-tags:*
  - TinyMCE integration with image upload
- **Navigation**: Added "Content" section to sidebar with Blog links

---

## Recent Changes (2026-01-17)

- **Log Level Philosophy**: Implemented 4xx = WARNING, 5xx = ERROR strategy
- **Developer Logs**: "Errors only" filter now includes WARNING + ERROR to capture all failures
- **AuditResultContext**: Added ambient context for capturing Result.Failure in Wolverine middleware
- **GetPermissionTemplatesQueryHandler**: Fixed by using IApplicationDbContext instead of IReadRepository (PermissionTemplate is Entity, not AggregateRoot)
- **HandlerAuditMiddleware**: Fixed Wolverine constructor resolution issue

---

## Recent Changes (2026-01-12)

- Test coverage increased from 1,808 to 2,050 tests (+13%)
- Added 21 new handler test files covering all major features
- Fixed Error.Validation/Error.Failure/Error.NotFound parameter order bugs

---

*Index size: ~8KB | Full codebase context: ~100KB+ tokens*
