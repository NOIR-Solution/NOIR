# Project Index: NOIR

> Enterprise-ready .NET 10 + React SaaS foundation with multi-tenancy, Clean Architecture, and comprehensive testing.

**Generated:** 2026-01-19
**Tests:** 3,768+ unit tests (2,937 Application + 831 Domain)

---

## Quick Reference

| Item | Value |
|------|-------|
| **Admin Login** | `admin@noir.local` / `123qwe` |
| **Backend URL** | http://localhost:4000 |
| **Frontend URL** | http://localhost:3000 |
| **API Docs** | http://localhost:3000/api/docs |

---

## Project Structure

```
NOIR/
├── src/
│   ├── NOIR.Domain/           # 60 files - Entities, interfaces, value objects
│   ├── NOIR.Application/      # 280 files - CQRS handlers, DTOs, specifications
│   ├── NOIR.Infrastructure/   # 180 files - EF Core, services, identity
│   └── NOIR.Web/              # 47 files - Endpoints, middleware
│       └── frontend/          # 162 files - React SPA (TypeScript)
├── tests/                     # 222 files - Unit, integration, architecture
├── docs/                      # 36 files - Patterns, decisions, guides
└── .github/                   # CI/CD, templates
```

---

## Entry Points

| Entry | Path | Purpose |
|-------|------|---------|
| **Backend** | `src/NOIR.Web/Program.cs` | ASP.NET Core host, DI setup |
| **Frontend** | `src/NOIR.Web/frontend/src/main.tsx` | React app bootstrap |
| **CLI Build** | `dotnet build src/NOIR.sln` | Build all projects |
| **CLI Test** | `dotnet test src/NOIR.sln` | Run all tests |
| **CLI Run** | `dotnet run --project src/NOIR.Web` | Start backend |

---

## Core Modules

### Domain Layer (`src/NOIR.Domain/`)

| Entity | Purpose |
|--------|---------|
| `Tenant` | Multi-tenant organization |
| `Permission` | Granular permission (`resource:action:scope`) |
| `PermissionTemplate` | Permission groupings for roles |
| `RefreshToken` | JWT refresh token with family tracking |
| `Notification` | User notification with preferences |
| `EmailTemplate` | Customizable email templates (Copy-on-Write) |
| `MediaFile` | File/image storage metadata |
| `Post`, `PostCategory`, `PostTag` | Blog/CMS entities |
| `EntityAuditLog` | Entity-level change tracking |
| `HandlerAuditLog` | Handler execution audit |
| `HttpRequestAuditLog` | HTTP request audit |
| `PasswordResetOtp`, `EmailChangeOtp` | OTP-based workflows |
| `ResourceShare` | Resource-level sharing permissions |
| `TenantSetting` | Tenant-specific configuration |

### Application Layer (`src/NOIR.Application/Features/`)

| Feature | Commands | Queries | Purpose |
|---------|----------|---------|---------|
| **Auth** | Login, Logout, RefreshToken, ChangePassword | GetSessions | Authentication & session management |
| **Users** | CreateUser, UpdateUser, DeleteUser, AssignRoles | GetUsers, GetUserById, GetUserRoles | User management |
| **Roles** | CreateRole, UpdateRole, DeleteRole | GetRoles, GetRoleById | Role management |
| **Permissions** | AssignToRole, RemoveFromRole | GetRolePermissions, GetUserPermissions | Permission management |
| **Tenants** | CreateTenant, UpdateTenant, DeleteTenant | GetTenants, GetTenantById | Multi-tenant management |
| **Notifications** | MarkAsRead, MarkAllAsRead, Delete, UpdatePreferences | GetNotifications, GetUnreadCount, GetPreferences | Notification system |
| **EmailTemplates** | UpdateEmailTemplate, SendTestEmail | GetEmailTemplates, GetEmailTemplate, PreviewTemplate | Email customization |
| **DeveloperLogs** | ClearLogs | GetLogs, SearchLogs | Developer debugging |
| **Audit** | - | GetEntityAuditLogs, GetHttpRequestLogs, GetHandlerLogs | Audit trail viewing |
| **Media** | UploadFile, DeleteFile | GetMedia | File management |
| **Blog** | CreatePost, UpdatePost, DeletePost | GetPosts, GetPostBySlug | CMS functionality |

### Infrastructure Layer (`src/NOIR.Infrastructure/`)

| Service | Interface | Purpose |
|---------|-----------|---------|
| `UserIdentityService` | `IUserIdentityService` | ASP.NET Identity operations |
| `TokenService` | `ITokenService` | JWT generation & validation |
| `RefreshTokenService` | `IRefreshTokenService` | Refresh token lifecycle |
| `EmailService` | `IEmailService` | Email sending with templates |
| `NotificationService` | `INotificationService` | Push & bell notifications |
| `DeviceFingerprintService` | `IDeviceFingerprintService` | Session fingerprinting |
| `CookieAuthService` | `ICookieAuthService` | Cookie-based auth tokens |
| `JsonLocalizationService` | `ILocalizationService` | i18n support |
| `BackgroundJobsService` | `IBackgroundJobs` | Hangfire job scheduling |

### Web Layer (`src/NOIR.Web/Endpoints/`)

