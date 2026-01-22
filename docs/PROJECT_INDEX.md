# NOIR - Project Index

> **Quick Navigation:** Jump to any part of the codebase with this comprehensive index.

**Last Updated:** 2026-01-22

---

## Table of Contents

- [Project Overview](#project-overview)
- [Architecture Layers](#architecture-layers)
- [Feature Modules](#feature-modules)
- [Core Components](#core-components)
- [Testing Structure](#testing-structure)
- [Documentation Map](#documentation-map)
- [Quick Reference](#quick-reference)

---

## Project Overview

**NOIR** is an enterprise-ready .NET 10 + React SaaS foundation implementing Clean Architecture with multi-tenancy, comprehensive audit logging, and 2,100+ tests.

### Key Statistics

- **Lines of Code:** ~150,000
- **Test Coverage:** 2,100+ tests across Unit, Integration, and Architecture layers
- **Feature Modules:** 11 domain-driven modules
- **API Endpoints:** 60+ REST endpoints
- **Technologies:** .NET 10, React 19, SQL Server, EF Core 10

### Directory Structure

```
NOIR/
├── src/
│   ├── NOIR.Domain/              # 📦 Domain entities and business rules
│   ├── NOIR.Application/         # 📋 Application logic and CQRS
│   ├── NOIR.Infrastructure/      # 🔧 Infrastructure and persistence
│   └── NOIR.Web/                 # 🌐 API endpoints and SPA host
│       └── frontend/             # ⚛️  React frontend application
├── tests/                        # ✅ Comprehensive test suite
│   ├── NOIR.Domain.UnitTests/
│   ├── NOIR.Application.UnitTests/
│   ├── NOIR.IntegrationTests/
│   └── NOIR.ArchitectureTests/
├── docs/                         # 📚 Documentation
└── .github/                      # ⚙️  CI/CD workflows

```

---

## Architecture Layers

### 1. Domain Layer (`src/NOIR.Domain/`)

**Pure business logic with zero dependencies.**

#### Structure

```
NOIR.Domain/
├── Common/
│   ├── BaseEntity.cs                    # Base entity with Id, audit fields
│   ├── Permissions.cs                   # Permission constants (resource:action)
│   └── Result.cs                        # Result pattern for error handling
├── Entities/                            # Core domain entities
│   ├── ApplicationUser.cs               # Identity user with multi-tenancy
│   ├── ApplicationRole.cs               # Role with permissions
│   ├── Tenant.cs                        # Tenant entity (Finbuckle)
│   ├── RefreshToken.cs                  # JWT refresh token
│   ├── Notification.cs                  # User notification
│   ├── EntityAuditLog.cs                # Entity-level audit trail
│   ├── EmailTemplate.cs                 # Multi-tenant email templates
│   ├── Post.cs                          # Blog post
│   ├── Category.cs                      # Blog category
│   └── Tag.cs                           # Blog tag
├── Enums/                               # Domain enumerations
│   ├── AuditOperationType.cs            # CRUD operations
│   ├── NotificationType.cs              # Notification types
│   └── PostStatus.cs                    # Draft, Published, Archived
├── Interfaces/
│   ├── IRepository.cs                   # Generic repository
│   ├── ISpecification.cs                # Specification pattern
│   └── ISoftDeletable.cs                # Soft delete marker
├── Specifications/
│   └── Specification<T>.cs              # Base specification
└── ValueObjects/                        # DDD value objects
    └── Address.cs                       # Address value object
```

#### Key Patterns

| Pattern | File | Purpose |
|---------|------|---------|
| **Base Entity** | `Common/BaseEntity.cs` | `Id`, `CreatedAt`, `UpdatedAt`, `CreatedBy`, `UpdatedBy` |
| **Permissions** | `Common/Permissions.cs` | `resource:action` format (e.g., `users:read`) |
| **Result Pattern** | `Common/Result.cs` | Type-safe error handling without exceptions |
| **Soft Delete** | `Interfaces/ISoftDeletable.cs` | `IsDeleted`, `DeletedAt`, `DeletedBy` |
| **Multi-Tenancy** | `Entities/ApplicationUser.cs` | `TenantId` on all tenant-scoped entities |

#### Navigation

- [Domain Layer Documentation](../src/NOIR.Domain/README.md)
- [Entity Configuration Guide](backend/patterns/entity-configuration.md)
- [Soft Delete Pattern](backend/patterns/soft-delete.md)

---

### 2. Application Layer (`src/NOIR.Application/`)

**Application logic, CQRS handlers, and DTOs.**

#### Structure

```
NOIR.Application/
├── Common/
│   ├── Interfaces/
│   │   ├── IApplicationDbContext.cs     # DbContext abstraction
│   │   ├── ICurrentUser.cs              # Current user service
│   │   ├── IEmailService.cs             # Email abstraction
│   │   ├── IUserIdentityService.cs      # Identity operations
│   │   └── IUnitOfWork.cs               # Unit of Work pattern
│   ├── Models/
│   │   ├── Result<T>.cs                 # Generic result wrapper
│   │   └── PaginatedList<T>.cs          # Pagination container
│   ├── Settings/
│   │   ├── JwtSettings.cs               # JWT configuration
│   │   ├── EmailSettings.cs             # SMTP configuration
│   │   └── PlatformSettings.cs          # Platform admin settings
│   └── Utilities/
│       └── PasswordHasher.cs            # Bcrypt password hashing
├── Behaviors/
│   ├── ValidationBehavior.cs            # FluentValidation pipeline
│   ├── LoggingBehavior.cs               # Request/response logging
│   └── PerformanceBehavior.cs           # Performance monitoring
├── Features/                            # Vertical Slice Architecture
│   ├── Auth/                            # Authentication & Profile
│   ├── Users/                           # User management
│   ├── Roles/                           # Role management
│   ├── Permissions/                     # Permission management
│   ├── Tenants/                         # Tenant administration
│   ├── Audit/                           # Audit log queries
│   ├── Notifications/                   # User notifications
│   ├── EmailTemplates/                  # Email template CRUD
│   ├── Media/                           # File upload/management
│   ├── Blog/                            # Blog CMS
│   └── DeveloperLogs/                   # Serilog streaming
└── Specifications/                      # EF Core query specs
    ├── RefreshTokens/
    ├── Notifications/
    ├── PasswordResetOtps/
    └── EmailChangeOtps/
```

#### Feature Module Pattern

Each feature follows **Vertical Slice Architecture** with co-located components:

```
Features/{Feature}/
├── Commands/
│   └── {Action}/
│       ├── {Action}Command.cs           # Command DTO
│       ├── {Action}CommandHandler.cs    # Business logic
│       └── {Action}CommandValidator.cs  # FluentValidation
├── Queries/
│   └── {Action}/
│       ├── {Action}Query.cs             # Query DTO
│       └── {Action}QueryHandler.cs      # Data retrieval
└── DTOs/
    └── {Entity}Dto.cs                   # Data transfer object
```

#### Feature Modules Summary

| Module | Commands | Queries | Description |
|--------|----------|---------|-------------|
| **Auth** | Login, Logout, RefreshToken, UpdateProfile, UploadAvatar, DeleteAvatar, SendPasswordResetOtp, VerifyPasswordResetOtp, ResetPassword | GetCurrentUser, GetUserById | Authentication, profile, password reset |
| **Users** | CreateUser, UpdateUser, DeleteUser, AssignRoles | GetUsers, GetUserRoles | User CRUD and role assignment |
| **Roles** | CreateRole, UpdateRole, DeleteRole | GetRoles, GetRoleById | Role management |
| **Permissions** | AssignToRole, RemoveFromRole | GetRolePermissions, GetUserPermissions | Permission assignment |
| **Tenants** | CreateTenant, UpdateTenant, DeleteTenant, RestoreTenant | GetTenants, GetTenantById, GetTenantSettings, GetArchivedTenants | Multi-tenant administration |
| **Audit** | BulkExport | GetAuditLogs, GetEntityHistory | Audit log queries and export |
| **Notifications** | MarkAsRead, MarkAllAsRead, DeleteNotification | GetNotifications, GetUnreadCount | User notifications |
| **EmailTemplates** | UpdateEmailTemplate | GetEmailTemplates, GetEmailTemplateById | Template customization |
| **Media** | UploadFile, DeleteFile | GetFiles | File storage |
| **Blog** | CreatePost, UpdatePost, DeletePost, PublishPost, CreateCategory, UpdateCategory, DeleteCategory, CreateTag, UpdateTag, DeleteTag | GetPosts, GetPost, GetCategories, GetTags | Blog CMS |
| **DeveloperLogs** | - | StreamLogs | Real-time Serilog streaming |

#### Navigation

- [Application Layer Documentation](../src/NOIR.Application/README.md)
- [CQRS Pattern](backend/patterns/cqrs-vertical-slice.md)
- [Validation Guide](backend/patterns/validation.md)
- [Audit Logging](backend/patterns/hierarchical-audit-logging.md)

---

### 3. Infrastructure Layer (`src/NOIR.Infrastructure/`)

**EF Core, Identity, services, and infrastructure concerns.**

#### Structure

```
NOIR.Infrastructure/
├── Audit/
│   ├── EntityAuditLogInterceptor.cs     # Entity change tracking
│   └── WolverineBeforeStateProvider.cs  # Handler audit support
├── BackgroundJobs/
│   ├── EmailCleanupJob.cs               # Hangfire recurring job
│   └── JobFailureNotificationFilter.cs  # Job failure alerts
├── Caching/
│   └── FusionCacheExtensions.cs         # FusionCache setup
├── Email/
│   ├── EmailService.cs                  # FluentEmail implementation
│   └── EmailSettings.cs                 # SMTP configuration
├── Hubs/
│   ├── NotificationHub.cs               # SignalR notifications
│   └── DeveloperLogHub.cs               # SignalR log streaming
├── Identity/
│   ├── UserIdentityService.cs           # UserManager wrapper
│   └── Authorization/
│       ├── PermissionAuthorizationHandler.cs
│       └── ResourceAuthorizationHandler.cs
├── Localization/
│   ├── LocalizationService.cs           # i18n service
│   └── LocalizationStartupValidator.cs  # Validates JSON resources
├── Logging/
│   ├── DeferredSignalRLogSink.cs        # Serilog SignalR sink
│   └── DeveloperLogStreamService.cs     # Log streaming
├── Media/
│   └── ImageProcessingService.cs        # Image resizing (SixLabors)
├── Persistence/
│   ├── ApplicationDbContext.cs          # Main DbContext
│   ├── TenantStoreDbContext.cs          # Finbuckle tenant store
│   ├── ApplicationDbContextSeeder.cs    # Database seeding
│   ├── Configurations/                  # EF Core entity configs
│   ├── Interceptors/
│   │   ├── AuditableEntityInterceptor.cs
│   │   ├── DomainEventInterceptor.cs
│   │   ├── EntityAuditLogInterceptor.cs
│   │   └── TenantIdSetterInterceptor.cs
│   └── Repositories/
│       └── Repository<T>.cs             # Generic repository
├── Services/
│   ├── CurrentUser.cs                   # HttpContext user extraction
│   ├── DateTimeService.cs               # UTC time provider
│   ├── NotificationService.cs           # SignalR push notifications
│   └── PasswordResetService.cs          # OTP-based password reset
└── Storage/
    └── StorageSettings.cs               # FluentStorage config (Local/Azure/S3)
```

#### Key Services

| Service | File | Purpose |
|---------|------|---------|
| **Repository** | `Persistence/Repositories/Repository<T>.cs` | Generic CRUD with specifications |
| **Unit of Work** | `ApplicationDbContext.cs` (implements `IUnitOfWork`) | Transaction management |
| **Email** | `Email/EmailService.cs` | Database-driven templates with FluentEmail |
| **Notifications** | `Services/NotificationService.cs` | SignalR push notifications |
| **Identity** | `Identity/UserIdentityService.cs` | User CRUD with Identity framework |
| **Authorization** | `Identity/Authorization/` | Permission and resource-based policies |
| **Caching** | `Caching/FusionCacheExtensions.cs` | FusionCache (L1/L2 hybrid) |

#### Navigation

- [Infrastructure Documentation](../src/NOIR.Infrastructure/README.md)
- [Repository Pattern](backend/patterns/repository-specification.md)
- [Entity Configuration](backend/patterns/entity-configuration.md)
- [DI Auto-Registration](backend/patterns/di-auto-registration.md)
- [Tenant Isolation](backend/architecture/tenant-id-interceptor.md)

---

### 4. Web Layer (`src/NOIR.Web/`)

**API endpoints, middleware, and SPA host.**

#### Structure

```
NOIR.Web/
├── Endpoints/                           # Minimal API endpoints
│   ├── AuthEndpoints.cs                 # /api/auth/*
│   ├── UserEndpoints.cs                 # /api/users/*
│   ├── RoleEndpoints.cs                 # /api/roles/*
│   ├── PermissionEndpoints.cs           # /api/permissions/*
│   ├── TenantEndpoints.cs               # /api/tenants/*
│   ├── AuditEndpoints.cs                # /api/audit/*
│   ├── NotificationEndpoints.cs         # /api/notifications/*
│   ├── EmailTemplateEndpoints.cs        # /api/email-templates/*
│   ├── MediaEndpoints.cs                # /api/media/*
│   └── BlogEndpoints.cs                 # /api/blog/*
├── Middleware/
│   ├── CurrentUserLoaderMiddleware.cs   # Loads user claims into context
│   ├── ExceptionHandlingMiddleware.cs   # Global error handler
│   └── TenantResolutionMiddleware.cs    # Resolves tenant from header/JWT
├── Program.cs                           # Application entry point
├── appsettings.json                     # Configuration
└── frontend/                            # React SPA (Vite)
    ├── src/
    │   ├── components/                  # Reusable components
    │   ├── contexts/                    # React contexts (Auth, Theme, Notification)
    │   ├── hooks/                       # Custom React hooks
    │   ├── layouts/                     # Layout components
    │   ├── pages/                       # Route pages
    │   ├── services/                    # API services
    │   └── lib/                         # Utilities
    ├── e2e/                             # Playwright E2E tests
    └── public/                          # Static assets
```

#### API Endpoints Summary

| Group | Base Path | Endpoints |
|-------|-----------|-----------|
| **Auth** | `/api/auth` | login, logout, refresh, me, profile, avatar, password-reset |
| **Users** | `/api/users` | CRUD, roles, pagination |
| **Roles** | `/api/roles` | CRUD, permissions |
| **Permissions** | `/api/permissions` | assign, remove, list |
| **Tenants** | `/api/tenants` | CRUD, archive, restore |
| **Audit** | `/api/audit` | logs, entity-history, export |
| **Notifications** | `/api/notifications` | list, mark-read, delete |
| **Email Templates** | `/api/email-templates` | CRUD, preview |
| **Media** | `/api/media` | upload, delete, list |
| **Blog** | `/api/blog` | posts, categories, tags (full CRUD) |
| **Hangfire** | `/hangfire` | Dashboard (requires `system:hangfire` permission) |

#### Navigation

- [API Documentation](API_INDEX.md)
- [Frontend Architecture](frontend/architecture.md)
- [Frontend README](frontend/README.md)

---

## Feature Modules

### Authentication & Identity

**Files:** `src/NOIR.Application/Features/Auth/`

- **Login** - JWT + refresh token generation
- **Logout** - Token revocation
- **RefreshToken** - Token rotation
- **Profile** - Update user profile
- **Avatar** - Upload/delete avatar (FluentStorage)
- **Password Reset** - OTP-based flow (SendOtp → VerifyOtp → ResetPassword)

**Key Files:**
- `Commands/Login/LoginCommand.cs` - Authentication logic
- `Commands/RefreshToken/RefreshTokenCommand.cs` - Token rotation
- `Commands/SendPasswordResetOtp/SendPasswordResetOtpCommand.cs` - OTP generation

**Tests:** `tests/NOIR.IntegrationTests/Features/Auth/`

**Docs:** [JWT Refresh Token Pattern](backend/patterns/jwt-refresh-token.md)

---

### User Management

**Files:** `src/NOIR.Application/Features/Users/`

- **CRUD** - Create, read, update, delete users
- **Role Assignment** - Assign/remove roles
- **Pagination** - Search, filter, sort

**Key Files:**
- `Commands/CreateUser/CreateUserCommand.cs`
- `Commands/AssignRoles/AssignRolesCommand.cs`
- `Queries/GetUsers/GetUsersQuery.cs`

**Tests:** `tests/NOIR.IntegrationTests/Features/Users/`

---

### Role & Permission Management

**Files:** `src/NOIR.Application/Features/Roles/`, `Features/Permissions/`

- **Roles** - CRUD with permission assignment
- **Permissions** - Granular `resource:action` format
- **Validation** - Tenant scope validation (system-only vs tenant-allowed)

**Key Files:**
- `Features/Roles/Commands/CreateRole/CreateRoleCommand.cs`
- `Features/Permissions/Commands/AssignToRole/AssignToRoleCommand.cs`
- `Domain/Common/Permissions.cs` - Permission constants

**Tests:** `tests/NOIR.Domain.UnitTests/Common/PermissionsTests.cs`

**Docs:** [Role Permission Best Practices](backend/research/role-permission-best-practices-2025.md)

---

### Multi-Tenancy

**Files:** `src/NOIR.Application/Features/Tenants/`

- **Tenant CRUD** - Create, update, delete tenants
- **Soft Delete** - Archive with restore capability
- **Isolation** - Automatic query filtering via `TenantIdSetterInterceptor`

**Key Files:**
- `Features/Tenants/Commands/CreateTenant/CreateTenantCommand.cs`
- `Infrastructure/Persistence/Interceptors/TenantIdSetterInterceptor.cs`
- `Domain/Entities/Tenant.cs`

**Tests:** `tests/NOIR.IntegrationTests/Features/Tenants/`

**Docs:** [Tenant ID Interceptor](backend/architecture/tenant-id-interceptor.md)

---

### Audit Logging

**Files:** `src/NOIR.Application/Features/Audit/`

- **3-Level Audit** - HTTP request, Handler command, Entity change
- **Query** - Search, filter, date range
- **Export** - Bulk CSV export
- **Entity History** - Track all changes to a specific entity

**Key Files:**
- `Features/Audit/Queries/GetAuditLogs/GetAuditLogsQuery.cs`
- `Infrastructure/Audit/EntityAuditLogInterceptor.cs`
- `Domain/Entities/EntityAuditLog.cs`

**Tests:** `tests/NOIR.IntegrationTests/Features/Audit/`

**Docs:** [Hierarchical Audit Logging](backend/patterns/hierarchical-audit-logging.md)

---

### Notifications

**Files:** `src/NOIR.Application/Features/Notifications/`

- **SignalR Push** - Real-time notifications via WebSocket
- **CRUD** - Mark as read, delete
- **Unread Count** - Efficient query
- **Types** - Success, Info, Warning, Error

**Key Files:**
- `Features/Notifications/Queries/GetNotifications/GetNotificationsQuery.cs`
- `Infrastructure/Hubs/NotificationHub.cs`
- `Infrastructure/Services/NotificationService.cs`

**Tests:** `tests/NOIR.IntegrationTests/Features/Notifications/`

---

### Email Templates

**Files:** `src/NOIR.Application/Features/EmailTemplates/`

- **Database-Driven** - Templates stored in DB, not .cshtml files
- **Multi-Tenant** - Copy-on-write pattern (platform defaults + tenant overrides)
- **Variables** - Mustache-style `{{variable}}` syntax
- **Preview** - Render template with sample data

**Key Files:**
- `Features/EmailTemplates/Queries/GetEmailTemplates/GetEmailTemplatesQuery.cs`
- `Infrastructure/Email/EmailService.cs`
- `Infrastructure/Persistence/ApplicationDbContextSeeder.cs` (template seeding)

**Tests:** `tests/NOIR.Application.UnitTests/Infrastructure/EmailServiceTests.cs`

---

### Blog CMS

**Files:** `src/NOIR.Application/Features/Blog/`

- **Posts** - CRUD, publish/unpublish, draft status
- **Categories** - Hierarchical categories
- **Tags** - Many-to-many tagging
- **Soft Delete** - Archive posts with restore

**Key Files:**
- `Features/Blog/Commands/CreatePost/CreatePostCommand.cs`
- `Features/Blog/Queries/GetPosts/GetPostsQuery.cs`
- `Domain/Entities/Post.cs`, `Category.cs`, `Tag.cs`

**Tests:** `tests/NOIR.IntegrationTests/Features/Blog/`

---

### Developer Logs

**Files:** `src/NOIR.Application/Features/DeveloperLogs/`

- **Serilog Streaming** - Real-time log streaming via SignalR
- **Dynamic Level** - Change log level at runtime
- **Filters** - By level, source, message

**Key Files:**
- `Features/DeveloperLogs/Queries/StreamLogs/StreamLogsQuery.cs`
- `Infrastructure/Logging/DeferredSignalRLogSink.cs`
- `Infrastructure/Hubs/DeveloperLogHub.cs`

**Tests:** `tests/NOIR.IntegrationTests/Hubs/DeveloperLogHubTests.cs`

---

## Core Components

### Specifications (Query Pattern)

**Location:** `src/NOIR.Domain/Specifications/`, `src/NOIR.Application/Specifications/`

**Purpose:** Encapsulate query logic for reusability and testability.

**Base Class:** `Ardalis.Specification.Specification<T>`

**Example:**

```csharp
// src/NOIR.Application/Specifications/RefreshTokens/ActiveRefreshTokenByTokenSpec.cs
public class ActiveRefreshTokenByTokenSpec : Specification<RefreshToken>
{
    public ActiveRefreshTokenByTokenSpec(string token)
    {
        Query.Where(t => t.Token == token && !t.IsRevoked && t.ExpiresAt > DateTime.UtcNow)
             .TagWith("GetActiveRefreshTokenByToken");
    }
}
```

**Usage:**

```csharp
var spec = new ActiveRefreshTokenByTokenSpec(token);
var refreshToken = await _repository.FirstOrDefaultAsync(spec, ct);
```

**Key Specs:**
- `RefreshTokens/` - Active token queries
- `Notifications/` - Unread count, user notifications
- `PasswordResetOtps/` - OTP validation
- `EmailChangeOtps/` - Email change flow
- `TenantSettings/` - Tenant configuration

**Docs:** [Repository & Specification Pattern](backend/patterns/repository-specification.md)

---

### Validation (FluentValidation)

**Location:** `src/NOIR.Application/Features/{Feature}/Commands/{Action}/{Action}CommandValidator.cs`

**Pattern:** Each command has a co-located validator.

**Example:**

```csharp
// CreateUserCommandValidator.cs
public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(6);
    }
}
```

**Pipeline:** `ValidationBehavior<TRequest, TResponse>` in Wolverine pipeline.

**Docs:** [Validation Guide](backend/patterns/validation.md)

---

### Mappings (Mapperly)

**Location:** Throughout `src/NOIR.Application/Features/`

**Pattern:** Static mapper classes using Mapperly source generator.

**Example:**

```csharp
[Mapper]
public static partial class UserMapper
{
    public static partial UserDto ToDto(this ApplicationUser user);
    public static partial IQueryable<UserDto> ProjectToDto(this IQueryable<ApplicationUser> query);
}
```

**Benefits:**
- Zero runtime reflection
- Compile-time validation
- High performance
- Type-safe

**Docs:** [Mapperly Documentation](https://mapperly.riok.app/)

---

### Middleware

**Location:** `src/NOIR.Web/Middleware/`

| Middleware | Purpose | Order |
|------------|---------|-------|
| `ExceptionHandlingMiddleware` | Global error handling, `Result<T>` conversion | 1 |
| `TenantResolutionMiddleware` | Extract tenant from header/JWT | 2 |
| `CurrentUserLoaderMiddleware` | Load user claims into `ICurrentUser` | 3 |

**Docs:** [Middleware Guide](backend/patterns/middleware.md)

---

## Testing Structure

### Test Projects

```
tests/
├── NOIR.Domain.UnitTests/           # Domain logic tests (500+ tests)
├── NOIR.Application.UnitTests/      # Application layer tests (600+ tests)
├── NOIR.IntegrationTests/           # API integration tests (900+ tests)
└── NOIR.ArchitectureTests/          # Architecture rules (100+ tests)
```

### Integration Tests

**Base Class:** `IntegrationTestBase` - Provides `WebApplicationFactory`, test database, and cleanup.

**Example:**

```csharp
public class CreateUserTests : IntegrationTestBase
{
    [Fact]
    public async Task CreateUser_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var command = new CreateUserCommand { Email = "test@example.com", ... };

        // Act
        var result = await SendAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be("test@example.com");
    }
}
```

**Features:**
- In-memory SQL Server database per test
- Automatic cleanup after each test
- Seeded with test users and roles
- Support for multi-tenancy testing

**Docs:** [Testing Guide](backend/testing/integration-tests.md)

---

### Architecture Tests

**Location:** `tests/NOIR.ArchitectureTests/`

**Purpose:** Enforce architectural rules and conventions.

**Rules:**
- Domain layer has no dependencies
- Application depends only on Domain
- Infrastructure depends on Application and Domain
- Web depends on all layers
- No circular dependencies
- Naming conventions (Commands end with "Command", etc.)

**Example:**

```csharp
[Fact]
public void Domain_Should_Not_HaveDependencyOn_Application()
{
    var result = Types.InAssembly(DomainAssembly)
        .Should().NotHaveDependencyOn(ApplicationAssembly.GetName().Name)
        .GetResult();

    result.IsSuccessful.Should().BeTrue();
}
```

**Docs:** [Architecture Testing](backend/testing/architecture-tests.md)

---

## Documentation Map

### Core Guides

| Document | Purpose |
|----------|---------|
| [KNOWLEDGE_BASE.md](KNOWLEDGE_BASE.md) | Comprehensive codebase reference |
| [API_INDEX.md](API_INDEX.md) | REST API endpoint documentation |
| [ARCHITECTURE.md](ARCHITECTURE.md) | High-level architecture overview |
| **[PROJECT_INDEX.md](PROJECT_INDEX.md)** | **This document - project navigation** |

### Backend

| Document | Purpose |
|----------|---------|
| [Backend Overview](backend/README.md) | Backend setup and conventions |
| [Repository Pattern](backend/patterns/repository-specification.md) | Data access with specifications |
| [DI Auto-Registration](backend/patterns/di-auto-registration.md) | Service registration with Scrutor |
| [Entity Configuration](backend/patterns/entity-configuration.md) | EF Core entity setup |
| [Audit Logging](backend/patterns/hierarchical-audit-logging.md) | 3-level audit system |
| [Before-State Resolver](backend/patterns/before-state-resolver.md) | Activity Timeline handler diffs |
| [Bulk Operations](backend/patterns/bulk-operations.md) | High-performance batch operations |
| [JSON Enum Serialization](backend/patterns/json-enum-serialization.md) | String-based enum serialization |
| [JWT Refresh Token](backend/patterns/jwt-refresh-token.md) | Token rotation and security |
| [Tenant Isolation](backend/architecture/tenant-id-interceptor.md) | Multi-tenancy implementation |

### Frontend

| Document | Purpose |
|----------|---------|
| [Frontend Overview](frontend/README.md) | Frontend architecture and setup |
| [Architecture](frontend/architecture.md) | Component structure and patterns |
| [Theme](frontend/theme.md) | Theme customization guide |
| [API Types](frontend/api-types.md) | Type generation from backend |
| [Localization](frontend/localization-guide.md) | i18n management |
| [Color Schema](frontend/COLOR_SCHEMA_GUIDE.md) | Color system and palettes |

### Architecture Decisions

| ADR | Title |
|-----|-------|
| [001](decisions/001-tech-stack.md) | Technology Stack Selection |
| [002](decisions/002-frontend-ui-stack.md) | Frontend UI Stack |
| [003](decisions/003-vertical-slice-cqrs.md) | Vertical Slice Architecture for CQRS |

### Research

| Document | Topic |
|----------|-------|
| [Role Permission Best Practices](backend/research/role-permission-best-practices-2025.md) | Role/permission patterns |
| [Hierarchical Audit Comparison](backend/research/hierarchical-audit-logging-comparison-2025.md) | Audit system design |
| [Validation Unification Plan](backend/research/validation-unification-plan.md) | Unified validation strategy |

---

## Quick Reference

### Common Commands

```bash
# Development
dotnet build src/NOIR.sln
dotnet run --project src/NOIR.Web
dotnet watch --project src/NOIR.Web
dotnet test src/NOIR.sln

# Database
dotnet ef migrations add NAME --project src/NOIR.Infrastructure --startup-project src/NOIR.Web --context ApplicationDbContext --output-dir Migrations/ApplicationDbContext
dotnet ef database update --project src/NOIR.Infrastructure --startup-project src/NOIR.Web --context ApplicationDbContext

# Frontend
cd src/NOIR.Web/frontend
npm install
npm run dev
npm run build
npm run generate:api
npm run test:e2e
```

### Key Directories

| Path | Purpose |
|------|---------|
| `src/NOIR.Domain/Entities/` | Domain entities |
| `src/NOIR.Application/Features/` | Vertical slices (CQRS) |
| `src/NOIR.Infrastructure/Persistence/` | EF Core, repositories |
| `src/NOIR.Web/Endpoints/` | Minimal API endpoints |
| `src/NOIR.Web/frontend/src/pages/` | React pages |
| `tests/NOIR.IntegrationTests/Features/` | API integration tests |
| `docs/backend/patterns/` | Backend patterns |
| `docs/frontend/` | Frontend guides |

### Key Concepts

| Concept | Files | Docs |
|---------|-------|------|
| **Vertical Slice** | `Features/{Feature}/` | [ADR 003](decisions/003-vertical-slice-cqrs.md) |
| **Specifications** | `Specifications/` | [Repository Pattern](backend/patterns/repository-specification.md) |
| **Multi-Tenancy** | `TenantIdSetterInterceptor.cs` | [Tenant Isolation](backend/architecture/tenant-id-interceptor.md) |
| **Audit Logging** | `EntityAuditLogInterceptor.cs` | [Audit Pattern](backend/patterns/hierarchical-audit-logging.md) |
| **Permissions** | `Domain/Common/Permissions.cs` | [Role Permission](backend/research/role-permission-best-practices-2025.md) |
| **Validation** | `*Validator.cs` | [Validation Guide](backend/patterns/validation.md) |
| **Email Templates** | `EmailTemplate` entity | Knowledge Base |
| **SignalR Hubs** | `NotificationHub`, `DeveloperLogHub` | Knowledge Base |

---

## Navigation Tips

### Finding a Feature

1. **API Endpoint** → Check `src/NOIR.Web/Endpoints/{Feature}Endpoints.cs`
2. **Command/Query** → Look in `src/NOIR.Application/Features/{Feature}/Commands|Queries/{Action}/`
3. **Entity** → Find in `src/NOIR.Domain/Entities/{Entity}.cs`
4. **Service** → Search in `src/NOIR.Infrastructure/Services/`
5. **Test** → Check `tests/NOIR.IntegrationTests/Features/{Feature}/`

### Finding Documentation

1. **Pattern** → `docs/backend/patterns/`
2. **Architecture** → `docs/backend/architecture/`
3. **Research** → `docs/backend/research/`
4. **Frontend** → `docs/frontend/`
5. **Decisions** → `docs/decisions/`

### Finding Configuration

1. **App Settings** → `src/NOIR.Web/appsettings.json`
2. **Settings Classes** → `src/NOIR.Application/Common/Settings/`
3. **DI Registration** → `src/NOIR.Infrastructure/DependencyInjection.cs`
4. **Middleware** → `src/NOIR.Web/Program.cs`

---

## Contributing

See [CONTRIBUTING.md](../CONTRIBUTING.md) for guidelines.

**Key Points:**
- Follow Vertical Slice Architecture for new features
- Add tests for all features (Unit + Integration)
- Update documentation for significant changes
- Use FluentValidation for command validation
- Tag all specifications with `TagWith("MethodName")`
- Implement `IAuditableCommand` for user actions

---

## Resources

- **GitHub:** https://github.com/NOIR-Solution/NOIR
- **Documentation:** [docs/README.md](README.md)
- **Knowledge Base:** [KNOWLEDGE_BASE.md](KNOWLEDGE_BASE.md)
- **AI Instructions:** [CLAUDE.md](../CLAUDE.md), [AGENTS.md](../AGENTS.md)

---

**Last Updated:** 2026-01-22
**Maintainer:** NOIR Team
