# NOIR E-commerce Payment Gateway Roadmap

> **Status:** Planning Complete - Ready for Implementation
> **Author:** Claude AI Assistant
> **Date:** January 2026
> **Scope:** Vietnam + International Payment Gateway Integration

---

## Key Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| **Gateway Credentials** | Per-tenant | SaaS model - each tenant configures their own merchant accounts |
| **COD Support** | Phase 6 (essential) | 20-30% of Vietnam e-commerce still uses COD |
| **UI Framework** | 21st.dev (MANDATORY) | All frontend components built with 21st.dev Magic MCP |
| **Best Practices** | Research-driven | Each phase requires business + implementation + UI/UX research |
| **Testing Strategy** | TDD / Comprehensive | Unit tests + integration tests for every feature; TDD encouraged |
| **Enum Location** | Flat `Domain/Enums/` | Consistent with existing project structure |
| **SignalR** | Add `PaymentHub` | Real-time payment status updates |
| **OrderId FK** | Nullable, no constraint until Phase 8 | Allows Payment Foundation before E-commerce Core |
| **Encryption Service** | `Infrastructure/Security/` | Dedicated folder for credential encryption |
| **COD Scope** | Admin panel only | COD confirmation via admin, not courier app |
| **Mocking Framework** | Moq | Matches existing project patterns |

---

## Development Standards (MANDATORY for ALL Phases)

### 1. UI Framework: 21st.dev

All frontend UI components and pages MUST use **21st.dev** (Magic MCP component builder):

- Payment method selectors, checkout flows, admin dashboards
- Product catalog, cart, order management pages
- Analytics dashboards, customer management UI
- Use `proxy_mcp__magic__21st_magic_component_builder` for consistent, production-quality components
- Ensure WCAG accessibility, responsive layouts, micro-interactions
- Reference existing NOIR patterns: shadcn/ui base + 21st.dev enhancements

### 2. Research-Driven Implementation

Before implementing ANY phase, conduct research to ensure best practices:

| Research Area | Scope | Output |
|---------------|-------|--------|
| **Business Logic** | Industry standards, competitor analysis, user flows | Decision document |
| **Implementation** | Architecture patterns, library choices, security | Technical spec |
| **UI/UX** | Best checkout flows, payment UX patterns, accessibility | Wireframes/references |

**Per-Phase Research Requirements:**
- Phase 5: Payment abstraction patterns (Stripe SDK patterns, gateway-agnostic design)
- Phase 6: Vietnam gateway API best practices, COD workflow UX research
- Phase 7: International payment UX, subscription billing patterns (Stripe Billing reference)
- Phase 8: E-commerce catalog UX (Shopify/WooCommerce patterns), cart abandonment prevention
- Phase 9: Customer segmentation best practices, RFM analysis implementation
- Phase 10: Vietnam shipping API integration patterns, real-time tracking UX
- Phase 11: Marketing automation patterns, flash sale concurrency handling
- Phase 12: Dashboard analytics UX (Metabase/Grafana patterns), data visualization

### 3. Testing Strategy: TDD / Comprehensive Coverage

Every feature MUST have comprehensive test coverage. TDD (Test-Driven Development) is encouraged.

#### Test Structure Per Feature
```
tests/NOIR.Domain.UnitTests/{Feature}/
├── {Entity}Tests.cs                    # Domain entity logic
├── {Entity}CreateTests.cs              # Factory method tests
└── {ValueObject}Tests.cs               # Value object tests

tests/NOIR.Application.UnitTests/Features/{Feature}/
├── Commands/
│   ├── {Action}CommandHandlerTests.cs
│   └── {Action}CommandValidatorTests.cs
├── Queries/
│   └── {Action}QueryHandlerTests.cs
└── Specifications/
    └── {Feature}SpecificationTests.cs

tests/NOIR.Infrastructure.UnitTests/{Feature}/
├── {Service}Tests.cs
└── {Provider}Tests.cs

tests/NOIR.IntegrationTests/{Feature}/
├── {Feature}EndpointsTests.cs
├── {Entity}RepositoryDiTests.cs        # DI verification (CLAUDE.md Rule 22)
└── {Feature}FlowTests.cs               # End-to-end flow tests
```

#### Testing Principles
- **TDD Encouraged**: Write failing tests first, then implement to pass
- **Unit Tests**: All Command Handlers, Validators, Domain entities, Specifications
- **Integration Tests**: All API endpoints, webhook processing, full payment flows
- **Mocking**: Use `Moq` for gateway providers in unit tests (NOT NSubstitute)
- **Test Data**: Use `Bogus` for realistic test data generation
- **Coverage Target**: Minimum 80% code coverage per phase
- **DI Verification**: New repositories MUST have DI registration tests
- **Signature Tests**: Each gateway's signature helper needs dedicated test vectors

#### Test Class Template
```csharp
public class CreatePaymentCommandHandlerTests
{
    private readonly Mock<IRepository<PaymentTransaction, Guid>> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IPaymentGatewayFactory> _gatewayFactoryMock;
    private readonly CreatePaymentCommandHandler _handler;
    private readonly Faker _faker = new();

    public CreatePaymentCommandHandlerTests()
    {
        _repositoryMock = new Mock<IRepository<PaymentTransaction, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _gatewayFactoryMock = new Mock<IPaymentGatewayFactory>();

        _handler = new CreatePaymentCommandHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _gatewayFactoryMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesPayment()
    {
        // Arrange
        var command = CreateValidCommand();
        // ... setup mocks

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private CreatePaymentCommand CreateValidCommand() => new(
        Amount: _faker.Finance.Amount(100, 10000),
        Currency: "VND",
        Provider: "vnpay"
    );
}
```

### 4. Entity Pattern (MANDATORY)

All domain entities MUST follow this pattern (from existing Post.cs):

```csharp
public class EntityName : TenantAggregateRoot<Guid>
{
    // 1. Private constructor for EF Core
    private EntityName() : base() { }

    // 2. Private constructor with ID for factory
    private EntityName(Guid id, string? tenantId = null) : base(id, tenantId) { }

    // 3. Properties with private setters
    public string Name { get; private set; } = string.Empty;
    public EntityStatus Status { get; private set; }

    // 4. Navigation properties
    public virtual ICollection<ChildEntity> Children { get; private set; } = new List<ChildEntity>();

    // 5. Factory method (REQUIRED)
    public static EntityName Create(string name, string? tenantId = null)
    {
        var entity = new EntityName(Guid.NewGuid(), tenantId)
        {
            Name = name,
            Status = EntityStatus.Active
        };
        entity.AddDomainEvent(new EntityCreatedEvent(entity.Id, name));
        return entity;
    }

    // 6. Business methods for state changes
    public void UpdateName(string name)
    {
        Name = name;
        // Domain event if needed
    }
}
```

**Child entities (non-aggregate roots):**
```csharp
public class ChildEntity : TenantEntity<Guid>
{
    private ChildEntity() { }

    internal static ChildEntity Create(Guid parentId, string value, string? tenantId = null)
    {
        return new ChildEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ParentId = parentId,
            Value = value
        };
    }
}
```

### 5. Repository & Configuration Pattern

For each entity, create:

**1. Repository** (`Infrastructure/Persistence/Repositories/{Entity}Repository.cs`):
```csharp
public class PaymentGatewayRepository : Repository<PaymentGateway, Guid>, IPaymentGatewayRepository
{
    public PaymentGatewayRepository(ApplicationDbContext context) : base(context) { }
}

public interface IPaymentGatewayRepository : IRepository<PaymentGateway, Guid> { }
```

**2. EF Configuration** (`Infrastructure/Persistence/Configurations/{Entity}Configuration.cs`):
```csharp
public class PaymentGatewayConfiguration : IEntityTypeConfiguration<PaymentGateway>
{
    public void Configure(EntityTypeBuilder<PaymentGateway> builder)
    {
        builder.ToTable("PaymentGateways");

        // Multi-tenancy unique constraint (CLAUDE.md Rule 19)
        builder.HasIndex(e => new { e.TenantId, e.Provider })
            .IsUnique()
            .HasDatabaseName("IX_PaymentGateways_TenantId_Provider");

        // Performance index for tenant queries
        builder.HasIndex(e => new { e.TenantId, e.IsActive })
            .HasDatabaseName("IX_PaymentGateways_TenantId_IsActive");

        builder.Property(e => e.Provider).IsRequired().HasMaxLength(50);
        builder.Property(e => e.DisplayName).IsRequired().HasMaxLength(100);
    }
}
```

**3. DI Verification Test** (`tests/NOIR.IntegrationTests/{Feature}/{Entity}RepositoryDiTests.cs`):
```csharp
public class PaymentGatewayRepositoryDiTests : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public void Repository_ShouldResolve_FromDI()
    {
        var repo = _serviceProvider.GetService<IRepository<PaymentGateway, Guid>>();
        repo.Should().NotBeNull();
    }
}
```

### 6. Specification Pattern

**Naming Convention:** `[Entity][Filter]Spec`

**Read-only specs (default):**
```csharp
public class ActivePaymentGatewaysSpec : Specification<PaymentGateway>
{
    public ActivePaymentGatewaysSpec()
    {
        Query.Where(g => g.IsActive)
             .OrderBy(g => g.SortOrder)
             .TagWith("GetActivePaymentGateways");  // REQUIRED
    }
}
```

**Update specs (with tracking):**
```csharp
public class PaymentGatewayByIdForUpdateSpec : Specification<PaymentGateway>
{
    public PaymentGatewayByIdForUpdateSpec(Guid id)
    {
        Query.Where(g => g.Id == id)
             .AsTracking()  // REQUIRED for mutations
             .TagWith("PaymentGatewayByIdForUpdate");
    }
}
```

**Projection specs:**
```csharp
public class PaymentGatewayListSpec : Specification<PaymentGateway, PaymentGatewayDto>
{
    public PaymentGatewayListSpec(bool? isActive = null)
    {
        Query.Where(g => !isActive.HasValue || g.IsActive == isActive.Value)
             .OrderBy(g => g.SortOrder)
             .TagWith("GetPaymentGatewayList");

        Query.Select(g => new PaymentGatewayDto
        {
            Id = g.Id,
            Provider = g.Provider,
            DisplayName = g.DisplayName,
            IsActive = g.IsActive
        });
    }
}
```

### 7. Auditable Commands (CLAUDE.md Rules 11, 13)

All mutation commands from frontend MUST implement `IAuditableCommand<TDto>`:

```csharp
public sealed record ConfigureGatewayCommand(
    Guid Id,
    string Provider,
    string DisplayName,
    bool IsActive) : IAuditableCommand<PaymentGatewayDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => Id;
    public string? GetTargetDisplayName() => DisplayName;
    public string? GetActionDescription() => $"Configured payment gateway '{DisplayName}'";
}
```

**Endpoint must set UserId:**
```csharp
group.MapPut("/{id}", async (
    Guid id,
    ConfigureGatewayRequest request,
    [FromServices] ICurrentUser currentUser,
    IMessageBus bus) =>
{
    var command = new ConfigureGatewayCommand(...) { UserId = currentUser.UserId };
    return (await bus.InvokeAsync<Result<PaymentGatewayDto>>(command)).ToHttpResult();
})
.RequireAuthorization(Permissions.PaymentGatewaysUpdate);
```

