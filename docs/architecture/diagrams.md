# NOIR Architecture Diagrams

**Last Updated:** 2026-02-18

These diagrams document the core architecture of the NOIR e-commerce platform. All diagrams use Mermaid syntax and can be rendered in GitHub, VS Code (with Mermaid extension), or any Mermaid-compatible viewer.

---

## Table of Contents

1. [E-commerce Entity Relationship Diagram](#1-e-commerce-entity-relationship-diagram)
2. [CQRS Command Flow Diagram](#2-cqrs-command-flow-diagram)
3. [Multi-Tenancy Architecture Diagram](#3-multi-tenancy-architecture-diagram)
4. [Order Lifecycle State Diagram](#4-order-lifecycle-state-diagram)

---

## 1. E-commerce Entity Relationship Diagram

Shows relationships between all e-commerce domain entities including products, orders, payments, cart, checkout, and inventory.

```mermaid
erDiagram
    %% ============================================
    %% PRODUCT CATALOG
    %% ============================================

    Product {
        Guid Id PK
        string Name
        string Slug
        string ShortDescription
        string Description
        decimal BasePrice
        string Currency
        ProductStatus Status
        Guid CategoryId FK
        Guid BrandId FK
        string Sku
        string Barcode
        bool TrackInventory
        int SortOrder
        string TenantId
    }

    ProductVariant {
        Guid Id PK
        Guid ProductId FK
        string Name
        string Sku
        decimal Price
        decimal CompareAtPrice
        int StockQuantity
        string OptionsJson
        int SortOrder
        Guid ImageId FK
        string TenantId
    }

    ProductImage {
        Guid Id PK
        Guid ProductId FK
        string Url
        string AltText
        bool IsPrimary
        int DisplayOrder
        string TenantId
    }

    ProductCategory {
        Guid Id PK
        string Name
        string Slug
        string Description
        Guid ParentId FK
        int SortOrder
        string ImageUrl
        int ProductCount
        string TenantId
    }

    Brand {
        Guid Id PK
        string Name
        string Slug
        string LogoUrl
        string TenantId
    }

    %% ============================================
    %% PRODUCT ATTRIBUTES
    %% ============================================

    ProductAttribute {
        Guid Id PK
        string Code
        string Name
        AttributeType Type
        bool IsFilterable
        bool IsSearchable
        bool IsRequired
        bool IsVariantAttribute
        bool IsGlobal
        string Unit
        decimal MinValue
        decimal MaxValue
        int SortOrder
        bool IsActive
        string TenantId
    }

    ProductAttributeValue {
        Guid Id PK
        Guid AttributeId FK
        string Value
        string DisplayValue
        string ColorCode
        string SwatchUrl
        int SortOrder
        bool IsActive
        int ProductCount
        string TenantId
    }

    CategoryAttribute {
        Guid Id PK
        Guid CategoryId FK
        Guid AttributeId FK
        bool IsRequired
        int SortOrder
        string TenantId
    }

    ProductAttributeAssignment {
        Guid Id PK
        Guid ProductId FK
        Guid AttributeId FK
        Guid VariantId FK
        Guid AttributeValueId FK
        string AttributeValueIds
        string TextValue
        decimal NumberValue
        bool BoolValue
        DateTime DateValue
        string ColorValue
        string FileUrl
        string DisplayValue
        string TenantId
    }

    %% ============================================
    %% SHOPPING CART
    %% ============================================

    Cart {
        Guid Id PK
        string UserId
        string SessionId
        CartStatus Status
        DateTimeOffset LastActivityAt
        DateTimeOffset ExpiresAt
        string Currency
        string TenantId
    }

    CartItem {
        Guid Id PK
        Guid CartId FK
        Guid ProductId FK
        Guid ProductVariantId FK
        string ProductName
        string VariantName
        string ImageUrl
        decimal UnitPrice
        int Quantity
        string TenantId
    }

    %% ============================================
    %% CHECKOUT
    %% ============================================

    CheckoutSession {
        Guid Id PK
        Guid CartId FK
        string UserId
        CheckoutSessionStatus Status
        DateTimeOffset ExpiresAt
        string CustomerEmail
        string CustomerPhone
        string TenantId
    }

    %% ============================================
    %% ORDERS
    %% ============================================

    Order {
        Guid Id PK
        string OrderNumber
        Guid CustomerId FK
        OrderStatus Status
        decimal SubTotal
        decimal DiscountAmount
        decimal ShippingAmount
        decimal TaxAmount
        decimal GrandTotal
        string Currency
        string CustomerEmail
        string ShippingMethod
        string TrackingNumber
        Guid CheckoutSessionId FK
        string CouponCode
        string CancellationReason
        string ReturnReason
        string TenantId
    }

    OrderItem {
        Guid Id PK
        Guid OrderId FK
        Guid ProductId FK
        Guid ProductVariantId FK
        string ProductName
        string VariantName
        string Sku
        string ImageUrl
        decimal UnitPrice
        int Quantity
        decimal DiscountAmount
        decimal TaxAmount
        string TenantId
    }

    %% ============================================
    %% PAYMENTS
    %% ============================================

    PaymentTransaction {
        Guid Id PK
        string TransactionNumber
        string GatewayTransactionId
        Guid PaymentGatewayId FK
        string Provider
        Guid OrderId FK
        Guid CustomerId FK
        decimal Amount
        string Currency
        decimal GatewayFee
        decimal NetAmount
        PaymentStatus Status
        PaymentMethod PaymentMethod
        string TenantId
    }

    PaymentGateway {
        Guid Id PK
        string Provider
        string DisplayName
        bool IsActive
        int SortOrder
        GatewayEnvironment Environment
        GatewayHealthStatus HealthStatus
        string SupportedCurrencies
        decimal MinAmount
        decimal MaxAmount
        string TenantId
    }

    Refund {
        Guid Id PK
        string RefundNumber
        Guid PaymentTransactionId FK
        string GatewayRefundId
        decimal Amount
        string Currency
        RefundStatus Status
        RefundReason Reason
        string ReasonDetail
        string TenantId
    }

    %% ============================================
    %% INVENTORY
    %% ============================================

    InventoryMovement {
        Guid Id PK
        Guid ProductVariantId FK
        Guid ProductId FK
        InventoryMovementType MovementType
        int QuantityBefore
        int QuantityMoved
        int QuantityAfter
        string Reference
        string Notes
        string UserId
        string CorrelationId
        string TenantId
    }

    %% ============================================
    %% RELATIONSHIPS
    %% ============================================

    %% Product Catalog
    Product ||--o{ ProductVariant : "has variants"
    Product ||--o{ ProductImage : "has images"
    Product }o--o| ProductCategory : "belongs to"
    Product }o--o| Brand : "branded by"
    ProductCategory ||--o{ ProductCategory : "parent-child"

    %% Product Attributes
    ProductAttribute ||--o{ ProductAttributeValue : "has predefined values"
    ProductCategory ||--o{ CategoryAttribute : "defines available attrs"
    ProductAttribute ||--o{ CategoryAttribute : "assigned to categories"
    Product ||--o{ ProductAttributeAssignment : "has attr values"
    ProductAttribute ||--o{ ProductAttributeAssignment : "value for attr"
    ProductAttributeValue ||--o{ ProductAttributeAssignment : "selected value"
    ProductVariant ||--o{ ProductAttributeAssignment : "variant-specific"

    %% Shopping Cart
    Cart ||--o{ CartItem : "contains"
    CartItem }o--|| Product : "references"
    CartItem }o--|| ProductVariant : "specific variant"

    %% Checkout
    CheckoutSession }o--|| Cart : "checks out"
    Order }o--o| CheckoutSession : "created from"

    %% Orders
    Order ||--o{ OrderItem : "contains"
    OrderItem }o--|| Product : "snapshot of"
    OrderItem }o--|| ProductVariant : "snapshot of"

    %% Payments
    Order ||--o{ PaymentTransaction : "paid via"
    PaymentTransaction }o--|| PaymentGateway : "processed by"
    PaymentTransaction ||--o{ Refund : "refunded via"

    %% Inventory
    ProductVariant ||--o{ InventoryMovement : "stock history"
    Product ||--o{ InventoryMovement : "stock history"
    ProductVariant ||--o| ProductImage : "variant image"
```

### Entity Legend

| Domain | Entities | Base Class |
|--------|----------|------------|
| **Product Catalog** | Product, ProductVariant, ProductImage, ProductCategory, Brand | TenantAggregateRoot / TenantEntity |
| **Attributes** | ProductAttribute, ProductAttributeValue, CategoryAttribute, ProductAttributeAssignment | TenantAggregateRoot / TenantEntity |
| **Cart** | Cart, CartItem | TenantAggregateRoot / TenantEntity |
| **Checkout** | CheckoutSession | TenantAggregateRoot |
| **Orders** | Order, OrderItem | TenantAggregateRoot / TenantEntity |
| **Payments** | PaymentTransaction, PaymentGateway, Refund | TenantAggregateRoot |
| **Inventory** | InventoryMovement | TenantAggregateRoot |

---

## 2. CQRS Command Flow Diagram

Shows how a request flows through the NOIR architecture using Wolverine message bus, from HTTP endpoint to database persistence.

```mermaid
flowchart TB
    subgraph Client ["Client (React SPA)"]
        FE["Frontend\n(React + TanStack Query)"]
    end

    subgraph Web ["NOIR.Web Layer"]
        EP["Minimal API Endpoint\n(OrderEndpoints.cs)"]
        AUTH["Authorization\nMiddleware\n(RequireAuthorization)"]
        TENANT["Finbuckle\nTenant Resolution"]
        AUDIT["IAuditableCommand\n(UserId set by endpoint)"]
    end

    subgraph Application ["NOIR.Application Layer"]
        CMD["Command\n(CreateOrderCommand)"]
        VAL["FluentValidation\nValidator\n(CreateOrderCommandValidator)"]
        BUS["Wolverine\nIMessageBus\n(InvokeAsync)"]
        HANDLER["Command Handler\n(CreateOrderCommandHandler)"]
        SPEC["Specification\n(OrderByIdSpec)"]
    end

    subgraph Domain ["NOIR.Domain Layer"]
        AGG["Aggregate Root\n(Order.Create())"]
        EVT["Domain Events\n(OrderCreatedEvent)"]
        REPO_I["IRepository&lt;Order, Guid&gt;"]
        UOW_I["IUnitOfWork"]
    end

    subgraph Infrastructure ["NOIR.Infrastructure Layer"]
        REPO["Repository&lt;Order, Guid&gt;\n(OrderRepository)"]
        UOW["UnitOfWork\n(SaveChangesAsync)"]
        INTERCEPT["TenantIdSetter\nInterceptor"]
        DBCTX["ApplicationDbContext\n(EF Core)"]
    end

    subgraph Database ["Database"]
        DB[("SQL Server\n+ Finbuckle\nTenantId filter")]
    end

    %% Flow
    FE -->|"HTTP POST /api/orders\n+ X-Tenant header\n+ Bearer token"| EP
    EP --> TENANT
    TENANT --> AUTH
    AUTH --> AUDIT
    AUDIT -->|"command with { UserId }"| CMD

    CMD --> BUS
    BUS -->|"1. Validate"| VAL
    VAL -->|"2. Route to handler"| HANDLER

    HANDLER -->|"3. Create aggregate"| AGG
    AGG -->|"Raise events"| EVT
    HANDLER -->|"4. repository.AddAsync()"| REPO_I
    REPO_I -.->|"implemented by"| REPO
    HANDLER -->|"5. unitOfWork.SaveChangesAsync()"| UOW_I
    UOW_I -.->|"implemented by"| UOW

    REPO --> DBCTX
    UOW --> DBCTX
    DBCTX --> INTERCEPT
    INTERCEPT -->|"Set TenantId\non new entities"| DBCTX
    DBCTX -->|"6. SaveChanges"| DB

    HANDLER -->|"7. Return Result&lt;OrderDto&gt;"| BUS
    BUS --> EP
    EP -->|"result.ToHttpResult()\n200 OK / 400 / 404"| FE

    %% Styling
    classDef web fill:#e0f2fe,stroke:#0284c7,color:#0c4a6e
    classDef app fill:#fef3c7,stroke:#d97706,color:#78350f
    classDef domain fill:#dcfce7,stroke:#16a34a,color:#14532d
    classDef infra fill:#fce7f3,stroke:#db2777,color:#831843
    classDef db fill:#f3e8ff,stroke:#9333ea,color:#581c87
    classDef client fill:#f0f9ff,stroke:#3b82f6,color:#1e3a5f

    class FE client
    class EP,AUTH,TENANT,AUDIT web
    class CMD,VAL,BUS,HANDLER,SPEC app
    class AGG,EVT,REPO_I,UOW_I domain
    class REPO,UOW,INTERCEPT,DBCTX infra
    class DB db
```

### CQRS Flow Steps

| Step | Component | Layer | Description |
|------|-----------|-------|-------------|
| 1 | Endpoint | Web | Receives HTTP request, resolves tenant, authenticates user |
| 2 | Endpoint | Web | Sets `UserId` on `IAuditableCommand` for audit trail |
| 3 | Validator | Application | FluentValidation validates the command before handler runs |
| 4 | Handler | Application | Business logic; creates/modifies aggregate roots |
| 5 | Aggregate | Domain | Factory methods enforce invariants, raise domain events |
| 6 | Repository | Infrastructure | `AddAsync()` / `UpdateAsync()` stages changes in DbContext |
| 7 | UnitOfWork | Infrastructure | `SaveChangesAsync()` persists all changes atomically |
| 8 | Interceptor | Infrastructure | `TenantIdSetterInterceptor` auto-sets TenantId on new entities |
| 9 | Handler | Application | Returns `Result<TDto>` with success/failure information |

### Query Flow (Read Path)

```mermaid
flowchart LR
    EP2["Endpoint"] -->|"GetOrdersQuery"| BUS2["Wolverine\nIMessageBus"]
    BUS2 --> QH["QueryHandler"]
    QH -->|"Specification\n(AsNoTracking)"| REPO2["Repository"]
    REPO2 -->|"EF Core\n+ TagWith()"| DB2[("Database")]
    DB2 --> REPO2
    REPO2 --> QH
    QH -->|"Map to DTO"| BUS2
    BUS2 --> EP2

    classDef web fill:#e0f2fe,stroke:#0284c7,color:#0c4a6e
    classDef app fill:#fef3c7,stroke:#d97706,color:#78350f
    classDef infra fill:#fce7f3,stroke:#db2777,color:#831843
    classDef db fill:#f3e8ff,stroke:#9333ea,color:#581c87

    class EP2 web
    class BUS2,QH app
    class REPO2 infra
    class DB2 db
```

**Key differences from Command path:**
- No validation step (queries are read-only)
- Specifications use `AsNoTracking` by default (no change tracking overhead)
- No UnitOfWork needed (no mutations)
- All specs tagged with `TagWith("MethodName")` for SQL debugging

---

## 3. Multi-Tenancy Architecture Diagram

Shows how Finbuckle.MultiTenant resolves the current tenant and how TenantId is enforced across the stack.

```mermaid
flowchart TB
    subgraph Clients ["Client Requests"]
        AUTH_REQ["Authenticated Request\n(Bearer JWT + X-Tenant header)"]
        PUBLIC_REQ["Public Request\n(X-Tenant header only)"]
        SYSTEM_REQ["System Process\n(Background job / Seeder)"]
    end

    subgraph Resolution ["Tenant Resolution (Finbuckle)"]
        HEADER["HeaderStrategy\n'X-Tenant' header"]
        CLAIM["ClaimStrategy\n'tenant_id' JWT claim"]
        STORE["EFCoreStore\n(TenantStoreDbContext)"]
        RESOLVE{{"Resolved\nTenant?"}}
    end

    subgraph TenantCtx ["Tenant Context"]
        MCTX["IMultiTenantContext\nTenantInfo.Id = 'default'"]
        NULL_CTX["No Tenant Context\n(TenantId = null)"]
    end

    subgraph DataAccess ["Data Access Layer"]
        INTERCEPTOR["TenantIdSetterInterceptor"]
        QUERY_FILTER["EF Core Global\nQuery Filter\n(.HasQueryFilter)"]
        NEW_ENTITY["New Entity\n(Added state)"]
        SYSTEM_USER["System User Check\n(IsSystemUser = true?)"]
    end

    subgraph Database ["Database"]
        DB_ROW["All tenant-scoped rows\nhave TenantId column"]
        SYS_ROW["System users have\nTenantId = NULL"]
    end

    %% Auth request flow
    AUTH_REQ -->|"1a"| HEADER
    AUTH_REQ -->|"1b"| CLAIM
    PUBLIC_REQ -->|"1a"| HEADER

    HEADER --> STORE
    CLAIM --> STORE
    STORE --> RESOLVE

    RESOLVE -->|"Yes"| MCTX
    RESOLVE -->|"No"| NULL_CTX

    %% System process
    SYSTEM_REQ -->|"No tenant header"| NULL_CTX

    %% Data access
    MCTX -->|"TenantId = 'default'"| INTERCEPTOR
    MCTX -->|"WHERE TenantId = 'default'"| QUERY_FILTER

    INTERCEPTOR --> SYSTEM_USER
    SYSTEM_USER -->|"IsSystemUser = true\nSKIP (keep null)"| SYS_ROW
    SYSTEM_USER -->|"Regular entity\nSET TenantId"| NEW_ENTITY
    NEW_ENTITY -->|"TenantId = 'default'"| DB_ROW

    QUERY_FILTER -->|"Auto-filter reads"| DB_ROW

    NULL_CTX -->|"No filter applied"| SYS_ROW

    %% Styling
    classDef client fill:#dbeafe,stroke:#2563eb,color:#1e3a5f
    classDef resolve fill:#fef9c3,stroke:#ca8a04,color:#713f12
    classDef ctx fill:#d1fae5,stroke:#059669,color:#064e3b
    classDef data fill:#fce7f3,stroke:#db2777,color:#831843
    classDef db fill:#f3e8ff,stroke:#9333ea,color:#581c87

    class AUTH_REQ,PUBLIC_REQ,SYSTEM_REQ client
    class HEADER,CLAIM,STORE,RESOLVE resolve
    class MCTX,NULL_CTX ctx
    class INTERCEPTOR,QUERY_FILTER,NEW_ENTITY,SYSTEM_USER data
    class DB_ROW,SYS_ROW db
```

### Tenant Resolution Priority

| Priority | Strategy | Source | When Used |
|----------|----------|--------|-----------|
| 1 | `WithHeaderStrategy("X-Tenant")` | HTTP header | All requests (auth + public) |
| 2 | `WithClaimStrategy("tenant_id")` | JWT claim | Authenticated requests |

**No fallback strategy**: If neither resolves a tenant, the tenant context is null. This is intentional -- system processes (seeders, background jobs) operate without tenant scope.

### Critical Rules

1. **System users (platform admins)** have `IsSystemUser = true` and `TenantId = null` -- the interceptor explicitly skips them
2. **Public endpoints** (login, forgot-password) use `apiClientPublic` which sends `X-Tenant: default` header
3. **Login endpoint** is special -- it resolves tenant from the user's email in the command body, not from the header
4. **Unique constraints** on tenant-scoped entities MUST include TenantId: `HasIndex(e => new { e.Slug, e.TenantId }).IsUnique()`
5. **Global query filters** automatically add `WHERE TenantId = @tenantId` to all queries for tenant-scoped entities

---

## 4. Order Lifecycle State Diagram

Shows all valid state transitions for an Order, derived directly from the `Order.cs` domain entity methods and their guard clauses.

```mermaid
stateDiagram-v2
    [*] --> Pending : Order.Create()

    %% Happy path (linear progression)
    Pending --> Confirmed : Confirm()\n[payment received]
    Confirmed --> Processing : StartProcessing()\n[prep for shipment]
    Processing --> Shipped : Ship(tracking, carrier)\n[handed to carrier]
    Shipped --> Delivered : MarkAsDelivered()\n[customer received]
    Delivered --> Completed : Complete()\n[order finalized]

    %% Cancellation (from early states only)
    Pending --> Cancelled : Cancel(reason?)\n[before shipment]
    Confirmed --> Cancelled : Cancel(reason?)\n[before shipment]
    Processing --> Cancelled : Cancel(reason?)\n[before shipment]

    %% Returns (from delivered/completed only)
    Delivered --> Returned : Return(reason?)\n[customer returns]
    Completed --> Returned : Return(reason?)\n[customer returns]

    %% Refunds (from most states except Pending/Cancelled/Refunded)
    Confirmed --> Refunded : MarkAsRefunded(amount)\n[full refund]
    Processing --> Refunded : MarkAsRefunded(amount)\n[full refund]
    Shipped --> Refunded : MarkAsRefunded(amount)\n[full refund]
    Delivered --> Refunded : MarkAsRefunded(amount)\n[full refund]
    Completed --> Refunded : MarkAsRefunded(amount)\n[full refund]

    %% Terminal states
    Completed --> [*]
    Cancelled --> [*]
    Refunded --> [*]
    Returned --> [*]

    %% State descriptions
    note right of Pending
        Awaiting payment
        confirmation
    end note

    note right of Shipped
        TrackingNumber and
        ShippingCarrier set
    end note

    note right of Cancelled
        CancellationReason
        + CancelledAt recorded
    end note

    note right of Returned
        ReturnReason
        + ReturnedAt recorded
    end note
```

### State Transition Matrix

This matrix shows which transitions are valid, derived from the guard clauses in `Order.cs`:

| From \ To | Pending | Confirmed | Processing | Shipped | Delivered | Completed | Cancelled | Refunded | Returned |
|-----------|---------|-----------|------------|---------|-----------|-----------|-----------|----------|----------|
| **Pending** | - | Confirm() | - | - | - | - | Cancel() | - | - |
| **Confirmed** | - | - | StartProcessing() | - | - | - | Cancel() | MarkAsRefunded() | - |
| **Processing** | - | - | - | Ship() | - | - | Cancel() | MarkAsRefunded() | - |
| **Shipped** | - | - | - | - | MarkAsDelivered() | - | - | MarkAsRefunded() | - |
| **Delivered** | - | - | - | - | - | Complete() | - | MarkAsRefunded() | Return() |
| **Completed** | - | - | - | - | - | - | - | MarkAsRefunded() | Return() |
| **Cancelled** | - | - | - | - | - | - | - | - | - |
| **Refunded** | - | - | - | - | - | - | - | - | - |
| **Returned** | - | - | - | - | - | - | - | - | - |

### Guard Clause Summary

| Method | Required Status | Side Effects |
|--------|----------------|--------------|
| `Confirm()` | Pending | Sets ConfirmedAt, raises OrderConfirmedEvent |
| `StartProcessing()` | Confirmed | Raises OrderStatusChangedEvent |
| `Ship(tracking, carrier)` | Processing | Sets ShippedAt, TrackingNumber, ShippingCarrier, raises OrderShippedEvent |
| `MarkAsDelivered()` | Shipped | Sets DeliveredAt, raises OrderDeliveredEvent |
| `Complete()` | Delivered | Sets CompletedAt, raises OrderCompletedEvent |
| `Cancel(reason?)` | Pending, Confirmed, Processing | Sets CancelledAt, CancellationReason, raises OrderCancelledEvent |
| `Return(reason?)` | Delivered, Completed | Sets ReturnedAt, ReturnReason, raises OrderReturnedEvent |
| `MarkAsRefunded(amount)` | NOT Pending, Cancelled, Refunded | Raises OrderRefundedEvent |

### Domain Events Raised

```mermaid
flowchart LR
    subgraph StatusEvents ["Status Change Events"]
        OSC["OrderStatusChangedEvent\n(all transitions)"]
    end

    subgraph SpecificEvents ["Lifecycle Events"]
        OC["OrderCreatedEvent"]
        OCONF["OrderConfirmedEvent"]
        OSHIP["OrderShippedEvent"]
        ODEL["OrderDeliveredEvent"]
        OCOMP["OrderCompletedEvent"]
        OCAN["OrderCancelledEvent"]
        ORET["OrderReturnedEvent"]
        OREF["OrderRefundedEvent"]
    end

    subgraph Handlers ["Event Handlers (Potential)"]
        EMAIL["Send notification email"]
        INV["Release/deduct inventory"]
        ANALYTICS["Update analytics"]
        WEBHOOK["Fire webhook"]
    end

    OC --> EMAIL
    OCONF --> EMAIL
    OSHIP --> EMAIL
    ODEL --> EMAIL
    OCAN --> INV
    OREF --> INV
    ORET --> INV
    OSC --> ANALYTICS
    OSC --> WEBHOOK

    classDef event fill:#fef3c7,stroke:#d97706,color:#78350f
    classDef handler fill:#d1fae5,stroke:#059669,color:#064e3b

    class OSC,OC,OCONF,OSHIP,ODEL,OCOMP,OCAN,ORET,OREF event
    class EMAIL,INV,ANALYTICS,WEBHOOK handler
```

---

## Appendix: Entity Base Classes

All domain entities inherit from a base class hierarchy that provides multi-tenancy and audit fields:

```
BaseEntity<TId>                    -- Id, DomainEvents
  |
  +-- AggregateRoot<TId>           -- (marker for aggregate boundary)
  |     |
  |     +-- TenantAggregateRoot<TId>  -- TenantId, CreatedAt, ModifiedAt
  |     |
  |     +-- PlatformTenantAggregateRoot<TId>  -- Cross-tenant access
  |
  +-- Entity<TId>                  -- (non-aggregate entities)
        |
        +-- TenantEntity<TId>     -- TenantId
        |
        +-- PlatformTenantEntity<TId>  -- Cross-tenant access
```

**Key pattern**: Aggregate roots are the only entities that can be directly persisted via `IRepository<T, TId>`. Child entities (like `OrderItem`, `CartItem`) are managed through their parent aggregate.

---

## Appendix: Technology Stack Reference

| Layer | Technology | Purpose |
|-------|-----------|---------|
| **Frontend** | React 19, TypeScript, TanStack Query v5 | SPA client |
| **UI Components** | shadcn/ui, Tailwind CSS 4, Radix UI | Component library |
| **API** | ASP.NET Core Minimal APIs | HTTP endpoints |
| **Message Bus** | Wolverine | CQRS command/query routing |
| **ORM** | EF Core 8 | Database access |
| **Multi-Tenancy** | Finbuckle.MultiTenant | Tenant isolation |
| **Validation** | FluentValidation | Command/query validation |
| **Database** | SQL Server | Persistence |
| **Auth** | ASP.NET Core Identity + JWT | Authentication |

---

**End of Architecture Diagrams**
