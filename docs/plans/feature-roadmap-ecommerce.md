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

---

## Development Standards (MANDATORY for ALL Phases)

### 1. UI Framework: 21st.dev

All frontend UI components and pages MUST use **21st.dev** (Magic MCP component builder):

- Payment method selectors, checkout flows, admin dashboards
- Product catalog, cart, order management pages
- Analytics dashboards, customer management UI
- Use `mcp__magic__21st_magic_component_builder` for consistent, production-quality components
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

#### Test Structure Per Phase
```
tests/NOIR.Domain.UnitTests/Payment/
├── PaymentTransactionTests.cs         # Domain entity logic
├── RefundTests.cs                     # Refund business rules
└── PaymentGatewayTests.cs             # Gateway entity logic

tests/NOIR.Application.UnitTests/Features/Payments/
├── Commands/
│   ├── CreatePaymentCommandHandlerTests.cs
│   ├── CreatePaymentCommandValidatorTests.cs
│   ├── ProcessWebhookCommandHandlerTests.cs
│   ├── RequestRefundCommandHandlerTests.cs
│   ├── ConfirmCodCollectionCommandHandlerTests.cs
│   └── ConfigureGatewayCommandHandlerTests.cs
├── Queries/
│   ├── GetPaymentByIdQueryHandlerTests.cs
│   ├── GetPaymentsQueryHandlerTests.cs
│   └── GetPendingCodPaymentsQueryHandlerTests.cs
└── Specifications/
    └── PaymentSpecificationTests.cs

tests/NOIR.Infrastructure.UnitTests/Payment/
├── Providers/
│   ├── VnPayProviderTests.cs          # Signature generation/verification
│   ├── MoMoProviderTests.cs           # HMAC-SHA256 validation
│   ├── ZaloPayProviderTests.cs        # MAC verification
│   └── CodProviderTests.cs            # COD flow logic
├── PaymentGatewayFactoryTests.cs
├── CredentialEncryptionServiceTests.cs
└── WebhookProcessorTests.cs

tests/NOIR.IntegrationTests/Payments/
├── PaymentEndpointsTests.cs           # API endpoint integration
├── WebhookEndpointsTests.cs           # Webhook processing E2E
├── PaymentGatewayConfigTests.cs       # Admin gateway CRUD
└── CodPaymentFlowTests.cs             # Full COD lifecycle
```

#### Testing Principles
- **TDD Encouraged**: Write failing tests first, then implement to pass
- **Unit Tests**: All Command Handlers, Validators, Domain entities, Specifications
- **Integration Tests**: All API endpoints, webhook processing, full payment flows
- **Mocking**: Use `NSubstitute` for gateway providers in unit tests
- **Test Data**: Use `Bogus` for realistic test data generation
- **Coverage Target**: Minimum 80% code coverage per phase
- **DI Verification**: New repositories MUST have DI registration tests
- **Signature Tests**: Each gateway's signature helper needs dedicated test vectors

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
| **5** | Payment Foundation | Critical | High | ⏳ Pending |
| **6** | Vietnam Domestic Gateways + COD | Critical | High | ⏳ Pending |
| **7** | International & Advanced | High | Medium | ⏳ Pending |
| **8** | E-commerce Core | Critical | Very High | ⏳ Pending |
| **9** | Customer Management | Medium | Medium | ⏳ Pending |
| **10** | Shipping Integration | High | High | ⏳ Pending |
| **11** | Marketing & Promotions | Medium | Medium | ⏳ Pending |
| **12** | Analytics & Reporting | Medium | Medium | ⏳ Pending |

---

## Phase 5: Payment Foundation

### 5.1 Overview
Build gateway-agnostic payment infrastructure. Per-tenant credential storage (SaaS model).

### 5.2 Domain Entities

**Location:** `src/NOIR.Domain/Entities/Payment/`

#### PaymentGateway (Per-Tenant Configuration)
```csharp
public class PaymentGateway : TenantAggregateRoot<Guid>
{
    public string Provider { get; private set; }          // "vnpay", "momo", "zalopay", "stripe", "cod"
    public string DisplayName { get; private set; }
    public bool IsActive { get; private set; }
    public int SortOrder { get; private set; }
    public GatewayEnvironment Environment { get; private set; } // Sandbox/Production
    public string? EncryptedCredentials { get; private set; }    // AES-256 encrypted JSON
    public string? WebhookSecret { get; private set; }
    public string? WebhookUrl { get; private set; }
    public decimal? MinAmount { get; private set; }
    public decimal? MaxAmount { get; private set; }
    public string SupportedCurrencies { get; private set; } // ["VND","USD"]
    public DateTimeOffset? LastHealthCheck { get; private set; }
    public GatewayHealthStatus HealthStatus { get; private set; }
}
```