**Register before-state resolver (for Update operations):**
```csharp
// In DependencyInjection.cs
services.AddBeforeStateResolver<PaymentGatewayDto, GetPaymentGatewayQuery>(
    targetId => new GetPaymentGatewayQuery(Guid.Parse(targetId.ToString()!)));
```

**Frontend page context:**
```typescript
// In payment admin page
usePageContext('PaymentGateways')
```

### 8. Settings Classes (MANDATORY)

All configuration sections MUST have strongly-typed settings with validation:

```csharp
// Application/Common/Settings/FeatureSettings.cs
public class PaymentSettings
{
    public const string SectionName = "Payment";

    [Required]
    [StringLength(10)]
    public string TransactionNumberPrefix { get; set; } = "NOIR";

    [Required]
    public string DefaultCurrency { get; set; } = "VND";

    [Range(5, 10080)]
    public int PaymentLinkExpiryMinutes { get; set; } = 30;

    // Nested settings
    public CodSettings COD { get; set; } = new();
}

public class CodSettings
{
    public bool Enabled { get; set; } = true;

    [Range(0, 100000000)]
    public decimal MaxAmount { get; set; } = 10000000;
}
```

**Registration in DependencyInjection.cs:**
```csharp
services.AddOptions<PaymentSettings>()
    .Bind(configuration.GetSection(PaymentSettings.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();  // Fail fast on startup
```

**Usage in services:**
```csharp
public class PaymentService : IPaymentService, IScopedService
{
    private readonly PaymentSettings _settings;

    public PaymentService(IOptions<PaymentSettings> settings)
    {
        _settings = settings.Value;
    }
}
```

### 9. HTTP Client Factory Pattern

External API calls MUST use HttpClientFactory with Polly resilience:

```csharp
public static class PaymentHttpClientExtensions
{
    public static IServiceCollection AddPaymentHttpClients(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpClient<IVnPayClient, VnPayClient>()
            .ConfigureHttpClient((sp, client) =>
            {
                var settings = sp.GetRequiredService<IOptions<VnPaySettings>>().Value;
                client.BaseAddress = new Uri(settings.ApiUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy());

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, retryAttempt =>
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
    }
}
```

**NuGet packages required:** `Microsoft.Extensions.Http.Polly`

---

## Executive Summary

This roadmap extends NOIR from an enterprise admin template to a **full e-commerce platform** with Vietnam-focused payment gateway integration.

### Prerequisites (All Complete - See feature-roadmap-basic.md)
- ✅ Phase 1: Caching Infrastructure (FusionCache)
- ✅ Phase 2: Image Processing Service
- ✅ Phase 3: Blog/CMS Feature (TinyMCE v6)
- ✅ Phase 3a: Blog SEO (RSS, Sitemap, JSON-LD)
- ✅ Phase 4: Performance Hardening

### E-commerce Phases
| Phase | Feature | Priority | Complexity | Status |
|-------|---------|----------|------------|--------|
| **5** | Payment Foundation | Critical | High | ✅ Complete |
| **6** | Vietnam Domestic Gateways + COD | Critical | High | ✅ Complete |
| **7** | International & Advanced | High | Medium | ✅ Complete |
| **8** | E-commerce Core | Critical | Very High | 🔄 Backend Complete (Frontend Pending) |
| **9** | Customer Management | Medium | Medium | ⏳ Pending |
| **10** | Shipping Integration | High | High | ⏳ Pending |
| **11** | Marketing & Promotions | Medium | Medium | ⏳ Pending |
| **12** | Analytics & Reporting | Medium | Medium | ⏳ Pending |

### Phase 8 Detailed Status (Updated: January 26, 2026)
| Sprint | Scope | Backend | Frontend | Status |
|--------|-------|---------|----------|--------|
| Sprint 1 | Enums, Address VO, ProductCategory, Product | ✅ | N/A | ✅ Complete |
| Sprint 2 | Cart, Checkout, Order | ✅ | N/A | ✅ Complete |
| Sprint 3 | Admin Product UI | ⏳ | ⏳ | Pending |
| Sprint 4 | Storefront UI (Catalog, Cart, Checkout) | N/A | ⏳ | Pending |

**Test Coverage:** 5,597 tests passing (842 Domain + 4,125 Application + 25 Architecture + 605 Integration)

---

## Phase 5: Payment Foundation ✅ COMPLETE

> **Status:** ✅ Complete (January 2026)
> **Test Coverage:** All 5,374 tests passing

### 5.1 Overview
Build gateway-agnostic payment infrastructure. Per-tenant credential storage (SaaS model).

### 5.2 Domain Entities

**Location:** `src/NOIR.Domain/Entities/Payment/`

#### PaymentGateway (Per-Tenant Configuration)
```csharp
public class PaymentGateway : TenantAggregateRoot<Guid>
{
    private PaymentGateway() : base() { }
    private PaymentGateway(Guid id, string? tenantId) : base(id, tenantId) { }

    public string Provider { get; private set; } = string.Empty;  // "vnpay", "momo", "zalopay", "stripe", "cod"
    public string DisplayName { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public int SortOrder { get; private set; }
    public GatewayEnvironment Environment { get; private set; } // Sandbox/Production
    public string? EncryptedCredentials { get; private set; }    // AES-256 encrypted JSON
    public string? WebhookSecret { get; private set; }
    public string? WebhookUrl { get; private set; }
    public decimal? MinAmount { get; private set; }
    public decimal? MaxAmount { get; private set; }
    public string SupportedCurrencies { get; private set; } = "[]"; // ["VND","USD"]
    public DateTimeOffset? LastHealthCheck { get; private set; }
    public GatewayHealthStatus HealthStatus { get; private set; }

    public static PaymentGateway Create(
        string provider,
        string displayName,
        GatewayEnvironment environment,
        string? tenantId = null)
    {
        var gateway = new PaymentGateway(Guid.NewGuid(), tenantId)
        {
            Provider = provider,
            DisplayName = displayName,
            Environment = environment,
            IsActive = false,
            HealthStatus = GatewayHealthStatus.Unknown
        };
        gateway.AddDomainEvent(new PaymentGatewayCreatedEvent(gateway.Id, provider));
        return gateway;
    }

    public void Configure(string encryptedCredentials, string? webhookSecret)
    {
        EncryptedCredentials = encryptedCredentials;
        WebhookSecret = webhookSecret;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    public void UpdateHealthStatus(GatewayHealthStatus status)
    {
        HealthStatus = status;
        LastHealthCheck = DateTimeOffset.UtcNow;
    }
}
```

#### PaymentTransaction
```csharp
public class PaymentTransaction : TenantAggregateRoot<Guid>
{
    private PaymentTransaction() : base() { }
    private PaymentTransaction(Guid id, string? tenantId) : base(id, tenantId) { }

    public string TransactionNumber { get; private set; } = string.Empty;  // NOIR-generated
    public string? GatewayTransactionId { get; private set; }
    public Guid PaymentGatewayId { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public Guid? OrderId { get; private set; }  // Nullable until Phase 8
    public Guid? CustomerId { get; private set; }

    // Financial
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "VND";
    public decimal? ExchangeRate { get; private set; }
    public decimal? GatewayFee { get; private set; }
    public decimal? NetAmount { get; private set; }

    // Status
    public PaymentStatus Status { get; private set; }
    public string? FailureReason { get; private set; }
    public string? FailureCode { get; private set; }

    // Details
    public PaymentMethod PaymentMethod { get; private set; }
    public string? PaymentMethodDetail { get; private set; }
    public string? PayerInfo { get; private set; }

    // Metadata
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public string? ReturnUrl { get; private set; }
    public string? GatewayResponseJson { get; private set; }
    public string? MetadataJson { get; private set; }

    // Timing
    public DateTimeOffset? PaidAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }

    // COD-specific
    public string? CodCollectorName { get; private set; }
    public DateTimeOffset? CodCollectedAt { get; private set; }

    // Idempotency
    public string IdempotencyKey { get; private set; } = string.Empty;

    // Navigation
    public virtual PaymentGateway? Gateway { get; private set; }
    public virtual ICollection<Refund> Refunds { get; private set; } = new List<Refund>();

    public static PaymentTransaction Create(
        string transactionNumber,
        Guid paymentGatewayId,
        string provider,
        decimal amount,
        string currency,
        PaymentMethod paymentMethod,
        string idempotencyKey,
        string? tenantId = null)
    {
        var transaction = new PaymentTransaction(Guid.NewGuid(), tenantId)
        {
            TransactionNumber = transactionNumber,
            PaymentGatewayId = paymentGatewayId,
            Provider = provider,
            Amount = amount,
            Currency = currency,
            PaymentMethod = paymentMethod,
            Status = PaymentStatus.Pending,
            IdempotencyKey = idempotencyKey
        };
        transaction.AddDomainEvent(new PaymentCreatedEvent(
            transaction.Id, transactionNumber, amount, currency, provider));
        return transaction;
    }

    public void MarkAsPaid(string gatewayTransactionId)
    {
        var oldStatus = Status;
        Status = PaymentStatus.Paid;
        PaidAt = DateTimeOffset.UtcNow;
        GatewayTransactionId = gatewayTransactionId;

        AddDomainEvent(new PaymentStatusChangedEvent(Id, oldStatus, Status));
        AddDomainEvent(new PaymentSucceededEvent(Id, Provider, Amount, gatewayTransactionId));
    }

    public void MarkAsFailed(string reason, string? failureCode = null)
    {
        var oldStatus = Status;
        Status = PaymentStatus.Failed;
        FailureReason = reason;
        FailureCode = failureCode;

        AddDomainEvent(new PaymentStatusChangedEvent(Id, oldStatus, Status, reason));
        AddDomainEvent(new PaymentFailedEvent(Id, reason, failureCode));
    }

    public void ConfirmCodCollection(string collectorName)
    {
        if (PaymentMethod != PaymentMethod.COD)
            throw new InvalidOperationException("Only COD payments can be confirmed for collection");

        var oldStatus = Status;
        Status = PaymentStatus.CodCollected;
        CodCollectorName = collectorName;
        CodCollectedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new PaymentStatusChangedEvent(Id, oldStatus, Status));
        AddDomainEvent(new CodCollectedEvent(Id, collectorName, CodCollectedAt.Value));
    }
}
```

#### PaymentWebhookLog
```csharp
public class PaymentWebhookLog : TenantAggregateRoot<Guid>
{
    private PaymentWebhookLog() : base() { }

    public Guid PaymentGatewayId { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public string EventType { get; private set; } = string.Empty;
    public string? GatewayEventId { get; private set; }
    public string RequestBody { get; private set; } = string.Empty;
    public string? RequestHeaders { get; private set; }
    public string? SignatureValue { get; private set; }
    public bool SignatureValid { get; private set; }
    public WebhookProcessingStatus ProcessingStatus { get; private set; }
    public string? ProcessingError { get; private set; }
    public int RetryCount { get; private set; }
    public Guid? PaymentTransactionId { get; private set; }
    public string? IpAddress { get; private set; }

    public static PaymentWebhookLog Create(
        Guid paymentGatewayId,
        string provider,
        string eventType,
        string requestBody,
        string? tenantId = null)
    {
        return new PaymentWebhookLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PaymentGatewayId = paymentGatewayId,
            Provider = provider,
            EventType = eventType,
            RequestBody = requestBody,
            ProcessingStatus = WebhookProcessingStatus.Received
        };
    }
}
```

