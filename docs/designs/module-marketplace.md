# Module: Marketplace / Multi-vendor

> Priority: **Phase 8+** (long-term). Complexity: Very Large. Depends on: Products, Orders, Payments (all existing).

---

## Why Long-Term

This module fundamentally changes the Product → Order → Payment flow. Transforms NOIR from single-store e-commerce to a marketplace platform. Only build if market demand exists.

**Strategy**: Build as a separate "marketplace mode" toggled via Feature Management. Single-store mode remains default.

---

## Entities

```
Vendor (TenantAggregateRoot<Guid>)
├── Id, Name, Slug, Description (rich text), Logo (MediaFileId)
├── UserId (FK → User — vendor account owner)
├── Status (Pending/Active/Suspended/Rejected)
├── CommissionRate (decimal %, default from tenant config)
├── PayoutSchedule (Weekly/BiWeekly/Monthly)
├── BankAccountNumber, BankName, BankBranch (encrypted)
├── TaxCode (MST for VN)
├── Phone, Email, Address
├── VendorSettings (JSON: custom config)
├── ApprovedAt, ApprovedById
├── TenantId
└── Rating (computed from reviews)

VendorProduct (TenantEntity — extends Product for vendor context)
├── Id, ProductId (FK → Product), VendorId (FK → Vendor)
├── VendorPrice (vendor's base price)
├── CommissionRate (override per product, nullable — falls back to vendor rate)
├── CommissionAmount (computed)
├── Status (PendingReview/Approved/Rejected)
├── ReviewedById, ReviewedAt, RejectionReason
└── TenantId

VendorOrder (TenantAggregateRoot<Guid>)
├── Id, OrderId (FK → Order — parent order)
├── VendorId (FK → Vendor)
├── Items[] (JSON: ordered items for this vendor)
├── SubTotal, CommissionAmount, VendorPayout (computed)
├── Status (Pending/Processing/Shipped/Delivered/Cancelled)
├── ShippingInfo (separate tracking per vendor)
├── TenantId
└── FulfilledAt

VendorPayout (TenantAggregateRoot<Guid>)
├── Id, VendorId (FK)
├── Amount, Currency
├── Period (start/end dates)
├── Status (Pending/Processing/Paid/Failed)
├── PaymentReference, PaidAt
├── TenantId
└── PayoutItems[]

PayoutItem (TenantEntity)
├── Id, VendorPayoutId (FK)
├── VendorOrderId (FK)
├── OrderAmount, CommissionAmount, PayoutAmount
└── TenantId
```

---

## Features (Commands + Queries)

### Vendor Management (Admin)
| Command/Query | Description |
|---------------|-------------|
| `ApproveVendorCommand` | Approve vendor application |
| `RejectVendorCommand` | Reject with reason |
| `SuspendVendorCommand` | Suspend vendor (hide products) |
| `UpdateVendorCommissionCommand` | Change vendor commission rate |
| `GetVendorsQuery` | Admin list, filter by status |
| `GetVendorByIdQuery` | Vendor detail with stats |

### Vendor Portal (Self-service)
| Command/Query | Description |
|---------------|-------------|
| `RegisterVendorCommand` | Submit vendor application |
| `UpdateVendorProfileCommand` | Update own profile |
| `AddVendorProductCommand` | Submit product for review |
| `UpdateVendorProductCommand` | Update own product |
| `GetMyProductsQuery` | Vendor's products with status |
| `GetMyOrdersQuery` | Vendor's orders |
| `GetMyPayoutsQuery` | Vendor's payout history |
| `GetVendorDashboardQuery` | Vendor stats: sales, commission, pending payout |

### Product Review (Admin)
| Command/Query | Description |
|---------------|-------------|
| `ApproveVendorProductCommand` | Approve product for marketplace |
| `RejectVendorProductCommand` | Reject with reason |
| `GetPendingProductsQuery` | Products awaiting review |

### Order Splitting
| Command/Query | Description |
|---------------|-------------|
| `SplitOrderByVendorCommand` | Auto-split order into VendorOrders on checkout |
| `FulfillVendorOrderCommand` | Vendor marks their portion as shipped |
| `GetVendorOrdersQuery` | Admin view: all vendor orders |