#### PaymentTransaction
```csharp
public class PaymentTransaction : TenantAggregateRoot<Guid>
{
    public string TransactionNumber { get; private set; }    // NOIR-generated
    public string? GatewayTransactionId { get; private set; }
    public Guid PaymentGatewayId { get; private set; }
    public string Provider { get; private set; }
    public Guid? OrderId { get; private set; }
    public Guid? CustomerId { get; private set; }

    // Financial
    public decimal Amount { get; private set; }
    public string Currency { get; private set; }
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
    public string IdempotencyKey { get; private set; }
}
```

#### PaymentWebhookLog
```csharp
public class PaymentWebhookLog : TenantAggregateRoot<Guid>
{
    public Guid PaymentGatewayId { get; private set; }
    public string Provider { get; private set; }
    public string EventType { get; private set; }
    public string? GatewayEventId { get; private set; }
    public string RequestBody { get; private set; }
    public string? RequestHeaders { get; private set; }
    public string? SignatureValue { get; private set; }
    public bool SignatureValid { get; private set; }
    public WebhookProcessingStatus ProcessingStatus { get; private set; }
    public string? ProcessingError { get; private set; }
    public int RetryCount { get; private set; }
    public Guid? PaymentTransactionId { get; private set; }
    public string? IpAddress { get; private set; }
}
```

#### Refund
```csharp
public class Refund : TenantAggregateRoot<Guid>
{
    public string RefundNumber { get; private set; }
    public Guid PaymentTransactionId { get; private set; }
    public string? GatewayRefundId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; }
    public RefundStatus Status { get; private set; }
    public RefundReason Reason { get; private set; }
    public string? ReasonDetail { get; private set; }
    public string? RequestedBy { get; private set; }
    public string? ApprovedBy { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public string? GatewayResponseJson { get; private set; }
}
```

### 5.3 Enums
```csharp
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

public enum GatewayEnvironment { Sandbox, Production }
public enum GatewayHealthStatus { Unknown, Healthy, Degraded, Unhealthy }
public enum WebhookProcessingStatus { Received, Processing, Processed, Failed, Skipped }
public enum RefundStatus { Pending, Approved, Processing, Completed, Rejected, Failed }
public enum RefundReason { CustomerRequest, Defective, WrongItem, NotDelivered, Duplicate, Other }
```

### 5.4 Gateway Abstraction
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

### 5.5 Files to Create
```
src/NOIR.Domain/Entities/Payment/
├── PaymentGateway.cs
├── PaymentTransaction.cs
├── PaymentWebhookLog.cs
├── Refund.cs
└── Enums/
    ├── PaymentStatus.cs
    ├── PaymentMethod.cs
    ├── GatewayEnvironment.cs
    ├── WebhookProcessingStatus.cs
    └── RefundStatus.cs

src/NOIR.Application/
├── Common/Interfaces/
│   ├── IPaymentGatewayProvider.cs
│   ├── IPaymentGatewayFactory.cs
│   └── IPaymentService.cs
└── Features/Payments/
    ├── Commands/
    │   ├── CreatePayment/
    │   ├── ProcessWebhook/
    │   ├── RequestRefund/
    │   ├── ConfigureGateway/
    │   ├── CancelPayment/
    │   └── ConfirmCodCollection/       # COD-specific
    ├── Queries/
    │   ├── GetPaymentById/
    │   ├── GetPayments/
    │   ├── GetPaymentsByOrder/
    │   ├── GetGatewayConfigs/
    │   ├── GetWebhookLogs/
    │   ├── GetRefunds/
    │   └── GetPendingCodPayments/      # COD-specific
    ├── DTOs/
    │   ├── PaymentTransactionDto.cs
    │   ├── PaymentGatewayDto.cs
    │   ├── RefundDto.cs
    │   └── WebhookLogDto.cs
    └── Specifications/
        ├── PaymentTransactionSpecs.cs
        ├── PaymentGatewaySpecs.cs
        └── WebhookLogSpecs.cs

src/NOIR.Infrastructure/Payment/
├── PaymentService.cs
├── PaymentGatewayFactory.cs
├── WebhookProcessor.cs
├── CredentialEncryptionService.cs
└── PaymentSettings.cs

src/NOIR.Web/Endpoints/
├── PaymentEndpoints.cs
└── WebhookEndpoints.cs
```