#### Refund
```csharp
public class Refund : TenantAggregateRoot<Guid>
{
    private Refund() : base() { }
    private Refund(Guid id, string? tenantId) : base(id, tenantId) { }

    public string RefundNumber { get; private set; } = string.Empty;
    public Guid PaymentTransactionId { get; private set; }
    public string? GatewayRefundId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "VND";
    public RefundStatus Status { get; private set; }
    public RefundReason Reason { get; private set; }
    public string? ReasonDetail { get; private set; }
    public string? RequestedBy { get; private set; }
    public string? ApprovedBy { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public string? GatewayResponseJson { get; private set; }

    // Navigation
    public virtual PaymentTransaction? PaymentTransaction { get; private set; }

    public static Refund Create(
        string refundNumber,
        Guid paymentTransactionId,
        decimal amount,
        string currency,
        RefundReason reason,
        string? reasonDetail,
        string requestedBy,
        string? tenantId = null)
    {
        var refund = new Refund(Guid.NewGuid(), tenantId)
        {
            RefundNumber = refundNumber,
            PaymentTransactionId = paymentTransactionId,
            Amount = amount,
            Currency = currency,
            Reason = reason,
            ReasonDetail = reasonDetail,
            RequestedBy = requestedBy,
            Status = RefundStatus.Pending
        };
        refund.AddDomainEvent(new RefundRequestedEvent(refund.Id, paymentTransactionId, amount, reason));
        return refund;
    }

    public void Approve(string approvedBy)
    {
        ApprovedBy = approvedBy;
        Status = RefundStatus.Approved;
    }

    public void Complete(string gatewayRefundId)
    {
        GatewayRefundId = gatewayRefundId;
        Status = RefundStatus.Completed;
        ProcessedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(new RefundCompletedEvent(Id, PaymentTransactionId, Amount));
    }
}
```

### 5.3 Enums

**Location:** `src/NOIR.Domain/Enums/` (flat folder, one file per enum)

```csharp
// PaymentStatus.cs
public enum PaymentStatus
{
    Pending,        // Created, awaiting action
    Processing,     // Sent to gateway
    RequiresAction, // 3DS, QR scan, etc.
    Authorized,     // Authorized but not captured
    Paid,           // Successfully paid
    Failed,         // Payment failed
    Cancelled,      // Cancelled by user
    Expired,        // Payment link expired
    Refunded,       // Fully refunded
    PartialRefund,  // Partially refunded
    CodPending,     // COD: Awaiting delivery collection
    CodCollected    // COD: Cash collected by courier
}

// PaymentMethod.cs
public enum PaymentMethod
{
    EWallet,        // MoMo, ZaloPay
    QRCode,         // VNPay QR, MoMo QR
    BankTransfer,   // ATM/Internet Banking
    CreditCard,
    DebitCard,
    Installment,
    COD,            // Cash on Delivery
    BuyNowPayLater
}

// GatewayEnvironment.cs
public enum GatewayEnvironment { Sandbox, Production }

// GatewayHealthStatus.cs
public enum GatewayHealthStatus { Unknown, Healthy, Degraded, Unhealthy }

// WebhookProcessingStatus.cs
public enum WebhookProcessingStatus { Received, Processing, Processed, Failed, Skipped }

// RefundStatus.cs
public enum RefundStatus { Pending, Approved, Processing, Completed, Rejected, Failed }

// RefundReason.cs
public enum RefundReason { CustomerRequest, Defective, WrongItem, NotDelivered, Duplicate, Other }
```

### 5.4 Domain Events

**Location:** `src/NOIR.Domain/Events/Payment/`

```csharp
public record PaymentCreatedEvent(
    Guid TransactionId,
    string TransactionNumber,
    decimal Amount,
    string Currency,
    string Provider) : DomainEvent;

public record PaymentStatusChangedEvent(
    Guid TransactionId,
    PaymentStatus OldStatus,
    PaymentStatus NewStatus,
    string? Reason = null) : DomainEvent;

public record PaymentSucceededEvent(
    Guid TransactionId,
    string Provider,
    decimal Amount,
    string? GatewayTransactionId) : DomainEvent;

public record PaymentFailedEvent(
    Guid TransactionId,
    string Reason,
    string? FailureCode) : DomainEvent;

public record CodCollectedEvent(
    Guid TransactionId,
    string CollectorName,
    DateTimeOffset CollectedAt) : DomainEvent;

public record PaymentGatewayCreatedEvent(
    Guid GatewayId,
    string Provider) : DomainEvent;

public record RefundRequestedEvent(
    Guid RefundId,
    Guid TransactionId,
    decimal Amount,
    RefundReason Reason) : DomainEvent;

public record RefundCompletedEvent(
    Guid RefundId,
    Guid TransactionId,
    decimal Amount) : DomainEvent;
```

### 5.5 Gateway Abstraction
```csharp
public interface IPaymentGatewayProvider
{
    string ProviderName { get; }
    bool SupportsCOD { get; }

    Task<Result<PaymentInitiationResult>> InitiatePaymentAsync(
        PaymentInitiationRequest request, CancellationToken ct = default);

    Task<Result<PaymentStatusResult>> QueryStatusAsync(
        string gatewayTransactionId, CancellationToken ct = default);

    Task<Result<RefundResult>> RefundAsync(
        RefundRequest request, CancellationToken ct = default);

    Task<Result<WebhookValidationResult>> ValidateWebhookAsync(
        WebhookPayload payload, CancellationToken ct = default);

    Task<Result<GatewayHealthStatus>> HealthCheckAsync(CancellationToken ct = default);
}

public interface IPaymentGatewayFactory : IScopedService
{
    IPaymentGatewayProvider GetProvider(string providerName);
    IReadOnlyList<string> GetAvailableProviders();
}
```

### 5.6 Specifications

**Location:** `src/NOIR.Application/Features/Payments/Specifications/`

| Spec Name | Purpose | Tracking |
|-----------|---------|----------|
| `PaymentTransactionByIdSpec` | Get by ID | No |
| `PaymentTransactionByIdForUpdateSpec` | Get for status update | Yes |
| `PaymentTransactionByNumberSpec` | Lookup by transaction number | No |
| `PaymentTransactionsByOrderSpec` | All payments for an order | No |
| `PendingPaymentsSpec` | Payments awaiting completion | No |
| `ExpiredPaymentsSpec` | Payments past expiry for cleanup | No |
| `PendingCodPaymentsSpec` | COD awaiting collection | No |
| `PaymentGatewayByIdSpec` | Get gateway config | No |
| `PaymentGatewayByIdForUpdateSpec` | Get for config update | Yes |
| `ActivePaymentGatewaysSpec` | Active gateways for checkout | No |
| `PaymentGatewayByProviderSpec` | Get by provider name | No |
| `RefundsByPaymentSpec` | Refunds for a payment | No |
| `PendingRefundsSpec` | Refunds awaiting processing | No |
| `WebhookLogsByPaymentSpec` | Webhook history for payment | No |
| `UnprocessedWebhooksSpec` | Failed webhooks for retry | No |

### 5.7 Files to Create
```
src/NOIR.Domain/
├── Entities/Payment/
│   ├── PaymentGateway.cs
│   ├── PaymentTransaction.cs
│   ├── PaymentWebhookLog.cs
│   └── Refund.cs
├── Enums/
│   ├── PaymentStatus.cs
│   ├── PaymentMethod.cs
│   ├── GatewayEnvironment.cs
│   ├── GatewayHealthStatus.cs
│   ├── WebhookProcessingStatus.cs
│   ├── RefundStatus.cs
│   └── RefundReason.cs
└── Events/Payment/
    ├── PaymentCreatedEvent.cs
    ├── PaymentStatusChangedEvent.cs
    ├── PaymentSucceededEvent.cs
    ├── PaymentFailedEvent.cs
    ├── CodCollectedEvent.cs
    ├── RefundRequestedEvent.cs
    └── RefundCompletedEvent.cs

src/NOIR.Application/
├── Common/
│   ├── Interfaces/
│   │   ├── IPaymentGatewayProvider.cs
│   │   ├── IPaymentGatewayFactory.cs
│   │   └── IPaymentService.cs
│   └── Settings/
│       └── PaymentSettings.cs
└── Features/Payments/
    ├── Commands/
    │   ├── CreatePayment/
    │   ├── ProcessWebhook/
    │   ├── RequestRefund/
    │   ├── ConfigureGateway/
    │   ├── CancelPayment/
    │   └── ConfirmCodCollection/
    ├── Queries/
    │   ├── GetPaymentById/
    │   ├── GetPayments/
    │   ├── GetPaymentsByOrder/
    │   ├── GetGatewayConfigs/
    │   ├── GetWebhookLogs/
    │   ├── GetRefunds/
    │   └── GetPendingCodPayments/
    ├── DTOs/
    │   ├── PaymentTransactionDto.cs
    │   ├── PaymentGatewayDto.cs
    │   ├── RefundDto.cs
    │   └── WebhookLogDto.cs
    └── Specifications/
        ├── PaymentTransactionSpecs.cs
        ├── PaymentGatewaySpecs.cs
        └── WebhookLogSpecs.cs

src/NOIR.Infrastructure/
├── Payment/
│   ├── PaymentService.cs
│   ├── PaymentGatewayFactory.cs
│   └── WebhookProcessor.cs
├── Security/
│   └── CredentialEncryptionService.cs
└── Persistence/
    ├── Repositories/
    │   ├── PaymentGatewayRepository.cs
    │   ├── PaymentTransactionRepository.cs
    │   ├── RefundRepository.cs
    │   └── PaymentWebhookLogRepository.cs
    └── Configurations/
        ├── PaymentGatewayConfiguration.cs
        ├── PaymentTransactionConfiguration.cs
        ├── RefundConfiguration.cs
        └── PaymentWebhookLogConfiguration.cs

src/NOIR.Web/
├── Endpoints/
│   ├── PaymentEndpoints.cs
│   └── WebhookEndpoints.cs
└── Hubs/
    └── PaymentHub.cs
```

### 5.8 API Endpoints
```
# Customer-facing
POST   /api/payments                    # Create payment
GET    /api/payments/{id}               # Get payment by ID
GET    /api/payments                    # List payments
POST   /api/payments/{id}/cancel        # Cancel pending payment
POST   /api/payments/{id}/refund        # Request refund
GET    /api/payments/{id}/refunds       # Get refunds for payment

# COD-specific (Admin only)
GET    /api/payments/cod/pending        # List pending COD payments
POST   /api/payments/{id}/cod/confirm   # Confirm COD collected

# Webhook endpoints (no auth - signature verification only)
POST   /api/webhooks/vnpay              # VNPay IPN
POST   /api/webhooks/momo               # MoMo webhook
POST   /api/webhooks/zalopay            # ZaloPay callback
POST   /api/webhooks/stripe             # Stripe webhook

# Admin gateway configuration
GET    /api/admin/payment-gateways             # List configured gateways
POST   /api/admin/payment-gateways             # Add gateway config
PUT    /api/admin/payment-gateways/{id}        # Update gateway config
DELETE /api/admin/payment-gateways/{id}        # Remove gateway
POST   /api/admin/payment-gateways/{id}/test   # Test gateway connection
GET    /api/admin/payment-gateways/{id}/health # Health check
GET    /api/admin/webhook-logs                 # View webhook logs
```

**Webhook Auth Exclusion:**
```csharp
// In Program.cs or endpoint configuration
app.MapGroup("/api/webhooks")
   .AllowAnonymous()  // No JWT auth
   .AddEndpointFilter<WebhookSignatureValidationFilter>();  // Custom signature validation
```

