# Phase 8: E-commerce Core - Requirements Specification

**Generated:** January 25, 2026
**Updated:** January 26, 2026
**Status:** Backend Complete, Frontend Pending
**Scope:** Full Phase 8 (excluding coupons, deferred to Phase 11)

---

## Implementation Status Summary

| Component | Status | Notes |
|-----------|--------|-------|
| **Domain Entities** | ✅ Complete | Product, ProductCategory, ProductVariant, ProductImage, Cart, CartItem, Order, OrderItem, CheckoutSession |
| **Enums** | ✅ Complete | ProductStatus, CartStatus, OrderStatus, CheckoutSessionStatus, ReservationStatus, InventoryMovementType |
| **Application Layer** | ✅ Complete | Full CQRS: Commands, Queries, Handlers, Validators, DTOs, Specifications |
| **API Endpoints** | ✅ Complete | ProductEndpoints, ProductCategoryEndpoints, CartEndpoints, OrderEndpoints, CheckoutEndpoints |
| **Unit Tests** | ✅ Complete | 18 test files covering Products, Cart, Orders |
| **Admin Product UI** | ⏳ Pending | Product management pages needed |
| **Storefront UI** | ⏳ Pending | Catalog, Cart, Checkout pages needed |

---

## Executive Summary

This document captures validated requirements for NOIR Phase 8 E-commerce Core based on:
1. Existing roadmap specifications
2. UX research findings (see `docs/backend/research/ecommerce-ux-patterns-2026.md`)
3. User decisions from requirements discovery session

---

## Confirmed Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| **MVP Scope** | Full Phase 8 | All features: Product, Cart, Checkout, Orders, Inventory |
| **Guest Cart** | localStorage + DB sync | Speed for UX + DB for abandonment recovery |
| **Coupons** | Deferred to Phase 11 | Focus on core e-commerce first |
| **Variants** | Flexible attributes (JSON) | Future-proof for any product type |
| **Inventory** | 15-minute soft hold | Reserve at checkout start, background cleanup |
| **Reviews** | Deferred to Phase 9 | Add with Customer Management |
| **Search** | Full faceted navigation | Multi-select filters, price range, dynamic counts |
| **Admin UI** | Full product management | Create, edit, publish, variant management |
| **Checkout UX** | Hybrid accordion | Single page with collapsible sections |
| **Payment Integration** | CheckoutSession pattern | Orchestrates cart → order → payment |
| **Currency** | VND only | Single currency for Phase 8 |

---

## Functional Requirements

### FR-1: Product Catalog

#### FR-1.1: Product Management (Admin)
- [ ] Create product with name, slug, description (HTML), base price
- [ ] Set product status: Draft → Active → Archived
- [ ] Assign product to category (single category)
- [ ] Upload multiple product images with sort order
- [ ] Set SEO metadata (meta title, meta description)
- [ ] Configure inventory tracking (on/off per product)

#### FR-1.2: Product Variants
- [ ] Add variants with flexible attributes (JSON storage)
- [ ] Each variant has: name, SKU, price, stock quantity
- [ ] Support for compare-at price (sale display)
- [ ] Concurrency-safe stock updates (`[ConcurrencyCheck]`)

#### FR-1.3: Product Categories
- [ ] Hierarchical categories (parent/child)
- [ ] Category name, slug, image, sort order
- [ ] Products belong to single category

#### FR-1.4: Product Images
- [ ] Multiple images per product
- [ ] Image URL, alt text, sort order
- [ ] First image = primary/thumbnail

### FR-2: Product Listing (Storefront)

#### FR-2.1: Category Browsing
- [ ] Display products by category
- [ ] Category hierarchy navigation (breadcrumbs)
- [ ] Product count per category

#### FR-2.2: Faceted Navigation
- [ ] Filter by category
- [ ] Filter by price range (slider or predefined ranges)
- [ ] Filter by attributes (extracted from variant JSON)
- [ ] Filter by stock availability (in-stock only toggle)
- [ ] Show filter counts (e.g., "Red (12)")
- [ ] Multiple filters simultaneously (AND logic within facet, OR between)
- [ ] AJAX-based filtering (no page reload)
- [ ] Clear all filters / remove individual filters