### 5.6 API Endpoints
```
# Customer-facing
POST   /api/payments                    # Create payment
GET    /api/payments/{id}               # Get payment by ID
GET    /api/payments                    # List payments
POST   /api/payments/{id}/cancel        # Cancel pending payment
POST   /api/payments/{id}/refund        # Request refund
GET    /api/payments/{id}/refunds       # Get refunds for payment

# COD-specific
GET    /api/payments/cod/pending        # List pending COD payments
POST   /api/payments/{id}/cod/confirm   # Confirm COD collected

# Webhook endpoints (no auth - signature verification)
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

### 5.7 Configuration
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

### 5.8 Background Jobs (Hangfire)
```csharp
// Recurring jobs
"payment:reconciliation"     // Daily reconciliation check
"payment:expire-pending"     // Expire stale pending payments
"payment:health-check"       // Gateway health monitoring
"payment:retry-webhooks"     // Retry failed webhook processing
"payment:cod-reminders"      // Send COD collection reminders
```

### 5.9 Domain Events
```csharp
public record PaymentCreatedEvent(Guid TransactionId, decimal Amount, string Currency) : DomainEvent;
public record PaymentSucceededEvent(Guid TransactionId, string Provider, decimal Amount) : DomainEvent;
public record PaymentFailedEvent(Guid TransactionId, string Reason) : DomainEvent;
public record PaymentCancelledEvent(Guid TransactionId) : DomainEvent;
public record CodPaymentCollectedEvent(Guid TransactionId, string CollectorName) : DomainEvent;
public record RefundRequestedEvent(Guid RefundId, Guid TransactionId, decimal Amount) : DomainEvent;
public record RefundCompletedEvent(Guid RefundId, Guid TransactionId) : DomainEvent;
```

### 5.10 Error Codes (extend ErrorCodes.cs)
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

### 5.11 Security Considerations
1. **Credential Encryption**: AES-256 encrypted in database (per-tenant)
2. **Webhook Signature Verification**: Per-provider HMAC/RSA schemes
3. **Idempotent Processing**: Deduplicate by `GatewayEventId`
4. **IP Whitelisting**: Optional per gateway
5. **Rate Limiting**: Per-tenant payment creation limits
6. **PCI DSS Level 3**: No card data stored - redirect/tokenization only
7. **Audit Trail**: All commands implement `IAuditableCommand`

---

## Phase 6: Vietnam Domestic Gateways + COD

### 6.1 Gateway Priority & Settlement
| Gateway | Users | Settlement | COD Support | Priority |
|---------|-------|------------|-------------|----------|
| **MoMo** | 60M+ | T+0/T+1 | No | 1 |
| **VNPay** | Leader | T+1 | No | 2 |
| **ZaloPay** | Growing | T+1 | No | 3 |
| **COD** | 20-30% | Manual | Yes (core) | 4 |

### 6.2 Implementation Structure
```
src/NOIR.Infrastructure/Payment/Providers/
├── VnPay/
│   ├── VnPayProvider.cs
│   ├── VnPaySettings.cs
│   ├── VnPaySignatureHelper.cs
│   ├── VnPayResponseParser.cs
│   └── VnPayWebhookHandler.cs
├── MoMo/
│   ├── MoMoProvider.cs
│   ├── MoMoSettings.cs
│   ├── MoMoSignatureHelper.cs
│   ├── MoMoResponseParser.cs
│   └── MoMoWebhookHandler.cs
├── ZaloPay/
│   ├── ZaloPayProvider.cs
│   ├── ZaloPaySettings.cs
│   ├── ZaloPaySignatureHelper.cs
│   ├── ZaloPayResponseParser.cs
│   └── ZaloPayWebhookHandler.cs
└── COD/
    ├── CodProvider.cs
    ├── CodSettings.cs
    └── CodCollectionService.cs