### 5.9 SignalR Hub

**Location:** `src/NOIR.Web/Hubs/PaymentHub.cs`

```csharp
public class PaymentHub : Hub
{
    public async Task JoinPaymentGroup(string transactionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"payment:{transactionId}");
    }

    public async Task LeavePaymentGroup(string transactionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"payment:{transactionId}");
    }
}

// Usage in handlers
public class PaymentStatusChangedEventHandler
{
    private readonly IHubContext<PaymentHub> _hubContext;

    public async Task Handle(PaymentStatusChangedEvent notification, CancellationToken ct)
    {
        await _hubContext.Clients
            .Group($"payment:{notification.TransactionId}")
            .SendAsync("PaymentStatusChanged", new
            {
                notification.TransactionId,
                notification.OldStatus,
                notification.NewStatus
            }, ct);
    }
}
```

### 5.10 Configuration
```json
{
  "Payment": {
    "TransactionNumberPrefix": "NOIR",
    "DefaultCurrency": "VND",
    "WebhookBaseUrl": "https://your-domain.com",
    "IdempotencyKeyExpirySeconds": 86400,
    "PaymentLinkExpiryMinutes": 30,
    "MaxRefundDays": 30,
    "RequireRefundApproval": true,
    "RefundApprovalThreshold": 1000000,
    "EncryptionKeyId": "payment-credentials-key",
    "COD": {
      "Enabled": true,
      "MaxAmount": 10000000,
      "CollectionReminderHours": 48
    },
    "Reconciliation": {
      "Enabled": true,
      "CronSchedule": "0 6 * * *",
      "AlertEmail": null
    }
  }
}
```

### 5.11 Background Jobs (Hangfire)
```csharp
// Recurring jobs
"payment:reconciliation"     // Daily reconciliation check
"payment:expire-pending"     // Expire stale pending payments
"payment:health-check"       // Gateway health monitoring
"payment:retry-webhooks"     // Retry failed webhook processing
"payment:cod-reminders"      // Send COD collection reminders
```

### 5.12 Error Codes (extend ErrorCodes.cs)
```csharp
public static class Payment
{
    public const string GatewayNotConfigured = "NOIR-PAY-5001";
    public const string GatewayUnavailable = "NOIR-PAY-5002";
    public const string InvalidAmount = "NOIR-PAY-5003";
    public const string CurrencyNotSupported = "NOIR-PAY-5004";
    public const string TransactionNotFound = "NOIR-PAY-5005";
    public const string TransactionExpired = "NOIR-PAY-5006";
    public const string InvalidWebhookSignature = "NOIR-PAY-5007";
    public const string DuplicateWebhook = "NOIR-PAY-5008";
    public const string RefundExceedsAmount = "NOIR-PAY-5009";
    public const string RefundWindowExpired = "NOIR-PAY-5010";
    public const string PaymentAlreadyCompleted = "NOIR-PAY-5011";
    public const string GatewayError = "NOIR-PAY-5012";
    public const string IdempotencyConflict = "NOIR-PAY-5013";
    public const string CodAmountExceedsLimit = "NOIR-PAY-5014";
    public const string CodAlreadyCollected = "NOIR-PAY-5015";
}
```

### 5.13 Security Considerations
1. **Credential Encryption**: AES-256 encrypted in database (per-tenant) via `CredentialEncryptionService`
2. **Webhook Signature Verification**: Per-provider HMAC/RSA schemes
3. **Idempotent Processing**: Deduplicate by `GatewayEventId`
4. **IP Whitelisting**: Optional per gateway
5. **Rate Limiting**: Per-tenant payment creation limits
6. **PCI DSS Level 3**: No card data stored - redirect/tokenization only
7. **Audit Trail**: All commands implement `IAuditableCommand`

---

## Phase 6: Vietnam Domestic Gateways + COD ✅ COMPLETE

> **Status:** ✅ Complete (January 2026)
> **Test Coverage:** All 5,421 tests passing (47 new payment signature tests added)

### 6.1 Gateway Priority & Settlement
| Gateway | Users | Settlement | COD Support | Priority |
|---------|-------|------------|-------------|----------|
| **MoMo** | 60M+ | T+0/T+1 | No | 1 |
| **VNPay** | Leader | T+1 | No | 2 |
| **ZaloPay** | Growing | T+1 | No | 3 |
| **SePay** | QR/Bank Transfer | Instant (T+0) | No | 4 |
| **COD** | 20-30% | Manual | Yes (core) | 5 |

> **SePay Note:** Unlike redirect-based gateways, SePay uses VietQR + direct bank transfer + webhook confirmation. Lower fees (~50% reduction), instant settlement, 30+ banks supported.

### 6.2 Implementation Structure
```
src/NOIR.Infrastructure/Payment/Providers/
├── VnPay/
│   ├── VnPayProvider.cs
│   ├── VnPaySettings.cs
│   ├── VnPaySignatureHelper.cs
│   ├── VnPayResponseParser.cs
│   ├── VnPayClient.cs
│   └── VnPayWebhookHandler.cs
├── MoMo/
│   ├── MoMoProvider.cs
│   ├── MoMoSettings.cs
│   ├── MoMoSignatureHelper.cs
│   ├── MoMoResponseParser.cs
│   ├── MoMoClient.cs
│   └── MoMoWebhookHandler.cs
├── ZaloPay/
│   ├── ZaloPayProvider.cs
│   ├── ZaloPaySettings.cs
│   ├── ZaloPaySignatureHelper.cs
│   ├── ZaloPayResponseParser.cs
│   ├── ZaloPayClient.cs
│   └── ZaloPayWebhookHandler.cs
├── SePay/
│   ├── SePayProvider.cs           # IPaymentGatewayProvider implementation
│   ├── SePaySettings.cs           # ApiToken, BankAccount, BankCode, WebhookAuth
│   ├── SePayClient.cs             # API client for transaction queries
│   ├── SePayWebhookHandler.cs     # Webhook receiver + payment matching
│   ├── SePayQrCodeGenerator.cs    # VietQR URL generation
│   └── SePayTransactionMatcher.cs # Reference code matching logic
└── COD/
    ├── CodProvider.cs
    ├── CodSettings.cs
    └── CodCollectionService.cs
```

### 6.3 Gateway Settings Classes

**VnPaySettings.cs:**
```csharp
public class VnPaySettings
{
    public const string SectionName = "VnPay";

    [Required]
    public string TmnCode { get; set; } = string.Empty;

    [Required]
    public string HashSecret { get; set; } = string.Empty;

    [Required]
    [Url]
    public string PaymentUrl { get; set; } = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";

    [Required]
    [Url]
    public string ApiUrl { get; set; } = "https://sandbox.vnpayment.vn/merchant_webapi/api/transaction";

    public string Version { get; set; } = "2.1.0";
}
```

**MoMoSettings.cs:**
```csharp
public class MoMoSettings
{
    public const string SectionName = "MoMo";

    [Required]
    public string PartnerCode { get; set; } = string.Empty;

    [Required]
    public string AccessKey { get; set; } = string.Empty;

    [Required]
    public string SecretKey { get; set; } = string.Empty;

    [Required]
    [Url]
    public string ApiEndpoint { get; set; } = "https://test-payment.momo.vn/v2/gateway/api";

    public string RequestType { get; set; } = "captureWallet";
}
```

**ZaloPaySettings.cs:**
```csharp
public class ZaloPaySettings
{
    public const string SectionName = "ZaloPay";

    [Required]
    public string AppId { get; set; } = string.Empty;

    [Required]
    public string Key1 { get; set; } = string.Empty;

    [Required]
    public string Key2 { get; set; } = string.Empty;

    [Required]
    [Url]
    public string Endpoint { get; set; } = "https://sb-openapi.zalopay.vn/v2";
}
```

### 6.4 VNPay Integration

**API Version:** VNPay v2.1
**Signature:** HMAC-SHA512
**Methods:** ATM, Internet Banking, QR Code, Credit/Debit Card, Installment

**Flow:**
1. Create payment URL with params
2. Redirect user to VNPay
3. User completes payment
4. VNPay sends IPN + redirects user
5. Verify HMAC-SHA512 signature
6. Update transaction status

### 6.5 MoMo Integration

**API Version:** MoMo Partner API v2
**Signature:** HMAC-SHA256
**Methods:** MoMo Wallet, QR Code, ATM, Credit Card

**Flow:**
1. Create payment request via API
2. Receive `payUrl` or `deeplink`
3. User pays in MoMo app/web
4. MoMo sends IPN webhook
5. Verify HMAC-SHA256 signature
6. Update transaction status

### 6.6 ZaloPay Integration

**API Version:** ZaloPay API v2
**Signature:** HMAC-SHA256
**Methods:** ZaloPay Wallet, ATM, Credit Card, QR

**Flow:**
1. Create order via API
2. Receive `order_url`
3. User pays in ZaloPay/bank
4. ZaloPay sends callback
5. Verify MAC signature
6. Update transaction status

### 6.6a SePay Integration (QR + Bank Transfer)

**API Version:** SePay User API
**Authentication:** Bearer Token (API Key)
**Methods:** VietQR Code + Direct Bank Transfer

**Key Difference from Other Gateways:**
SePay does NOT use redirect-based payment flow. Instead:
- Generate VietQR code containing bank account + amount + reference
- Customer scans QR and transfers directly from their banking app
- SePay monitors merchant's bank account for incoming transfers
- Webhook sent when payment detected (within ~10 seconds)

**Flow:**
1. Create PaymentTransaction with unique reference code (e.g., `NOIR-ABC-TXN123`)
2. Generate VietQR URL: `https://qr.sepay.vn/img?acc={BankAccount}&bank={BankCode}&amount={Amount}&des={Reference}`
3. Display QR to customer
4. Customer scans QR with banking app (VCB, MB, ACB, etc.)
5. Customer completes bank transfer
6. SePay detects balance change, sends webhook to configured URL
7. Webhook handler matches payment by reference code in transfer content
8. Verify amount matches, update transaction to Paid

**SePaySettings.cs:**
```csharp
public class SePaySettings
{
    public const string SectionName = "SePay";

    [Required]
    public string ApiToken { get; set; } = string.Empty;

    [Required]
    public string BankAccountNumber { get; set; } = string.Empty;

    [Required]
    public string BankCode { get; set; } = string.Empty;  // e.g., "MBBank", "VCB"

    [Required]
    [Url]
    public string ApiBaseUrl { get; set; } = "https://my.sepay.vn/userapi";

    public string QrBaseUrl { get; set; } = "https://qr.sepay.vn/img";

    public SePayWebhookAuthType WebhookAuthType { get; set; } = SePayWebhookAuthType.ApiKey;

    public string? WebhookApiKey { get; set; }
}

public enum SePayWebhookAuthType { None, ApiKey, OAuth2 }
```

**Webhook Payload:**
```json
{
  "id": 123456,
  "gateway": "MBBank",
  "transactionDate": "2024-01-25 10:30:00",
  "accountNumber": "1234567890",
  "code": "TXN123",
  "content": "NOIR-ABC-TXN123 Payment for Order",
  "transferType": "in",
  "transferAmount": 500000,
  "accumulated": 1500000,
  "referenceCode": "NOIR-ABC-TXN123"
}
```

**Transaction Matching Logic:**
1. Extract reference code from `content` or `referenceCode` field
2. Query PaymentTransaction by reference (TransactionNumber)
3. Verify `transferAmount` matches expected Amount
4. If mismatch: flag for manual review, don't auto-confirm
5. If match: mark as Paid, send SignalR notification

