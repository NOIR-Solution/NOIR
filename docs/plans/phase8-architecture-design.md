# Phase 8: E-commerce Core - Architecture Design

**Generated:** January 25, 2026
**Updated:** January 26, 2026
**Status:** Backend Complete, Frontend Pending
**Based On:** [phase8-requirements-specification.md](phase8-requirements-specification.md)

> **Implementation Note:** All backend components (Domain, Application, Endpoints) are complete.
> Frontend UI (Admin Product Pages, Storefront) is pending.

---

## Table of Contents

1. [System Overview](#1-system-overview)
2. [Domain Model](#2-domain-model)
3. [Entity Designs](#3-entity-designs)
4. [API Contracts](#4-api-contracts)
5. [Database Schema](#5-database-schema)
6. [Component Architecture](#6-component-architecture)
7. [Integration Patterns](#7-integration-patterns)

---

## 1. System Overview

### 1.1 Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              STOREFRONT (React)                              │
├─────────────────────────────────────────────────────────────────────────────┤
│  ProductListingPage │ ProductDetailPage │ CartPage │ CheckoutPage │ Orders  │
└────────────┬────────────────┬──────────────┬───────────────┬────────────────┘
             │                │              │               │
             ▼                ▼              ▼               ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                              API LAYER (Endpoints)                           │
├─────────────────────────────────────────────────────────────────────────────┤
│  /api/products  │  /api/cart  │  /api/checkout  │  /api/orders              │
└────────┬────────────────┬──────────────┬────────────────┬───────────────────┘
         │                │              │                │
         ▼                ▼              ▼                ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                         APPLICATION LAYER (CQRS)                             │
├─────────────────────────────────────────────────────────────────────────────┤
│  Products/     │  Cart/        │  Checkout/           │  Orders/            │
│  ├─Commands    │  ├─Commands   │  ├─Commands          │  ├─Commands         │
│  ├─Queries     │  ├─Queries    │  │ StartCheckout     │  ├─Queries          │
│  └─Specs       │  └─Specs      │  │ UpdateAddress     │  └─Specs            │
│                │               │  │ SelectShipping    │                     │
│                │               │  │ ProcessPayment    │                     │
│                │               │  └─PlaceOrder        │                     │
└────────┬────────────────┬──────────────┬────────────────┬───────────────────┘
         │                │              │                │
         ▼                ▼              ▼                ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                            DOMAIN LAYER (Entities)                           │
├─────────────────────────────────────────────────────────────────────────────┤
│  Product         │  Cart           │  CheckoutSession   │  Order            │
│  ProductVariant  │  CartItem       │  InventoryReserv.  │  OrderItem        │
│  ProductCategory │                 │                    │                   │
│  ProductImage    │                 │                    │                   │
└────────┬────────────────┬──────────────┬────────────────┬───────────────────┘
         │                │              │                │
         ▼                ▼              ▼                ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                         INFRASTRUCTURE LAYER                                 │
├─────────────────────────────────────────────────────────────────────────────┤
│  Repositories   │  DbContext      │  Background Jobs   │  Payment Gateway  │
│  Configurations │  Specifications │  (Hangfire)        │  (Phase 5-7)      │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 1.2 Key Data Flows

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                         CHECKOUT FLOW (CheckoutSession Pattern)              │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  1. START CHECKOUT                                                           │
│  ┌─────────┐    ┌──────────────────┐    ┌─────────────────────┐             │
│  │  Cart   │───▶│ StartCheckout    │───▶│ CheckoutSession     │             │
│  │         │    │ Command          │    │ (Status: Started)   │             │
│  └─────────┘    └──────────────────┘    └──────────┬──────────┘             │
│                                                     │                        │
│  2. CREATE INVENTORY RESERVATIONS                   ▼                        │
│  ┌──────────────────┐    ┌─────────────────────────────────────────┐        │
│  │ ProductVariant   │───▶│ InventoryReservation (Status: Temporary)│        │
│  │ (Stock -= Qty)   │    │ (ExpiresAt: +15 minutes)                │        │
│  └──────────────────┘    └─────────────────────────────────────────┘        │
│                                                                              │
│  3. COLLECT CHECKOUT DATA                                                    │
│  ┌───────────────┐  ┌─────────────────┐  ┌──────────────────┐               │
│  │ UpdateAddress │─▶│ SelectShipping  │─▶│ Enter Payment    │               │
│  │ Command       │  │ Command         │  │ Info             │               │
│  └───────────────┘  └─────────────────┘  └──────────────────┘               │
│                                                                              │
│  4. PLACE ORDER (Transaction)                                                │
│  ┌──────────────────────────────────────────────────────────────────┐       │
│  │ PlaceOrderCommand:                                                │       │
│  │  1. Create Order from CheckoutSession                            │       │
│  │  2. Create PaymentTransaction (Phase 5-7 integration)            │       │
│  │  3. Confirm InventoryReservations (Status: Confirmed)            │       │
│  │  4. Mark Cart as Converted                                        │       │
│  │  5. Mark CheckoutSession as Completed                             │       │
│  │  6. Raise OrderCreatedEvent                                       │       │
│  └──────────────────────────────────────────────────────────────────┘       │
│                                                                              │
│  5. ABANDONED CHECKOUT (Background Job)                                      │
│  ┌──────────────────────────────────────────────────────────────────┐       │
│  │ ExpiredReservationCleanupJob (every minute):                      │       │
│  │  1. Find InventoryReservations where ExpiresAt < Now              │       │
│  │  2. Release stock: ProductVariant.StockQuantity += Qty            │       │
│  │  3. Delete or mark InventoryReservation as Released               │       │
│  │  4. Mark CheckoutSession as Expired                               │       │
│  └──────────────────────────────────────────────────────────────────┘       │
│                                                                              │
└──────────────────────────────────────────────────────────────────────────────┘
```

---

## 2. Domain Model

### 2.1 Entity Relationship Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              PRODUCT AGGREGATE                               │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌─────────────────┐         ┌─────────────────────┐                        │
│  │ ProductCategory │◄────────│ Product             │                        │
│  │ (AggregateRoot) │  1    * │ (AggregateRoot)     │                        │
│  ├─────────────────┤         ├─────────────────────┤                        │
│  │ Id              │         │ Id                  │                        │
│  │ Name            │         │ Name                │                        │
│  │ Slug            │         │ Slug                │                        │
│  │ ParentId?       │────┐    │ CategoryId?         │                        │
│  │ SortOrder       │    │    │ BasePrice           │                        │
│  │ ImageUrl?       │    │    │ Status              │                        │
│  └─────────────────┘    │    │ ...                 │                        │
│           ▲             │    └─────────────────────┘                        │
│           │             │              │                                     │
│           └─────────────┘              │ 1                                   │
│         (Self-referencing)             │                                     │
│                                        ▼ *                                   │
│                          ┌─────────────────────────┐                        │
│                          │ ProductVariant (Entity) │                        │
│                          ├─────────────────────────┤                        │
│                          │ Id                      │                        │
│                          │ ProductId (FK)          │                        │
│                          │ Name                    │                        │
│                          │ Sku                     │                        │
│                          │ Price                   │                        │
│                          │ StockQuantity [CC]      │◄─── ConcurrencyCheck   │
│                          │ OptionsJson             │                        │
│                          └─────────────────────────┘                        │
│                                        │ 1                                   │
│                                        │                                     │
│                                        ▼ *                                   │
│                          ┌─────────────────────────┐                        │
│                          │ ProductImage (Entity)   │                        │
│                          ├─────────────────────────┤                        │
│                          │ Id                      │                        │
│                          │ ProductId (FK)          │                        │
│                          │ Url                     │                        │
│                          │ AltText                 │                        │
│                          │ SortOrder               │                        │
│                          │ IsPrimary               │                        │
│                          └─────────────────────────┘                        │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                              CART AGGREGATE                                  │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌─────────────────────┐                                                    │
│  │ Cart (AggregateRoot)│                                                    │
│  ├─────────────────────┤                                                    │
│  │ Id                  │                                                    │
│  │ CustomerId?         │────────────────▶ (User - external)                 │
│  │ SessionId?          │                                                    │
│  │ Status              │                                                    │
│  │ Currency            │                                                    │
│  │ AbandonedAt?        │                                                    │
│  └─────────────────────┘                                                    │
│           │ 1                                                                │
│           │                                                                  │
│           ▼ *                                                                │
│  ┌─────────────────────┐         ┌─────────────────────┐                    │
│  │ CartItem (Entity)   │────────▶│ ProductVariant      │                    │
│  ├─────────────────────┤    *  1 └─────────────────────┘                    │
│  │ Id                  │                                                    │
│  │ CartId (FK)         │                                                    │
│  │ ProductId (FK)      │────────▶ Product                                   │
│  │ VariantId? (FK)     │                                                    │
│  │ Quantity            │                                                    │
│  │ UnitPrice           │◄─── Snapshot at add time                           │
│  └─────────────────────┘                                                    │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                           CHECKOUT AGGREGATE (NEW)                           │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌───────────────────────────┐                                              │
│  │ CheckoutSession           │                                              │
│  │ (AggregateRoot) ──────────┼──────────────────────────────────────┐       │
│  ├───────────────────────────┤                                      │       │
│  │ Id                        │                                      │       │
│  │ CartId (FK)               │────────────────▶ Cart                │       │
│  │ CustomerId?               │                                      │       │
│  │ Status                    │◄─── CheckoutSessionStatus            │       │
│  │ ExpiresAt                 │◄─── Created + 15 minutes             │       │
│  │                           │                                      │       │
│  │ // Contact                │                                      │       │
│  │ Email                     │                                      │       │
│  │ Phone?                    │                                      │       │
│  │                           │                                      │       │
│  │ // Addresses (owned)      │                                      │       │
│  │ ShippingAddress?          │◄─── Address value object             │       │
│  │ BillingAddress?           │                                      │       │
│  │ BillingSameAsShipping     │                                      │       │
│  │                           │                                      │       │
│  │ // Shipping               │                                      │       │
│  │ ShippingMethodId?         │                                      │       │
│  │ ShippingAmount?           │                                      │       │
│  │                           │                                      │       │
│  │ // Totals (calculated)    │                                      │       │
│  │ SubTotal                  │                                      │       │
│  │ TaxAmount?                │                                      │       │
│  │ GrandTotal                │                                      │       │
│  │                           │                                      │       │
│  │ // Result                 │                                      │       │
│  │ OrderId?                  │────────────────▶ Order (after place) │       │
│  │ PaymentTransactionId?     │────────────────▶ PaymentTransaction  │       │
│  └───────────────────────────┘                                      │       │
│                                                                      │       │
│  ┌───────────────────────────┐                                      │       │
│  │ InventoryReservation      │◄─────────────────────────────────────┘       │
│  │ (AggregateRoot)           │                                              │
│  ├───────────────────────────┤                                              │
│  │ Id                        │                                              │
│  │ CheckoutSessionId (FK)    │                                              │
│  │ ProductVariantId (FK)     │────────────────▶ ProductVariant              │
│  │ Quantity                  │                                              │
│  │ Status                    │◄─── ReservationStatus (Temporary/Confirmed)  │
│  │ ExpiresAt                 │                                              │
│  │ ConfirmedAt?              │                                              │
│  │ OrderId?                  │────────────────▶ Order (after confirm)       │
│  └───────────────────────────┘                                              │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                              ORDER AGGREGATE                                 │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌─────────────────────┐                                                    │
│  │ Order (AggregateRoot│                                                    │
│  ├─────────────────────┤                                                    │
│  │ Id                  │                                                    │
│  │ OrderNumber         │◄─── "NOIR-2024-000001" (tenant + sequence)         │
│  │ CustomerId?         │                                                    │
│  │ Status              │◄─── OrderStatus                                    │
│  │                     │                                                    │
│  │ // Financial        │                                                    │
│  │ SubTotal            │                                                    │
│  │ DiscountAmount?     │                                                    │
│  │ ShippingAmount?     │                                                    │
│  │ TaxAmount?          │                                                    │
│  │ GrandTotal          │                                                    │
│  │ Currency            │                                                    │
│  │                     │                                                    │
│  │ // Addresses        │                                                    │
│  │ ShippingAddress     │◄─── Address value object (owned)                   │
│  │ BillingAddress      │                                                    │
│  │                     │                                                    │
│  │ // Customer Info    │                                                    │
│  │ CustomerEmail       │                                                    │
│  │ CustomerPhone?      │                                                    │
│  │ CustomerName?       │                                                    │
│  │                     │                                                    │
│  │ // Shipping         │                                                    │
│  │ ShippingMethod?     │                                                    │
│  │ TrackingNumber?     │                                                    │
│  │ ShippingCarrier?    │                                                    │
│  └─────────────────────┘                                                    │
│           │ 1                                                                │
│           │                                                                  │
│           ▼ *                                                                │
│  ┌─────────────────────┐         ┌─────────────────────┐                    │
│  │ OrderItem (Entity)  │         │ PaymentTransaction  │                    │
│  ├─────────────────────┤         │ (Phase 5-7)         │                    │
│  │ Id                  │         └─────────────────────┘                    │
│  │ OrderId (FK)        │                   ▲                                │
│  │ ProductId           │                   │                                │
│  │ ProductVariantId?   │                   │                                │
│  │ ProductName         │◄─── Snapshot      │                                │
│  │ VariantName?        │                   │                                │
│  │ Sku?                │                   │                                │
│  │ Quantity            │         Order.Payments ────────┘                   │
│  │ UnitPrice           │                                                    │
│  │ LineTotal           │                                                    │
│  └─────────────────────┘                                                    │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                           INVENTORY AGGREGATE                                │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌─────────────────────────────┐                                            │
│  │ InventoryMovement (Entity)  │                                            │
│  ├─────────────────────────────┤                                            │
│  │ Id                          │                                            │
│  │ ProductVariantId (FK)       │────────────────▶ ProductVariant            │
│  │ Type                        │◄─── InventoryMovementType                  │
│  │ Quantity                    │◄─── Positive or negative                   │
│  │ Reason?                     │                                            │
│  │ ReferenceType?              │◄─── "Order", "Adjustment", etc.            │
│  │ ReferenceId?                │◄─── OrderId, etc.                          │
│  │ CreatedBy?                  │                                            │
│  │ CreatedAt                   │                                            │
│  └─────────────────────────────┘                                            │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 2.2 Aggregate Boundaries

| Aggregate | Root Entity | Child Entities | Invariants |
|-----------|-------------|----------------|------------|
| **Product** | `Product` | `ProductVariant`, `ProductImage` | Slug unique per tenant, Stock >= 0 |
| **ProductCategory** | `ProductCategory` | None | Slug unique per tenant, No circular refs |
| **Cart** | `Cart` | `CartItem` | Only one active cart per customer/session |
| **CheckoutSession** | `CheckoutSession` | None | Links to InventoryReservation, max 15 min |
| **InventoryReservation** | `InventoryReservation` | None | Must have valid CheckoutSession |
| **Order** | `Order` | `OrderItem` | OrderNumber unique per tenant |
| **InventoryMovement** | `InventoryMovement` | None | Audit trail only |

---

## 3. Entity Designs

### 3.1 New Enums

**Location:** `src/NOIR.Domain/Enums/`

```csharp
// CheckoutSessionStatus.cs
public enum CheckoutSessionStatus
{
    Started,
    AddressComplete,
    ShippingSelected,
    PaymentPending,
    PaymentProcessing,
    Completed,
    Expired,
    Abandoned
}

// ReservationStatus.cs
public enum ReservationStatus
{
    Temporary,    // Created during checkout, expires in 15 min
    Confirmed,    // Order placed, permanent
    Released,     // Checkout abandoned or expired
    Cancelled     // Order cancelled
}
```

### 3.2 CheckoutSession Entity (NEW)

**Location:** `src/NOIR.Domain/Entities/Checkout/CheckoutSession.cs`

```csharp
namespace NOIR.Domain.Entities.Checkout;

/// <summary>
/// Orchestrates the checkout flow from cart to order.
/// Manages inventory reservations and payment processing.
/// </summary>
public class CheckoutSession : TenantAggregateRoot<Guid>
{
    private CheckoutSession() : base() { }
    private CheckoutSession(Guid id, string? tenantId) : base(id, tenantId) { }

    // References
    public Guid CartId { get; private set; }
    public Guid? CustomerId { get; private set; }
    public Guid? OrderId { get; private set; }
    public Guid? PaymentTransactionId { get; private set; }

    // Status & Timing
    public CheckoutSessionStatus Status { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    // Contact
    public string Email { get; private set; } = string.Empty;
    public string? Phone { get; private set; }

    // Addresses (Owned)
    public Address? ShippingAddress { get; private set; }
    public Address? BillingAddress { get; private set; }
    public bool BillingSameAsShipping { get; private set; } = true;

    // Shipping
    public Guid? ShippingMethodId { get; private set; }
    public string? ShippingMethodName { get; private set; }
    public decimal ShippingAmount { get; private set; }

    // Totals
    public decimal SubTotal { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal GrandTotal { get; private set; }
    public string Currency { get; private set; } = "VND";

    // Navigation
    public virtual Cart Cart { get; private set; } = null!;
    public virtual Order? Order { get; private set; }
    public virtual ICollection<InventoryReservation> Reservations { get; private set; }
        = new List<InventoryReservation>();

    /// <summary>
    /// Start a new checkout session for a cart.
    /// </summary>
    public static CheckoutSession Create(
        Guid cartId,
        Guid? customerId,
        string email,
        decimal subTotal,
        string currency = "VND",
        int expirationMinutes = 15,
        string? tenantId = null)
    {
        var session = new CheckoutSession(Guid.NewGuid(), tenantId)
        {
            CartId = cartId,
            CustomerId = customerId,
            Email = email,
            SubTotal = subTotal,
            GrandTotal = subTotal, // Initial, before shipping/tax
            Currency = currency,
            Status = CheckoutSessionStatus.Started,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(expirationMinutes)
        };

        session.AddDomainEvent(new CheckoutStartedEvent(
            session.Id, cartId, customerId, subTotal));

        return session;
    }

    public void SetShippingAddress(Address address)
    {
        ShippingAddress = address;
        if (BillingSameAsShipping)
            BillingAddress = address;

        if (Status == CheckoutSessionStatus.Started)
            Status = CheckoutSessionStatus.AddressComplete;
    }

    public void SetBillingAddress(Address address, bool sameAsShipping = false)
    {
        BillingAddress = address;
        BillingSameAsShipping = sameAsShipping;
    }

    public void SelectShippingMethod(Guid methodId, string methodName, decimal amount)
    {
        ShippingMethodId = methodId;
        ShippingMethodName = methodName;
        ShippingAmount = amount;
        RecalculateTotal();
        Status = CheckoutSessionStatus.ShippingSelected;
    }

    public void SetTaxAmount(decimal taxAmount)
    {
        TaxAmount = taxAmount;
        RecalculateTotal();
    }

    public void ApplyDiscount(decimal discountAmount)
    {
        DiscountAmount = discountAmount;
        RecalculateTotal();
    }

    public void StartPaymentProcessing(Guid paymentTransactionId)
    {
        PaymentTransactionId = paymentTransactionId;
        Status = CheckoutSessionStatus.PaymentProcessing;
    }

    public void Complete(Guid orderId)
    {
        OrderId = orderId;
        Status = CheckoutSessionStatus.Completed;
        CompletedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new CheckoutCompletedEvent(Id, orderId, GrandTotal));
    }

    public void MarkExpired()
    {
        Status = CheckoutSessionStatus.Expired;
        AddDomainEvent(new CheckoutExpiredEvent(Id, CartId));
    }

    public void MarkAbandoned()
    {
        Status = CheckoutSessionStatus.Abandoned;
    }

    public bool IsExpired => DateTimeOffset.UtcNow > ExpiresAt;

    private void RecalculateTotal()
    {
        GrandTotal = SubTotal + ShippingAmount + TaxAmount - DiscountAmount;
    }
}
```

### 3.3 InventoryReservation Entity (NEW)

**Location:** `src/NOIR.Domain/Entities/Inventory/InventoryReservation.cs`

```csharp
namespace NOIR.Domain.Entities.Inventory;

/// <summary>
/// Temporary inventory hold during checkout.
/// Expires after 15 minutes if checkout not completed.
/// </summary>
public class InventoryReservation : TenantAggregateRoot<Guid>
{
    private InventoryReservation() : base() { }
    private InventoryReservation(Guid id, string? tenantId) : base(id, tenantId) { }

    public Guid CheckoutSessionId { get; private set; }
    public Guid ProductVariantId { get; private set; }
    public int Quantity { get; private set; }
    public ReservationStatus Status { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? ConfirmedAt { get; private set; }
    public DateTimeOffset? ReleasedAt { get; private set; }
    public Guid? OrderId { get; private set; }

    // Navigation
    public virtual CheckoutSession CheckoutSession { get; private set; } = null!;
    public virtual ProductVariant ProductVariant { get; private set; } = null!;

    /// <summary>
    /// Create a temporary inventory reservation for checkout.
    /// </summary>
    public static InventoryReservation CreateTemporary(
        Guid checkoutSessionId,
        Guid productVariantId,
        int quantity,
        int expirationMinutes = 15,
        string? tenantId = null)
    {
        return new InventoryReservation(Guid.NewGuid(), tenantId)
        {
            CheckoutSessionId = checkoutSessionId,
            ProductVariantId = productVariantId,
            Quantity = quantity,
            Status = ReservationStatus.Temporary,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(expirationMinutes)
        };
    }

    /// <summary>
    /// Confirm reservation when order is placed.
    /// </summary>
    public void Confirm(Guid orderId)
    {
        if (Status != ReservationStatus.Temporary)
            throw new InvalidOperationException($"Cannot confirm reservation in status {Status}");

        Status = ReservationStatus.Confirmed;
        OrderId = orderId;
        ConfirmedAt = DateTimeOffset.UtcNow;
        ExpiresAt = DateTimeOffset.MaxValue; // Never expires once confirmed
    }

    /// <summary>
    /// Release reservation (checkout abandoned or expired).
    /// </summary>
    public void Release()
    {
        if (Status == ReservationStatus.Confirmed)
            throw new InvalidOperationException("Cannot release confirmed reservation");

        Status = ReservationStatus.Released;
        ReleasedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Cancel reservation (order cancelled, trigger stock return).
    /// </summary>
    public void Cancel()
    {
        Status = ReservationStatus.Cancelled;
        ReleasedAt = DateTimeOffset.UtcNow;
    }

    public bool IsExpired => Status == ReservationStatus.Temporary
                          && DateTimeOffset.UtcNow > ExpiresAt;
}
```

### 3.4 ProductImage Entity (NEW)

**Location:** `src/NOIR.Domain/Entities/Product/ProductImage.cs`

```csharp
namespace NOIR.Domain.Entities.Product;

/// <summary>
/// Product image for gallery display.
/// </summary>
public class ProductImage : TenantEntity<Guid>
{
    private ProductImage() { }

    public Guid ProductId { get; private set; }
    public string Url { get; private set; } = string.Empty;
    public string? AltText { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsPrimary { get; private set; }

    // Navigation
    public virtual Product Product { get; private set; } = null!;

    internal static ProductImage Create(
        Guid productId,
        string url,
        string? altText = null,
        int sortOrder = 0,
        bool isPrimary = false,
        string? tenantId = null)
    {
        return new ProductImage
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ProductId = productId,
            Url = url,
            AltText = altText,
            SortOrder = sortOrder,
            IsPrimary = isPrimary
        };
    }

    public void SetAsPrimary()
    {
        IsPrimary = true;
    }

    public void ClearPrimary()
    {
        IsPrimary = false;
    }
}
```

### 3.5 InventoryMovement Entity

**Location:** `src/NOIR.Domain/Entities/Inventory/InventoryMovement.cs`

```csharp
namespace NOIR.Domain.Entities.Inventory;

/// <summary>
/// Audit trail for all inventory changes.
/// </summary>
public class InventoryMovement : TenantEntity<Guid>
{
    private InventoryMovement() { }

    public Guid ProductVariantId { get; private set; }
    public InventoryMovementType Type { get; private set; }
    public int Quantity { get; private set; } // Positive for in, negative for out
    public int StockBefore { get; private set; }
    public int StockAfter { get; private set; }
    public string? Reason { get; private set; }
    public string? ReferenceType { get; private set; } // "Order", "Adjustment", etc.
    public Guid? ReferenceId { get; private set; }
    public string? CreatedBy { get; private set; }

    // Navigation
    public virtual ProductVariant ProductVariant { get; private set; } = null!;

    public static InventoryMovement Create(
        Guid productVariantId,
        InventoryMovementType type,
        int quantity,
        int stockBefore,
        string? reason = null,
        string? referenceType = null,
        Guid? referenceId = null,
        string? createdBy = null,
        string? tenantId = null)
    {
        return new InventoryMovement
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ProductVariantId = productVariantId,
            Type = type,
            Quantity = quantity,
            StockBefore = stockBefore,
            StockAfter = stockBefore + quantity,
            Reason = reason,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            CreatedBy = createdBy
        };
    }
}
```

---

## 4. API Contracts

### 4.1 Products API

**Base Path:** `/api/products`

#### Endpoints

| Method | Path | Description | Auth |
|--------|------|-------------|------|
| `GET` | `/` | List products with faceted filters | Public |
| `GET` | `/{idOrSlug}` | Get product by ID or slug | Public |
| `GET` | `/categories` | List all categories (tree) | Public |
| `GET` | `/categories/{idOrSlug}/products` | Products by category | Public |
| `POST` | `/admin` | Create product | Admin |
| `PUT` | `/admin/{id}` | Update product | Admin |
| `POST` | `/admin/{id}/publish` | Publish product | Admin |
| `POST` | `/admin/{id}/archive` | Archive product | Admin |
| `POST` | `/admin/{id}/variants` | Add variant | Admin |
| `PUT` | `/admin/{id}/variants/{variantId}` | Update variant | Admin |
| `DELETE` | `/admin/{id}/variants/{variantId}` | Delete variant | Admin |
| `POST` | `/admin/{id}/images` | Upload image | Admin |
| `DELETE` | `/admin/{id}/images/{imageId}` | Delete image | Admin |

#### Request/Response DTOs

```csharp
// GET /api/products (with faceted filters)
public record ProductFilterParams
{
    public Guid? CategoryId { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public string[]? Brands { get; init; }
    public string[]? Sizes { get; init; }
    public string[]? Colors { get; init; }
    public bool? InStockOnly { get; init; }
    public string? Search { get; init; }
    public string SortBy { get; init; } = "relevance"; // price_asc, price_desc, newest
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 24;
}

public record ProductListResponse
{
    public IEnumerable<ProductCardDto> Products { get; init; } = [];
    public FacetResults Facets { get; init; } = new();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public bool HasMore { get; init; }
}

public record ProductCardDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public decimal? CompareAtPrice { get; init; }
    public string? PrimaryImageUrl { get; init; }
    public string Currency { get; init; } = "VND";
    public bool InStock { get; init; }
    public int StockQuantity { get; init; }
    public string? Brand { get; init; }
}

public record FacetResults
{
    public IEnumerable<FacetValue> Categories { get; init; } = [];
    public IEnumerable<FacetValue> Brands { get; init; } = [];
    public IEnumerable<FacetValue> Sizes { get; init; } = [];
    public IEnumerable<FacetValue> Colors { get; init; } = [];
    public PriceRange PriceRange { get; init; } = new();
}

public record FacetValue(string Value, int Count, bool Selected = false);
public record PriceRange(decimal Min, decimal Max);

// GET /api/products/{idOrSlug}
public record ProductDetailDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? DescriptionHtml { get; init; }
    public decimal BasePrice { get; init; }
    public string Currency { get; init; } = "VND";
    public ProductStatus Status { get; init; }
    public CategorySummaryDto? Category { get; init; }
    public IEnumerable<ProductVariantDto> Variants { get; init; } = [];
    public IEnumerable<ProductImageDto> Images { get; init; } = [];
    public string? MetaTitle { get; init; }
    public string? MetaDescription { get; init; }
}

public record ProductVariantDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Sku { get; init; }
    public decimal Price { get; init; }
    public decimal? CompareAtPrice { get; init; }
    public int StockQuantity { get; init; }
    public Dictionary<string, string> Options { get; init; } = new(); // From OptionsJson
    public bool InStock => StockQuantity > 0;
    public bool LowStock => StockQuantity > 0 && StockQuantity <= 5;
}

public record ProductImageDto
{
    public Guid Id { get; init; }
    public string Url { get; init; } = string.Empty;
    public string? AltText { get; init; }
    public int SortOrder { get; init; }
    public bool IsPrimary { get; init; }
}
```

### 4.2 Cart API

**Base Path:** `/api/cart`

#### Endpoints

| Method | Path | Description | Auth |
|--------|------|-------------|------|
| `GET` | `/` | Get current cart | Public* |
| `POST` | `/items` | Add item to cart | Public* |
| `PUT` | `/items/{itemId}` | Update item quantity | Public* |
| `DELETE` | `/items/{itemId}` | Remove item from cart | Public* |
| `DELETE` | `/` | Clear cart | Public* |
| `POST` | `/merge` | Merge guest cart on login | Auth |

*Public endpoints use session ID for guests, user ID for authenticated users.

#### Request/Response DTOs

```csharp
// GET /api/cart
public record CartDto
{
    public Guid Id { get; init; }
    public CartStatus Status { get; init; }
    public IEnumerable<CartItemDto> Items { get; init; } = [];
    public decimal SubTotal { get; init; }
    public int ItemCount { get; init; }
    public string Currency { get; init; } = "VND";
}

public record CartItemDto
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public Guid? VariantId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string? VariantName { get; init; }
    public string? Sku { get; init; }
    public string? ImageUrl { get; init; }
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal LineTotal { get; init; }
    public int StockQuantity { get; init; } // For validation display
    public bool InStock { get; init; }
}

// POST /api/cart/items
public record AddToCartRequest
{
    public Guid ProductId { get; init; }
    public Guid? VariantId { get; init; }
    public int Quantity { get; init; } = 1;
}

// PUT /api/cart/items/{itemId}
public record UpdateCartItemRequest
{
    public int Quantity { get; init; }
}
```

### 4.3 Checkout API

**Base Path:** `/api/checkout`

#### Endpoints

| Method | Path | Description | Auth |
|--------|------|-------------|------|
| `POST` | `/start` | Start checkout session | Public* |
| `GET` | `/session` | Get current checkout session | Public* |
| `PUT` | `/address/shipping` | Set shipping address | Public* |
| `PUT` | `/address/billing` | Set billing address | Public* |
| `GET` | `/shipping-methods` | Get available shipping methods | Public* |
| `PUT` | `/shipping-method` | Select shipping method | Public* |
| `POST` | `/payment` | Process payment | Public* |
| `POST` | `/place-order` | Complete order | Public* |

#### Request/Response DTOs

```csharp
// POST /api/checkout/start
public record StartCheckoutRequest
{
    public string Email { get; init; } = string.Empty;
    public string? Phone { get; init; }
}

public record StartCheckoutResponse
{
    public Guid SessionId { get; init; }
    public CheckoutSessionStatus Status { get; init; }
    public DateTimeOffset ExpiresAt { get; init; }
    public int SecondsRemaining { get; init; }
    public CartSummaryDto Cart { get; init; } = new();
}

// GET /api/checkout/session
public record CheckoutSessionDto
{
    public Guid Id { get; init; }
    public CheckoutSessionStatus Status { get; init; }
    public DateTimeOffset ExpiresAt { get; init; }
    public int SecondsRemaining { get; init; }

    // Contact
    public string Email { get; init; } = string.Empty;
    public string? Phone { get; init; }

    // Addresses
    public AddressDto? ShippingAddress { get; init; }
    public AddressDto? BillingAddress { get; init; }
    public bool BillingSameAsShipping { get; init; }

    // Shipping
    public ShippingMethodDto? SelectedShippingMethod { get; init; }

    // Totals
    public decimal SubTotal { get; init; }
    public decimal ShippingAmount { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal GrandTotal { get; init; }
    public string Currency { get; init; } = "VND";

    // Cart
    public CartSummaryDto Cart { get; init; } = new();

    // Payment methods available
    public IEnumerable<PaymentMethodDto> AvailablePaymentMethods { get; init; } = [];
}

// PUT /api/checkout/address/shipping
public record SetShippingAddressRequest
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
    public bool SaveAsDefault { get; init; }
}

// GET /api/checkout/shipping-methods
public record ShippingMethodDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string EstimatedDelivery { get; init; } = string.Empty; // "2-3 business days"
}

// PUT /api/checkout/shipping-method
public record SelectShippingMethodRequest
{
    public Guid ShippingMethodId { get; init; }
}

// POST /api/checkout/payment
public record ProcessPaymentRequest
{
    public string Provider { get; init; } = string.Empty; // "vnpay", "momo", "stripe"
    public PaymentMethod PaymentMethod { get; init; }
    // Gateway-specific fields handled by existing payment system
}

public record ProcessPaymentResponse
{
    public Guid PaymentTransactionId { get; init; }
    public PaymentStatus Status { get; init; }
    public string? RedirectUrl { get; init; } // For gateway redirects
    public string? QrCodeUrl { get; init; } // For QR payments
}

// POST /api/checkout/place-order
public record PlaceOrderResponse
{
    public Guid OrderId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public OrderStatus Status { get; init; }
    public decimal GrandTotal { get; init; }
    public string Currency { get; init; } = "VND";
}
```

### 4.4 Orders API

**Base Path:** `/api/orders`

#### Endpoints

| Method | Path | Description | Auth |
|--------|------|-------------|------|
| `GET` | `/` | List my orders | Auth |
| `GET` | `/{id}` | Get order details | Auth |
| `GET` | `/admin` | List all orders (admin) | Admin |
| `GET` | `/admin/{id}` | Get order details (admin) | Admin |
| `PUT` | `/admin/{id}/status` | Update order status | Admin |
| `POST` | `/admin/{id}/refund` | Process refund | Admin |

#### Request/Response DTOs

```csharp
// GET /api/orders
public record OrderListDto
{
    public Guid Id { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public OrderStatus Status { get; init; }
    public decimal GrandTotal { get; init; }
    public string Currency { get; init; } = "VND";
    public int ItemCount { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

// GET /api/orders/{id}
public record OrderDetailDto
{
    public Guid Id { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public OrderStatus Status { get; init; }

    // Financial
    public decimal SubTotal { get; init; }
    public decimal? DiscountAmount { get; init; }
    public decimal? ShippingAmount { get; init; }
    public decimal? TaxAmount { get; init; }
    public decimal GrandTotal { get; init; }
    public string Currency { get; init; } = "VND";

    // Addresses
    public AddressDto ShippingAddress { get; init; } = new();
    public AddressDto? BillingAddress { get; init; }

    // Shipping
    public string? ShippingMethod { get; init; }
    public string? TrackingNumber { get; init; }
    public string? ShippingCarrier { get; init; }

    // Customer
    public string CustomerEmail { get; init; } = string.Empty;
    public string? CustomerPhone { get; init; }
    public string? CustomerName { get; init; }

    // Items
    public IEnumerable<OrderItemDto> Items { get; init; } = [];

    // Payments
    public IEnumerable<PaymentSummaryDto> Payments { get; init; } = [];

    // Timestamps
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? ShippedAt { get; init; }
    public DateTimeOffset? DeliveredAt { get; init; }
}

public record OrderItemDto
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public Guid? VariantId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string? VariantName { get; init; }
    public string? Sku { get; init; }
    public string? ImageUrl { get; init; }
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal LineTotal { get; init; }
}

// PUT /api/orders/admin/{id}/status
public record UpdateOrderStatusRequest
{
    public OrderStatus Status { get; init; }
    public string? TrackingNumber { get; init; }
    public string? ShippingCarrier { get; init; }
    public string? InternalNote { get; init; }
}
```

---

## 5. Database Schema

### 5.1 Table Definitions

```sql
-- =============================================
-- PRODUCT TABLES
-- =============================================

CREATE TABLE ProductCategories (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    TenantId NVARCHAR(64) NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    Slug NVARCHAR(100) NOT NULL,
    ParentId UNIQUEIDENTIFIER NULL,
    SortOrder INT NOT NULL DEFAULT 0,
    ImageUrl NVARCHAR(500) NULL,
    CreatedAt DATETIMEOFFSET NOT NULL,
    UpdatedAt DATETIMEOFFSET NOT NULL,

    CONSTRAINT FK_ProductCategories_Parent
        FOREIGN KEY (ParentId) REFERENCES ProductCategories(Id),
    CONSTRAINT UQ_ProductCategories_TenantId_Slug
        UNIQUE (TenantId, Slug)
);

CREATE TABLE Products (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    TenantId NVARCHAR(64) NOT NULL,
    Name NVARCHAR(200) NOT NULL,
    Slug NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    DescriptionHtml NVARCHAR(MAX) NULL,
    BasePrice DECIMAL(18,2) NOT NULL,
    Currency NVARCHAR(10) NOT NULL DEFAULT 'VND',
    Status INT NOT NULL, -- ProductStatus enum
    CategoryId UNIQUEIDENTIFIER NULL,
    Sku NVARCHAR(50) NULL,
    Barcode NVARCHAR(50) NULL,
    Weight DECIMAL(10,2) NULL,
    TrackInventory BIT NOT NULL DEFAULT 1,
    MetaTitle NVARCHAR(100) NULL,
    MetaDescription NVARCHAR(300) NULL,
    SortOrder INT NOT NULL DEFAULT 0,
    CreatedAt DATETIMEOFFSET NOT NULL,
    UpdatedAt DATETIMEOFFSET NOT NULL,

    CONSTRAINT FK_Products_Category
        FOREIGN KEY (CategoryId) REFERENCES ProductCategories(Id),
    CONSTRAINT UQ_Products_TenantId_Slug
        UNIQUE (TenantId, Slug)
);

CREATE TABLE ProductVariants (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    TenantId NVARCHAR(64) NOT NULL,
    ProductId UNIQUEIDENTIFIER NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    Sku NVARCHAR(50) NULL,
    Price DECIMAL(18,2) NOT NULL,
    CompareAtPrice DECIMAL(18,2) NULL,
    StockQuantity INT NOT NULL DEFAULT 0,
    OptionsJson NVARCHAR(MAX) NULL, -- {"color": "Red", "size": "M"}
    SortOrder INT NOT NULL DEFAULT 0,
    CreatedAt DATETIMEOFFSET NOT NULL,
    UpdatedAt DATETIMEOFFSET NOT NULL,
    RowVersion ROWVERSION, -- For concurrency

    CONSTRAINT FK_ProductVariants_Product
        FOREIGN KEY (ProductId) REFERENCES Products(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_ProductVariants_TenantId_Sku
        UNIQUE (TenantId, Sku) WHERE Sku IS NOT NULL
);

CREATE TABLE ProductImages (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    TenantId NVARCHAR(64) NOT NULL,
    ProductId UNIQUEIDENTIFIER NOT NULL,
    Url NVARCHAR(500) NOT NULL,
    AltText NVARCHAR(200) NULL,
    SortOrder INT NOT NULL DEFAULT 0,
    IsPrimary BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIMEOFFSET NOT NULL,

    CONSTRAINT FK_ProductImages_Product
        FOREIGN KEY (ProductId) REFERENCES Products(Id) ON DELETE CASCADE
);

-- =============================================
-- CART TABLES
-- =============================================

CREATE TABLE Carts (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    TenantId NVARCHAR(64) NOT NULL,
    CustomerId UNIQUEIDENTIFIER NULL,
    SessionId NVARCHAR(100) NULL,
    Status INT NOT NULL, -- CartStatus enum
    Currency NVARCHAR(10) NOT NULL DEFAULT 'VND',
    AbandonedAt DATETIMEOFFSET NULL,
    CreatedAt DATETIMEOFFSET NOT NULL,
    UpdatedAt DATETIMEOFFSET NOT NULL,

    -- Either CustomerId or SessionId must be set
    CONSTRAINT CK_Carts_CustomerOrSession
        CHECK (CustomerId IS NOT NULL OR SessionId IS NOT NULL)
);

CREATE TABLE CartItems (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    TenantId NVARCHAR(64) NOT NULL,
    CartId UNIQUEIDENTIFIER NOT NULL,
    ProductId UNIQUEIDENTIFIER NOT NULL,
    VariantId UNIQUEIDENTIFIER NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    CreatedAt DATETIMEOFFSET NOT NULL,
    UpdatedAt DATETIMEOFFSET NOT NULL,

    CONSTRAINT FK_CartItems_Cart
        FOREIGN KEY (CartId) REFERENCES Carts(Id) ON DELETE CASCADE,
    CONSTRAINT FK_CartItems_Product
        FOREIGN KEY (ProductId) REFERENCES Products(Id),
    CONSTRAINT FK_CartItems_Variant
        FOREIGN KEY (VariantId) REFERENCES ProductVariants(Id),
    CONSTRAINT UQ_CartItems_Cart_Product_Variant
        UNIQUE (CartId, ProductId, VariantId)
);

-- =============================================
-- CHECKOUT TABLES
-- =============================================

CREATE TABLE CheckoutSessions (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    TenantId NVARCHAR(64) NOT NULL,
    CartId UNIQUEIDENTIFIER NOT NULL,
    CustomerId UNIQUEIDENTIFIER NULL,
    Status INT NOT NULL, -- CheckoutSessionStatus enum
    ExpiresAt DATETIMEOFFSET NOT NULL,
    CompletedAt DATETIMEOFFSET NULL,

    -- Contact
    Email NVARCHAR(256) NOT NULL,
    Phone NVARCHAR(20) NULL,

    -- Shipping Address (Owned)
    ShippingAddress_FullName NVARCHAR(100) NULL,
    ShippingAddress_Phone NVARCHAR(20) NULL,
    ShippingAddress_AddressLine1 NVARCHAR(200) NULL,
    ShippingAddress_AddressLine2 NVARCHAR(200) NULL,
    ShippingAddress_Ward NVARCHAR(100) NULL,
    ShippingAddress_District NVARCHAR(100) NULL,
    ShippingAddress_Province NVARCHAR(100) NULL,
    ShippingAddress_Country NVARCHAR(100) NULL,
    ShippingAddress_PostalCode NVARCHAR(20) NULL,

    -- Billing Address (Owned)
    BillingAddress_FullName NVARCHAR(100) NULL,
    BillingAddress_Phone NVARCHAR(20) NULL,
    BillingAddress_AddressLine1 NVARCHAR(200) NULL,
    BillingAddress_AddressLine2 NVARCHAR(200) NULL,
    BillingAddress_Ward NVARCHAR(100) NULL,
    BillingAddress_District NVARCHAR(100) NULL,
    BillingAddress_Province NVARCHAR(100) NULL,
    BillingAddress_Country NVARCHAR(100) NULL,
    BillingAddress_PostalCode NVARCHAR(20) NULL,
    BillingSameAsShipping BIT NOT NULL DEFAULT 1,

    -- Shipping
    ShippingMethodId UNIQUEIDENTIFIER NULL,
    ShippingMethodName NVARCHAR(100) NULL,
    ShippingAmount DECIMAL(18,2) NOT NULL DEFAULT 0,

    -- Totals
    SubTotal DECIMAL(18,2) NOT NULL,
    DiscountAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    TaxAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    GrandTotal DECIMAL(18,2) NOT NULL,
    Currency NVARCHAR(10) NOT NULL DEFAULT 'VND',

    -- Result
    OrderId UNIQUEIDENTIFIER NULL,
    PaymentTransactionId UNIQUEIDENTIFIER NULL,

    CreatedAt DATETIMEOFFSET NOT NULL,
    UpdatedAt DATETIMEOFFSET NOT NULL,

    CONSTRAINT FK_CheckoutSessions_Cart
        FOREIGN KEY (CartId) REFERENCES Carts(Id),
    CONSTRAINT FK_CheckoutSessions_Order
        FOREIGN KEY (OrderId) REFERENCES Orders(Id),
    CONSTRAINT FK_CheckoutSessions_Payment
        FOREIGN KEY (PaymentTransactionId) REFERENCES PaymentTransactions(Id)
);

CREATE TABLE InventoryReservations (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    TenantId NVARCHAR(64) NOT NULL,
    CheckoutSessionId UNIQUEIDENTIFIER NOT NULL,
    ProductVariantId UNIQUEIDENTIFIER NOT NULL,
    Quantity INT NOT NULL,
    Status INT NOT NULL, -- ReservationStatus enum
    ExpiresAt DATETIMEOFFSET NOT NULL,
    ConfirmedAt DATETIMEOFFSET NULL,
    ReleasedAt DATETIMEOFFSET NULL,
    OrderId UNIQUEIDENTIFIER NULL,
    CreatedAt DATETIMEOFFSET NOT NULL,
    UpdatedAt DATETIMEOFFSET NOT NULL,

    CONSTRAINT FK_InventoryReservations_CheckoutSession
        FOREIGN KEY (CheckoutSessionId) REFERENCES CheckoutSessions(Id),
    CONSTRAINT FK_InventoryReservations_Variant
        FOREIGN KEY (ProductVariantId) REFERENCES ProductVariants(Id),
    CONSTRAINT FK_InventoryReservations_Order
        FOREIGN KEY (OrderId) REFERENCES Orders(Id)
);

-- =============================================
-- ORDER TABLES
-- =============================================

CREATE TABLE Orders (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    TenantId NVARCHAR(64) NOT NULL,
    OrderNumber NVARCHAR(50) NOT NULL,
    CustomerId UNIQUEIDENTIFIER NULL,
    Status INT NOT NULL, -- OrderStatus enum

    -- Financial
    SubTotal DECIMAL(18,2) NOT NULL,
    DiscountAmount DECIMAL(18,2) NULL,
    ShippingAmount DECIMAL(18,2) NULL,
    TaxAmount DECIMAL(18,2) NULL,
    GrandTotal DECIMAL(18,2) NOT NULL,
    Currency NVARCHAR(10) NOT NULL DEFAULT 'VND',

    -- Shipping Address (Owned)
    ShippingAddress_FullName NVARCHAR(100) NOT NULL,
    ShippingAddress_Phone NVARCHAR(20) NOT NULL,
    ShippingAddress_AddressLine1 NVARCHAR(200) NOT NULL,
    ShippingAddress_AddressLine2 NVARCHAR(200) NULL,
    ShippingAddress_Ward NVARCHAR(100) NOT NULL,
    ShippingAddress_District NVARCHAR(100) NOT NULL,
    ShippingAddress_Province NVARCHAR(100) NOT NULL,
    ShippingAddress_Country NVARCHAR(100) NOT NULL,
    ShippingAddress_PostalCode NVARCHAR(20) NULL,

    -- Billing Address (Owned)
    BillingAddress_FullName NVARCHAR(100) NULL,
    BillingAddress_Phone NVARCHAR(20) NULL,
    BillingAddress_AddressLine1 NVARCHAR(200) NULL,
    BillingAddress_AddressLine2 NVARCHAR(200) NULL,
    BillingAddress_Ward NVARCHAR(100) NULL,
    BillingAddress_District NVARCHAR(100) NULL,
    BillingAddress_Province NVARCHAR(100) NULL,
    BillingAddress_Country NVARCHAR(100) NULL,
    BillingAddress_PostalCode NVARCHAR(20) NULL,

    -- Shipping
    ShippingMethod NVARCHAR(100) NULL,
    TrackingNumber NVARCHAR(100) NULL,
    ShippingCarrier NVARCHAR(100) NULL,

    -- Customer
    CustomerEmail NVARCHAR(256) NOT NULL,
    CustomerPhone NVARCHAR(20) NULL,
    CustomerName NVARCHAR(100) NULL,

    -- Notes
    CustomerNote NVARCHAR(500) NULL,
    InternalNote NVARCHAR(1000) NULL,

    -- Coupon (future Phase 11)
    CouponCode NVARCHAR(50) NULL,
    CouponId UNIQUEIDENTIFIER NULL,

    CreatedAt DATETIMEOFFSET NOT NULL,
    UpdatedAt DATETIMEOFFSET NOT NULL,
    ShippedAt DATETIMEOFFSET NULL,
    DeliveredAt DATETIMEOFFSET NULL,

    CONSTRAINT UQ_Orders_TenantId_OrderNumber
        UNIQUE (TenantId, OrderNumber)
);

CREATE TABLE OrderItems (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    TenantId NVARCHAR(64) NOT NULL,
    OrderId UNIQUEIDENTIFIER NOT NULL,
    ProductId UNIQUEIDENTIFIER NOT NULL,
    ProductVariantId UNIQUEIDENTIFIER NULL,
    ProductName NVARCHAR(200) NOT NULL,
    VariantName NVARCHAR(100) NULL,
    Sku NVARCHAR(50) NULL,
    ImageUrl NVARCHAR(500) NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    LineTotal DECIMAL(18,2) NOT NULL,
    CreatedAt DATETIMEOFFSET NOT NULL,

    CONSTRAINT FK_OrderItems_Order
        FOREIGN KEY (OrderId) REFERENCES Orders(Id) ON DELETE CASCADE
);

-- =============================================
-- INVENTORY TABLES
-- =============================================

CREATE TABLE InventoryMovements (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    TenantId NVARCHAR(64) NOT NULL,
    ProductVariantId UNIQUEIDENTIFIER NOT NULL,
    Type INT NOT NULL, -- InventoryMovementType enum
    Quantity INT NOT NULL,
    StockBefore INT NOT NULL,
    StockAfter INT NOT NULL,
    Reason NVARCHAR(500) NULL,
    ReferenceType NVARCHAR(50) NULL,
    ReferenceId UNIQUEIDENTIFIER NULL,
    CreatedBy NVARCHAR(256) NULL,
    CreatedAt DATETIMEOFFSET NOT NULL,

    CONSTRAINT FK_InventoryMovements_Variant
        FOREIGN KEY (ProductVariantId) REFERENCES ProductVariants(Id)
);
```

### 5.2 Indexes

```sql
-- =============================================
-- PERFORMANCE INDEXES
-- =============================================

-- Product filtering and listing
CREATE INDEX IX_Products_TenantId_Status_CategoryId
    ON Products(TenantId, Status, CategoryId);
CREATE INDEX IX_Products_TenantId_BasePrice
    ON Products(TenantId, BasePrice);
CREATE INDEX IX_Products_TenantId_CreatedAt
    ON Products(TenantId, CreatedAt DESC);

-- Full-text search (if using SQL Server)
CREATE FULLTEXT INDEX ON Products(Name, Description)
    KEY INDEX PK_Products;

-- Variant stock lookups
CREATE INDEX IX_ProductVariants_TenantId_StockQuantity
    ON ProductVariants(TenantId, StockQuantity)
    WHERE StockQuantity > 0;

-- Cart lookups
CREATE INDEX IX_Carts_TenantId_CustomerId_Status
    ON Carts(TenantId, CustomerId, Status)
    WHERE CustomerId IS NOT NULL;
CREATE INDEX IX_Carts_TenantId_SessionId_Status
    ON Carts(TenantId, SessionId, Status)
    WHERE SessionId IS NOT NULL;

-- Checkout session expiration
CREATE INDEX IX_CheckoutSessions_Status_ExpiresAt
    ON CheckoutSessions(Status, ExpiresAt)
    WHERE Status = 0; -- Started

-- Inventory reservation cleanup
CREATE INDEX IX_InventoryReservations_Status_ExpiresAt
    ON InventoryReservations(Status, ExpiresAt)
    WHERE Status = 0; -- Temporary

-- Order lookups
CREATE INDEX IX_Orders_TenantId_CustomerId_CreatedAt
    ON Orders(TenantId, CustomerId, CreatedAt DESC);
CREATE INDEX IX_Orders_TenantId_Status_CreatedAt
    ON Orders(TenantId, Status, CreatedAt DESC);
```

---

## 6. Component Architecture

### 6.1 Application Layer Structure

```
src/NOIR.Application/Features/
├── Products/
│   ├── Commands/
│   │   ├── CreateProduct/
│   │   │   ├── CreateProductCommand.cs
│   │   │   ├── CreateProductCommandHandler.cs
│   │   │   └── CreateProductCommandValidator.cs
│   │   ├── UpdateProduct/
│   │   ├── PublishProduct/
│   │   ├── ArchiveProduct/
│   │   ├── AddVariant/
│   │   ├── UpdateVariant/
│   │   ├── DeleteVariant/
│   │   ├── UploadImage/
│   │   └── DeleteImage/
│   ├── Queries/
│   │   ├── GetProductById/
│   │   ├── GetProductBySlug/
│   │   ├── GetProductsWithFilters/
│   │   └── GetProductsForAdmin/
│   ├── Specifications/
│   │   ├── ProductByIdSpec.cs
│   │   ├── ProductBySlugSpec.cs
│   │   ├── ProductsByFiltersSpec.cs
│   │   └── ActiveProductsSpec.cs
│   └── DTOs/
│       ├── ProductCardDto.cs
│       ├── ProductDetailDto.cs
│       └── ProductVariantDto.cs
│
├── ProductCategories/
│   ├── Commands/
│   │   ├── CreateCategory/
│   │   ├── UpdateCategory/
│   │   └── DeleteCategory/
│   ├── Queries/
│   │   ├── GetCategoryTree/
│   │   └── GetCategoryById/
│   └── Specifications/
│
├── Cart/
│   ├── Commands/
│   │   ├── AddToCart/
│   │   ├── UpdateCartItem/
│   │   ├── RemoveFromCart/
│   │   ├── ClearCart/
│   │   └── MergeGuestCart/
│   ├── Queries/
│   │   ├── GetCart/
│   │   └── GetCartSummary/
│   └── Specifications/
│       ├── ActiveCartByCustomerSpec.cs
│       └── ActiveCartBySessionSpec.cs
│
├── Checkout/
│   ├── Commands/
│   │   ├── StartCheckout/
│   │   ├── SetShippingAddress/
│   │   ├── SetBillingAddress/
│   │   ├── SelectShippingMethod/
│   │   ├── ProcessPayment/
│   │   └── PlaceOrder/
│   ├── Queries/
│   │   ├── GetCheckoutSession/
│   │   └── GetShippingMethods/
│   └── Specifications/
│
├── Orders/
│   ├── Commands/
│   │   ├── UpdateOrderStatus/
│   │   ├── CancelOrder/
│   │   └── ProcessRefund/
│   ├── Queries/
│   │   ├── GetOrderById/
│   │   ├── GetMyOrders/
│   │   └── GetOrdersForAdmin/
│   └── Specifications/
│
└── Inventory/
    ├── Commands/
    │   ├── AdjustStock/
    │   └── ReleaseExpiredReservations/
    ├── Queries/
    │   └── GetInventoryMovements/
    └── Specifications/
```

### 6.2 Background Jobs

**Location:** `src/NOIR.Infrastructure/Services/Jobs/`

```csharp
// ExpiredReservationCleanupJob.cs
public class ExpiredReservationCleanupJob : IScopedService
{
    private readonly IRepository<InventoryReservation, Guid> _reservationRepo;
    private readonly IRepository<ProductVariant, Guid> _variantRepo;
    private readonly IRepository<CheckoutSession, Guid> _sessionRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ExpiredReservationCleanupJob> _logger;

    public async Task ExecuteAsync(CancellationToken ct)
    {
        // Find expired temporary reservations
        var expiredSpec = new ExpiredReservationsSpec();
        var expiredReservations = await _reservationRepo.ListAsync(expiredSpec, ct);

        foreach (var reservation in expiredReservations)
        {
            // Release stock back to variant
            var variantSpec = new ProductVariantByIdForUpdateSpec(reservation.ProductVariantId);
            var variant = await _variantRepo.FirstOrDefaultAsync(variantSpec, ct);

            if (variant != null)
            {
                variant.ReleaseStock(reservation.Quantity);
            }

            // Mark reservation as released
            reservation.Release();

            // Optionally mark checkout session as expired
            var session = await _sessionRepo.GetByIdAsync(reservation.CheckoutSessionId, ct);
            if (session?.Status == CheckoutSessionStatus.Started)
            {
                session.MarkExpired();
            }
        }

        await _unitOfWork.SaveChangesAsync(ct);
        _logger.LogInformation("Released {Count} expired reservations", expiredReservations.Count);
    }
}

// Register in Hangfire
RecurringJob.AddOrUpdate<ExpiredReservationCleanupJob>(
    "cleanup-expired-reservations",
    job => job.ExecuteAsync(CancellationToken.None),
    Cron.Minutely);
```

### 6.3 Domain Events

**Location:** `src/NOIR.Domain/Events/Ecommerce/`

```csharp
// ProductEvents.cs
public record ProductCreatedEvent(Guid ProductId, string Name, string Slug) : DomainEvent;
public record ProductPublishedEvent(Guid ProductId, string Name) : DomainEvent;
public record ProductArchivedEvent(Guid ProductId) : DomainEvent;
public record ProductStockChangedEvent(
    Guid ProductVariantId,
    int OldQuantity,
    int NewQuantity,
    string ChangeReason) : DomainEvent;

// CartEvents.cs
public record CartCreatedEvent(Guid CartId, Guid? CustomerId, string? SessionId) : DomainEvent;
public record CartItemAddedEvent(Guid CartId, Guid ProductId, int Quantity) : DomainEvent;
public record CartAbandonedEvent(Guid CartId, decimal SubTotal) : DomainEvent;
public record CartConvertedEvent(Guid CartId, Guid OrderId) : DomainEvent;

// CheckoutEvents.cs
public record CheckoutStartedEvent(
    Guid CheckoutSessionId,
    Guid CartId,
    Guid? CustomerId,
    decimal SubTotal) : DomainEvent;
public record CheckoutCompletedEvent(
    Guid CheckoutSessionId,
    Guid OrderId,
    decimal GrandTotal) : DomainEvent;
public record CheckoutExpiredEvent(Guid CheckoutSessionId, Guid CartId) : DomainEvent;

// OrderEvents.cs
public record OrderCreatedEvent(Guid OrderId, string OrderNumber) : DomainEvent;
public record OrderConfirmedEvent(Guid OrderId, string OrderNumber) : DomainEvent;
public record OrderShippedEvent(
    Guid OrderId,
    string TrackingNumber,
    string Carrier) : DomainEvent;
public record OrderDeliveredEvent(Guid OrderId) : DomainEvent;
public record OrderCancelledEvent(Guid OrderId, string? Reason) : DomainEvent;
public record OrderRefundedEvent(Guid OrderId, decimal Amount) : DomainEvent;
```

---

## 7. Integration Patterns

### 7.1 Payment Integration (Phase 5-7)

```csharp
// PlaceOrderCommandHandler.cs
public async Task<Result<PlaceOrderResponse>> Handle(
    PlaceOrderCommand command,
    CancellationToken ct)
{
    // 1. Get checkout session
    var session = await _sessionRepo.FirstOrDefaultAsync(
        new CheckoutSessionByIdSpec(command.SessionId), ct);

    if (session == null || session.IsExpired)
        return Result.Failure<PlaceOrderResponse>(Errors.Checkout.SessionExpired);

    // 2. Validate cart items still available
    var cart = await _cartRepo.FirstOrDefaultAsync(
        new CartByIdWithItemsSpec(session.CartId), ct);

    var validationResult = await ValidateCartItems(cart, ct);
    if (!validationResult.IsSuccess)
        return validationResult;

    // 3. Create payment transaction (use existing Phase 5-7 infrastructure)
    var createPaymentCommand = new CreatePaymentCommand
    {
        Amount = session.GrandTotal,
        Currency = session.Currency,
        Provider = command.PaymentProvider,
        PaymentMethod = command.PaymentMethod,
        CustomerId = session.CustomerId,
        Metadata = new Dictionary<string, string>
        {
            ["checkout_session_id"] = session.Id.ToString(),
            ["cart_id"] = session.CartId.ToString()
        }
    };

    var paymentResult = await _messageBus.InvokeAsync<Result<PaymentTransactionDto>>(
        createPaymentCommand, ct);

    if (!paymentResult.IsSuccess)
        return Result.Failure<PlaceOrderResponse>(paymentResult.Error);

    // 4. Create order
    var order = Order.Create(
        orderNumber: await _orderNumberGenerator.GenerateAsync(ct),
        customerEmail: session.Email,
        subTotal: session.SubTotal,
        grandTotal: session.GrandTotal,
        tenantId: session.TenantId);

    order.SetShippingAddress(session.ShippingAddress!);
    order.SetShipping(session.ShippingMethodName, session.ShippingAmount);

    // Add order items from cart
    foreach (var cartItem in cart.Items)
    {
        order.AddItem(
            cartItem.ProductId,
            cartItem.VariantId,
            await GetProductName(cartItem.ProductId, ct),
            await GetVariantName(cartItem.VariantId, ct),
            cartItem.Quantity,
            cartItem.UnitPrice);
    }

    await _orderRepo.AddAsync(order, ct);

    // 5. Confirm inventory reservations
    var reservations = await _reservationRepo.ListAsync(
        new ReservationsByCheckoutSessionSpec(session.Id), ct);

    foreach (var reservation in reservations)
    {
        reservation.Confirm(order.Id);
    }

    // 6. Update checkout session
    session.Complete(order.Id);

    // 7. Mark cart as converted
    cart.MarkAsConverted();

    // 8. Link payment to order
    // (PaymentTransaction already has OrderId field from Phase 5-7)

    await _unitOfWork.SaveChangesAsync(ct);

    // 9. Send order confirmation email (via domain event handler)
    // OrderCreatedEvent is raised in Order.Create()

    return Result.Success(new PlaceOrderResponse
    {
        OrderId = order.Id,
        OrderNumber = order.OrderNumber,
        Status = order.Status,
        GrandTotal = order.GrandTotal,
        Currency = order.Currency
    });
}
```

### 7.2 Guest Cart Synchronization

```csharp
// Frontend: CartProvider.tsx
const CartProvider = ({ children }: { children: React.ReactNode }) => {
  const { user, isAuthenticated } = useAuth();
  const [cart, setCart] = useState<Cart | null>(null);

  useEffect(() => {
    const syncCart = async () => {
      if (isAuthenticated && user?.id) {
        // Check for guest cart in localStorage
        const guestSessionId = localStorage.getItem('guest_session_id');

        if (guestSessionId) {
          // Merge guest cart with user cart
          await api.post('/api/cart/merge', { guestSessionId });
          localStorage.removeItem('guest_session_id');
        }

        // Fetch user cart
        const { data } = await api.get('/api/cart');
        setCart(data);
      } else {
        // Use guest session
        let sessionId = localStorage.getItem('guest_session_id');
        if (!sessionId) {
          sessionId = crypto.randomUUID();
          localStorage.setItem('guest_session_id', sessionId);
        }

        // Fetch guest cart with session ID in header
        const { data } = await api.get('/api/cart', {
          headers: { 'X-Guest-Session-Id': sessionId }
        });
        setCart(data);
      }
    };

    syncCart();
  }, [isAuthenticated, user?.id]);

  // ... rest of context
};
```

---

## Summary

This architecture design provides:

1. **Clear Entity Boundaries**: Aggregate roots and entities with proper encapsulation
2. **CheckoutSession Pattern**: Orchestrates the entire checkout flow with inventory reservations
3. **15-Minute Inventory Holds**: Prevents overselling during checkout
4. **Comprehensive API Contracts**: RESTful endpoints with detailed DTOs
5. **Optimized Database Schema**: Proper indexes for e-commerce query patterns
6. **Integration with Phase 5-7**: Seamless payment gateway usage

**Next Step**: Use `/sc:implement` to start building Sprint 1 (Enums, Address, ProductCategory, Product).

---

*Architecture design generated based on validated requirements. Ready for implementation.*