| Endpoint | Route | Auth |
|----------|-------|------|
| `AuthEndpoints` | `/api/auth/*` | Public + Authenticated |
| `UserEndpoints` | `/api/users/*` | `users:read`, `users:write` |
| `RoleEndpoints` | `/api/roles/*` | `roles:read`, `roles:write` |
| `PermissionEndpoints` | `/api/permissions/*` | `permissions:read`, `permissions:write` |
| `TenantEndpoints` | `/api/tenants/*` | `tenants:read`, `tenants:write` |
| `NotificationEndpoints` | `/api/notifications/*` | Authenticated |
| `EmailTemplateEndpoints` | `/api/email-templates/*` | `email-templates:read`, `email-templates:write` |
| `AuditEndpoints` | `/api/audit/*` | `audit:view` |
| `MediaEndpoints` | `/api/media/*` | `media:read`, `media:write` |
| `BlogEndpoints` | `/api/blog/*` | Public + `blog:write` |
| `DeveloperLogEndpoints` | `/api/dev-logs/*` | Development only |

### Frontend (`src/NOIR.Web/frontend/src/`)

| Directory | Purpose |
|-----------|---------|
| `pages/` | Route components (Landing, Login, Portal) |
| `pages/portal/` | Authenticated dashboard pages |
| `pages/portal/admin/` | Admin management (users, roles, tenants) |
| `components/ui/` | shadcn/ui components |
| `components/notifications/` | Notification bell & list |
| `services/` | API client functions |
| `hooks/` | Custom React hooks |
| `contexts/` | React context providers (Auth, Theme) |
| `i18n/` | Internationalization |
| `types/` | TypeScript type definitions |
| `validation/` | Zod schemas (generated) |

---

## Key Patterns

| Pattern | Location | Purpose |
|---------|----------|---------|
| **Repository + Specification** | `IRepository<T>`, `Specification<T>` | Database queries with criteria encapsulation |
| **CQRS with Wolverine** | `Application/Features/*/Commands|Queries/` | Command/Query separation |
| **Unit of Work** | `IUnitOfWork.SaveChangesAsync()` | Transaction management |
| **Copy-on-Write** | `EmailTemplate` handlers | Tenant template inheritance |
| **Soft Delete** | `AuditableEntity.IsDeleted` | Data safety |
| **3-Level Audit** | HTTP → Handler → Entity | Comprehensive audit trail |
| **Permission-based Auth** | `[HasPermission("resource:action")]` | Fine-grained authorization |
| **Multi-Tenancy** | Finbuckle + global query filters | Tenant isolation |

---

## Configuration

| File | Purpose |
|------|---------|
| `appsettings.json` | App configuration |
| `appsettings.Development.json` | Dev overrides |
| `src/NOIR.Web/frontend/vite.config.ts` | Vite build config |
| `src/NOIR.Web/frontend/tailwind.config.js` | Tailwind CSS config |
| `src/NOIR.Web/frontend/tsconfig.json` | TypeScript config |

---

## Key Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| **Wolverine** | 3.x | CQRS messaging |
| **EF Core** | 10.x | ORM |
| **Finbuckle.MultiTenant** | 10.x | Multi-tenancy |
| **FluentValidation** | 11.x | Request validation |
| **Mapperly** | 3.x | DTO mapping (source gen) |
| **Hangfire** | 1.8.x | Background jobs |
| **Serilog** | 10.x | Structured logging |
| **FluentEmail** | 3.x | Email sending |
| **SixLabors.ImageSharp** | 3.1.12 | Image processing |
| **React** | 19.x | Frontend framework |
| **Tailwind CSS** | 4.x | Styling |
| **shadcn/ui** | - | UI components |
| **React Router** | 7.x | Routing |
| **i18next** | - | Internationalization |

---

## Testing

| Project | Tests | Coverage |
|---------|-------|----------|
| `NOIR.Application.UnitTests` | 2,937 | Handlers, services, validators |
| `NOIR.Domain.UnitTests` | 831 | Entities, value objects |
| `NOIR.IntegrationTests` | ~100 | Database, API endpoints |
| `NOIR.ArchitectureTests` | ~40 | Architecture rules |

**Total: 3,768+ tests**

---

## Quick Commands

```bash
# Development
dotnet build src/NOIR.sln              # Build all
dotnet run --project src/NOIR.Web      # Start backend
cd src/NOIR.Web/frontend && npm run dev # Start frontend

# Testing
dotnet test src/NOIR.sln               # All tests
dotnet test --filter "Category=Unit"   # Unit tests only

# Database
dotnet ef migrations add NAME --project src/NOIR.Infrastructure --startup-project src/NOIR.Web

# Frontend
cd src/NOIR.Web/frontend
npm run generate:api                   # Sync TypeScript types from backend
npm run build                          # Production build
```

---

## Documentation Links

| Document | Purpose |
|----------|---------|
| [CLAUDE.md](../CLAUDE.md) | AI assistant instructions |
| [AGENTS.md](../AGENTS.md) | Universal AI agent guidelines |
| [KNOWLEDGE_BASE.md](KNOWLEDGE_BASE.md) | Cross-referenced codebase guide |
| [Backend Patterns](backend/README.md) | Architecture patterns |
| [Frontend Guide](frontend/README.md) | React SPA structure |
| [ADRs](decisions/README.md) | Architecture decisions |

---

*This index provides a high-level overview. For detailed implementation, use the linked documentation or explore the source code directly.*