**Timeout Handling:**
- Background job queries SePay API every 5 minutes for transactions
- Matches any pending transactions older than 30 minutes
- Prevents missed webhooks from leaving transactions stuck

### 6.7 Signature Verification Tests

Each gateway signature helper MUST have test vectors:

```csharp
public class VnPaySignatureHelperTests
{
    [Theory]
    [InlineData("vnp_Amount=1000000&vnp_Command=pay", "expected_hash_here")]
    [InlineData("vnp_Amount=2000000&vnp_Command=pay", "expected_hash_here")]
    public void CreateSignature_ValidInput_ReturnsExpectedHash(string data, string expectedHash)
    {
        var result = VnPaySignatureHelper.CreateSignature(data, TestHashSecret);
        result.Should().Be(expectedHash);
    }

    [Fact]
    public void VerifySignature_ValidSignature_ReturnsTrue()
    {
        // Use known test vectors from VNPay documentation
    }

    [Fact]
    public void VerifySignature_TamperedData_ReturnsFalse()
    {
        // Modify one character and verify failure
    }
}
```

### 6.8 COD Integration

**Flow:**
1. User selects COD at checkout
2. Order created with `PaymentStatus.CodPending`
3. Order shipped to customer
4. Courier collects cash
5. Admin confirms collection via admin panel
6. Transaction marked `CodCollected`

**COD-specific Commands:**
- `ConfirmCodCollectionCommand` - Mark COD as collected (Admin only)
- `GetPendingCodPaymentsQuery` - List awaiting collection

**COD Reconciliation:**
- Daily job to check unconfirmed COD payments
- Send reminders for overdue collections
- Generate COD reconciliation report

### 6.9 Frontend Components (21st.dev MANDATORY)

**IMPORTANT:** All UI components MUST be built using 21st.dev Magic MCP. Research best payment UX patterns (Stripe Checkout, Shopify) before implementation.

**Payment Method Selection:**
```tsx
// src/NOIR.Web/frontend/src/portal-app/checkout/components/PaymentMethodSelector.tsx
// Built with 21st.dev: glassmorphism cards, smooth animations, mobile-first
interface PaymentMethodOption {
  provider: string;
  displayName: string;
  iconUrl: string;
  description: string;
  isAvailable: boolean;
}
```

**21st.dev Component Requirements:**
- `PaymentMethodCard` - Selectable payment option with provider logo, hover states
- `PaymentProcessingOverlay` - Loading state with skeleton, QR code display for MoMo/ZaloPay
- `PaymentResultCard` - Success/failure state with clear CTA
- `CodConfirmationDialog` - Admin dialog for COD collection confirmation
- `GatewayConfigForm` - Admin form for gateway credential setup (masked inputs)

**Payment Flow Pages:**
- `/checkout/payment` - Method selection (grid of PaymentMethodCards)
- `/checkout/payment/processing` - Loading state (PaymentProcessingOverlay)
- `/checkout/payment/return` - Gateway return handler (auto-redirect)
- `/checkout/success` - Success confirmation (PaymentResultCard + order summary)
- `/checkout/failed` - Failure message (PaymentResultCard + retry options)

**Admin Pages (21st.dev):**
- `/admin/payment-gateways` - Gateway configuration list + CRUD
- `/admin/webhook-logs` - Webhook log viewer with filtering
- `/admin/cod-pending` - COD collection management dashboard

---

## Phase 7: International & Advanced ✅ COMPLETE

> **Status:** ✅ Complete (January 2026)
> **Test Coverage:** All 5,421 tests passing

### 7.0 Overview

Phase 7 adds international payment capabilities with Stripe integration, PayOS support for Vietnam, subscription billing infrastructure, installment payments, and multi-currency conversion services.

**Implemented Components:**
- Stripe payment gateway provider (Payment Intents, 3D Secure, webhooks)
- PayOS payment gateway provider (modern Vietnam gateway with QR payments)
- Subscription billing domain entities (SubscriptionPlan, Subscription)
- Installment payment entity (PaymentInstallment)
- Multi-currency service with exchange rate caching
- Application layer DTOs and specifications

### 7.1 Stripe Integration

**NuGet:** `Stripe.net`
**Features:** Payment Intents, 3D Secure, Multi-currency, Subscriptions

**Configuration:**
```json
{
  "Stripe": {
    "SecretKey": "...",
    "PublishableKey": "...",
    "WebhookSecret": "...",
    "ApiVersion": "2023-10-16"
  }
}
```

### 7.2 PayOS Integration

Modern Vietnam gateway with excellent developer experience. (TBD - Research required)

### 7.3 Subscription Billing

**Enums (Location: `src/NOIR.Domain/Enums/`):**
```csharp
// BillingInterval.cs
public enum BillingInterval
{
    Daily = 1,
    Weekly = 7,
    Monthly = 30,
    Quarterly = 90,
    Yearly = 365
}

// SubscriptionStatus.cs
public enum SubscriptionStatus
{
    Trialing,
    Active,
    PastDue,
    Cancelled,
    Expired,
    Paused
}
```

**Entities:**
```csharp
public class Subscription : TenantAggregateRoot<Guid>
{
    private Subscription() : base() { }
    private Subscription(Guid id, string? tenantId) : base(id, tenantId) { }

    public Guid CustomerId { get; private set; }
    public Guid PlanId { get; private set; }
    public SubscriptionStatus Status { get; private set; }
    public DateTimeOffset CurrentPeriodStart { get; private set; }
    public DateTimeOffset CurrentPeriodEnd { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public DateTimeOffset? TrialEnd { get; private set; }
    public BillingInterval Interval { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "VND";

    public static Subscription Create(
        Guid customerId,
        Guid planId,
        BillingInterval interval,
        decimal amount,
        string currency,
        int? trialDays = null,
        string? tenantId = null)
    {
        var subscription = new Subscription(Guid.NewGuid(), tenantId)
        {
            CustomerId = customerId,
            PlanId = planId,
            Interval = interval,
            Amount = amount,
            Currency = currency,
            Status = trialDays.HasValue ? SubscriptionStatus.Trialing : SubscriptionStatus.Active,
            CurrentPeriodStart = DateTimeOffset.UtcNow,
            CurrentPeriodEnd = DateTimeOffset.UtcNow.AddDays((int)interval),
            TrialEnd = trialDays.HasValue ? DateTimeOffset.UtcNow.AddDays(trialDays.Value) : null
        };
        return subscription;
    }
}

public class SubscriptionPlan : TenantAggregateRoot<Guid>
{
    private SubscriptionPlan() : base() { }

    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public decimal Price { get; private set; }
    public string Currency { get; private set; } = "VND";
    public BillingInterval Interval { get; private set; }
    public int? TrialDays { get; private set; }
    public string? FeaturesJson { get; private set; }
    public bool IsActive { get; private set; }
}
```

### 7.4 Installment Payments

**Entity:**
```csharp
public class PaymentInstallment : TenantEntity<Guid>
{
    public Guid PaymentTransactionId { get; private set; }
    public int InstallmentNumber { get; private set; }  // 1, 2, 3...
    public int TotalInstallments { get; private set; }  // 3, 6, 12
    public decimal Amount { get; private set; }
    public DateTimeOffset DueDate { get; private set; }
    public InstallmentStatus Status { get; private set; }
    public DateTimeOffset? PaidAt { get; private set; }
    public string? GatewayReference { get; private set; }
}

public enum InstallmentStatus
{
    Scheduled,
    Pending,
    Paid,
    Failed,
    Cancelled
}
```

**Background job:**
```csharp
// Daily job to process due installments
_backgroundJobs.RecurringJob<IInstallmentService>(
    "payment:process-installments",
    x => x.ProcessDueInstallmentsAsync(),
    Cron.Daily(6));  // 6 AM daily
```

### 7.5 Multi-Currency
```csharp
public interface ICurrencyService : IScopedService
{
    Task<decimal> ConvertAsync(
        decimal amount,
        string fromCurrency,
        string toCurrency,
        CancellationToken ct = default);

    Task<ExchangeRate> GetRateAsync(
        string fromCurrency,
        string toCurrency,
        CancellationToken ct = default);

    IReadOnlyList<string> GetSupportedCurrencies();
}
```

**Implementation notes:**
- Cache exchange rates with FusionCache (1 hour TTL)
- Source: External API (exchangerate-api.com) or manual configuration
- Fallback: Use last known rate if API fails
- Audit: Log all conversions for financial reporting

---

## Phase 8: E-commerce Core 🔄 BACKEND COMPLETE

> **Status:** Backend Complete (January 26, 2026), Frontend Pending
> **Test Coverage:** 18 test files, all passing
> **What's Done:** Domain entities, Application layer, API endpoints
> **What's Pending:** Admin Product UI, Storefront UI (Catalog, Cart, Checkout pages)

### 8.1 E-commerce Enums

**Location:** `src/NOIR.Domain/Enums/`

```csharp
// ProductStatus.cs
public enum ProductStatus
{
    Draft,
    Active,
    Archived,
    OutOfStock
}

// CartStatus.cs
public enum CartStatus
{
    Active,
    Abandoned,
    Converted,  // Became an order
    Expired,
    Merged      // Merged with another cart
}

// OrderStatus.cs
public enum OrderStatus
{
    Pending,
    Confirmed,
    Processing,
    Shipped,
    Delivered,
    Completed,
    Cancelled,
    Refunded
}

// CouponType.cs
public enum CouponType
{
    Percentage,
    FixedAmount,
    FreeShipping,
    BuyXGetY
}

// InventoryMovementType.cs
public enum InventoryMovementType
{
    StockIn,
    StockOut,
    Adjustment,
    Return,
    Reservation,
    ReservationRelease,
    Damaged,
    Expired
}
```

### 8.2 Address Value Object

**Location:** `src/NOIR.Domain/ValueObjects/Address.cs`

```csharp
public record Address
{
    public string FullName { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string AddressLine1 { get; init; } = string.Empty;
    public string? AddressLine2 { get; init; }
    public string Ward { get; init; } = string.Empty;
    public string District { get; init; } = string.Empty;
    public string Province { get; init; } = string.Empty;
    public string Country { get; init; } = "Vietnam";
    public string? PostalCode { get; init; }
    public bool IsDefault { get; init; }
}
```

**EF Core configuration:**
```csharp
// In OrderConfiguration.cs
builder.OwnsOne(o => o.ShippingAddress, address =>
{
    address.Property(a => a.FullName).HasMaxLength(100);
    address.Property(a => a.Phone).HasMaxLength(20);
    address.Property(a => a.AddressLine1).HasMaxLength(200);
    address.Property(a => a.AddressLine2).HasMaxLength(200);
    address.Property(a => a.Ward).HasMaxLength(100);
    address.Property(a => a.District).HasMaxLength(100);
    address.Property(a => a.Province).HasMaxLength(100);
    address.Property(a => a.Country).HasMaxLength(100);
    address.Property(a => a.PostalCode).HasMaxLength(20);
});

builder.OwnsOne(o => o.BillingAddress, address =>
{
    // Same configuration
});
```

### 8.3 Product Catalog