### Payout Management (Admin)
| Command/Query | Description |
|---------------|-------------|
| `CreatePayoutCommand` | Create payout batch for period |
| `ProcessPayoutCommand` | Execute bank transfers |
| `GetPayoutsQuery` | Payout history, filter by vendor/status |
| `GetPayoutByIdQuery` | Detail with items |

---

## Frontend Pages

### Admin Pages
| Route | Page |
|-------|------|
| `/portal/marketplace/vendors` | Vendor list, approve/reject |
| `/portal/marketplace/vendors/:id` | Vendor detail, commission config |
| `/portal/marketplace/products/review` | Pending product review queue |
| `/portal/marketplace/payouts` | Payout management |
| `/portal/marketplace/payouts/:id` | Payout detail |
| `/portal/marketplace/settings` | Commission defaults, payout schedule |

### Vendor Portal Pages
| Route | Page |
|-------|------|
| `/vendor/dashboard` | Vendor dashboard: sales, payouts, orders |
| `/vendor/products` | My products list |
| `/vendor/products/new` | Add product |
| `/vendor/orders` | My orders |
| `/vendor/payouts` | My payout history |
| `/vendor/settings` | Profile, bank info |

---

## Order Flow Changes

```
Current (single-store):
  Cart → Checkout → Order → Payment → Fulfillment

Marketplace mode:
  Cart (multi-vendor) → Checkout → Order → Payment
    → SplitOrderByVendor → VendorOrder[vendor1] + VendorOrder[vendor2]
    → Each vendor fulfills independently
    → Track shipping per vendor
    → Payout: aggregate completed VendorOrders → batch payout
```

---

## Integration Points

| Module | Integration |
|--------|-------------|
| **Products** | VendorProduct extends Product with vendor context |
| **Orders** | Order split into VendorOrders |
| **Payments** | Single payment for customer, split payout for vendors |
| **Notifications** | Vendor: new order, payout processed. Admin: new vendor, product pending |
| **Webhooks** | vendor.approved, vendor_order.shipped, payout.processed |
| **Reviews** | Customer reviews per vendor (future) |

---

## Phased Implementation

### Phase 1 — Vendor Registration + Product Review
```
Backend:
├── Domain: Vendor, VendorProduct
├── Application: Vendor registration/approval, product submission/review
├── Endpoints: VendorEndpoints, VendorProductEndpoints
├── Module: MarketplaceModuleDefinition (default disabled, opt-in)
├── Permissions: marketplace:vendors:*, marketplace:products:review
└── Vendor Portal: Separate route group /vendor/*

Frontend:
├── Admin: Vendor list, product review queue
├── Vendor: Registration, dashboard, product management
└── i18n: EN + VI
```

### Phase 2 — Order Splitting + Vendor Fulfillment
```
├── Domain: VendorOrder
├── Order splitting logic: Auto-split on checkout by vendor
├── Vendor fulfillment: Independent shipping per vendor
├── Multi-vendor cart: Show items grouped by vendor
└── Shipping: Per-vendor tracking
```

### Phase 3 — Payouts + Commission + Advanced
```
├── Domain: VendorPayout, PayoutItem
├── Payout batch: Admin creates, reviews, processes
├── Commission: Configurable per vendor, per product, per category
├── Vendor ratings: Customer rates vendor after delivery
├── Vendor analytics: Sales, returns, rating trends
└── Automated payouts: Background job for scheduled payouts
```

---

## Architecture Notes

### Marketplace Mode Toggle
```csharp
// Feature gate the entire module
public sealed class MarketplaceModuleDefinition : IModuleDefinition, ISingletonService
{
    public string Name => ModuleNames.Erp.Marketplace;
    public bool IsCore => false;
    public bool DefaultEnabled => false; // Explicit opt-in only
}
```

### Commission Calculation
```
For each VendorOrder item:
  EffectiveRate = VendorProduct.CommissionRate ?? Vendor.CommissionRate ?? TenantDefault
  CommissionAmount = ItemTotal * EffectiveRate / 100
  VendorPayout = ItemTotal - CommissionAmount
```

### Security
- Vendor portal is a separate authenticated area (not admin portal)
- Vendors can only see/edit their own products, orders, payouts
- Bank info encrypted at rest (use existing encryption infrastructure)
- Admin approval required for: vendor registration, product listing