```

### 6.3 VNPay Integration

**API Version:** VNPay v2.1
**Signature:** HMAC-SHA512
**Methods:** ATM, Internet Banking, QR Code, Credit/Debit Card, Installment

**Configuration:**
```json
{
  "VnPay": {
    "TmnCode": "...",
    "HashSecret": "...",
    "PaymentUrl": "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html",
    "ApiUrl": "https://sandbox.vnpayment.vn/merchant_webapi/api/transaction",
    "Version": "2.1.0"
  }
}
```

**Flow:**
1. Create payment URL with params
2. Redirect user to VNPay
3. User completes payment
4. VNPay sends IPN + redirects user
5. Verify HMAC-SHA512 signature
6. Update transaction status

### 6.4 MoMo Integration

**API Version:** MoMo Partner API v2
**Signature:** HMAC-SHA256
**Methods:** MoMo Wallet, QR Code, ATM, Credit Card

**Configuration:**
```json
{
  "MoMo": {
    "PartnerCode": "...",
    "AccessKey": "...",
    "SecretKey": "...",
    "ApiEndpoint": "https://test-payment.momo.vn/v2/gateway/api",
    "RequestType": "captureWallet"
  }
}
```

**Flow:**
1. Create payment request via API
2. Receive `payUrl` or `deeplink`
3. User pays in MoMo app/web
4. MoMo sends IPN webhook
5. Verify HMAC-SHA256 signature
6. Update transaction status

### 6.5 ZaloPay Integration

**API Version:** ZaloPay API v2
**Signature:** HMAC-SHA256
**Methods:** ZaloPay Wallet, ATM, Credit Card, QR

**Configuration:**
```json
{
  "ZaloPay": {
    "AppId": "...",
    "Key1": "...",
    "Key2": "...",
    "Endpoint": "https://sb-openapi.zalopay.vn/v2"
  }
}
```

**Flow:**
1. Create order via API
2. Receive `order_url`
3. User pays in ZaloPay/bank
4. ZaloPay sends callback
5. Verify MAC signature
6. Update transaction status

### 6.6 COD Integration

**Flow:**
1. User selects COD at checkout
2. Order created with `PaymentStatus.CodPending`
3. Order shipped to customer
4. Courier collects cash
5. Admin/courier confirms collection via API
6. Transaction marked `CodCollected`

**COD-specific Commands:**
- `ConfirmCodCollectionCommand` - Mark COD as collected
- `GetPendingCodPaymentsQuery` - List awaiting collection

**COD Reconciliation:**
- Daily job to check unconfirmed COD payments
- Send reminders for overdue collections
- Generate COD reconciliation report

### 6.7 Frontend Components (21st.dev MANDATORY)

**IMPORTANT:** All UI components MUST be built using 21st.dev Magic MCP. Research best payment UX patterns (Stripe Checkout, Shopify) before implementation.

**Payment Method Selection:**
```tsx
// src/NOIR.Web/frontend/src/pages/Checkout/PaymentMethodSelector.tsx
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

## Phase 7: International & Advanced

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

Modern Vietnam gateway with excellent developer experience.

### 7.3 Subscription Billing

**Entities:**
```csharp
public class Subscription : TenantAggregateRoot<Guid>
{
    public Guid CustomerId { get; private set; }
    public Guid PlanId { get; private set; }
    public SubscriptionStatus Status { get; private set; }
    public DateTimeOffset CurrentPeriodStart { get; private set; }
    public DateTimeOffset CurrentPeriodEnd { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public DateTimeOffset? TrialEnd { get; private set; }
    public BillingInterval Interval { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; }
}

public class SubscriptionPlan : TenantAggregateRoot<Guid>
{
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public decimal Price { get; private set; }
    public string Currency { get; private set; }
    public BillingInterval Interval { get; private set; }
    public int? TrialDays { get; private set; }
    public string? FeaturesJson { get; private set; }
    public bool IsActive { get; private set; }
}
```

### 7.4 Installment Payments

For VNPay card installments (3/6/12 months).

### 7.5 Multi-Currency
```csharp
public interface ICurrencyService : IScopedService
{
    Task<decimal> ConvertAsync(decimal amount, string from, string to, CancellationToken ct);
    Task<ExchangeRate> GetRateAsync(string from, string to, CancellationToken ct);
}
```

---

## Phase 8: E-commerce Core

### 8.1 Product Catalog

```csharp
public class Product : TenantAggregateRoot<Guid>
{
    public string Name { get; private set; }
    public string Slug { get; private set; }
    public string? Description { get; private set; }
    public string? DescriptionHtml { get; private set; }
    public decimal BasePrice { get; private set; }
    public string Currency { get; private set; }
    public ProductStatus Status { get; private set; }
    public Guid? CategoryId { get; private set; }
    public string? Sku { get; private set; }
    public string? Barcode { get; private set; }
    public decimal? Weight { get; private set; }
    public bool TrackInventory { get; private set; }
    public string? MetaTitle { get; private set; }
    public string? MetaDescription { get; private set; }
    public int SortOrder { get; private set; }

    public ICollection<ProductVariant> Variants { get; private set; }
    public ICollection<ProductImage> Images { get; private set; }
}

public class ProductVariant : TenantEntity<Guid>
{
    public Guid ProductId { get; private set; }
    public string Name { get; private set; }
    public string? Sku { get; private set; }
    public decimal Price { get; private set; }
    public decimal? CompareAtPrice { get; private set; }
    public int StockQuantity { get; private set; }
    public string? OptionsJson { get; private set; }
}

public class ProductCategory : TenantAggregateRoot<Guid>
{
    public string Name { get; private set; }
    public string Slug { get; private set; }
    public Guid? ParentId { get; private set; }
    public int SortOrder { get; private set; }
    public string? ImageUrl { get; private set; }
}
```