```csharp
public class Product : TenantAggregateRoot<Guid>
{
    private Product() : base() { }
    private Product(Guid id, string? tenantId) : base(id, tenantId) { }

    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? DescriptionHtml { get; private set; }
    public decimal BasePrice { get; private set; }
    public string Currency { get; private set; } = "VND";
    public ProductStatus Status { get; private set; }
    public Guid? CategoryId { get; private set; }
    public string? Sku { get; private set; }
    public string? Barcode { get; private set; }
    public decimal? Weight { get; private set; }
    public bool TrackInventory { get; private set; }
    public string? MetaTitle { get; private set; }
    public string? MetaDescription { get; private set; }
    public int SortOrder { get; private set; }

    public virtual ICollection<ProductVariant> Variants { get; private set; } = new List<ProductVariant>();
    public virtual ICollection<ProductImage> Images { get; private set; } = new List<ProductImage>();

    public static Product Create(
        string name,
        string slug,
        decimal basePrice,
        string currency = "VND",
        string? tenantId = null)
    {
        var product = new Product(Guid.NewGuid(), tenantId)
        {
            Name = name,
            Slug = slug,
            BasePrice = basePrice,
            Currency = currency,
            Status = ProductStatus.Draft,
            TrackInventory = true
        };

        product.AddDomainEvent(new ProductCreatedEvent(product.Id, name, slug));
        return product;
    }

    public ProductVariant AddVariant(string name, decimal price, string? sku = null)
    {
        var variant = ProductVariant.Create(Id, name, price, sku, TenantId);
        Variants.Add(variant);
        return variant;
    }

    public void Publish()
    {
        if (Status == ProductStatus.Draft)
        {
            Status = ProductStatus.Active;
            AddDomainEvent(new ProductPublishedEvent(Id, Name));
        }
    }

    public void Archive()
    {
        Status = ProductStatus.Archived;
        AddDomainEvent(new ProductArchivedEvent(Id));
    }
}

public class ProductVariant : TenantEntity<Guid>
{
    private ProductVariant() { }

    public Guid ProductId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Sku { get; private set; }
    public decimal Price { get; private set; }
    public decimal? CompareAtPrice { get; private set; }

    [ConcurrencyCheck]
    public int StockQuantity { get; private set; }

    public string? OptionsJson { get; private set; }

    internal static ProductVariant Create(
        Guid productId,
        string name,
        decimal price,
        string? sku,
        string? tenantId)
    {
        return new ProductVariant
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ProductId = productId,
            Name = name,
            Price = price,
            Sku = sku,
            StockQuantity = 0
        };
    }

    public void ReserveStock(int quantity, Guid orderId)
    {
        if (StockQuantity < quantity)
            throw new InvalidOperationException("Insufficient stock");

        StockQuantity -= quantity;
        // Create InventoryMovement record in handler
    }

    public void ReleaseStock(int quantity)
    {
        StockQuantity += quantity;
    }
}

public class ProductCategory : TenantAggregateRoot<Guid>
{
    private ProductCategory() : base() { }

    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public Guid? ParentId { get; private set; }
    public int SortOrder { get; private set; }
    public string? ImageUrl { get; private set; }
}
```

### 8.4 Shopping Cart

```csharp
public class Cart : TenantAggregateRoot<Guid>
{
    private Cart() : base() { }
    private Cart(Guid id, string? tenantId) : base(id, tenantId) { }

    public Guid? CustomerId { get; private set; }
    public string? SessionId { get; private set; }
    public CartStatus Status { get; private set; }
    public string Currency { get; private set; } = "VND";
    public DateTimeOffset? AbandonedAt { get; private set; }
    public virtual ICollection<CartItem> Items { get; private set; } = new List<CartItem>();

    public decimal SubTotal => Items.Sum(i => i.LineTotal);
    public int ItemCount => Items.Sum(i => i.Quantity);

    public static Cart Create(Guid? customerId, string? sessionId, string? tenantId = null)
    {
        return new Cart(Guid.NewGuid(), tenantId)
        {
            CustomerId = customerId,
            SessionId = sessionId,
            Status = CartStatus.Active
        };
    }

    public void AddItem(Guid productId, Guid? variantId, int quantity, decimal unitPrice)
    {
        var existingItem = Items.FirstOrDefault(i =>
            i.ProductId == productId && i.VariantId == variantId);

        if (existingItem != null)
        {
            existingItem.UpdateQuantity(existingItem.Quantity + quantity);
        }
        else
        {
            Items.Add(CartItem.Create(Id, productId, variantId, quantity, unitPrice, TenantId));
        }
    }

    public void MarkAsAbandoned()
    {
        Status = CartStatus.Abandoned;
        AbandonedAt = DateTimeOffset.UtcNow;
    }

    public void MarkAsConverted()
    {
        Status = CartStatus.Converted;
    }
}

public class CartItem : TenantEntity<Guid>
{
    private CartItem() { }

    public Guid CartId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid? VariantId { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal LineTotal => UnitPrice * Quantity;

    internal static CartItem Create(
        Guid cartId,
        Guid productId,
        Guid? variantId,
        int quantity,
        decimal unitPrice,
        string? tenantId)
    {
        return new CartItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CartId = cartId,
            ProductId = productId,
            VariantId = variantId,
            Quantity = quantity,
            UnitPrice = unitPrice
        };
    }

    public void UpdateQuantity(int quantity)
    {
        Quantity = quantity;
    }
}
```

### 8.5 Orders

```csharp
public class Order : TenantAggregateRoot<Guid>
{
    private Order() : base() { }
    private Order(Guid id, string? tenantId) : base(id, tenantId) { }

    public string OrderNumber { get; private set; } = string.Empty;
    public Guid? CustomerId { get; private set; }
    public OrderStatus Status { get; private set; }

    // Financial
    public decimal SubTotal { get; private set; }
    public decimal? DiscountAmount { get; private set; }
    public decimal? ShippingAmount { get; private set; }
    public decimal? TaxAmount { get; private set; }
    public decimal GrandTotal { get; private set; }
    public string Currency { get; private set; } = "VND";

    // Addresses (Owned types)
    public Address? ShippingAddress { get; private set; }
    public Address? BillingAddress { get; private set; }

    // Shipping
    public string? ShippingMethod { get; private set; }
    public string? TrackingNumber { get; private set; }
    public string? ShippingCarrier { get; private set; }

    // Customer
    public string CustomerEmail { get; private set; } = string.Empty;
    public string? CustomerPhone { get; private set; }
    public string? CustomerName { get; private set; }

    // Notes & Coupons
    public string? CustomerNote { get; private set; }
    public string? InternalNote { get; private set; }
    public string? CouponCode { get; private set; }
    public Guid? CouponId { get; private set; }

    public virtual ICollection<OrderItem> Items { get; private set; } = new List<OrderItem>();
    public virtual ICollection<PaymentTransaction> Payments { get; private set; } = new List<PaymentTransaction>();

    public static Order Create(
        string orderNumber,
        string customerEmail,
        decimal subTotal,
        decimal grandTotal,
        string? tenantId = null)
    {
        var order = new Order(Guid.NewGuid(), tenantId)
        {
            OrderNumber = orderNumber,
            CustomerEmail = customerEmail,
            SubTotal = subTotal,
            GrandTotal = grandTotal,
            Status = OrderStatus.Pending
        };
        order.AddDomainEvent(new OrderCreatedEvent(order.Id, orderNumber));
        return order;
    }

    public void Confirm()
    {
        Status = OrderStatus.Confirmed;
        AddDomainEvent(new OrderConfirmedEvent(Id, OrderNumber));
    }

    public void Ship(string trackingNumber, string carrier)
    {
        TrackingNumber = trackingNumber;
        ShippingCarrier = carrier;
        Status = OrderStatus.Shipped;
        AddDomainEvent(new OrderShippedEvent(Id, trackingNumber));
    }
}
```

### 8.6 Inventory Concurrency Handling

**Problem:** Two customers buying the last item simultaneously.

**Solution:** Optimistic concurrency with retry:

```csharp
public class ReserveStockCommandHandler
{
    private readonly IRepository<ProductVariant, Guid> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public async Task<Result> Handle(ReserveStockCommand cmd, CancellationToken ct)
    {
        const int maxRetries = 3;

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                var variant = await _repository.FirstOrDefaultAsync(
                    new ProductVariantByIdForUpdateSpec(cmd.VariantId), ct);

                if (variant == null)
                    return Result.Failure(Error.NotFound("Variant not found"));

                if (variant.StockQuantity < cmd.Quantity)
                    return Result.Failure(Error.Validation("stock", "Insufficient stock"));

                variant.ReserveStock(cmd.Quantity, cmd.OrderId);
                await _unitOfWork.SaveChangesAsync(ct);

                return Result.Success();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (attempt == maxRetries - 1)
                    return Result.Failure(Error.Conflict("Stock changed, please retry"));

                await Task.Delay(100 * (attempt + 1), ct);  // Exponential backoff
            }
        }

        return Result.Failure(Error.Conflict("Failed to reserve stock"));
    }
}
```

### 8.7 Inventory Management

```csharp
public class InventoryMovement : TenantEntity<Guid>
{
    private InventoryMovement() { }

    public Guid ProductVariantId { get; private set; }
    public int QuantityChange { get; private set; }
    public InventoryMovementType Type { get; private set; }
    public string? ReferenceId { get; private set; }
    public string? Note { get; private set; }
    public int StockAfter { get; private set; }

    public static InventoryMovement Create(
        Guid variantId,
        int quantityChange,
        InventoryMovementType type,
        int stockAfter,
        string? referenceId = null,
        string? tenantId = null)
    {
        return new InventoryMovement
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ProductVariantId = variantId,
            QuantityChange = quantityChange,
            Type = type,
            StockAfter = stockAfter,
            ReferenceId = referenceId
        };
    }
}
```

### 8.8 Coupon System

```csharp
public class Coupon : TenantAggregateRoot<Guid>
{
    private Coupon() : base() { }
    private Coupon(Guid id, string? tenantId) : base(id, tenantId) { }

    public string Code { get; private set; } = string.Empty;
    public CouponType Type { get; private set; }
    public decimal Value { get; private set; }
    public decimal? MinOrderAmount { get; private set; }
    public decimal? MaxDiscountAmount { get; private set; }
    public int? UsageLimit { get; private set; }
    public int UsageCount { get; private set; }
    public int? PerUserLimit { get; private set; }
    public DateTimeOffset? StartsAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public bool IsActive { get; private set; }
    public string? ApplicableProductsJson { get; private set; }
    public string? ApplicableCategoriesJson { get; private set; }

    public static Coupon Create(
        string code,
        CouponType type,
        decimal value,
        string? tenantId = null)
    {
        return new Coupon(Guid.NewGuid(), tenantId)
        {
            Code = code.ToUpperInvariant(),
            Type = type,
            Value = value,
            IsActive = true
        };
    }

    public Result<decimal> CalculateDiscount(decimal orderTotal)
    {
        if (!IsActive)
            return Result<decimal>.Failure(Error.Validation("coupon", "Coupon is not active"));

        if (ExpiresAt.HasValue && DateTimeOffset.UtcNow > ExpiresAt.Value)
            return Result<decimal>.Failure(Error.Validation("coupon", "Coupon has expired"));

        if (MinOrderAmount.HasValue && orderTotal < MinOrderAmount.Value)
            return Result<decimal>.Failure(Error.Validation("coupon", $"Minimum order amount is {MinOrderAmount}"));

        var discount = Type switch
        {
            CouponType.Percentage => orderTotal * (Value / 100),
            CouponType.FixedAmount => Value,
            CouponType.FreeShipping => 0, // Handled separately
            _ => 0
        };

        if (MaxDiscountAmount.HasValue && discount > MaxDiscountAmount.Value)
            discount = MaxDiscountAmount.Value;

        return Result<decimal>.Success(discount);
    }
}
```

