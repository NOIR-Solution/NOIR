# NOIR Test Cases Reference

**Complete Test Case Matrices by Feature Module**

---

## Table of Contents

1. [Authentication Module](#1-authentication-module)
2. [Product Management Module](#2-product-management-module)
3. [User Management Module](#3-user-management-module)
4. [Role Management Module](#4-role-management-module)
5. [Tenant Management Module](#5-tenant-management-module)
6. [Shopping Cart Module](#6-shopping-cart-module)
7. [Checkout Module](#7-checkout-module)
8. [Order Management Module](#8-order-management-module)
9. [Blog Management Module](#9-blog-management-module)
10. [Settings Module](#10-settings-module)
11. [Activity & Audit Module](#11-activity--audit-module)
12. [Smoke Test Suite](#12-smoke-test-suite)

---

## Legend

| Priority | Description | SLA |
|----------|-------------|-----|
| **P0** | Critical - Core functionality must work | 100% pass required |
| **P1** | High - Main feature operations | 95%+ pass required |
| **P2** | Medium - Edge cases, validation | 90%+ pass recommended |
| **P3** | Low - UX polish, visual | Best effort |

| Status | Icon |
|--------|------|
| Not Started | ⬜ |
| In Progress | 🔄 |
| Passed | ✅ |
| Failed | ❌ |
| Blocked | 🚫 |

---

## 1. Authentication Module

### AUTH-001: Login Flow (8 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| AUTH-001-01 | Valid login as Platform Admin | P0 | Platform admin exists | 1. Navigate to `/login`<br>2. Enter `platform@noir.local`<br>3. Enter `123qwe`<br>4. Click Login | Dashboard loads, user menu shows email, Platform Admin badge visible | ⬜ |
| AUTH-001-02 | Valid login as Tenant Admin | P0 | Tenant admin exists | 1. Navigate to `/login`<br>2. Enter `admin@noir.local`<br>3. Enter `123qwe`<br>4. Click Login | Dashboard loads, tenant context set, correct permissions | ⬜ |
| AUTH-001-03 | Invalid email format | P1 | None | 1. Enter "invalid-email" in email field<br>2. Tab to password field | Validation error: "Invalid email format" appears below field | ⬜ |
| AUTH-001-04 | Wrong password | P1 | User exists | 1. Enter valid email<br>2. Enter wrong password<br>3. Click Login | Error toast: "Invalid credentials" | ⬜ |
| AUTH-001-05 | Empty email field | P1 | None | 1. Leave email empty<br>2. Enter password<br>3. Click Login | Validation: "Email is required" | ⬜ |
| AUTH-001-06 | Empty password field | P1 | None | 1. Enter email<br>2. Leave password empty<br>3. Click Login | Validation: "Password is required" | ⬜ |
| AUTH-001-07 | Locked account login | P2 | User account is locked | 1. Login with locked user credentials | Error: "Account is locked. Contact administrator." | ⬜ |
| AUTH-001-08 | Remember me checkbox | P2 | None | 1. Check "Remember me"<br>2. Login<br>3. Close browser<br>4. Reopen and navigate to site | Session persists, user still logged in | ⬜ |

### AUTH-002: Password Reset (8 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| AUTH-002-01 | Request OTP - valid email | P0 | User exists | 1. Click "Forgot Password"<br>2. Enter registered email<br>3. Click Send OTP | Success message, redirect to OTP verification page | ⬜ |
| AUTH-002-02 | Request OTP - non-existent email | P1 | Email not registered | 1. Enter non-existent email<br>2. Click Send OTP | Same success message (security - don't reveal email existence) | ⬜ |
| AUTH-002-03 | Verify OTP - correct code | P0 | OTP sent | 1. Enter correct 6-digit OTP<br>2. Click Verify | Proceed to password reset form | ⬜ |
| AUTH-002-04 | Verify OTP - wrong code | P1 | OTP sent | 1. Enter wrong OTP<br>2. Click Verify | Error: "Invalid OTP" | ⬜ |
| AUTH-002-05 | Verify OTP - expired | P1 | OTP expired (>10 min) | 1. Wait > 10 minutes<br>2. Enter OTP | Error: "OTP has expired. Please request a new one." | ⬜ |
| AUTH-002-06 | Reset password - valid | P0 | OTP verified | 1. Enter new password (strong)<br>2. Confirm password<br>3. Click Reset | Success, redirect to login, can login with new password | ⬜ |
| AUTH-002-07 | Reset password - mismatch | P1 | OTP verified | 1. Enter password<br>2. Enter different confirmation | Error: "Passwords don't match" | ⬜ |
| AUTH-002-08 | Reset password - weak | P1 | OTP verified | 1. Enter weak password "123" | Error: "Password must be at least 8 characters with uppercase, lowercase, and number" | ⬜ |

### AUTH-003: Session Management (4 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| AUTH-003-01 | Logout | P0 | Logged in | 1. Click user menu<br>2. Click Logout | Redirected to login page, session cleared | ⬜ |
| AUTH-003-02 | Token auto-refresh | P1 | Logged in | 1. Stay logged in past access token expiry (15 min)<br>2. Perform action | Token auto-refreshed, action succeeds | ⬜ |
| AUTH-003-03 | Session timeout | P2 | Logged in | 1. Stay idle past session timeout<br>2. Try to perform action | Redirected to login with "Session expired" message | ⬜ |
| AUTH-003-04 | Concurrent sessions | P2 | Logged in on device A | 1. Login on device B<br>2. Both sessions active | Both sessions work independently | ⬜ |

---

## 2. Product Management Module

> **COMPREHENSIVE TESTING:** For robust end-to-end browser testing of the complete Product ecosystem (Categories, Attributes, Brands, Products, Variants, Images, Options, Bulk Operations, Import/Export), see the dedicated **[Product E2E Test Suite](./PRODUCT_E2E_TESTS.md)** with **247 detailed test cases**.

The tests below provide a quick reference. For full coverage, use the comprehensive suite.

### PROD-001: Product List View (8 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| PROD-001-01 | View products page | P0 | Logged as Admin | Navigate to `/portal/ecommerce/products` | Products table displayed with columns: Image, Name, SKU, Status, Price, Stock | ⬜ |
| PROD-001-02 | Search by product name | P0 | Products exist | 1. Type product name in search<br>2. Wait for results | Only matching products shown | ⬜ |
| PROD-001-03 | Search by SKU | P1 | Products exist | 1. Type SKU in search | Product with matching SKU shown | ⬜ |
| PROD-001-04 | Filter by status - Active | P1 | Mixed status products | 1. Open status filter<br>2. Select "Active" | Only active products shown | ⬜ |
| PROD-001-05 | Filter by status - Draft | P1 | Draft products exist | 1. Select "Draft" filter | Only draft products shown | ⬜ |
| PROD-001-06 | Filter by category | P1 | Products in categories | 1. Open category filter<br>2. Select category | Products in that category shown | ⬜ |
| PROD-001-07 | Pagination | P1 | >10 products | 1. Click page 2<br>2. Click Next<br>3. Click Previous | Correct pages load each time | ⬜ |
| PROD-001-08 | Sort by columns | P1 | Multiple products | 1. Click Name column<br>2. Click Price column | Sorted by respective column | ⬜ |

### PROD-002: Create Product (12 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| PROD-002-01 | Create simple product | P0 | Logged as Admin | 1. Click "Create Product"<br>2. Enter name, SKU, price<br>3. Click Save | Product created, redirect to list, product visible | ⬜ |
| PROD-002-02 | Create with description | P1 | Form open | 1. Fill required fields<br>2. Enter rich text description<br>3. Save | Description saved with formatting | ⬜ |
| PROD-002-03 | Create with category | P1 | Categories exist | 1. Fill required fields<br>2. Select category<br>3. Save | Product created in category | ⬜ |
| PROD-002-04 | Create with single variant option | P1 | Form open | 1. Fill required fields<br>2. Add "Size" option with S, M, L<br>3. Generate variants<br>4. Save | 3 variants created (S, M, L) | ⬜ |
| PROD-002-05 | Create with multiple variant options | P1 | Form open | 1. Add "Size" (S, M) and "Color" (Red, Blue)<br>2. Generate variants<br>3. Save | 4 variants created (S-Red, S-Blue, M-Red, M-Blue) | ⬜ |
| PROD-002-06 | Upload single image | P1 | Form open | 1. Click image upload<br>2. Select image file<br>3. Wait for upload | Image thumbnail displayed | ⬜ |
| PROD-002-07 | Upload multiple images | P1 | Form open | 1. Drag multiple images<br>2. Wait for upload | All images uploaded, gallery shows thumbnails | ⬜ |
| PROD-002-08 | Set primary image | P2 | Images uploaded | 1. Upload 3 images<br>2. Click "Set as Primary" on image 2 | Image 2 marked with primary badge | ⬜ |
| PROD-002-09 | Reorder images | P2 | Images uploaded | 1. Drag image 3 to position 1<br>2. Save | Image order persisted | ⬜ |
| PROD-002-10 | Validation - empty name | P1 | Form open | 1. Leave name empty<br>2. Click Save | Error: "Name is required" | ⬜ |
| PROD-002-11 | Validation - empty SKU | P1 | Form open | 1. Leave SKU empty<br>2. Click Save | Error: "SKU is required" | ⬜ |
| PROD-002-12 | Validation - duplicate SKU | P2 | Product with SKU exists | 1. Enter existing SKU<br>2. Click Save | Error: "SKU already exists" | ⬜ |

### PROD-003: Update Product (6 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| PROD-003-01 | Update basic info | P0 | Product exists | 1. Open product<br>2. Change name<br>3. Save | Name updated, success toast | ⬜ |
| PROD-003-02 | Update pricing | P0 | Product exists | 1. Change price<br>2. Add compare price<br>3. Save | Both prices updated | ⬜ |
| PROD-003-03 | Update inventory | P1 | Product exists | 1. Change stock quantity<br>2. Save | Stock updated, reflected in list | ⬜ |
| PROD-003-04 | Add variant to existing product | P1 | Product with variants | 1. Add new option value<br>2. Generate variants<br>3. Save | New variants created | ⬜ |
| PROD-003-05 | Delete variant | P2 | Product with variants | 1. Delete variant<br>2. Confirm<br>3. Save | Variant removed | ⬜ |
| PROD-003-06 | Update variant prices individually | P1 | Product with variants | 1. Edit variant 1 price<br>2. Edit variant 2 price<br>3. Save | Different prices for each variant | ⬜ |

### PROD-004: Product Lifecycle (6 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| PROD-004-01 | Publish draft product | P0 | Draft product exists | 1. Open draft product<br>2. Click Publish | Status changes to Active | ⬜ |
| PROD-004-02 | Archive product | P1 | Active product exists | 1. Open product actions<br>2. Click Archive | Status changes to Archived | ⬜ |
| PROD-004-03 | Unarchive product | P2 | Archived product | 1. Open archived product<br>2. Click Restore | Status changes to Active | ⬜ |
| PROD-004-04 | Delete product | P1 | Product exists | 1. Click Delete<br>2. Confirm in dialog | Product soft-deleted, not visible in list | ⬜ |
| PROD-004-05 | Bulk publish | P2 | Multiple draft products | 1. Select multiple products<br>2. Click Bulk Actions > Publish | All selected published | ⬜ |
| PROD-004-06 | Duplicate product | P2 | Product exists | 1. Click Duplicate | New product created with "Copy of" prefix | ⬜ |

### PROD-005: Product Attributes (4 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| PROD-005-01 | View attributes by category | P1 | Category has attributes | 1. Create product<br>2. Select category | Category attributes shown in form | ⬜ |
| PROD-005-02 | Fill text attribute | P1 | Text attribute exists | 1. Enter text value<br>2. Save | Text attribute saved | ⬜ |
| PROD-005-03 | Fill select attribute | P1 | Select attribute exists | 1. Choose option<br>2. Save | Selection saved | ⬜ |
| PROD-005-04 | Fill multi-select attribute | P1 | Multi-select exists | 1. Choose multiple options<br>2. Save | All selections saved | ⬜ |

---

## 3. User Management Module

### USER-001: User List (4 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| USER-001-01 | View users page | P0 | Logged as Admin | Navigate to `/portal/admin/users` | Users table with columns: Avatar, Name, Email, Roles, Status | ⬜ |
| USER-001-02 | Search users | P1 | Users exist | 1. Type name/email in search | Matching users shown | ⬜ |
| USER-001-03 | Filter by role | P1 | Users with roles | 1. Select role filter | Users with that role shown | ⬜ |
| USER-001-04 | Filter by status | P1 | Mixed status users | 1. Select Active/Locked | Matching users shown | ⬜ |

### USER-002: Create User (4 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| USER-002-01 | Create user with all fields | P0 | Logged as Admin | 1. Click "Create User"<br>2. Fill email, first name, last name<br>3. Select roles<br>4. Submit | User created, appears in list | ⬜ |
| USER-002-02 | Create user - required fields only | P1 | Form open | 1. Fill only required fields<br>2. Submit | User created with defaults | ⬜ |
| USER-002-03 | Validation - duplicate email | P1 | User with email exists | 1. Enter existing email<br>2. Submit | Error: "Email already exists" | ⬜ |
| USER-002-04 | Validation - invalid email | P1 | Form open | 1. Enter invalid email format<br>2. Submit | Error: "Invalid email format" | ⬜ |

### USER-003: Update User (4 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| USER-003-01 | Update user details | P0 | User exists | 1. Click Edit on user<br>2. Change name<br>3. Save | Name updated | ⬜ |
| USER-003-02 | Assign roles | P0 | User exists, roles exist | 1. Click Assign Roles<br>2. Select roles<br>3. Save | Roles updated | ⬜ |
| USER-003-03 | Lock user | P1 | Active user exists | 1. Click Lock button | User status = Locked | ⬜ |
| USER-003-04 | Unlock user | P1 | Locked user exists | 1. Click Unlock button | User status = Active | ⬜ |

### USER-004: Delete User (2 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| USER-004-01 | Delete user | P1 | Non-admin user exists | 1. Click Delete<br>2. Confirm | User soft-deleted | ⬜ |
| USER-004-02 | Cannot delete self | P2 | Logged in as user | Try to delete own account | Error: "Cannot delete your own account" | ⬜ |

---

## 4. Role Management Module

### ROLE-001: Role CRUD (8 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| ROLE-001-01 | View roles list | P0 | Logged as Admin | Navigate to `/portal/admin/roles` | Roles table with columns: Name, Description, Users, Permissions | ⬜ |
| ROLE-001-02 | Create role | P0 | Form open | 1. Click "Create Role"<br>2. Enter name, description<br>3. Save | Role created | ⬜ |
| ROLE-001-03 | Assign permissions to role | P0 | Role exists | 1. Click Permissions on role<br>2. Select permissions<br>3. Save | Permissions saved | ⬜ |
| ROLE-001-04 | Update role | P1 | Role exists | 1. Click Edit<br>2. Change name<br>3. Save | Name updated | ⬜ |
| ROLE-001-05 | Remove permissions | P1 | Role has permissions | 1. Open permissions<br>2. Uncheck some<br>3. Save | Permissions removed | ⬜ |
| ROLE-001-06 | Delete custom role | P1 | Custom role exists | 1. Click Delete<br>2. Confirm | Role deleted | ⬜ |
| ROLE-001-07 | Cannot delete system role | P2 | System role (Admin) | Try to delete Admin role | Error or delete button disabled | ⬜ |
| ROLE-001-08 | View role users | P1 | Role has users | Click role row | Users with role displayed | ⬜ |

---

## 5. Tenant Management Module

### TENANT-001: Tenant CRUD (8 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| TENANT-001-01 | View tenants (Platform Admin) | P0 | Logged as Platform Admin | Navigate to `/portal/admin/tenants` | Tenants list shown | ⬜ |
| TENANT-001-02 | Create tenant | P0 | Platform Admin | 1. Click "Create Tenant"<br>2. Fill name, identifier, admin email<br>3. Submit | Tenant created with admin user | ⬜ |
| TENANT-001-03 | View tenant details | P1 | Tenant exists | Click tenant row | Details page with settings, users, stats | ⬜ |
| TENANT-001-04 | Update tenant name | P1 | Tenant exists | 1. Edit tenant<br>2. Change name<br>3. Save | Name updated | ⬜ |
| TENANT-001-05 | Deactivate tenant | P1 | Active tenant | Click Deactivate | Tenant status = Inactive | ⬜ |
| TENANT-001-06 | Activate tenant | P1 | Inactive tenant | Click Activate | Tenant status = Active | ⬜ |
| TENANT-001-07 | Reset admin password | P2 | Tenant exists | 1. Click Reset Password<br>2. Enter new password<br>3. Submit | Admin can login with new password | ⬜ |
| TENANT-001-08 | Delete tenant | P1 | Tenant exists | 1. Click Delete<br>2. Confirm | Tenant soft-deleted | ⬜ |

---

## 6. Shopping Cart Module

### CART-001: Cart Operations (10 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| CART-001-01 | Add item to cart | P0 | Product exists | Click "Add to Cart" on product | Item added, cart icon count updates | ⬜ |
| CART-001-02 | View cart | P0 | Items in cart | Click cart icon | Cart drawer/page shows items with details | ⬜ |
| CART-001-03 | Increase quantity | P1 | Item in cart | Click + button | Quantity +1, total updates | ⬜ |
| CART-001-04 | Decrease quantity | P1 | Item qty > 1 | Click - button | Quantity -1, total updates | ⬜ |
| CART-001-05 | Decrease to remove | P2 | Item qty = 1 | Click - button | Item removed from cart | ⬜ |
| CART-001-06 | Remove item directly | P1 | Item in cart | Click remove/trash button | Item removed | ⬜ |
| CART-001-07 | Clear entire cart | P2 | Multiple items | Click "Clear Cart" | All items removed | ⬜ |
| CART-001-08 | Cart persistence | P1 | Items in cart | 1. Refresh page | Cart still has items | ⬜ |
| CART-001-09 | Cart total calculation | P0 | Multiple items | Add items with different qty | Total = sum(price * qty) | ⬜ |
| CART-001-10 | Cart merge on login | P2 | Guest cart + user cart | 1. Add items as guest<br>2. Login | Both carts merged | ⬜ |

---

## 7. Checkout Module

### CHECKOUT-001: Checkout Flow (10 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| CHECKOUT-001-01 | Initiate checkout | P0 | Items in cart | Click "Checkout" button | Checkout page loads with address form | ⬜ |
| CHECKOUT-001-02 | Fill shipping address | P0 | Checkout initiated | 1. Fill all address fields<br>2. Click Continue | Address saved, proceed to shipping selection | ⬜ |
| CHECKOUT-001-03 | Select shipping method | P0 | Address filled | Choose shipping option | Method selected, shipping cost added to total | ⬜ |
| CHECKOUT-001-04 | Select payment method | P0 | Shipping selected | Choose payment method | Method selected, proceed to review | ⬜ |
| CHECKOUT-001-05 | Complete checkout | P0 | All steps complete | Click "Place Order" | Order created, confirmation page shown | ⬜ |
| CHECKOUT-001-06 | Validation - incomplete address | P1 | Address form open | Submit with missing fields | Validation errors on required fields | ⬜ |
| CHECKOUT-001-07 | Back navigation | P2 | On shipping step | Click "Back" | Return to address, data preserved | ⬜ |
| CHECKOUT-001-08 | Edit address mid-checkout | P1 | On review step | Go back and edit address | Changes reflected in summary | ⬜ |
| CHECKOUT-001-09 | Empty cart checkout | P2 | Cart is empty | Navigate to checkout | Redirect to cart with "Cart is empty" message | ⬜ |
| CHECKOUT-001-10 | Session timeout | P2 | Checkout initiated | Wait > 30 minutes | Session expired, redirect to cart | ⬜ |

---

## 8. Order Management Module

### ORDER-001: Order Operations (10 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| ORDER-001-01 | View orders list | P0 | Logged as Admin | Navigate to `/portal/ecommerce/orders` | Orders table with columns: Order #, Customer, Status, Total, Date | ⬜ |
| ORDER-001-02 | View order details | P0 | Order exists | Click order row | Order details: items, address, payment, timeline | ⬜ |
| ORDER-001-03 | Filter by status | P1 | Orders in various status | Select status filter | Matching orders shown | ⬜ |
| ORDER-001-04 | Search by order number | P1 | Order exists | Search by order # | Order found | ⬜ |
| ORDER-001-05 | Confirm order | P1 | Pending order | Click "Confirm" | Status = Confirmed | ⬜ |
| ORDER-001-06 | Ship order | P1 | Confirmed order | 1. Click "Ship"<br>2. Add tracking number | Status = Shipped, tracking saved | ⬜ |
| ORDER-001-07 | Mark delivered | P1 | Shipped order | Click "Mark Delivered" | Status = Delivered | ⬜ |
| ORDER-001-08 | Complete order | P1 | Delivered order | Click "Complete" | Status = Completed | ⬜ |
| ORDER-001-09 | Cancel order | P1 | Pending/Confirmed order | 1. Click Cancel<br>2. Confirm | Status = Cancelled | ⬜ |
| ORDER-001-10 | Verify inventory release on cancel | P0 | Order with items | Cancel order | Item stock restored | ⬜ |

---

## 9. Blog Management Module

### BLOG-001: Blog Posts (8 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| BLOG-001-01 | View posts list | P1 | Logged as Admin | Navigate to `/portal/blog/posts` | Posts table displayed | ⬜ |
| BLOG-001-02 | Create post | P1 | Categories exist | 1. Click "Create Post"<br>2. Fill title, content<br>3. Save | Post created as Draft | ⬜ |
| BLOG-001-03 | Edit post | P1 | Post exists | 1. Open post<br>2. Edit content<br>3. Save | Changes saved | ⬜ |
| BLOG-001-04 | Publish post | P1 | Draft post exists | Click "Publish" | Status = Published | ⬜ |
| BLOG-001-05 | Unpublish post | P1 | Published post | Click "Unpublish" | Status = Draft | ⬜ |
| BLOG-001-06 | Delete post | P1 | Post exists | 1. Click Delete<br>2. Confirm | Post soft-deleted | ⬜ |
| BLOG-001-07 | Assign categories | P1 | Categories exist | 1. Edit post<br>2. Select categories<br>3. Save | Categories saved | ⬜ |
| BLOG-001-08 | Add tags | P1 | Tags exist or create new | 1. Edit post<br>2. Add tags<br>3. Save | Tags saved | ⬜ |

### BLOG-002: Blog Categories (4 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| BLOG-002-01 | View categories | P1 | Logged as Admin | Navigate to blog categories | Categories listed | ⬜ |
| BLOG-002-02 | Create category | P0 | None | 1. Click Create<br>2. Enter name, slug<br>3. Save | Category created | ⬜ |
| BLOG-002-03 | Edit category | P1 | Category exists | Edit and save | Changes saved | ⬜ |
| BLOG-002-04 | Delete empty category | P2 | Category with no posts | Delete and confirm | Category deleted | ⬜ |

### BLOG-003: Blog Tags (3 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| BLOG-003-01 | View tags | P1 | Logged as Admin | Navigate to blog tags | Tags listed | ⬜ |
| BLOG-003-02 | Create tag | P1 | None | Enter tag name, save | Tag created | ⬜ |
| BLOG-003-03 | Delete unused tag | P2 | Tag not used | Delete and confirm | Tag deleted | ⬜ |

---

## 10. Settings Module

### SET-001: Tenant Settings (10 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| SET-001-01 | View tenant settings | P0 | Logged as Tenant Admin | Navigate to settings | Settings tabs displayed | ⬜ |
| SET-001-02 | Update branding - logo | P1 | Settings page | 1. Upload logo<br>2. Save | Logo updated, visible in header | ⬜ |
| SET-001-03 | Update branding - colors | P1 | Settings page | 1. Change primary color<br>2. Save | Color applied to UI elements | ⬜ |
| SET-001-04 | Update contact info | P1 | Settings page | 1. Change email, phone<br>2. Save | Contact info saved | ⬜ |
| SET-001-05 | Configure SMTP | P1 | Settings page | 1. Enter SMTP settings<br>2. Save | SMTP configured | ⬜ |
| SET-001-06 | Test SMTP connection | P2 | SMTP configured | Click "Test Connection" | Success/failure notification | ⬜ |
| SET-001-07 | Configure payment gateway | P1 | Settings page | 1. Add Stripe keys<br>2. Save | Gateway configured | ⬜ |
| SET-001-08 | Regional settings | P1 | Settings page | 1. Change timezone, currency<br>2. Save | Settings saved | ⬜ |
| SET-001-09 | Edit legal page | P1 | Settings page | 1. Edit Terms of Service<br>2. Save | Legal page updated | ⬜ |
| SET-001-10 | Customize email template | P1 | Templates tab | 1. Edit welcome email<br>2. Save | Template saved | ⬜ |

---

## 11. Activity & Audit Module

### AUDIT-001: Activity Timeline (5 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| AUDIT-001-01 | View activity timeline | P1 | Logged as Admin | Navigate to activity timeline | Recent activities listed | ⬜ |
| AUDIT-001-02 | Filter by entity type | P1 | Activities exist | Select entity type filter | Matching activities shown | ⬜ |
| AUDIT-001-03 | Filter by user | P1 | Activities exist | Select user filter | User's activities shown | ⬜ |
| AUDIT-001-04 | Filter by date range | P1 | Activities exist | Set date range | Activities in range shown | ⬜ |
| AUDIT-001-05 | View activity details | P1 | Activity exists | Click activity row | Details with before/after diff | ⬜ |

---

## 12. Smoke Test Suite

### SMOKE-001: Critical Path Tests (10 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| SMOKE-001-01 | Application loads | P0 | App deployed | Navigate to base URL | Login page displayed | ⬜ |
| SMOKE-001-02 | Login as Tenant Admin | P0 | Admin exists | Login with admin@noir.local | Dashboard loads | ⬜ |
| SMOKE-001-03 | Navigate to products | P0 | Logged in | Click Products menu | Products page loads | ⬜ |
| SMOKE-001-04 | Create product | P0 | Logged in | Create simple product | Product created | ⬜ |
| SMOKE-001-05 | Navigate to users | P0 | Logged in | Click Users menu | Users page loads | ⬜ |
| SMOKE-001-06 | Navigate to roles | P0 | Logged in | Click Roles menu | Roles page loads | ⬜ |
| SMOKE-001-07 | Navigate to settings | P0 | Logged in | Click Settings | Settings page loads | ⬜ |
| SMOKE-001-08 | Logout | P0 | Logged in | Click Logout | Redirected to login | ⬜ |
| SMOKE-001-09 | Login as Platform Admin | P0 | Platform admin exists | Login with platform@noir.local | Dashboard loads, tenants visible | ⬜ |
| SMOKE-001-10 | Navigate to tenants | P0 | Platform Admin | Click Tenants menu | Tenants page loads | ⬜ |

---

## Test Execution Tracking

### Summary by Module

| Module | Total | P0 | P1 | P2 | P3 | Passed | Failed | Skipped |
|--------|-------|----|----|----|----|--------|--------|---------|
| Authentication | 20 | 6 | 10 | 4 | 0 | 0 | 0 | 0 |
| Products (Quick) | 36 | 6 | 20 | 10 | 0 | 0 | 0 | 0 |
| Users | 14 | 4 | 8 | 2 | 0 | 0 | 0 | 0 |
| Roles | 8 | 3 | 4 | 1 | 0 | 0 | 0 | 0 |
| Tenants | 8 | 3 | 4 | 1 | 0 | 0 | 0 | 0 |
| Cart | 10 | 4 | 4 | 2 | 0 | 0 | 0 | 0 |
| Checkout | 10 | 5 | 3 | 2 | 0 | 0 | 0 | 0 |
| Orders | 10 | 4 | 6 | 0 | 0 | 0 | 0 | 0 |
| Blog | 15 | 2 | 10 | 3 | 0 | 0 | 0 | 0 |
| Settings | 10 | 1 | 7 | 2 | 0 | 0 | 0 | 0 |
| Activity | 5 | 0 | 5 | 0 | 0 | 0 | 0 | 0 |
| Smoke | 10 | 10 | 0 | 0 | 0 | 0 | 0 | 0 |
| **Quick Reference Total** | **156** | **48** | **81** | **27** | **0** | **0** | **0** | **0** |

### Comprehensive Product E2E Suite (Detailed)

> See [PRODUCT_E2E_TESTS.md](./PRODUCT_E2E_TESTS.md) for full coverage

| Module | Total | P0 | P1 | P2 | P3 |
|--------|-------|----|----|----|----|
| Categories | 30 | 5 | 18 | 7 | 0 |
| Attributes | 45 | 6 | 32 | 7 | 0 |
| Brands | 12 | 2 | 8 | 2 | 0 |
| Product CRUD | 48 | 12 | 30 | 6 | 0 |
| Variants | 28 | 5 | 18 | 5 | 0 |
| Images | 22 | 4 | 14 | 4 | 0 |
| Options | 14 | 2 | 10 | 2 | 0 |
| Filters & Search | 20 | 4 | 14 | 2 | 0 |
| Bulk Operations | 16 | 4 | 10 | 2 | 0 |
| Import/Export | 18 | 3 | 13 | 2 | 0 |
| Integration | 14 | 5 | 9 | 0 | 0 |
| **Product Suite Total** | **247** | **52** | **158** | **35** | **2** |

### Grand Total (All Test Suites)

| Suite | Tests | P0 | P1 | P2 |
|-------|-------|----|----|-----|
| Quick Reference | 156 | 48 | 81 | 27 |
| Product E2E (Comprehensive) | 247 | 52 | 158 | 35 |
| **Grand Total** | **403** | **100** | **239** | **62** |

### Execution Log

| Date | Executor | Environment | Suite | Passed | Failed | Notes |
|------|----------|-------------|-------|--------|--------|-------|
| | | | | | | |

---

## References

- [Test Plan](./TEST_PLAN.md)
- [E2E Testing Guide](./E2E-TESTING-GUIDE.md)
- [Product E2E Test Suite](./PRODUCT_E2E_TESTS.md) - **247 comprehensive product tests**
- [NOIR Feature Catalog](../FEATURE_CATALOG.md)

---

**Last Updated:** 2026-02-05