### 8.2 Shopping Cart

```csharp
public class Cart : TenantAggregateRoot<Guid>
{
    public Guid? CustomerId { get; private set; }
    public string? SessionId { get; private set; }
    public CartStatus Status { get; private set; }
    public string Currency { get; private set; }
    public DateTimeOffset? AbandonedAt { get; private set; }
    public ICollection<CartItem> Items { get; private set; }

    public decimal SubTotal => Items.Sum(i => i.LineTotal);
    public int ItemCount => Items.Sum(i => i.Quantity);
}

public class CartItem : TenantEntity<Guid>
{
    public Guid CartId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid? VariantId { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal LineTotal => UnitPrice * Quantity;
}
```

### 8.3 Orders

```csharp
public class Order : TenantAggregateRoot<Guid>
{
    public string OrderNumber { get; private set; }
    public Guid? CustomerId { get; private set; }
    public OrderStatus Status { get; private set; }

    // Financial
    public decimal SubTotal { get; private set; }
    public decimal? DiscountAmount { get; private set; }
    public decimal? ShippingAmount { get; private set; }
    public decimal? TaxAmount { get; private set; }
    public decimal GrandTotal { get; private set; }
    public string Currency { get; private set; }

    // Addresses (JSON)
    public string? ShippingAddressJson { get; private set; }
    public string? BillingAddressJson { get; private set; }

    // Shipping
    public string? ShippingMethod { get; private set; }
    public string? TrackingNumber { get; private set; }
    public string? ShippingCarrier { get; private set; }

    // Customer
    public string CustomerEmail { get; private set; }
    public string? CustomerPhone { get; private set; }
    public string? CustomerName { get; private set; }

    // Notes & Coupons
    public string? CustomerNote { get; private set; }
    public string? InternalNote { get; private set; }
    public string? CouponCode { get; private set; }
    public Guid? CouponId { get; private set; }

    public ICollection<OrderItem> Items { get; private set; }
    public ICollection<PaymentTransaction> Payments { get; private set; }
}
```

### 8.4 Inventory Management

```csharp
public class InventoryMovement : TenantEntity<Guid>
{
    public Guid ProductVariantId { get; private set; }
    public int QuantityChange { get; private set; }
    public InventoryMovementType Type { get; private set; }
    public string? ReferenceId { get; private set; }
    public string? Note { get; private set; }
    public int StockAfter { get; private set; }
}

public enum InventoryMovementType
{
    StockIn, StockOut, Adjustment, Return, Reservation, ReservationRelease
}
```

### 8.5 Coupon System

```csharp
public class Coupon : TenantAggregateRoot<Guid>
{
    public string Code { get; private set; }
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
}

public enum CouponType { Percentage, FixedAmount, FreeShipping }
```

### 8.6 Checkout Flow Commands

```csharp
CreateCheckoutCommand        // Cart → Checkout session
UpdateShippingCommand        // Set shipping address/method
ApplyCouponCommand           // Validate and apply coupon
RemoveCouponCommand          // Remove applied coupon
PlaceOrderCommand            // Finalize order + initiate payment
ConfirmPaymentReturnCommand  // Handle gateway return
```

---

## Phases 9-12: Future Enhancements

### Phase 9: Customer Management
- Customer profiles, addresses, purchase history
- Customer groups/tiers (VIP, loyalty)
- RFM analysis (Recency, Frequency, Monetary)

### Phase 10: Shipping Integration
- **Vietnam Carriers:** GHN, GHTK, Viettel Post, J&T
- Shipping rate calculator
- Tracking integration
- COD reconciliation with carriers

### Phase 11: Marketing & Promotions
- Abandoned cart recovery (Hangfire emails)
- Product reviews/ratings
- Wishlist
- Flash sales with countdown

### Phase 12: Analytics & Reporting
- Revenue dashboards
- Gateway performance metrics
- Reconciliation reports
- VAT/tax reporting for Vietnam

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
