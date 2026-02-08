<div align="center">

# NOIR E2E Test Plan

**Comprehensive End-to-End Testing Strategy**

| Version | Date | Author | Status |
|---------|------|--------|--------|
| 1.0 | 2026-02-05 | QA Team | Active |

</div>

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [Scope & Objectives](#2-scope--objectives)
3. [Test Environment](#3-test-environment)
4. [Test Architecture](#4-test-architecture)
5. [Test Categories & Priorities](#5-test-categories--priorities)
6. [Feature Test Matrices](#6-feature-test-matrices)
7. [Test Data Strategy](#7-test-data-strategy)
8. [Execution Strategy](#8-execution-strategy)
9. [Reporting & Metrics](#9-reporting--metrics)
10. [Risk Assessment](#10-risk-assessment)
11. [Implementation Roadmap](#11-implementation-roadmap)

---

## 1. Executive Summary

### 1.1 Purpose

This document defines the comprehensive end-to-end (E2E) testing strategy for the NOIR platform, a multi-tenant e-commerce and content management system. The goal is to achieve **100% verification of all critical user flows** through automated browser testing.

### 1.2 Current Test Coverage

| Test Type | Count | Coverage |
|-----------|-------|----------|
| Unit Tests | 6,073+ | Domain (842) + Application (5,231) |
| Integration Tests | 654 | API endpoints |
| Architecture Tests | 25 | Architectural rules |
| **E2E Tests** | **490+** | **32 spec files, 31 page objects (Chromium + Firefox)** |

### 1.3 Target Coverage

| Metric | Target |
|--------|--------|
| P0 (Critical) Tests | 100% passing |
| P1 (High) Tests | 95%+ passing |
| P2 (Medium) Tests | 90%+ passing |
| Flaky Test Rate | < 5% |
| Total Execution Time | < 60 minutes (full suite) |

### 1.4 Test Suite Summary (Actual - 2026-02-08)

| Suite | Spec Files | Test Scenarios | Browsers |
|-------|-----------|----------------|----------|
| Main Tests | 27 | ~460 | Chromium, Firefox |
| Smoke Tests | 5 | ~30 | Chromium |
| **Total** | **32** | **~490** | **Chromium + Firefox** |

**Page Objects:** 31 dedicated POM files in `e2e-tests/pages/`

> **Note:** Tests cover authentication, e-commerce (products, categories, brands, attributes), admin features (users, roles, tenants, settings), content management (blog, legal pages), and system features (notifications, command palette, developer logs).

---

## 2. Scope & Objectives

### 2.1 In Scope

#### E-Commerce Features
- Product Management (CRUD, variants, images, attributes)
- Product Categories (hierarchical CRUD)
- Brands Management
- Shopping Cart (add, update, remove, merge)
- Checkout Flow (address, shipping, payment, complete)
- Order Management (lifecycle, tracking)
- Inventory Management

#### Admin Features
- User Management (CRUD, roles, lock/unlock)
- Role Management (CRUD, permissions)
- Permission Assignment
- Tenant Management (multi-tenant operations)
- Tenant Settings (branding, email, payment, regional)
- Platform Settings

#### Content Management
- Blog Posts (CRUD, publish/unpublish)
- Blog Categories & Tags
- Legal Pages (terms, privacy)
- Email Templates

#### System Features
- Authentication (login, logout, password reset, OTP)
- Session Management
- Activity Timeline (audit logs)
- Developer Logs
- Notifications

### 2.2 Out of Scope

- Performance/Load Testing (separate initiative)
- Security Penetration Testing (separate initiative)
- Mobile Native Apps (web-only)
- Third-party Payment Gateway Integration Testing (mocked)
- Email Delivery Verification (mocked)

### 2.3 Objectives

1. **Verify all CRUD operations** for each feature module
2. **Validate business workflows** end-to-end (e.g., complete checkout)
3. **Ensure cross-browser compatibility** (Chrome, Firefox, Safari)
4. **Catch regressions early** through CI/CD integration
5. **Provide visual evidence** of test results (screenshots, videos)

---

## 3. Test Environment

### 3.1 Browser Matrix

| Browser | Engine | Desktop | Mobile |
|---------|--------|---------|--------|
| Chrome | Chromium | Yes | Android (Pixel 5) |
| Firefox | Gecko | Yes | No |
| Safari | WebKit | Yes | iPhone 12 |
| Edge | Chromium | Optional | No |

### 3.2 Screen Resolutions

| Device Type | Resolution | Viewport |
|-------------|------------|----------|
| Desktop Large | 1920x1080 | 1920x969 |
| Desktop Medium | 1440x900 | 1440x789 |
| Tablet | 768x1024 | 768x1024 |
| Mobile | 375x667 | 375x667 |

### 3.3 Environment URLs

| Environment | Frontend | Backend API | Purpose |
|-------------|----------|-------------|---------|
| Local | http://localhost:3000 | http://localhost:4000 | Development |
| CI | http://localhost:3000 | http://localhost:4000 | Automated tests |
| Staging | https://staging.noir.app | https://api.staging.noir.app | Pre-production |

### 3.4 Test Accounts

| Role | Email | Password | Permissions |
|------|-------|----------|-------------|
| Platform Admin | platform@noir.local | 123qwe | Full system access |
| Tenant Admin | admin@noir.local | 123qwe | Tenant-scoped admin |
| Regular User | user@noir.local | 123qwe | Limited access |
| Guest | N/A | N/A | Public pages only |

---

## 4. Test Architecture

### 4.1 Technology Stack

| Component | Technology | Purpose |
|-----------|------------|---------|
| Framework | Playwright | Cross-browser automation |
| Language | TypeScript | Type-safe test code |
| Pattern | Page Object Model | Maintainable selectors |
| Runner | Playwright Test | Parallel execution |
| CI/CD | GitHub Actions | Automated pipeline |
| Reporting | HTML Reporter | Visual results |

### 4.2 Project Structure

```
e2e-tests/
├── playwright.config.ts          # Configuration
├── package.json                  # Dependencies
├── tsconfig.json                 # TypeScript config
│
├── fixtures/                     # Test fixtures
│   ├── auth.fixture.ts          # Authentication state
│   ├── test-data.fixture.ts     # Data generation
│   └── base.fixture.ts          # Extended fixture
│
├── pages/                        # Page Object Model
│   ├── base.page.ts             # Common methods
│   ├── login.page.ts
│   ├── dashboard.page.ts
│   ├── products/
│   │   ├── products-list.page.ts
│   │   ├── product-form.page.ts
│   │   └── product-detail.page.ts
│   ├── users/
│   │   ├── users-list.page.ts
│   │   └── user-dialog.page.ts
│   ├── roles/
│   │   └── roles.page.ts
│   ├── tenants/
│   │   ├── tenants-list.page.ts
│   │   └── tenant-detail.page.ts
│   ├── cart/
│   │   └── cart.page.ts
│   ├── checkout/
│   │   └── checkout.page.ts
│   └── orders/
│       └── orders.page.ts
│
├── tests/                        # Test specifications
│   ├── smoke/                   # P0 Critical paths
│   │   └── critical-paths.spec.ts
│   ├── auth/
│   │   ├── login.spec.ts
│   │   └── password-reset.spec.ts
│   ├── products/
│   │   ├── product-list.spec.ts
│   │   ├── product-crud.spec.ts
│   │   └── product-bulk.spec.ts
│   ├── categories/
│   │   └── product-categories.spec.ts
│   ├── users/
│   │   └── user-management.spec.ts
│   ├── roles/
│   │   └── role-management.spec.ts
│   ├── tenants/
│   │   └── tenant-management.spec.ts
│   ├── ecommerce/
│   │   ├── cart.spec.ts
│   │   ├── checkout.spec.ts
│   │   └── orders.spec.ts
│   ├── blog/
│   │   └── blog-posts.spec.ts
│   └── settings/
│       └── tenant-settings.spec.ts
│
├── utils/                       # Utilities
│   ├── api-helpers.ts          # API calls for setup
│   ├── test-data.ts            # Data generators
│   └── assertions.ts           # Custom assertions
│
└── playwright/.auth/            # Stored auth states
    ├── admin.json
    └── platform-admin.json
```

### 4.3 Test Isolation Strategy

| Aspect | Strategy |
|--------|----------|
| Database | Seeded test data, transaction rollback after tests |
| Authentication | Stored browser state, reused across tests |
| Test Data | Unique identifiers per test (UUID suffix) |
| Parallel | Worker isolation, no shared mutable state |

---

## 5. Test Categories & Priorities

### 5.1 Priority Definitions

| Priority | Definition | SLA | Blocking |
|----------|------------|-----|----------|
| **P0** | Critical Path - Core functionality | Must pass 100% | Deployment blocker |
| **P1** | High - Main feature operations | Should pass 95%+ | Review required |
| **P2** | Medium - Edge cases, validation | Should pass 90%+ | Non-blocking |
| **P3** | Low - Visual polish, UX | Nice to have | Non-blocking |

### 5.2 Test Categories

| Category | Purpose | Trigger | Runtime |
|----------|---------|---------|---------|
| **Smoke** | Verify deployment works | Every deploy | < 5 min |
| **Regression** | Verify existing functionality | Every PR | < 30 min |
| **Full Suite** | Complete coverage | Nightly | < 60 min |

### 5.3 Priority Distribution

#### Quick Reference Tests

| Module | P0 | P1 | P2 | P3 | Total |
|--------|----|----|----|----|-------|
| Authentication | 6 | 10 | 4 | 0 | 20 |
| Products (Quick) | 6 | 20 | 10 | 0 | 36 |
| Users | 4 | 8 | 2 | 0 | 14 |
| Roles | 3 | 4 | 1 | 0 | 8 |
| Tenants | 3 | 4 | 1 | 0 | 8 |
| Cart | 4 | 4 | 2 | 0 | 10 |
| Checkout | 5 | 3 | 2 | 0 | 10 |
| Orders | 4 | 6 | 0 | 0 | 10 |
| Blog | 2 | 10 | 3 | 0 | 15 |
| Settings | 1 | 7 | 2 | 0 | 10 |
| Activity | 0 | 5 | 0 | 0 | 5 |
| Smoke | 10 | 0 | 0 | 0 | 10 |
| **Quick Total** | **48** | **81** | **27** | **0** | **156** |

#### Comprehensive Product E2E Suite

> See [PRODUCT_E2E_TESTS.md](./PRODUCT_E2E_TESTS.md) for complete 247 test cases

| Module | P0 | P1 | P2 | P3 | Total |
|--------|----|----|----|----|-------|
| Categories | 5 | 18 | 7 | 0 | 30 |
| Attributes | 6 | 32 | 7 | 0 | 45 |
| Brands | 2 | 8 | 2 | 0 | 12 |
| Product CRUD | 12 | 30 | 6 | 0 | 48 |
| Variants | 5 | 18 | 5 | 0 | 28 |
| Images | 4 | 14 | 4 | 0 | 22 |
| Options | 2 | 10 | 2 | 0 | 14 |
| Filters & Search | 4 | 14 | 2 | 0 | 20 |
| Bulk Operations | 4 | 10 | 2 | 0 | 16 |
| Import/Export | 3 | 13 | 2 | 0 | 18 |
| Integration | 5 | 9 | 0 | 0 | 14 |
| **Product Total** | **52** | **158** | **35** | **2** | **247** |

#### Grand Total

| Suite | P0 | P1 | P2 | P3 | Total |
|-------|----|----|----|----|-------|
| Quick Reference | 48 | 81 | 27 | 0 | 156 |
| Product E2E | 52 | 158 | 35 | 2 | 247 |
| **Grand Total** | **100** | **239** | **62** | **2** | **403** |

---

## 6. Feature Test Matrices

### 6.1 Authentication Module (16 Tests)

#### AUTH-001: Login Flow

| ID | Test Case | Priority | Precondition | Steps | Expected Result |
|----|-----------|----------|--------------|-------|-----------------|
| AUTH-001-01 | Valid login as Platform Admin | P0 | User exists | 1. Navigate to /login<br>2. Enter platform@noir.local / 123qwe<br>3. Click Login | Dashboard loads, user menu shows email |
| AUTH-001-02 | Valid login as Tenant Admin | P0 | User exists | 1. Navigate to /login<br>2. Enter admin@noir.local / 123qwe<br>3. Click Login | Dashboard loads, tenant context set |
| AUTH-001-03 | Invalid email format | P1 | None | 1. Enter "invalid-email"<br>2. Tab to next field | Validation error: "Invalid email format" |
| AUTH-001-04 | Wrong password | P1 | User exists | 1. Enter valid email<br>2. Enter wrong password<br>3. Click Login | Error: "Invalid credentials" |
| AUTH-001-05 | Empty email field | P1 | None | 1. Leave email empty<br>2. Click Login | Validation: "Email is required" |
| AUTH-001-06 | Empty password field | P1 | None | 1. Enter email<br>2. Leave password empty<br>3. Click Login | Validation: "Password is required" |
| AUTH-001-07 | Locked account | P2 | User is locked | 1. Login with locked user | Error: "Account is locked" |
| AUTH-001-08 | Remember me functionality | P2 | None | 1. Check Remember me<br>2. Login<br>3. Close browser<br>4. Reopen | Session persists |

#### AUTH-002: Password Reset

| ID | Test Case | Priority | Precondition | Steps | Expected Result |
|----|-----------|----------|--------------|-------|-----------------|
| AUTH-002-01 | Request OTP - valid email | P0 | User exists | 1. Click "Forgot Password"<br>2. Enter email<br>3. Submit | OTP sent message, redirect to verify |
| AUTH-002-02 | Verify OTP - correct code | P0 | OTP sent | 1. Enter 6-digit OTP<br>2. Submit | Proceed to password reset form |
| AUTH-002-03 | Verify OTP - wrong code | P1 | OTP sent | 1. Enter wrong OTP<br>2. Submit | Error: "Invalid OTP" |
| AUTH-002-04 | Verify OTP - expired | P1 | OTP expired (>10 min) | 1. Enter OTP after expiry | Error: "OTP expired" |
| AUTH-002-05 | Reset password - valid | P0 | OTP verified | 1. Enter new password<br>2. Confirm password<br>3. Submit | Success, redirect to login |
| AUTH-002-06 | Reset password - mismatch | P1 | OTP verified | 1. Enter password<br>2. Enter different confirm | Error: "Passwords don't match" |
| AUTH-002-07 | Reset password - weak | P1 | OTP verified | 1. Enter "123" | Error: "Password too weak" |
| AUTH-002-08 | Complete flow E2E | P0 | User exists | Full flow: forgot → OTP → reset → login | Can login with new password |

---

### 6.2 Product Management Module (24 Tests)

#### PROD-001: Product List View

| ID | Test Case | Priority | Precondition | Steps | Expected Result |
|----|-----------|----------|--------------|-------|-----------------|
| PROD-001-01 | View products page | P0 | Logged as Admin | Navigate to /portal/ecommerce/products | Products table displayed |
| PROD-001-02 | Search by name | P0 | Products exist | 1. Type product name in search<br>2. Wait for results | Filtered products shown |
| PROD-001-03 | Filter by status - Active | P1 | Mixed status products | Select "Active" filter | Only active products shown |
| PROD-001-04 | Filter by status - Draft | P1 | Draft products exist | Select "Draft" filter | Only draft products shown |
| PROD-001-05 | Filter by category | P1 | Products in categories | Select category filter | Products in category shown |
| PROD-001-06 | Pagination - next page | P1 | >10 products | Click "Next" | Page 2 loads correctly |
| PROD-001-07 | Sort by name | P1 | Multiple products | Click "Name" column header | Sorted alphabetically |
| PROD-001-08 | Empty state | P2 | No products | Clear filters, no results | "No products found" message |

#### PROD-002: Create Product

| ID | Test Case | Priority | Precondition | Steps | Expected Result |
|----|-----------|----------|--------------|-------|-----------------|
| PROD-002-01 | Create simple product | P0 | Logged as Admin | 1. Click "Create Product"<br>2. Fill name, SKU, price<br>3. Save | Product created, redirect to list |
| PROD-002-02 | Create with description | P1 | Form open | 1. Fill all fields including description<br>2. Save | Description saved in rich text |
| PROD-002-03 | Create with variants | P1 | Form open | 1. Add Size option (S, M, L)<br>2. Generate variants<br>3. Save | 3 variants created |
| PROD-002-04 | Upload single image | P1 | Form open | 1. Click image upload<br>2. Select image<br>3. Wait for upload | Image thumbnail shown |
| PROD-002-05 | Upload multiple images | P1 | Form open | 1. Drag multiple images<br>2. Wait for upload | All images uploaded |
| PROD-002-06 | Set primary image | P2 | Images uploaded | Click "Set as Primary" on image 2 | Image 2 marked as primary |
| PROD-002-07 | Validation - empty name | P1 | Form open | Submit with empty name | "Name is required" error |
| PROD-002-08 | Validation - empty SKU | P1 | Form open | Submit with empty SKU | "SKU is required" error |
| PROD-002-09 | Validation - duplicate SKU | P2 | Product with SKU exists | Enter existing SKU | "SKU already exists" error |
| PROD-002-10 | Assign attributes | P1 | Category has attributes | 1. Select category<br>2. Fill attribute values<br>3. Save | Attributes saved |

#### PROD-003: Update Product

| ID | Test Case | Priority | Precondition | Steps | Expected Result |
|----|-----------|----------|--------------|-------|-----------------|
| PROD-003-01 | Update basic info | P0 | Product exists | 1. Open product<br>2. Change name<br>3. Save | Name updated |
| PROD-003-02 | Update pricing | P0 | Product exists | 1. Change price<br>2. Add compare price<br>3. Save | Prices updated |
| PROD-003-03 | Update inventory | P1 | Product exists | 1. Change stock quantity<br>2. Save | Stock updated |
| PROD-003-04 | Add variant | P1 | Product exists | 1. Add new option value<br>2. Save | New variant created |
| PROD-003-05 | Delete variant | P2 | Product has variants | 1. Delete variant<br>2. Confirm<br>3. Save | Variant removed |
| PROD-003-06 | Reorder images | P2 | Product has images | 1. Drag image to new position<br>2. Save | Order persisted |

---

### 6.3 User Management Module (12 Tests)

#### USER-001: User List

| ID | Test Case | Priority | Steps | Expected Result |
|----|-----------|----------|-------|-----------------|
| USER-001-01 | View users page | P0 | Navigate to /portal/admin/users | Users table displayed |
| USER-001-02 | Search by name/email | P1 | Enter search term | Filtered results |
| USER-001-03 | Filter by role | P1 | Select role filter | Users with role shown |
| USER-001-04 | Filter by status | P1 | Select Active/Locked | Correct users shown |

#### USER-002: Create User

| ID | Test Case | Priority | Steps | Expected Result |
|----|-----------|----------|-------|-----------------|
| USER-002-01 | Create user with roles | P0 | 1. Click Create<br>2. Fill form with roles<br>3. Submit | User created with roles |
| USER-002-02 | Validation - duplicate email | P1 | Enter existing email | "Email exists" error |
| USER-002-03 | Validation - invalid email | P1 | Enter invalid format | Validation error |

#### USER-003: Update User

| ID | Test Case | Priority | Steps | Expected Result |
|----|-----------|----------|-------|-----------------|
| USER-003-01 | Update user details | P0 | 1. Edit user<br>2. Change name<br>3. Save | Changes saved |
| USER-003-02 | Assign roles | P0 | 1. Open assign roles<br>2. Select roles<br>3. Save | Roles updated |
| USER-003-03 | Lock user | P1 | Click Lock button | User status = Locked |
| USER-003-04 | Unlock user | P1 | Click Unlock button | User status = Active |

#### USER-004: Delete User

| ID | Test Case | Priority | Steps | Expected Result |
|----|-----------|----------|-------|-----------------|
| USER-004-01 | Delete user | P1 | 1. Click Delete<br>2. Confirm | User soft-deleted |

---

### 6.4 Role Management Module (8 Tests)

| ID | Test Case | Priority | Steps | Expected Result |
|----|-----------|----------|-------|-----------------|
| ROLE-001-01 | View roles page | P0 | Navigate to /portal/admin/roles | Roles list shown |
| ROLE-001-02 | Create role | P0 | 1. Click Create<br>2. Enter name, description<br>3. Save | Role created |
| ROLE-001-03 | Assign permissions | P0 | 1. Select permissions<br>2. Save | Permissions saved |
| ROLE-001-04 | Update role | P1 | 1. Edit role<br>2. Change name<br>3. Save | Changes saved |
| ROLE-001-05 | Remove permissions | P1 | 1. Uncheck permissions<br>2. Save | Permissions removed |
| ROLE-001-06 | Delete role | P1 | 1. Delete non-system role<br>2. Confirm | Role deleted |
| ROLE-001-07 | Cannot delete system role | P2 | Try to delete Admin role | Error: Cannot delete |
| ROLE-001-08 | View role users | P1 | Click role to view details | Users with role shown |

---

### 6.5 Tenant Management Module (8 Tests)

| ID | Test Case | Priority | Steps | Expected Result |
|----|-----------|----------|-------|-----------------|
| TENANT-001-01 | View tenants (Platform Admin) | P0 | Navigate to /portal/admin/tenants | Tenants list shown |
| TENANT-001-02 | Create tenant | P0 | 1. Click Create<br>2. Fill form<br>3. Submit | Tenant created with admin |
| TENANT-001-03 | View tenant details | P1 | Click tenant row | Details page shown |
| TENANT-001-04 | Update tenant | P1 | 1. Edit tenant<br>2. Change settings<br>3. Save | Changes saved |
| TENANT-001-05 | Deactivate tenant | P1 | Click Deactivate | Tenant status = Inactive |
| TENANT-001-06 | Activate tenant | P1 | Click Activate | Tenant status = Active |
| TENANT-001-07 | Reset admin password | P2 | 1. Click Reset Password<br>2. Enter new<br>3. Submit | Password reset |
| TENANT-001-08 | Delete tenant | P1 | 1. Click Delete<br>2. Confirm | Tenant soft-deleted |

---

### 6.6 Shopping Cart Module (10 Tests)

| ID | Test Case | Priority | Steps | Expected Result |
|----|-----------|----------|-------|-----------------|
| CART-001-01 | Add item to cart | P0 | Click "Add to Cart" on product | Item added, cart count updates |
| CART-001-02 | View cart | P0 | Click cart icon | Cart drawer/page opens |
| CART-001-03 | Increase quantity | P1 | Click + button | Quantity increases |
| CART-001-04 | Decrease quantity | P1 | Click - button | Quantity decreases |
| CART-001-05 | Remove item | P1 | Click remove button | Item removed from cart |
| CART-001-06 | Clear cart | P2 | Click Clear Cart | All items removed |
| CART-001-07 | Cart persistence | P1 | 1. Add item<br>2. Refresh page | Cart still has item |
| CART-001-08 | Cart total calculation | P0 | Add multiple items | Total correctly calculated |
| CART-001-09 | Cart merge on login | P2 | 1. Add as guest<br>2. Login | Carts merged |
| CART-001-10 | Maximum quantity | P2 | Exceed stock limit | Error: "Insufficient stock" |

---

### 6.7 Checkout Module (10 Tests)

| ID | Test Case | Priority | Steps | Expected Result |
|----|-----------|----------|-------|-----------------|
| CHECKOUT-001-01 | Initiate checkout | P0 | Click "Checkout" button | Checkout page loads |
| CHECKOUT-001-02 | Fill shipping address | P0 | Fill all address fields | Address validated, saved |
| CHECKOUT-001-03 | Select shipping method | P0 | Choose shipping option | Method selected, total updated |
| CHECKOUT-001-04 | Select payment method | P0 | Choose payment option | Method selected |
| CHECKOUT-001-05 | Complete checkout | P0 | Click "Complete Order" | Order created, confirmation shown |
| CHECKOUT-001-06 | Address validation | P1 | Submit incomplete address | Validation errors shown |
| CHECKOUT-001-07 | Back navigation | P2 | Navigate between steps | State preserved |
| CHECKOUT-001-08 | Edit address mid-checkout | P1 | Go back and edit address | Changes reflected in total |
| CHECKOUT-001-09 | Empty cart checkout | P2 | Try checkout with empty cart | Error: "Cart is empty" |
| CHECKOUT-001-10 | Session timeout | P2 | Wait > 30 minutes | Session expired message |

---

### 6.8 Order Management Module (10 Tests)

| ID | Test Case | Priority | Steps | Expected Result |
|----|-----------|----------|-------|-----------------|
| ORDER-001-01 | View orders list | P0 | Navigate to orders page | Orders listed |
| ORDER-001-02 | View order details | P0 | Click order row | Order details shown |
| ORDER-001-03 | Filter by status | P1 | Select status filter | Filtered results |
| ORDER-001-04 | Search by order number | P1 | Enter order number | Order found |
| ORDER-001-05 | Confirm order (Admin) | P1 | Click Confirm | Status = Confirmed |
| ORDER-001-06 | Ship order (Admin) | P1 | 1. Click Ship<br>2. Add tracking | Status = Shipped |
| ORDER-001-07 | Complete order | P1 | Mark as delivered | Status = Completed |
| ORDER-001-08 | Cancel order | P1 | 1. Click Cancel<br>2. Confirm | Status = Cancelled |
| ORDER-001-09 | Inventory release on cancel | P0 | Cancel order with items | Stock restored |
| ORDER-001-10 | Order timeline | P2 | View order history | All status changes shown |

---

### 6.9 Blog Management Module (15 Tests)

| ID | Test Case | Priority | Steps | Expected Result |
|----|-----------|----------|-------|-----------------|
| BLOG-001-01 | View posts list | P1 | Navigate to blog posts | Posts listed |
| BLOG-001-02 | Create post | P1 | Fill title, content, save | Post created as draft |
| BLOG-001-03 | Edit post | P1 | Open post, edit, save | Changes saved |
| BLOG-001-04 | Publish post | P1 | Click Publish | Status = Published |
| BLOG-001-05 | Unpublish post | P1 | Click Unpublish | Status = Draft |
| BLOG-001-06 | Delete post | P1 | Delete with confirmation | Post soft-deleted |
| BLOG-001-07 | Assign categories | P1 | Select categories | Categories saved |
| BLOG-001-08 | Add tags | P1 | Add/create tags | Tags saved |
| BLOG-002-01 | View categories | P1 | Navigate to categories | Categories listed |
| BLOG-002-02 | Create category | P0 | Fill name, slug | Category created |
| BLOG-002-03 | Edit category | P1 | Update name | Changes saved |
| BLOG-002-04 | Delete category | P2 | Delete empty category | Category deleted |
| BLOG-003-01 | View tags | P1 | Navigate to tags | Tags listed |
| BLOG-003-02 | Create tag | P1 | Enter tag name | Tag created |
| BLOG-003-03 | Delete tag | P2 | Delete unused tag | Tag deleted |

---

### 6.10 Settings Module (10 Tests)

| ID | Test Case | Priority | Steps | Expected Result |
|----|-----------|----------|-------|-----------------|
| SET-001-01 | View tenant settings | P0 | Navigate to settings | Settings tabs shown |
| SET-001-02 | Update branding | P1 | Change logo, colors | Branding updated |
| SET-001-03 | Update contact info | P1 | Change email, phone | Contact saved |
| SET-001-04 | Configure SMTP | P1 | Enter SMTP settings | Settings saved |
| SET-001-05 | Test email | P2 | Click "Send Test" | Test email sent |
| SET-001-06 | Configure payment gateway | P1 | Add payment gateway | Gateway configured |
| SET-001-07 | Regional settings | P1 | Change timezone, currency | Settings saved |
| SET-001-08 | Legal pages | P1 | Edit terms/privacy | Pages updated |
| SET-001-09 | Email templates | P1 | Customize template | Template saved |
| SET-001-10 | Preview email template | P2 | Click Preview | Preview shown |

---

## 7. Test Data Strategy

### 7.1 Data Seeding

| Data Type | Method | Lifecycle |
|-----------|--------|-----------|
| Users | Database Seeder | Persistent |
| Products | Database Seeder + API | Per-test cleanup |
| Orders | API Setup | Per-test cleanup |
| Test Entities | API with UUID suffix | Auto-cleanup |

### 7.2 Test Data Generators

```typescript
// utils/test-data.ts
export function generateTestProduct() {
  const id = crypto.randomUUID().slice(0, 8);
  return {
    name: `Test Product ${id}`,
    sku: `SKU-${id}`,
    price: Math.floor(Math.random() * 10000) + 100,
    description: `Test product description ${id}`,
  };
}

export function generateTestUser() {
  const id = crypto.randomUUID().slice(0, 8);
  return {
    email: `test.user.${id}@noir.local`,
    firstName: 'Test',
    lastName: `User ${id}`,
    password: 'Test@123',
  };
}
```

### 7.3 Cleanup Strategy

| Strategy | When | How |
|----------|------|-----|
| Transaction Rollback | Integration tests | DB transaction per test |
| API Cleanup | E2E tests | Delete via API in afterEach |
| Time-based Cleanup | Nightly | Delete entities > 24h old |

---

## 8. Execution Strategy

### 8.1 CI/CD Integration

```yaml
# Trigger conditions
on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]
  schedule:
    - cron: '0 2 * * *'  # Nightly at 2 AM
```

### 8.2 Test Execution Tiers

| Tier | Tests | Trigger | Blocking |
|------|-------|---------|----------|
| Smoke | P0 only (37) | Every commit | Yes |
| PR Validation | P0 + P1 (94) | Pull request | Yes |
| Nightly | All (123) | 2 AM daily | No |

### 8.3 Parallel Execution

| Setting | Value | Rationale |
|---------|-------|-----------|
| Workers (Local) | CPU count | Maximum speed |
| Workers (CI) | 2-4 | Balance speed/stability |
| Retries (CI) | 2 | Handle flakiness |
| Retries (Local) | 0 | Immediate feedback |

### 8.4 Commands

```bash
# Run all tests
npm run test

# Run smoke tests only
npm run test:smoke

# Run P0 priority tests
npm run test:p0

# Run specific feature
npm run test:products
npm run test:auth
npm run test:ecommerce

# Run with UI (debugging)
npm run test:ui

# Run headed mode
npm run test:headed

# Generate code with recorder
npm run codegen
```

---

## 9. Reporting & Metrics

### 9.1 Report Types

| Report | Format | Purpose |
|--------|--------|---------|
| HTML Report | Interactive HTML | Detailed results with screenshots |
| JSON Report | Machine-readable | CI/CD integration |
| JUnit XML | XML | Test management tools |
| Console | Text | Real-time feedback |

### 9.2 Metrics Tracked

| Metric | Target | Action if Missed |
|--------|--------|------------------|
| Pass Rate (P0) | 100% | Block deployment |
| Pass Rate (P1) | 95% | Review required |
| Pass Rate (Overall) | 90% | Investigation |
| Execution Time | < 30 min | Optimize tests |
| Flaky Rate | < 5% | Fix or quarantine |

### 9.3 Screenshot & Video

| Artifact | Capture When | Retention |
|----------|--------------|-----------|
| Screenshot | On failure | 30 days |
| Video | On failure (CI) | 30 days |
| Trace | First retry | 30 days |

---

## 10. Risk Assessment

### 10.1 Technical Risks

| Risk | Impact | Mitigation |
|------|--------|------------|
| Flaky tests | False negatives | Retry logic, stable selectors |
| Slow execution | Late feedback | Parallelization, API setup |
| Environment drift | Test failures | Docker, infrastructure as code |
| Selector changes | Broken tests | data-testid attributes |

### 10.2 Process Risks

| Risk | Impact | Mitigation |
|------|--------|------------|
| Test maintenance debt | Reduced coverage | Page Object Model, DRY |
| Missing test updates | Stale tests | PR checklist, code review |
| Inadequate coverage | Escaped bugs | Coverage tracking, test matrices |

---

## 11. Implementation Roadmap

### Phase 1: Foundation (Week 1)
- [x] Test plan documentation
- [ ] Playwright project setup
- [ ] Authentication fixtures
- [ ] Base page objects
- [ ] 10 smoke tests (P0)

### Phase 2: Core CRUD (Week 2-3)
- [ ] Products page objects + tests (24)
- [ ] Users page objects + tests (12)
- [ ] Roles page objects + tests (8)
- [ ] Tenants page objects + tests (8)

### Phase 3: E-Commerce (Week 4)
- [ ] Cart page objects + tests (10)
- [ ] Checkout page objects + tests (10)
- [ ] Orders page objects + tests (10)
- [ ] End-to-end purchase flow

### Phase 4: Content & Settings (Week 5)
- [ ] Blog tests (15)
- [ ] Settings tests (10)
- [ ] Activity timeline tests
- [ ] Email templates tests

### Phase 5: CI/CD & Polish (Week 6)
- [ ] GitHub Actions workflow
- [ ] Test report dashboard
- [ ] Visual regression baseline
- [ ] Documentation finalization

---

## Appendices

### A. Glossary

| Term | Definition |
|------|------------|
| E2E | End-to-end testing |
| POM | Page Object Model |
| SUT | System Under Test |
| Flaky | Test that sometimes passes, sometimes fails |

### B. References

- [Playwright Documentation](https://playwright.dev)
- [NOIR Architecture](../ARCHITECTURE.md)
- [NOIR Feature Catalog](../FEATURE_CATALOG.md)
- [Product E2E Test Suite](./PRODUCT_E2E_TESTS.md) - **247 comprehensive product tests**
- [Test Cases Reference](./TEST_CASES.md)
- [E2E Testing Guide](./E2E-TESTING-GUIDE.md)

### C. Changelog

| Version | Date | Changes |
|---------|------|---------|
| 1.1 | 2026-02-05 | Added comprehensive Product E2E test suite (247 tests) |
| 1.0 | 2026-02-05 | Initial test plan |

---

<div align="center">

**NOIR QA Team**

[Documentation Index](../DOCUMENTATION_INDEX.md) | [Feature Catalog](../FEATURE_CATALOG.md) | [Architecture](../ARCHITECTURE.md)

</div>