#### FR-2.3: Sorting
- [ ] Sort by: Relevance (default), Price Low→High, Price High→Low
- [ ] Sort by: Newest, Best Sellers (requires sales tracking)

#### FR-2.4: Product Grid
- [ ] Product card: image, name, price, rating placeholder, stock badge
- [ ] Infinite scroll or "Load More" pagination
- [ ] Responsive grid (4 cols desktop, 2 cols mobile)

### FR-3: Product Detail Page (Storefront)

#### FR-3.1: Product Information
- [ ] Product images gallery (zoom, multiple images)
- [ ] Product name, price, compare-at price
- [ ] Description (HTML rendered)
- [ ] Stock status: In Stock / Low Stock (≤5) / Out of Stock

#### FR-3.2: Variant Selection
- [ ] Display variant options (from JSON attributes)
- [ ] Visual swatches for colors
- [ ] Size/option dropdowns or buttons
- [ ] Price updates on variant selection
- [ ] Stock check per variant

#### FR-3.3: Add to Cart
- [ ] Quantity selector (+/-)
- [ ] "Add to Cart" button
- [ ] Disable if out of stock
- [ ] Show mini-cart confirmation on add

### FR-4: Shopping Cart

#### FR-4.1: Mini-Cart
- [ ] Slide-out panel or dropdown
- [ ] Show last 3-5 added items
- [ ] Item count badge on cart icon
- [ ] Subtotal display
- [ ] "View Cart" and "Checkout" CTAs

#### FR-4.2: Cart Page
- [ ] Full item list with images, names, variants
- [ ] Quantity adjustment (+/- or input)
- [ ] Remove item ("Remove" link, not "Delete")
- [ ] Line totals and subtotal
- [ ] "Continue Shopping" link
- [ ] Prominent "Proceed to Checkout" CTA

#### FR-4.3: Cart Persistence
- [ ] **Guest users**: localStorage (30 days) + sync to DB
- [ ] **Authenticated users**: Database-backed
- [ ] **Cart merge**: On login, merge guest cart with user cart
- [ ] **Session ID**: Generate for guests, store in cookie

#### FR-4.4: Cart Operations
- [ ] Add item (upsert if same product+variant)
- [ ] Update quantity
- [ ] Remove item
- [ ] Clear cart
- [ ] Mark as abandoned (after 30 minutes inactivity)

### FR-5: Checkout

#### FR-5.1: CheckoutSession Entity
- [ ] Create when user clicks "Checkout"
- [ ] Links: CartId, CustomerId (nullable), PaymentTransactionId
- [ ] Status: Started → AddressComplete → ShippingSelected → PaymentPending → Completed
- [ ] 15-minute expiration with inventory reservation

#### FR-5.2: Checkout Flow (Hybrid Accordion)
- [ ] **Step 1: Contact Information**
  - Email (required)
  - Phone (required, with explanation)
  - Guest checkout by default
  - Optional: "Create account" checkbox

- [ ] **Step 2: Shipping Address**
  - Full name
  - Address Line 1 (required), Line 2 (optional)
  - Ward, District, Province (Vietnam-specific)
  - Country (default: Vietnam)
  - "Save as default address" for logged-in users

- [ ] **Step 3: Shipping Method**
  - Radio button list of available methods
  - Show price and estimated delivery time
  - (Shipping providers integration in Phase 10)
  - For Phase 8: Fixed shipping options seeded in DB

- [ ] **Step 4: Payment**
  - Integration with existing PaymentGateway (Phase 5-7)
  - Show available payment methods from tenant config
  - Payment form or redirect based on gateway

- [ ] **Step 5: Order Review**
  - Summary of all items
  - Shipping address display
  - Order totals (subtotal, shipping, tax placeholder, total)
  - "Place Order" button

#### FR-5.3: Checkout Progress
- [ ] Visual step indicator (1-2-3-4)
- [ ] Completed steps show checkmark
- [ ] Click to edit previous step (accordion expand)

#### FR-5.4: Guest Checkout
- [ ] No account required to complete order
- [ ] Post-order: "Create account to track orders" prompt
- [ ] Pre-fill registration form with order info

### FR-6: Inventory Management

#### FR-6.1: Stock Tracking
- [ ] Per-variant stock quantity
- [ ] Stock movements: StockIn, StockOut, Adjustment, Return, Reservation, Release, Damaged
- [ ] Movement history with reason and reference