### 8.9 E-commerce Specifications

| Spec Name | Purpose |
|-----------|---------|
| `ProductBySlugSpec` | Get product by URL slug |
| `ProductByIdForUpdateSpec` | Get for editing (tracking) |
| `ProductsByCategorySpec` | Products in category (paginated) |
| `ActiveProductsSpec` | Published products for storefront |
| `LowStockVariantsSpec` | Variants below threshold |
| `CartByIdSpec` | Get cart |
| `CartByIdForUpdateSpec` | Get for modification (tracking) |
| `ActiveCartForCustomerSpec` | Customer's current cart |
| `AbandonedCartsSpec` | Carts older than X days |
| `OrderByIdSpec` | Get order |
| `OrderByNumberSpec` | Get by order number |
| `OrdersByCustomerSpec` | Customer's orders (paginated) |
| `PendingOrdersSpec` | Orders awaiting processing |
| `CouponByCodeSpec` | Lookup coupon by code |
| `ValidCouponsForCartSpec` | Coupons applicable to cart |

### 8.10 Checkout Flow Commands

```csharp
// Cart Management
AddToCartCommand              // Add item to cart
UpdateCartItemCommand         // Change quantity
RemoveCartItemCommand         // Remove item
ClearCartCommand              // Empty cart
MergeCartsCommand             // Merge guest cart with user cart

// Checkout Flow
CreateCheckoutCommand         // Cart → Checkout session
UpdateShippingAddressCommand  // Set shipping address
UpdateBillingAddressCommand   // Set billing address (optional)
SelectShippingMethodCommand   // Choose shipping option
ApplyCouponCommand            // Validate and apply coupon
RemoveCouponCommand           // Remove applied coupon
RecalculateTotalsCommand      // Recalculate after changes
ValidateCheckoutCommand       // Final validation before payment
PlaceOrderCommand             // Create order + initiate payment

// Post-Checkout
ConfirmPaymentReturnCommand   // Handle gateway return
UpdateOrderStatusCommand      // Admin status updates
CancelOrderCommand            // Cancel order
```

---

## Phase 9: Customer Management

### 9.1 Customer Entity

```csharp
public class Customer : TenantAggregateRoot<Guid>
{
    private Customer() : base() { }
    private Customer(Guid id, string? tenantId) : base(id, tenantId) { }

    public Guid? UserId { get; private set; }  // Link to Identity user
    public string Email { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public string FullName => $"{FirstName} {LastName}".Trim();

    // RFM Analysis (denormalized for performance)
    public DateTimeOffset? LastOrderDate { get; private set; }
    public int TotalOrders { get; private set; }
    public decimal TotalSpent { get; private set; }
    public CustomerSegment Segment { get; private set; }

    // Tier/Group
    public CustomerTier Tier { get; private set; }
    public int LoyaltyPoints { get; private set; }

    // Preferences
    public string? PreferredCurrency { get; private set; }
    public string? PreferredLanguage { get; private set; }
    public bool MarketingOptIn { get; private set; }

    public virtual ICollection<CustomerAddress> Addresses { get; private set; } = new List<CustomerAddress>();

    public static Customer Create(string email, string? tenantId = null)
    {
        return new Customer(Guid.NewGuid(), tenantId)
        {
            Email = email,
            Segment = CustomerSegment.New,
            Tier = CustomerTier.Standard
        };
    }

    public void UpdateRfmMetrics(DateTimeOffset orderDate, decimal orderAmount)
    {
        LastOrderDate = orderDate;
        TotalOrders++;
        TotalSpent += orderAmount;
        RecalculateSegment();
    }

    private void RecalculateSegment()
    {
        // RFM scoring logic
        var daysSinceLastOrder = LastOrderDate.HasValue
            ? (DateTimeOffset.UtcNow - LastOrderDate.Value).Days
            : int.MaxValue;

        Segment = (daysSinceLastOrder, TotalOrders, TotalSpent) switch
        {
            (< 30, > 10, > 10000000) => CustomerSegment.VIP,
            (< 60, > 3, _) => CustomerSegment.Active,
            (< 90, _, _) => CustomerSegment.AtRisk,
            _ => CustomerSegment.Lost
        };
    }

    public void AddLoyaltyPoints(int points)
    {
        LoyaltyPoints += points;
        RecalculateTier();
    }

    private void RecalculateTier()
    {
        Tier = LoyaltyPoints switch
        {
            >= 10000 => CustomerTier.Platinum,
            >= 5000 => CustomerTier.Gold,
            >= 1000 => CustomerTier.Silver,
            _ => CustomerTier.Standard
        };
    }
}

public enum CustomerSegment { New, Active, AtRisk, Lost, VIP }
public enum CustomerTier { Standard, Silver, Gold, Platinum }
```

### 9.2 Customer Address

```csharp
public class CustomerAddress : TenantEntity<Guid>
{
    private CustomerAddress() { }

    public Guid CustomerId { get; private set; }
    public string Label { get; private set; } = string.Empty;  // "Home", "Work"
    public Address Address { get; private set; } = null!;
    public bool IsDefault { get; private set; }

    internal static CustomerAddress Create(
        Guid customerId,
        string label,
        Address address,
        bool isDefault,
        string? tenantId)
    {
        return new CustomerAddress
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CustomerId = customerId,
            Label = label,
            Address = address,
            IsDefault = isDefault
        };
    }
}
```

### 9.3 RFM Analysis Background Job

```csharp
_backgroundJobs.RecurringJob<ICustomerAnalyticsService>(
    "customer:rfm-analysis",
    x => x.RecalculateAllSegmentsAsync(),
    Cron.Daily(2));  // 2 AM daily
```

**Query for segment distribution:**
```csharp
public record CustomerSegmentDistributionQuery : IQuery<CustomerSegmentDistributionDto>;

public record CustomerSegmentDistributionDto(
    int NewCount,
    int ActiveCount,
    int AtRiskCount,
    int LostCount,
    int VipCount);
```

### 9.4 Customer Specifications

| Spec Name | Purpose |
|-----------|---------|
| `CustomerByIdSpec` | Get by ID |
| `CustomerByEmailSpec` | Get by email |
| `CustomerByUserIdSpec` | Get by Identity user ID |
| `CustomersBySegmentSpec` | Filter by segment |
| `CustomersByTierSpec` | Filter by tier |
| `VipCustomersSpec` | VIP customers for special offers |
| `AtRiskCustomersSpec` | Customers needing retention |

---

## Phase 10: Shipping Integration

### 10.1 Shipping Entities

```csharp
public class ShippingCarrier : TenantAggregateRoot<Guid>
{
    private ShippingCarrier() : base() { }

    public string Code { get; private set; } = string.Empty;  // "ghn", "ghtk", "viettelpost"
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public string? EncryptedCredentials { get; private set; }
    public string? WebhookSecret { get; private set; }
    public int SortOrder { get; private set; }

    public static ShippingCarrier Create(string code, string name, string? tenantId = null)
    {
        return new ShippingCarrier
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Code = code,
            Name = name,
            IsActive = false
        };
    }
}

public class ShippingRate : TenantEntity<Guid>
{
    public Guid CarrierId { get; private set; }
    public string? FromProvince { get; private set; }
    public string? ToProvince { get; private set; }
    public decimal? MinWeight { get; private set; }
    public decimal? MaxWeight { get; private set; }
    public decimal BaseRate { get; private set; }
    public decimal? PerKgRate { get; private set; }
    public int EstimatedDays { get; private set; }
}

public class Shipment : TenantAggregateRoot<Guid>
{
    private Shipment() : base() { }

    public Guid OrderId { get; private set; }
    public Guid CarrierId { get; private set; }
    public string? TrackingNumber { get; private set; }
    public ShipmentStatus Status { get; private set; }
    public decimal? CodAmount { get; private set; }
    public bool CodCollected { get; private set; }
    public string? CarrierResponseJson { get; private set; }

    public virtual ICollection<TrackingEvent> TrackingEvents { get; private set; } = new List<TrackingEvent>();

    public static Shipment Create(Guid orderId, Guid carrierId, decimal? codAmount, string? tenantId = null)
    {
        return new Shipment
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrderId = orderId,
            CarrierId = carrierId,
            CodAmount = codAmount,
            Status = ShipmentStatus.Pending
        };
    }

    public void UpdateTracking(string trackingNumber)
    {
        TrackingNumber = trackingNumber;
        Status = ShipmentStatus.InTransit;
    }
}

public class TrackingEvent : TenantEntity<Guid>
{
    public Guid ShipmentId { get; private set; }
    public DateTimeOffset EventTime { get; private set; }
    public string Status { get; private set; } = string.Empty;
    public string? Location { get; private set; }
    public string? Description { get; private set; }
}

public enum ShipmentStatus
{
    Pending,
    PickedUp,
    InTransit,
    OutForDelivery,
    Delivered,
    Failed,
    Returned
}
```

### 10.2 Shipping Provider Abstraction

```csharp
public interface IShippingProvider
{
    string ProviderCode { get; }

    Task<Result<ShippingRateResult>> GetRatesAsync(
        ShippingRateRequest request,
        CancellationToken ct = default);

    Task<Result<CreateShipmentResult>> CreateShipmentAsync(
        CreateShipmentRequest request,
        CancellationToken ct = default);

    Task<Result<TrackingResult>> GetTrackingAsync(
        string trackingNumber,
        CancellationToken ct = default);

    Task<Result<CancelShipmentResult>> CancelShipmentAsync(
        string trackingNumber,
        CancellationToken ct = default);
}

public interface IShippingProviderFactory : IScopedService
{
    IShippingProvider GetProvider(string providerCode);
    IReadOnlyList<string> GetAvailableProviders();
}
```

### 10.3 Vietnam Carriers

| Carrier | Code | API Docs | COD Support |
|---------|------|----------|-------------|
| GHN (Giao Hàng Nhanh) | `ghn` | Yes | Yes |
| GHTK (Giao Hàng Tiết Kiệm) | `ghtk` | Yes | Yes |
| Viettel Post | `viettelpost` | Yes | Yes |
| J&T Express | `jt` | Yes | Yes |

---

## Phase 11: Marketing & Promotions

### 11.1 Flash Sale with Distributed Lock

```csharp
public class FlashSale : TenantAggregateRoot<Guid>
{
    private FlashSale() : base() { }

    public string Name { get; private set; } = string.Empty;
    public DateTimeOffset StartTime { get; private set; }
    public DateTimeOffset EndTime { get; private set; }
    public FlashSaleStatus Status { get; private set; }

    public virtual ICollection<FlashSaleItem> Items { get; private set; } = new List<FlashSaleItem>();

    public static FlashSale Create(string name, DateTimeOffset startTime, DateTimeOffset endTime, string? tenantId = null)
    {
        return new FlashSale
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            StartTime = startTime,
            EndTime = endTime,
            Status = FlashSaleStatus.Scheduled
        };
    }

    public bool IsActive => Status == FlashSaleStatus.Active &&
        DateTimeOffset.UtcNow >= StartTime &&
        DateTimeOffset.UtcNow <= EndTime;
}

public class FlashSaleItem : TenantEntity<Guid>
{
    public Guid FlashSaleId { get; private set; }
    public Guid ProductVariantId { get; private set; }
    public decimal SalePrice { get; private set; }
    public int TotalQuantity { get; private set; }
    public int SoldQuantity { get; private set; }
    public int RemainingQuantity => TotalQuantity - SoldQuantity;

    public void Reserve(int quantity)
    {
        if (RemainingQuantity < quantity)
            throw new InvalidOperationException("Not enough items available");

        SoldQuantity += quantity;
    }
}

public enum FlashSaleStatus { Scheduled, Active, Ended, Cancelled }
```

