# NOIR - Project Index

> **Quick Navigation:** Jump to any part of the codebase with this comprehensive index.

**Last Updated:** 2026-02-05

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

**NOIR** is an enterprise-ready .NET 10 + React 19 SaaS foundation implementing Clean Architecture with multi-tenancy, comprehensive audit logging, and 5,188+ tests.

### Key Statistics

- **Lines of Code:** ~200,000
- **Test Coverage:** 5,188+ tests across Unit, Integration, and Architecture layers
- **Feature Modules:** 26 domain-driven modules (E-commerce, CMS, Auth, Multi-tenant, Payments, Shipping)
- **API Endpoints:** 200+ REST endpoints (27+ endpoint groups)
- **Domain Entities:** 45+ entities, 22+ enums, 8 domain event files
- **Application Layer:** 126 Commands, 74 Queries, 39+ Common Interfaces
- **Source Files:** ~1,998 files (*.cs, *.tsx, *.ts combined)
- **Frontend:** 60+ UI components, 28+ custom hooks, 23+ API services
- **Database Indexes:** 233+ indexes (including 14 new filtered indexes for sparse data)
- **Technologies:** .NET 10, React 19, SQL Server, EF Core 10, Wolverine, SignalR

### Directory Structure

```
NOIR/
├── src/
│   ├── NOIR.Domain/              # 📦 Domain entities and business rules
│   ├── NOIR.Application/         # 📋 Application logic and CQRS
│   ├── NOIR.Infrastructure/      # 🔧 Infrastructure and persistence
│   └── NOIR.Web/                 # 🌐 API endpoints and SPA host
│       └── frontend/             # ⚛️  React frontend application
├── tests/                        # ✅ 5,188+ tests across 4 projects
│   ├── NOIR.Domain.UnitTests/    # 842 domain logic tests
│   ├── NOIR.Application.UnitTests/ # 4,321 handler/service tests
│   ├── NOIR.IntegrationTests/    # ~605 API integration tests (requires DB)
│   └── NOIR.ArchitectureTests/   # 25 architectural rule tests
├── docs/                         # 📚 46 documentation files
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
│   ├── Tenant.cs                        # Tenant entity (Finbuckle, immutable factory methods)
│   ├── RefreshToken.cs                  # JWT refresh token
│   ├── Notification.cs                  # User notification
│   ├── EntityAuditLog.cs                # Entity-level audit trail
│   ├── HandlerAuditLog.cs               # Handler-level audit (CQRS)
│   ├── HttpRequestAuditLog.cs           # HTTP request audit
│   ├── EmailTemplate.cs                 # Multi-tenant email templates
│   ├── LegalPage.cs                     # Multi-tenant legal pages (COW)
│   ├── MediaFile.cs                     # File storage tracking
│   ├── Post.cs                          # Blog post
│   ├── PostCategory.cs                  # Blog category
│   ├── PostTag.cs                       # Blog tag
│   ├── Payment/                         # Payment domain
│   │   ├── PaymentGateway.cs            # Gateway configuration (encrypted credentials)
│   │   ├── PaymentTransaction.cs        # Payment lifecycle tracking
│   │   ├── PaymentWebhookLog.cs         # Webhook audit trail
│   │   ├── PaymentOperationLog.cs       # Gateway API call audit trail
│   │   └── Refund.cs                    # Refund tracking with approval workflow
│   ├── Product/                         # ⭐ NEW: Product domain (Phase 8)
│   │   ├── Product.cs                   # Product aggregate root with variants
│   │   ├── ProductVariant.cs            # SKU, price, inventory
│   │   ├── ProductImage.cs              # Product images
│   │   └── ProductCategory.cs           # Hierarchical categories
│   ├── Cart/                            # ⭐ NEW: Shopping Cart domain (Phase 8)
│   │   ├── Cart.cs                      # Cart aggregate root (user/guest)
│   │   └── CartItem.cs                  # Cart line items
│   ├── Checkout/                        # ⭐ NEW: Checkout domain (Phase 8 Sprint 2)
│   │   └── CheckoutSession.cs           # Checkout session aggregate (address, shipping, payment)
│   └── Order/                           # ⭐ NEW: Order domain (Phase 8 Sprint 2)
│       ├── Order.cs                     # Order aggregate root with lifecycle
│       └── OrderItem.cs                 # Order line items (product snapshot)
├── Enums/                               # Domain enumerations
│   ├── AuditOperationType.cs            # CRUD operations
│   ├── NotificationType.cs              # Notification types
│   ├── PostStatus.cs                    # Draft, Published, Archived
│   ├── PaymentStatus.cs                 # ⭐ NEW: Payment lifecycle states
│   ├── PaymentMethod.cs                 # ⭐ NEW: Card, eWallet, COD, etc.
│   ├── RefundStatus.cs                  # ⭐ NEW: Refund workflow states
│   ├── RefundReason.cs                  # ⭐ NEW: Refund reasons
│   ├── GatewayEnvironment.cs            # ⭐ NEW: Sandbox/Production
│   ├── GatewayHealthStatus.cs           # ⭐ NEW: Gateway operational status
│   ├── WebhookProcessingStatus.cs       # ⭐ NEW: Webhook processing states
│   ├── PaymentOperationType.cs          # Operation types for logging
│   ├── ProductStatus.cs                 # ⭐ NEW: Draft, Active, Archived
│   ├── CartStatus.cs                    # ⭐ NEW: Active, Merged, Abandoned, Converted
│   ├── OrderStatus.cs                   # ⭐ NEW: Pending, Confirmed, Processing, Shipped, Delivered, etc.
│   ├── CheckoutSessionStatus.cs         # ⭐ NEW: Active, Completed, Expired, Abandoned
│   ├── ReservationStatus.cs             # ⭐ NEW: Pending, Reserved, Released, Expired
│   └── InventoryMovementType.cs         # ⭐ NEW: StockIn, StockOut, Adjustment, Return, etc.
├── Events/                              # Domain events
│   ├── Payment/                         # Payment domain events
│   │   └── PaymentEvents.cs             # Created, Succeeded, Failed, Refunded
│   ├── Product/                         # ⭐ NEW: Product domain events
│   │   └── ProductEvents.cs             # Created, Published, Archived
│   ├── Cart/                            # ⭐ NEW: Cart domain events
│   │   └── CartEvents.cs                # ItemAdded, ItemUpdated, ItemRemoved, Cleared
│   ├── Checkout/                        # ⭐ NEW: Checkout domain events
│   │   └── CheckoutEvents.cs            # Started, AddressSet, ShippingSelected, PaymentSelected, Completed
│   └── Order/                           # ⭐ NEW: Order domain events
│       └── OrderEvents.cs               # Created, Confirmed, Shipped, Delivered, Cancelled
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
│   ├── Payments/                        # ⭐ NEW: Payment processing
│   ├── Audit/                           # Audit log queries
│   ├── Notifications/                   # User notifications
│   ├── EmailTemplates/                  # Email template CRUD
│   ├── LegalPages/                      # Legal pages (Terms, Privacy)
│   ├── Media/                           # File upload/management
│   ├── Blog/                            # Blog CMS (Posts, Categories, Tags)
│   ├── DeveloperLogs/                   # Serilog streaming
│   ├── TenantSettings/                  # Tenant configuration (Branding, SMTP, etc.)
│   └── PlatformSettings/                # Platform-level settings
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
| **Payments** | CreatePayment, CancelPayment, ConfigureGateway, UpdateGateway, ProcessWebhook, RequestRefund, ApproveRefund, RejectRefund, ConfirmCodCollection | GetPaymentTransactions, GetPaymentTransaction, GetOrderPayments, GetPaymentGateways, GetPaymentGateway, GetActiveGateways, GetRefunds, GetPendingCodPayments, GetWebhookLogs | ⭐ **NEW:** Payment gateway integration, transactions, refunds |
| **Audit** | BulkExport | GetAuditLogs, GetEntityHistory | Audit log queries and export |
| **Notifications** | MarkAsRead, MarkAllAsRead, DeleteNotification | GetNotifications, GetUnreadCount | User notifications |
| **EmailTemplates** | UpdateEmailTemplate | GetEmailTemplates, GetEmailTemplateById | Template customization |
| **LegalPages** | UpdateLegalPage, RevertToDefault | GetLegalPages, GetLegalPage, GetPublicLegalPage | Legal page COW |
| **Media** | UploadFile, DeleteFile | GetFiles | File storage |
| **Blog** | CreatePost, UpdatePost, DeletePost, PublishPost, CreateCategory, UpdateCategory, DeleteCategory, CreateTag, UpdateTag, DeleteTag | GetPosts, GetPost, GetCategories, GetTags | Blog CMS |
| **DeveloperLogs** | - | StreamLogs | Real-time Serilog streaming |
| **TenantSettings** | UpdateBranding, UpdateContact, UpdateSmtp, UpdateRegional | GetTenantSettings, GetBranding | Tenant configuration |
| **PlatformSettings** | UpdatePlatformSettings | GetPlatformSettings | Platform-level config |
| **Products** | CreateProduct, UpdateProduct, ArchiveProduct, PublishProduct, AddProductVariant, UpdateProductVariant, DeleteProductVariant, AddProductImage, UpdateProductImage, DeleteProductImage, SetPrimaryProductImage, CreateProductCategory, UpdateProductCategory, DeleteProductCategory | GetProducts, GetProductById, GetProductCategories, GetProductCategoryById | ⭐ Product catalog with variants & images |
| **Cart** | AddToCart, UpdateCartItem, RemoveCartItem, ClearCart, MergeCart | GetCart, GetCartSummary | ⭐ Shopping cart with guest support |
| **Checkout** | InitiateCheckout, SetCheckoutAddress, SelectShipping, SelectPayment, CompleteCheckout | GetCheckoutSession | ⭐ **NEW:** Hybrid accordion checkout flow |
| **Orders** | CreateOrder, ConfirmOrder, ShipOrder, CancelOrder | GetOrders, GetOrderById | ⭐ **NEW:** Order lifecycle management |

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
│   ├── DeveloperLogHub.cs               # SignalR log streaming
│   ├── PaymentHub.cs                    # ⭐ NEW: Real-time payment updates
│   ├── IPaymentClient.cs                # ⭐ NEW: Payment hub client interface
│   └── PaymentHubContext.cs             # ⭐ NEW: Payment hub abstraction (IPaymentHubContext)
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
│   ├── ApplicationDbContextSeeder.cs    # Seeder orchestrator
│   ├── Seeders/                         # Individual domain seeders (ISeeder)
│   │   ├── TenantSeeder.cs              # Default tenant
│   │   ├── RoleSeeder.cs                # Roles and permissions
│   │   ├── UserSeeder.cs                # Platform/tenant admins
│   │   ├── EmailTemplateSeeder.cs       # Email templates
│   │   └── ...                          # LegalPage, Notification, etc.
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
│   ├── PaymentEndpoints.cs              # ⭐ NEW: /api/payments/*
│   ├── AuditEndpoints.cs                # /api/audit/*
│   ├── NotificationEndpoints.cs         # /api/notifications/*
│   ├── EmailTemplateEndpoints.cs        # /api/email-templates/*
│   ├── LegalPageEndpoints.cs            # /api/legal-pages/*
│   ├── PublicLegalPageEndpoints.cs      # /api/public/legal/*
│   ├── MediaEndpoints.cs                # /api/media/*
│   ├── FileEndpoints.cs                 # /api/files/*
│   ├── BlogEndpoints.cs                 # /api/blog/*
│   ├── FeedEndpoints.cs                 # /api/feeds/* (RSS/Atom)
│   ├── DeveloperLogEndpoints.cs         # /api/developer-logs/*
│   ├── TenantSettingsEndpoints.cs       # /api/tenant-settings/*
│   └── PlatformSettingsEndpoints.cs     # /api/platform-settings/*
├── Middleware/
│   ├── CurrentUserLoaderMiddleware.cs   # Loads user claims into context
│   ├── ExceptionHandlingMiddleware.cs   # Global error handler
│   └── TenantResolutionMiddleware.cs    # Resolves tenant from header/JWT
├── Program.cs                           # Application entry point
├── appsettings.json                     # Configuration
└── frontend/                            # React SPA (Vite)
    ├── src/
    │   ├── components/                  # Reusable components
    │   │   └── ui/                      # shadcn/ui components
    │   │       ├── combobox.tsx         # ⭐ Searchable dropdown with scroll, bank selection
    │   │       └── ...                  # Button, Dialog, Input, etc.
    │   ├── contexts/                    # React contexts (Auth, Theme, Notification, Branding, Regional)
    │   ├── hooks/                       # Custom React hooks
    │   │   ├── usePaymentGateways.ts    # ⭐ Payment gateway API hooks (TanStack Query)
    │   │   └── ...                      # useLogin, useUsers, useRoles, etc.
    │   ├── layouts/                     # Layout components
    │   ├── pages/                       # Route pages
    │   │   └── portal/admin/
    │   │       ├── tenant-settings/     # Tabbed tenant settings
    │   │       │   └── components/
    │   │       │       └── PaymentGatewaysTab.tsx  # ⭐ Gateway configuration UI
    │   │       └── payment-gateways/    # ⭐ Payment gateway management
    │   │           └── components/
    │   │               ├── GatewayCard.tsx           # Gateway provider cards
    │   │               └── ConfigureGatewayDialog.tsx # Credential configuration
    │   ├── services/                    # API services
    │   │   ├── paymentGateways.ts       # ⭐ Payment gateway API client
    │   │   └── ...                      # auth, users, roles, etc.
    │   ├── types/                       # TypeScript types
    │   │   ├── payment.ts               # ⭐ Payment gateway types
    │   │   └── ...                      # user, role, tenant types
    │   └── lib/                         # Utilities
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
| **Payments** | `/api/payments` | ⭐ **NEW:** transactions, gateways, refunds, webhooks, COD |
| **Audit** | `/api/audit` | logs, entity-history, export |
| **Notifications** | `/api/notifications` | list, mark-read, delete |
| **Email Templates** | `/api/email-templates` | CRUD, preview |
| **Legal Pages** | `/api/legal-pages`, `/api/public/legal` | CRUD, revert, public |
| **Media** | `/api/media` | upload, delete, list |
| **Blog** | `/api/blog` | posts, categories, tags (full CRUD) |
| **Feeds** | `/api/feeds` | RSS/Atom blog feeds |
| **Files** | `/api/files` | File upload/download |
| **Developer Logs** | `/api/developer-logs` | Serilog streaming, error clusters |
| **Tenant Settings** | `/api/tenant-settings` | Branding, SMTP, regional, contact |
| **Platform Settings** | `/api/platform-settings` | Platform-level configuration |
| **Products** | `/api/products` | ⭐ CRUD, variants, images, publish, archive |
| **Product Categories** | `/api/product-categories` | ⭐ CRUD, hierarchical |
| **Cart** | `/api/cart` | ⭐ add, update, remove, clear, get, merge |
| **Checkout** | `/api/checkout` | ⭐ **NEW:** initiate, address, shipping, payment, complete |
| **Orders** | `/api/orders` | ⭐ **NEW:** create, confirm, ship, cancel, list, details |
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

**Docs:** [Role Permission System](backend/research/role-permission-system-research.md)

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

### Payment Processing (NEW)

**Files:** `src/NOIR.Application/Features/Payments/`

- **Gateway Configuration** - Multi-provider support with encrypted credentials
- **Transactions** - Full payment lifecycle tracking (Pending → Paid/Failed)
- **Refunds** - Request, approve/reject workflow with audit trail
- **Webhooks** - Process payment provider callbacks with signature verification
- **COD Support** - Cash-on-Delivery collection confirmation
- **Operation Logging** - ⭐ NEW: Database audit trail for all gateway API calls

**Key Files:**
- `Commands/CreatePayment/CreatePaymentCommand.cs` - Initiate payment (implements IAuditableCommand)
- `Commands/ConfigureGateway/ConfigureGatewayCommand.cs` - Gateway setup
- `Commands/ProcessWebhook/ProcessWebhookCommand.cs` - Webhook handling
- `Commands/RequestRefund/RequestRefundCommand.cs` - Refund workflow
- `Queries/GetPaymentTransactions/GetPaymentTransactionsQuery.cs` - Transaction list
- `Queries/GetOperationLogs/GetOperationLogsQuery.cs` - ⭐ NEW: Query gateway API call logs

**Domain Entities:**
- `PaymentGateway` - Gateway configuration (Provider, EncryptedCredentials, WebhookSecret)
- `PaymentTransaction` - Transaction lifecycle (Amount, Status, PaymentMethod)
- `PaymentWebhookLog` - Webhook audit (EventType, ProcessingStatus)
- `PaymentOperationLog` - ⭐ NEW: Gateway API call audit (Request/Response, Duration, Errors)
- `Refund` - Refund tracking (Amount, Status, Reason, ApprovedBy)

**Enums:**
- `PaymentStatus` - Pending, Processing, Authorized, Paid, Failed, Cancelled, Refunded
- `PaymentMethod` - Card, eWallet, QRCode, BankTransfer, COD, BuyNowPayLater
- `RefundStatus` - Pending, Approved, Processing, Completed, Rejected, Failed
- `GatewayEnvironment` - Sandbox, Production
- `GatewayHealthStatus` - Unknown, Healthy, Degraded, Unhealthy
- `PaymentOperationType` - ⭐ NEW: InitiatePayment, ValidateWebhook, InitiateRefund, TestConnection, etc.

**Services:**
- `IPaymentService` - Payment orchestration abstraction
- `IPaymentGatewayFactory` - Gateway provider instantiation
- `IPaymentGatewayProvider` - Gateway-specific implementation interface
- `ICredentialEncryptionService` - Credential encryption/decryption
- `IPaymentOperationLogger` - ⭐ NEW: Database logging for gateway API operations

**Endpoints:**
- `GET /api/payment-webhooks/operations` - ⭐ NEW: Query operation logs with filtering

**Tests:** `tests/NOIR.Application.UnitTests/Features/Payments/`, `tests/NOIR.IntegrationTests/Features/Payments/`

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

### Legal Pages

**Files:** `src/NOIR.Application/Features/LegalPages/`

- **Copy-on-Write** - Platform defaults with tenant overrides (same as Email Templates)
- **SEO** - MetaTitle, MetaDescription, CanonicalUrl, AllowIndexing
- **Rich Editor** - TinyMCE (self-hosted) with image upload
- **Public API** - Slug-based resolution (tenant override → platform default)

**Key Files:**
- `Features/LegalPages/Commands/UpdateLegalPage/UpdateLegalPageCommand.cs` - COW update
- `Features/LegalPages/Commands/RevertLegalPageToDefault/RevertLegalPageToDefaultCommand.cs`
- `Features/LegalPages/Queries/GetPublicLegalPage/GetPublicLegalPageQuery.cs`
- `Domain/Entities/LegalPage.cs`

**Tests:** `tests/NOIR.Application.UnitTests/Features/LegalPages/`, `tests/NOIR.IntegrationTests/Features/LegalPages/`

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
├── NOIR.Domain.UnitTests/           # Domain logic tests (842 tests)
├── NOIR.Application.UnitTests/      # Handler, service, validator tests (4,125 tests)
├── NOIR.IntegrationTests/           # API integration tests (605 tests)
├── NOIR.ArchitectureTests/          # Architecture rule validation (25 tests)
└── coverage.runsettings             # Test coverage configuration
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
| [Payment Gateway Pattern](backend/patterns/payment-gateway-abstraction.md) | ⭐ **NEW:** Payment provider abstraction |

### Frontend

| Document | Purpose |
|----------|---------|
| [Frontend Overview](frontend/README.md) | Frontend architecture and setup |
| [Architecture](frontend/architecture.md) | Component structure and patterns |
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
| [Role Permission System](backend/research/role-permission-system-research.md) | Role/permission patterns |
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
dotnet ef migrations add NAME --project src/NOIR.Infrastructure --startup-project src/NOIR.Web --context ApplicationDbContext --output-dir Migrations/App
dotnet ef database update --project src/NOIR.Infrastructure --startup-project src/NOIR.Web --context ApplicationDbContext

# Frontend
cd src/NOIR.Web/frontend
npm install
npm run dev
npm run build
npm run generate:api
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
| **Permissions** | `Domain/Common/Permissions.cs` | [Role Permission](backend/research/role-permission-system-research.md) |
| **Validation** | `*Validator.cs` | [Validation Guide](backend/patterns/validation.md) |
| **Email Templates** | `EmailTemplate` entity | Knowledge Base |
| **SignalR Hubs** | `NotificationHub`, `DeveloperLogHub` | Knowledge Base |
| **Payment Processing** | `Features/Payments/`, `Services/Payment/` | ⭐ NEW: [Payment Gateway](backend/patterns/payment-gateway-abstraction.md) |

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

**Last Updated:** 2026-02-05
**Version:** 3.0
**Maintainer:** NOIR Team
**Machine-Readable Index:** [PROJECT_INDEX.json](../PROJECT_INDEX.json)

---

## Changelog

### Version 3.0 (2026-02-05) - Complete Repository Index Refresh

- **Statistics Refresh**
  - Updated file count: ~1,998 source files (*.cs, *.tsx, *.ts)
  - 26 feature modules (added Shipping integration)
  - 60+ UI components in shadcn/ui + custom
  - 28+ custom React hooks
  - 23+ API service modules
- **E2E Testing Improvements**
  - Fixed CAT-013 (subcategory creation) - added CAT-014 for partial coverage
  - Added `expectPageLoaded()` pattern across all smoke tests
  - Fixed navigation and product smoke test stability
  - CategoryDialog now fetches fresh categories via `useProductCategories` hook
- **Code Quality**
  - Updated CategoryDialog to use hook-based category fetching
  - Fixed parent dropdown showing newly created categories
  - Improved test documentation with known limitations

### Version 2.9 (2026-02-03) - Repository Index Update

- **Statistics Update**
  - Updated source file counts: 1,255 C# files, 456 test files, 305 TypeScript files
  - 43 documentation files in `docs/` folder
  - 4 test projects: Domain.UnitTests, Application.UnitTests, IntegrationTests, ArchitectureTests
- **UI/UX Improvements** (from recent sessions)
  - Fixed Active toggle alignment in BrandDialog (full-width row pattern)
  - Fixed delete button colors across 14 dialogs (softer destructive pattern)
  - Enhanced attribute input components for all 13 attribute types
  - Added localization keys for brands, product attributes
- **Inline Variant Editing** - Product variants now editable inline with auto-save

### Version 2.8 (2026-02-01) - Database Index Optimization

- **NEW: Filtered Indexes for Sparse Data** - Performance optimization for boolean columns
  - `NotificationConfiguration.cs` - 3 filtered indexes:
    - `IX_Notifications_Unread` - TenantId + UserId + CreatedAt WHERE IsRead = 0
    - `IX_Notifications_PendingDigest` - TenantId + UserId + CreatedAt WHERE IncludedInDigest = 0
    - `IX_Notifications_UnsentEmail` - TenantId + UserId + CreatedAt WHERE EmailSent = 0
  - `PostConfiguration.cs` - Filtered index for scheduled posts:
    - `IX_Posts_TenantId_ScheduledPublish` - TenantId + ScheduledPublishAt WHERE ScheduledPublishAt IS NOT NULL
  - `ProductImageConfiguration.cs` - Filtered index for primary image lookup:
    - `IX_ProductImages_TenantId_Primary` - TenantId + ProductId WHERE IsPrimary = 1
  - `RefreshTokenConfiguration.cs` - Active token lookup:
    - `IX_RefreshTokens_Active` - TenantId + UserId + ExpiresAt WHERE IsDeleted = 0
  - `PasswordResetOtpConfiguration.cs` - Active OTP lookup:
    - `IX_PasswordResetOtps_Active` - TenantId + Email + ExpiresAt WHERE IsUsed = 0 AND IsDeleted = 0
- **TenantId as Leading Column** - All filtered indexes include TenantId as first column for Finbuckle multi-tenant query optimization
- **233+ Database Indexes** - Comprehensive index coverage across all entity configurations
- **Global Query Filters** - Soft delete handled via EF Core query filters (no standalone IsDeleted indexes needed)
- **TagWith() for SQL Debugging** - All specifications tagged for SQL Profiler identification

### Version 2.7 (2026-01-29) - Product Attribute System Complete

- **Product Attribute System** - 9 phases fully implemented
  - Phase 1: Brand Entity - Product brands with logo, banner, SEO
  - Phase 2: Attribute Entities - ProductAttribute, ProductAttributeValue, CategoryAttribute (13 attribute types)
  - Phase 3: CategoryAttribute Management UI - Assign attributes to categories
  - Phase 4: ProductAttributeAssignment Entity - Product-level attribute values
  - Phase 5: ProductFilterIndex + Sync - Denormalized search/filter index
  - Phase 6: Filter API Endpoints - Faceted filtering with `FilterProductsQuery`, `GetCategoryFiltersQuery`
  - Phase 7: Analytics Events - `FilterAnalyticsEvent` for usage tracking
  - Phase 8: Frontend Filter UI - `FilterSidebar`, `FilterMobileDrawer`, facet components
  - Phase 9: Product Form Integration - `ProductAttributesSection` with dynamic attribute inputs
- **NEW: Product Attribute Components**
  - `AttributeInputFactory` - Routes to correct input by AttributeType (13 types)
  - `FacetCheckbox`, `FacetColorSwatch`, `FacetPriceRange` - Filter facet components
  - `AppliedFilters` - Removable filter chips
- **NEW: Filter Analytics**
  - `CreateFilterEventCommand` - Track filter usage
  - `GetPopularFiltersQuery` - Analytics for admin dashboard
- **Updated Statistics**: 47 entities, 22 enums, 25 features, 200+ CQRS operations
- **Tests**: Fixed Weight property removal from Product entity (5,188 tests passing)

### Version 2.6 (2026-01-26) - Phase 8 E-commerce Backend Complete
- **Phase 8 Status:** Backend 100% Complete, Frontend Pending
- **NEW: Checkout Flow** - Complete checkout session management
  - CheckoutSession entity with hybrid accordion pattern
  - 5 commands: InitiateCheckout, SetCheckoutAddress, SelectShipping, SelectPayment, CompleteCheckout
  - 1 query: GetCheckoutSession
  - Inventory reservation with configurable timeout
- **NEW: Order Management** - Complete order lifecycle
  - Order aggregate with OrderItem child entities
  - 4 commands: CreateOrder, ConfirmOrder, ShipOrder, CancelOrder
  - 2 queries: GetOrders, GetOrderById
  - Full OrderStatus workflow (Pending → Confirmed → Shipped → Delivered)
- **Enhanced Products** - Added variant and image management
  - 6 new commands: AddProductVariant, UpdateProductVariant, DeleteProductVariant, AddProductImage, UpdateProductImage, DeleteProductImage, SetPrimaryProductImage
- **Tests**: 5,188 tests (up from 5,571)
- **Statistics**: 19 feature modules, 100+ endpoints, 36 entities, 21 enums

### Version 2.5 (2026-01-25) - Phase 8 E-commerce Sprint 1
- **NEW: Product Catalog** - Complete product management with variants, pricing, inventory
  - Product entity with variants, images, and categories
  - ProductStatus workflow (Draft, Active, Archived)
  - 6 commands: CreateProduct, UpdateProduct, ArchiveProduct, PublishProduct, CreateProductCategory, UpdateProductCategory, DeleteProductCategory
  - 4 queries: GetProducts, GetProductById, GetProductCategories, GetProductCategoryById
- **NEW: Shopping Cart** - Full cart functionality with guest support
  - Cart aggregate root with CartItem child entities
  - Guest cart support via SessionId (merge on login)
  - 5 commands: AddToCart, UpdateCartItem, RemoveCartItem, ClearCart, MergeCart
  - 2 queries: GetCart, GetCartSummary
  - IAuditableCommand on all cart commands for Activity Timeline
- **Infrastructure**: EF Core migrations restructured (Migrations/App, Migrations/Tenant)
- **Tests**: 5,571 tests (up from 5,431)
- **Statistics**: 17 feature modules, 90+ endpoints, 32 entities, 13 enums

### Version 2.4 (2026-01-25)
- Added **PaymentOperationLog** entity for database audit trail of gateway API calls
- Added **PaymentOperationType** enum (InitiatePayment, ValidateWebhook, InitiateRefund, TestConnection, etc.)
- Added **IPaymentOperationLogger** service interface with Start/Complete pattern
- Added **GetOperationLogsQuery** for querying operation logs with filtering
- Added `GET /api/payment-webhooks/operations` endpoint for admin operation log access
- Added sensitive data sanitization with compiled regex patterns
- Integrated operation logging into all payment handlers (CreatePayment, ProcessWebhook, TestConnection, Refund)
- Added new error codes: GatewayError (NOIR-PAY-015), RefundFailed (NOIR-PAY-016)

### Version 2.3 (2026-01-25)
- Updated frontend structure documentation with new components:
  - `Combobox` component for searchable dropdowns (bank selection)
  - `GatewayCard` component for payment gateway display
  - `ConfigureGatewayDialog` component for credential configuration
  - `PaymentGatewaysTab` component for tenant settings
  - `usePaymentGateways` hook for payment gateway API integration
- Added payment types and services documentation

### Version 2.2 (2026-01-25)
- Added **Payments** feature module with 9 commands, 9 queries
- Added 4 new Payment domain entities (PaymentGateway, PaymentTransaction, PaymentWebhookLog, Refund)
- Added 7 new Payment enums (PaymentStatus, PaymentMethod, RefundStatus, etc.)
- Added Payment domain events
- Updated statistics: 15 feature modules, 80+ endpoints, 26 entities
- Added Payment Gateway pattern documentation reference