#### FR-6.2: Inventory Reservation
- [ ] Create temporary reservation when checkout starts
- [ ] 15-minute expiration
- [ ] Confirm reservation when order placed
- [ ] Release reservation if checkout abandoned or expired
- [ ] Background job: `ExpiredReservationCleanupJob` (run every minute)

#### FR-6.3: Low Stock Alerts
- [ ] Configure low stock threshold per product/variant
- [ ] Display "Only X left" on PDP when ≤ threshold
- [ ] Admin notification when stock falls below threshold

### FR-7: Orders

#### FR-7.1: Order Creation
- [ ] Generate order number (tenant prefix + sequence)
- [ ] Capture: items, quantities, prices at order time
- [ ] Store shipping and billing addresses
- [ ] Link to payment transaction
- [ ] Send order confirmation email

#### FR-7.2: Order Status Flow
```
Pending → Confirmed → Processing → Shipped → Delivered → Completed
                ↘ Cancelled    ↘ Cancelled     ↘ Refunded
```

#### FR-7.3: Order Management (Admin)
- [ ] List orders with filters (status, date range, customer)
- [ ] View order details
- [ ] Update order status
- [ ] Add internal notes
- [ ] Process refund (links to payment refund)

#### FR-7.4: Customer Order History
- [ ] "My Orders" page for logged-in users
- [ ] Order list with status, date, total
- [ ] Order detail with items and tracking

### FR-8: Domain Events

#### FR-8.1: Product Events
- [ ] `ProductCreatedEvent`
- [ ] `ProductPublishedEvent`
- [ ] `ProductArchivedEvent`
- [ ] `ProductStockChangedEvent`

#### FR-8.2: Cart Events
- [ ] `CartCreatedEvent`
- [ ] `CartItemAddedEvent`
- [ ] `CartAbandonedEvent`
- [ ] `CartConvertedEvent`

#### FR-8.3: Order Events
- [ ] `OrderCreatedEvent`
- [ ] `OrderConfirmedEvent`
- [ ] `OrderShippedEvent`
- [ ] `OrderDeliveredEvent`
- [ ] `OrderCancelledEvent`
- [ ] `OrderRefundedEvent`

---

## Non-Functional Requirements

### NFR-1: Performance
- [ ] Product listing page: < 200ms response (with caching)
- [ ] Add to cart: < 100ms response
- [ ] Checkout page load: < 300ms
- [ ] Product search: < 500ms

### NFR-2: Scalability
- [ ] Support 10,000+ products per tenant
- [ ] Handle 100 concurrent checkout sessions
- [ ] Cart persistence for 1M+ guest sessions