**Distributed lock for purchase:**
```csharp
public class PurchaseFlashSaleItemCommandHandler
{
    private readonly IDistributedLockProvider _lockProvider;
    private readonly IRepository<FlashSaleItem, Guid> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public async Task<Result> Handle(PurchaseFlashSaleItemCommand cmd, CancellationToken ct)
    {
        var lockKey = $"flashsale:{cmd.FlashSaleItemId}";

        await using var lockHandle = await _lockProvider.TryAcquireAsync(
            lockKey,
            TimeSpan.FromSeconds(5),
            ct);

        if (lockHandle == null)
            return Result.Failure(Error.Conflict("Item is being purchased by another customer"));

        var item = await _repository.FirstOrDefaultAsync(
            new FlashSaleItemByIdForUpdateSpec(cmd.FlashSaleItemId), ct);

        if (item == null)
            return Result.Failure(Error.NotFound("Flash sale item not found"));

        if (item.RemainingQuantity < cmd.Quantity)
            return Result.Failure(Error.Validation("quantity", "Not enough items available"));

        item.Reserve(cmd.Quantity);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
```

### 11.2 Abandoned Cart Recovery

**Settings:**
```csharp
public class AbandonedCartSettings
{
    public const string SectionName = "AbandonedCart";

    public int AbandonmentThresholdHours { get; set; } = 24;
    public int FirstReminderHours { get; set; } = 24;
    public int SecondReminderHours { get; set; } = 72;
    public int MaxReminders { get; set; } = 2;
}
```

**Background job:**
```csharp
_backgroundJobs.RecurringJob<IAbandonedCartService>(
    "marketing:abandoned-cart-recovery",
    x => x.ProcessAbandonedCartsAsync(),
    "0 */4 * * *");  // Every 4 hours
```

**Flow:**
1. Cart not modified for X hours → Mark as Abandoned
2. If customer email exists → Send recovery email with magic link
3. Magic link: `/cart/recover?token={encrypted-cart-id}`
4. Token validates cart belongs to customer, restores cart

### 11.3 Product Reviews

```csharp
public class ProductReview : TenantAggregateRoot<Guid>
{
    private ProductReview() : base() { }
    private ProductReview(Guid id, string? tenantId) : base(id, tenantId) { }

    public Guid ProductId { get; private set; }
    public Guid? CustomerId { get; private set; }
    public Guid? OrderId { get; private set; }  // Verified purchase
    public int Rating { get; private set; }  // 1-5
    public string? Title { get; private set; }
    public string? Content { get; private set; }
    public ReviewStatus Status { get; private set; }
    public bool IsVerifiedPurchase { get; private set; }
    public int HelpfulVotes { get; private set; }
    public string? AdminResponse { get; private set; }
    public DateTimeOffset? AdminRespondedAt { get; private set; }

    public static ProductReview Create(
        Guid productId,
        Guid? customerId,
        Guid? orderId,
        int rating,
        string? title,
        string? content,
        string? tenantId = null)
    {
        var review = new ProductReview(Guid.NewGuid(), tenantId)
        {
            ProductId = productId,
            CustomerId = customerId,
            OrderId = orderId,
            Rating = rating,
            Title = title,
            Content = content,
            IsVerifiedPurchase = orderId.HasValue,
            Status = orderId.HasValue && rating >= 3 ? ReviewStatus.Approved : ReviewStatus.Pending
        };
        return review;
    }

    public void Approve()
    {
        Status = ReviewStatus.Approved;
    }

    public void Reject()
    {
        Status = ReviewStatus.Rejected;
    }

    public void AddAdminResponse(string response)
    {
        AdminResponse = response;
        AdminRespondedAt = DateTimeOffset.UtcNow;
    }
}

public enum ReviewStatus { Pending, Approved, Rejected }
```

**Validation rules:**
- One review per customer per product
- Rating required (1-5)
- Content optional but recommended
- Auto-approve if verified purchase and rating >= 3
- Manual review for low ratings or flagged keywords

### 11.4 Wishlist

```csharp
public class Wishlist : TenantAggregateRoot<Guid>
{
    private Wishlist() : base() { }

    public Guid CustomerId { get; private set; }
    public virtual ICollection<WishlistItem> Items { get; private set; } = new List<WishlistItem>();

    public static Wishlist Create(Guid customerId, string? tenantId = null)
    {
        return new Wishlist
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CustomerId = customerId
        };
    }
}

public class WishlistItem : TenantEntity<Guid>
{
    public Guid WishlistId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid? VariantId { get; private set; }
    public DateTimeOffset AddedAt { get; private set; }
}
```

---

## Phase 12: Analytics & Reporting

### 12.1 Analytics Queries

```csharp
public record RevenueDashboardQuery(
    DateTimeOffset StartDate,
    DateTimeOffset EndDate,
    string? GroupBy = "day"  // day, week, month
) : IQuery<RevenueDashboardDto>;

public record GatewayPerformanceQuery(
    DateTimeOffset StartDate,
    DateTimeOffset EndDate
) : IQuery<GatewayPerformanceDto>;

public record ProductPerformanceQuery(
    DateTimeOffset StartDate,
    DateTimeOffset EndDate,
    int TopN = 10
) : IQuery<ProductPerformanceDto>;

public record CustomerAcquisitionQuery(
    DateTimeOffset StartDate,
    DateTimeOffset EndDate
) : IQuery<CustomerAcquisitionDto>;

public record ConversionFunnelQuery(
    DateTimeOffset StartDate,
    DateTimeOffset EndDate
) : IQuery<ConversionFunnelDto>;
```

### 12.2 Dashboard DTOs

```csharp
public record RevenueDashboardDto(
    decimal TotalRevenue,
    decimal AverageOrderValue,
    int TotalOrders,
    int TotalCustomers,
    IReadOnlyList<RevenueDataPoint> TimeSeries);

public record RevenueDataPoint(
    DateTimeOffset Date,
    decimal Revenue,
    int Orders);

public record GatewayPerformanceDto(
    IReadOnlyList<GatewayMetric> Gateways);

public record GatewayMetric(
    string Provider,
    int TotalTransactions,
    int SuccessfulTransactions,
    decimal SuccessRate,
    decimal TotalAmount,
    TimeSpan AverageProcessingTime);

public record ProductPerformanceDto(
    IReadOnlyList<ProductMetric> TopProducts,
    IReadOnlyList<CategoryMetric> TopCategories);

public record ProductMetric(
    Guid ProductId,
    string ProductName,
    int UnitsSold,
    decimal Revenue);

public record ConversionFunnelDto(
    int TotalVisitors,
    int AddedToCart,
    int StartedCheckout,
    int CompletedPurchase,
    decimal CartToCheckoutRate,
    decimal CheckoutToOrderRate);
```

### 12.3 Vietnam Tax Configuration

```csharp
public class TaxSettings
{
    public const string SectionName = "Tax";

    public decimal DefaultVatRate { get; set; } = 0.10m;  // 10% VAT
    public bool PricesIncludeVat { get; set; } = true;    // Vietnam standard
    public bool EnableTaxExemption { get; set; } = false;
}

public class TaxRate : TenantEntity<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public decimal Rate { get; private set; }
    public bool IsDefault { get; private set; }
    public string? ApplicableProductCategoriesJson { get; private set; }
}
```

**Tax calculation:**
```csharp
public interface ITaxService : IScopedService
{
    Task<TaxCalculationResult> CalculateTaxAsync(
        Order order,
        CancellationToken ct = default);

    Task<byte[]> GenerateVatInvoiceAsync(
        Order order,
        CancellationToken ct = default);
}
```

### 12.4 Reconciliation Reports

```csharp
public record ReconciliationReportQuery(
    DateTimeOffset StartDate,
    DateTimeOffset EndDate,
    string? Provider = null
) : IQuery<ReconciliationReportDto>;

public record ReconciliationReportDto(
    decimal ExpectedAmount,
    decimal ReceivedAmount,
    decimal Discrepancy,
    IReadOnlyList<ReconciliationItem> Items);

public record ReconciliationItem(
    string TransactionNumber,
    string GatewayTransactionId,
    decimal ExpectedAmount,
    decimal? ReceivedAmount,
    ReconciliationStatus Status);

public enum ReconciliationStatus
{
    Matched,
    Pending,
    Discrepancy,
    Missing
}
```

---

## Compliance Requirements

### Vietnam Regulations
| Requirement | Implementation |
|-------------|----------------|
| SBV Authorization | Use licensed gateways (VNPay, MoMo, ZaloPay) |
| KYC/AML | Flag transactions >VND 20M |
| Data Localization | Vietnam transactions in Vietnam region |
| 3D Secure | Enforced for international cards |

### PCI DSS Level 3
- No card data stored (tokenization by gateway)
- TLS 1.2+ for all communication
- Webhook signature verification
- Audit logging for all payment operations

---

## Implementation Dependencies

```
Phase 5 (Foundation) ─┬─> Phase 6 (Vietnam + COD)
                      │        ↓
                      │   Phase 7 (International)
                      │
                      └─> Phase 8 (E-commerce) ─┬─> Phase 9 (Customers)
                                                │
                                                ├─> Phase 10 (Shipping)
                                                │
                                                └─> Phase 11-12 (Marketing/Analytics)
```

---

## Critical Reference Files

| Pattern | File |
|---------|------|
| TenantAggregateRoot | [ITenantEntity.cs](src/NOIR.Domain/Common/ITenantEntity.cs) |
| Complex entity | [Post.cs](src/NOIR.Domain/Entities/Post.cs) |
| CQRS pattern | [CreatePost/](src/NOIR.Application/Features/Blog/Commands/CreatePost/) |
| Background jobs | [IBackgroundJobs.cs](src/NOIR.Application/Common/Interfaces/IBackgroundJobs.cs) |
| Error codes | [ErrorCodes.cs](src/NOIR.Domain/Common/ErrorCodes.cs) |
| Settings pattern | [EmailSettings.cs](src/NOIR.Infrastructure/Email/EmailSettings.cs) |

---

## Implementation Workflow (Per Phase)

```
1. Research Phase
   ├── Business best practices research (use /sc:research)
   ├── Implementation patterns research (Context7 docs)
   └── UI/UX best practices research (21st.dev patterns)

2. Design Phase
   ├── Domain entities + specifications
   ├── CQRS commands/queries
   └── 21st.dev component specifications

3. TDD Implementation
   ├── Write failing unit tests (Handlers, Validators, Domain)
   ├── Write failing integration tests (Endpoints, Flows)
   ├── Implement to pass all tests
   └── Verify 80%+ coverage

4. UI Implementation
   ├── Build components with 21st.dev Magic MCP
   ├── Ensure WCAG accessibility
   └── Mobile-responsive verification

5. Verification
   ├── dotnet build src/NOIR.sln (zero errors)
   ├── dotnet test src/NOIR.sln (all pass)
   └── Manual flow testing
```