### NFR-3: Security
- [ ] Price validation on server (don't trust client)
- [ ] Stock validation before order confirmation
- [ ] XSS prevention in product descriptions
- [ ] CSRF protection on all mutations

### NFR-4: Caching Strategy
| Data | Cache Duration | Invalidation |
|------|---------------|--------------|
| Product catalog | 15 minutes | Product update |
| Facet counts | 5 minutes | Stock change |
| Cart | No cache | - |
| Category tree | 1 hour | Category change |

### NFR-5: Multi-tenancy
- [ ] All entities are tenant-scoped
- [ ] Unique constraints include TenantId
- [ ] Product slugs unique per tenant

---

## User Stories

### Epic: Product Management
```
US-1: As an admin, I can create a product with variants so that I can list items for sale
US-2: As an admin, I can upload multiple product images so customers can see the product
US-3: As an admin, I can publish/unpublish products so I control what's visible
US-4: As an admin, I can manage product categories so products are organized
US-5: As an admin, I can adjust inventory so stock levels are accurate
```

### Epic: Storefront Browsing
```
US-6: As a customer, I can browse products by category so I can find what I need
US-7: As a customer, I can filter products by price, color, size so I can narrow results
US-8: As a customer, I can sort products by price or newest so I see relevant items first
US-9: As a customer, I can view product details and images so I can make a decision
US-10: As a customer, I can select product options (size, color) so I get the right variant
```

### Epic: Shopping Cart
```
US-11: As a customer, I can add items to cart so I can purchase multiple items
US-12: As a customer, I can view my cart and adjust quantities so I control my order
US-13: As a customer, I can remove items from cart so I can change my mind
US-14: As a guest, my cart persists across sessions so I don't lose my selections
US-15: As a returning user, my guest cart merges with my account cart on login
```

### Epic: Checkout
```
US-16: As a customer, I can checkout as guest so I don't need to create an account
US-17: As a customer, I can enter my shipping address so my order is delivered
US-18: As a customer, I can select a shipping method so I know when to expect delivery
US-19: As a customer, I can pay using available payment methods so I complete my purchase
US-20: As a customer, I can review my order before placing so I confirm everything is correct
US-21: As a customer, I receive an order confirmation email so I have a record
```

### Epic: Order Management
```
US-22: As a customer, I can view my order history so I can track purchases
US-23: As an admin, I can view and filter orders so I can manage fulfillment
US-24: As an admin, I can update order status so customers know their order progress
US-25: As an admin, I can process refunds so I can handle returns
```

---

## Acceptance Criteria Highlights

### AC: Faceted Navigation
```gherkin
Given I am on the product listing page
When I select "Red" in the color filter
And I select "Large" in the size filter
Then I see only products that have Red AND Large variants
And the product count updates
And the URL updates with filter parameters
And I can remove individual filters
```

### AC: Inventory Reservation
```gherkin
Given a product has 5 units in stock
When Customer A starts checkout with 3 units
Then a 15-minute reservation is created
And available stock shows as 2
When Customer A completes purchase
Then reservation is confirmed
And stock is permanently decremented
When Customer B's checkout expires
Then reservation is released
And stock is restored
```

### AC: Cart Merge on Login
```gherkin
Given I have 2 items in my guest cart
And I have 1 item in my account cart
When I log in
Then my cart contains 3 items
And the guest cart is cleared
And I see a notification about merged items
```

---

## Open Questions (Resolved)

| Question | Resolution |
|----------|------------|
| Coupon system scope | Deferred to Phase 11 |
| Product reviews | Deferred to Phase 9 |
| Multi-currency | VND only for Phase 8 |
| Guest checkout | Required, DB + localStorage |
| Inventory reservation | 15-minute soft hold |

---

## Dependencies

### From Previous Phases
- [x] Phase 5: Payment Foundation (PaymentTransaction, PaymentGateway)
- [x] Phase 6: Vietnam Gateways (VnPay, MoMo, ZaloPay)
- [x] Phase 7: International (Stripe, PayOS, ICurrencyService)

### For Future Phases
- Phase 9: Customer entity (currently uses CustomerId reference)
- Phase 10: Shipping providers (currently fixed shipping options)
- Phase 11: Coupon entity (removed from Phase 8)

---

## Implementation Sprints (Recommended)

### Sprint 1: Foundation (Week 1-2)
- Enums (ProductStatus, CartStatus, OrderStatus, InventoryMovementType)
- Address value object
- ProductCategory entity + repository + admin UI
- Product entity + repository + specifications

### Sprint 2: Products (Week 2-3)
- ProductVariant entity with flexible attributes
- ProductImage entity
- Product admin UI (CRUD, publish, images, variants)
- Product listing API with faceted filters

### Sprint 3: Cart (Week 3-4)
- Cart + CartItem entities
- Guest cart persistence (localStorage + DB sync)
- Cart merge on login
- Mini-cart and cart page UI

### Sprint 4: Checkout (Week 4-5)
- CheckoutSession entity
- InventoryReservation entity + background job
- Hybrid accordion checkout UI
- Payment integration (existing gateways)

### Sprint 5: Orders (Week 5-6)
- Order + OrderItem entities
- Order creation from checkout
- Order confirmation email
- Order management admin UI
- Customer order history

### Sprint 6: Polish (Week 6-7)
- Faceted navigation optimization
- Low stock alerts
- Abandoned cart detection
- Performance tuning
- Integration tests

---

## Next Steps

1. **Design**: `/sc:design` - Create detailed architecture and API contracts
2. **Workflow**: `/sc:workflow` - Generate implementation task breakdown
3. **Implement**: `/sc:implement` - Start coding with Sprint 1

---

*Document generated from requirements discovery session. All decisions validated with user.*
