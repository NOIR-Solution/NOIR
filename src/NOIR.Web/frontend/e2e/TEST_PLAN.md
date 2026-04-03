# NOIR E2E Test Plan

## 1. Overview

### Goals
- Verify critical user flows work end-to-end across all modules
- Catch regressions before they reach production
- Validate cross-module data integrity (e.g., winning a CRM lead auto-creates a customer)
- Ensure multi-tenant auth and permission enforcement work correctly

### Scope
- **In scope:** All portal modules (Auth, Dashboard, E-commerce [Products, Orders, Customers, Customer Groups, Reviews], Marketing [Promotions], CRM, HR, Admin, Blog, Inventory, PM, Notifications), cross-module data linking, permission enforcement
- **Out of scope:** Unit-level logic (covered by 11,700+ backend tests), visual regression (Storybook), performance benchmarks, third-party integrations (payment gateways, shipping providers)

### Approach
- **Page Object Model (POM):** Each page has a dedicated PO class in `e2e/pages/`
- **API-first data seeding:** Use `ApiHelper` to create test data via API (10-50x faster than UI setup)
- **Shared auth state:** Login once via `auth.setup.ts`, reuse `storageState` across all tests
- **Parallel execution:** Tests are independent and can run concurrently via `fullyParallel: true`
- **Deterministic cleanup:** Each test creates and cleans up its own data in `beforeAll`/`afterAll`

### Conventions
- Test IDs use `MODULE-NNN` format (e.g., `AUTH-001`, `ECOM-PROD-003`)
- Priority: **P0** = blocks release, **P1** = important regression, **P2** = nice-to-have coverage
- Tags: `@smoke` (every PR, <5min total), `@regression` (daily, <15min), `@nightly` (full suite, all browsers)
- Time estimates assume warm server; add 2-3s for first navigation due to lazy loading

---

## 2. Test Strategy

| Level | Trigger | Target Time | Browser(s) | Tag |
|-------|---------|-------------|------------|-----|
| **Smoke** | Every PR, pre-merge | <5 min | Chromium only | `@smoke` |
| **Regression** | Daily CI schedule | <15 min | Chromium only | `@regression` |
| **Nightly** | Nightly CI schedule | <30 min | Chromium + Firefox + WebKit + Mobile Chrome | `@nightly` |

### Smoke Suite (~25 tests, <5min)
All P0 tests: login, dashboard load, CRUD happy path for each module, key cross-module links.

### Regression Suite (~80 tests, <18min)
Smoke + P1 tests: validation errors, edge cases, bulk operations, search/filter, data linking.

### Nightly Suite (~102 tests, <30min)
All tests including P2: cross-browser, import/export, advanced workflows, permission enforcement.

### Data Isolation
- Each test file uses unique prefixes for created entities (e.g., `E2E-PROD-{timestamp}`)
- Tests clean up created data in `afterAll` or `afterEach`
- Tests do NOT depend on pre-existing seed data (except the default admin user)

---

## 3. Module Test Cases

---

### 3.1 Auth

| ID | Flow | Priority | Tags | Est. |
|----|------|----------|------|------|
| AUTH-001 | Login success | P0 | @smoke | 10s |
| AUTH-002 | Login validation errors | P0 | @smoke | 10s |
| AUTH-003 | Login invalid credentials | P0 | @smoke | 10s |
| AUTH-004 | Forgot password flow | P1 | @regression | 20s |
| AUTH-005 | Session persistence on refresh | P1 | @regression | 15s |
| AUTH-006 | Unauthenticated redirect | P0 | @smoke | 10s |
| AUTH-007 | Logout | P1 | @regression | 10s |

#### AUTH-001: Login success
- **Preconditions:** User is logged out (no storageState)
- **Steps:**
  1. Navigate to `/login`
  2. Enter email `admin@noir.local`
  3. Enter password `123qwe`
  4. Click "Sign In" button
  5. Wait for redirect to `/portal`
- **Expected:** Dashboard page loads. Sidebar navigation is visible. User avatar/name appears in header.

#### AUTH-002: Login validation errors
- **Preconditions:** User is logged out
- **Steps:**
  1. Navigate to `/login`
  2. Click "Sign In" without entering any fields
  3. Observe validation messages
  4. Enter invalid email format (e.g., `not-an-email`)
  5. Observe validation message
- **Expected:** Required field errors appear for email and password. Invalid email format error shown.

#### AUTH-003: Login invalid credentials
- **Preconditions:** User is logged out
- **Steps:**
  1. Navigate to `/login`
  2. Enter email `admin@noir.local`
  3. Enter password `wrongpassword`
  4. Click "Sign In"
- **Expected:** Error alert appears with invalid credentials message. User remains on login page.

#### AUTH-004: Forgot password flow initiation
- **Preconditions:** User is logged out
- **Steps:**
  1. Navigate to `/login`
  2. Click "Forgot Password" link
  3. Verify redirect to `/forgot-password`
  4. Enter email `admin@noir.local`
  5. Submit the form
  6. Verify redirect to `/forgot-password/verify`
- **Expected:** OTP verification page loads. Email field shows the entered address.

#### AUTH-005: Session persistence on refresh
- **Preconditions:** User is logged in (storageState loaded)
- **Steps:**
  1. Navigate to `/portal`
  2. Verify dashboard loads
  3. Reload the page (F5)
  4. Verify dashboard loads again without redirect to login
- **Expected:** User remains authenticated after page refresh. No flash of login page.

#### AUTH-006: Unauthenticated redirect
- **Preconditions:** User is logged out (clear storageState)
- **Steps:**
  1. Navigate directly to `/portal` without auth
  2. Observe redirect behavior
- **Expected:** User is redirected to `/login`. After login, user is redirected back to the originally requested page.

#### AUTH-007: Logout
- **Preconditions:** User is logged in
- **Steps:**
  1. Navigate to `/portal`
  2. Open user menu (avatar/profile dropdown)
  3. Click "Logout" / "Sign Out"
  4. Observe redirect
- **Expected:** User is redirected to `/login` or `/`. Navigating to `/portal` redirects back to login.

---

### 3.2 Dashboard

| ID | Flow | Priority | Tags | Est. |
|----|------|----------|------|------|
| DASH-001 | Dashboard widgets load | P0 | @smoke | 15s |
| DASH-002 | Feature-gated widgets | P2 | @nightly | 20s |
| DASH-003 | Dashboard navigation links | P1 | @regression | 15s |

#### DASH-001: Dashboard widgets load
- **Preconditions:** Logged in as tenant admin
- **Steps:**
  1. Navigate to `/portal`
  2. Wait for dashboard to fully load
  3. Verify at least one widget/card is visible
  4. Check that no error states are shown
- **Expected:** Dashboard page renders with widget cards. No loading spinners stuck indefinitely. No error messages.

#### DASH-002: Feature-gated widgets
- **Preconditions:** Logged in as platform admin. Some modules disabled.
- **Steps:**
  1. Navigate to platform settings and disable a toggleable module (e.g., Blog)
  2. Navigate to `/portal` dashboard
  3. Verify that widgets related to the disabled module are not shown
  4. Re-enable the module
- **Expected:** Widget groups respect feature flags. Disabled modules do not show dashboard widgets.

#### DASH-003: Dashboard navigation links
- **Preconditions:** Logged in as tenant admin
- **Steps:**
  1. Navigate to `/portal`
  2. Click on a dashboard widget or quick-action link (e.g., "View Orders")
  3. Verify navigation to the correct module page
  4. Click browser back button
  5. Verify return to dashboard
- **Expected:** Widget links navigate to the correct module pages. Back navigation works.

---

### 3.3 E-commerce: Products

| ID | Flow | Priority | Tags | Est. |
|----|------|----------|------|------|
| ECOM-PROD-001 | Product list loads with pagination | P0 | @smoke | 15s |
| ECOM-PROD-002 | Create product (happy path) | P0 | @smoke | 20s |
| ECOM-PROD-003 | Create product validation errors | P1 | @regression | 15s |
| ECOM-PROD-004 | Edit product | P1 | @regression | 20s |
| ECOM-PROD-005 | Product search and filter | P1 | @regression | 15s |
| ECOM-PROD-006 | Product categories CRUD | P1 | @regression | 20s |
| ECOM-PROD-007 | Product attributes management | P2 | @nightly | 20s |
| ECOM-PROD-008 | Product status lifecycle | P1 | @regression | 20s |
| ECOM-PROD-009 | Delete product | P1 | @regression | 15s |
| ECOM-PROD-010 | Brands CRUD | P2 | @nightly | 15s |

#### ECOM-PROD-001: Product list loads with pagination
- **Preconditions:** Logged in. At least 1 product exists (seed via API).
- **Steps:**
  1. Navigate to `/portal/ecommerce/products`
  2. Verify product table renders with at least one row
  3. Check that pagination controls are visible (if >10 products)
  4. Verify table columns: Name, SKU, Price, Status, Actions
- **Expected:** Product table loads with correct data. Pagination works if applicable.

#### ECOM-PROD-002: Create product (happy path)
- **Preconditions:** Logged in as tenant admin
- **Steps:**
  1. Navigate to `/portal/ecommerce/products`
  2. Click "Create Product" / "New Product" button
  3. Verify redirect to `/portal/ecommerce/products/new`
  4. Fill in Name: `E2E Test Product {timestamp}`
  5. Fill in SKU: `E2E-SKU-{timestamp}`
  6. Fill in Price: `150000`
  7. Select Status: Draft
  8. Click "Save" / "Create"
  9. Verify success toast notification
  10. Verify redirect to product edit page or product list
- **Expected:** Product is created. Success toast shown. Product appears in list.
- **Cleanup:** Delete created product via API.

#### ECOM-PROD-003: Create product validation errors
- **Preconditions:** Logged in as tenant admin
- **Steps:**
  1. Navigate to `/portal/ecommerce/products/new`
  2. Leave Name empty
  3. Leave SKU empty
  4. Click "Save"
  5. Observe validation messages
- **Expected:** Validation errors shown for required fields (Name, SKU). Form is not submitted.

#### ECOM-PROD-004: Edit product
- **Preconditions:** Product created via API with name `E2E Edit Product`
- **Steps:**
  1. Navigate to `/portal/ecommerce/products`
  2. Click on the test product row or edit button
  3. Change the product name to `E2E Edit Product Updated`
  4. Click "Save"
  5. Verify success toast
  6. Verify the updated name is displayed
- **Expected:** Product name is updated. Success notification shown.
- **Cleanup:** Delete product via API.

#### ECOM-PROD-005: Product search and filter
- **Preconditions:** At least 2 products exist with distinct names (seed via API)
- **Steps:**
  1. Navigate to `/portal/ecommerce/products`
  2. Type a search query matching one product name
  3. Verify filtered results show only matching products
  4. Clear search
  5. Apply status filter (e.g., "Draft")
  6. Verify only products with that status are shown
- **Expected:** Search filters the list correctly. Status filter works.
- **Cleanup:** Delete seeded products.

#### ECOM-PROD-006: Product categories CRUD
- **Preconditions:** Logged in as tenant admin
- **Steps:**
  1. Navigate to `/portal/ecommerce/categories`
  2. Click "Create Category"
  3. Fill in Name: `E2E Test Category {timestamp}`
  4. Click Save
  5. Verify category appears in list
  6. Click edit on the new category
  7. Change name to `E2E Updated Category`
  8. Save and verify update
  9. Delete the category via confirmation dialog
  10. Verify category is removed from list
- **Expected:** Full CRUD lifecycle works for product categories.

#### ECOM-PROD-007: Product attributes management
- **Preconditions:** Logged in as tenant admin
- **Steps:**
  1. Navigate to `/portal/ecommerce/attributes`
  2. Verify attribute list loads
  3. Create a new attribute (e.g., "E2E Color" with type "Text")
  4. Verify it appears in the list
  5. Delete the attribute
- **Expected:** Attribute CRUD works. Attribute types are selectable.
- **Cleanup:** Delete created attribute.

#### ECOM-PROD-008: Product status lifecycle
- **Preconditions:** Product created via API with status `Draft`
- **Steps:**
  1. Navigate to product edit page
  2. Change status from Draft to Active
  3. Save and verify status badge shows "Active"
  4. Change status to Archived
  5. Save and verify status badge shows "Archived"
- **Expected:** Product status transitions work correctly. Status badges update.
- **Cleanup:** Delete product.

#### ECOM-PROD-009: Delete product
- **Preconditions:** Product created via API
- **Steps:**
  1. Navigate to `/portal/ecommerce/products`
  2. Click delete action on the test product
  3. Verify confirmation dialog appears
  4. Confirm deletion
  5. Verify success toast
  6. Verify product is removed from the list
- **Expected:** Soft-delete removes product from list. Confirmation dialog prevents accidental deletion.

#### ECOM-PROD-010: Brands CRUD
- **Preconditions:** Logged in as tenant admin
- **Steps:**
  1. Navigate to `/portal/ecommerce/brands`
  2. Create a new brand: `E2E Brand {timestamp}`
  3. Verify it appears in list
  4. Edit the brand name
  5. Delete the brand
- **Expected:** Brand CRUD works end-to-end.

---

### 3.4 E-commerce: Orders

| ID | Flow | Priority | Tags | Est. |
|----|------|----------|------|------|
| ECOM-ORD-001 | Order list loads with filters | P0 | @smoke | 15s |
| ECOM-ORD-002 | Order detail page | P0 | @smoke | 15s |
| ECOM-ORD-003 | Order lifecycle (confirm to complete) | P0 | @smoke | 30s |
| ECOM-ORD-004 | Manual create order | P1 | @regression | 25s |
| ECOM-ORD-005 | Cancel order | P1 | @regression | 20s |
| ECOM-ORD-006 | Order bulk actions | P2 | @nightly | 20s |
| ECOM-ORD-007 | Order status filter | P1 | @regression | 15s |

#### ECOM-ORD-001: Order list loads with filters
- **Preconditions:** Logged in. At least 1 order exists.
- **Steps:**
  1. Navigate to `/portal/ecommerce/orders`
  2. Verify order table loads with columns: Order #, Customer, Status, Total, Date
  3. Verify at least one order row is visible
- **Expected:** Order list renders correctly with data.

#### ECOM-ORD-002: Order detail page
- **Preconditions:** At least 1 order exists
- **Steps:**
  1. Navigate to `/portal/ecommerce/orders`
  2. Click on an order row
  3. Verify redirect to `/portal/ecommerce/orders/{id}`
  4. Verify order detail sections: items, customer info, status timeline, payment info
- **Expected:** Order detail page shows all sections with correct data.

#### ECOM-ORD-003: Order lifecycle (confirm to complete)
- **Preconditions:** Order in "Pending" status (seed via API or manual create)
- **Steps:**
  1. Navigate to order detail page
  2. Click "Confirm" action button
  3. Verify status changes to "Confirmed"
  4. Click "Process" action
  5. Verify status changes to "Processing"
  6. Click "Ship" action (enter tracking info if prompted)
  7. Verify status changes to "Shipped"
  8. Click "Deliver" action
  9. Verify status changes to "Delivered"
  10. Click "Complete" action
  11. Verify status changes to "Completed"
- **Expected:** Order progresses through all lifecycle states. Status badge updates after each action. Activity timeline records each transition.

#### ECOM-ORD-004: Manual create order
- **Preconditions:** At least 1 product and 1 customer exist (seed via API)
- **Steps:**
  1. Navigate to `/portal/ecommerce/orders/create`
  2. Search and select a customer
  3. Add a product line item
  4. Set quantity
  5. Review order summary
  6. Click "Create Order"
  7. Verify success notification
  8. Verify redirect to order detail page
- **Expected:** Order is created with correct line items and customer. Status is "Pending".
- **Cleanup:** Cancel and delete order.

#### ECOM-ORD-005: Cancel order
- **Preconditions:** Order in "Pending" or "Confirmed" status
- **Steps:**
  1. Navigate to order detail page
  2. Click "Cancel" action
  3. Verify confirmation dialog appears
  4. Confirm cancellation
  5. Verify status changes to "Cancelled"
  6. Verify no further status actions are available
- **Expected:** Order is cancelled. Status badge shows "Cancelled". No forward transitions available.

#### ECOM-ORD-006: Order bulk actions
- **Preconditions:** Multiple orders exist (seed via API)
- **Steps:**
  1. Navigate to `/portal/ecommerce/orders`
  2. Select multiple orders using checkboxes
  3. Verify bulk action toolbar appears
  4. Select a bulk action (e.g., "Confirm Selected")
  5. Confirm the action
  6. Verify selected orders are updated
- **Expected:** Bulk action toolbar shows correct count. Action applies to all selected orders.

#### ECOM-ORD-007: Order status filter
- **Preconditions:** Orders in various statuses exist
- **Steps:**
  1. Navigate to `/portal/ecommerce/orders`
  2. Select status filter "Pending"
  3. Verify only pending orders are shown
  4. Switch to "Completed"
  5. Verify only completed orders are shown
  6. Clear filter, verify all orders shown
- **Expected:** Status filter correctly narrows results. Clearing shows all.

---

### 3.5 E-commerce: Customers

| ID | Flow | Priority | Tags | Est. |
|----|------|----------|------|------|
| CUST-001 | Customer list loads | P0 | @smoke | 15s |
| CUST-002 | Create customer via UI | P0 | @smoke | 20s |
| CUST-003 | Customer validation errors | P1 | @regression | 10s |
| CUST-004 | Edit customer | P1 | @regression | 20s |
| CUST-005 | Customer detail page | P1 | @regression | 15s |
| CUST-006 | Delete customer | P1 | @regression | 15s |
| CUST-007 | Customer search and filter | P1 | @regression | 15s |

#### CUST-001: Customer list loads
- **Preconditions:** Logged in. At least 1 customer exists (seed via API).
- **Steps:**
  1. Navigate to `/portal/ecommerce/customers`
  2. Verify customer table renders with at least one row
  3. Verify no error states
- **Expected:** Customer list page loads with table data.

#### CUST-002: Create customer via UI
- **Preconditions:** Logged in as tenant admin
- **Steps:**
  1. Navigate to `/portal/ecommerce/customers`
  2. Click "Create" / "Add" button
  3. Fill in: First Name, Last Name, Email, Phone
  4. Click Save
  5. Verify success toast
  6. Verify customer appears in list
- **Expected:** Customer is created. Success toast shown.
- **Cleanup:** Delete customer via API.

#### CUST-003: Customer validation errors
- **Preconditions:** Logged in
- **Steps:**
  1. Navigate to customer create form
  2. Submit without filling required fields
- **Expected:** Validation errors for required fields. Form not submitted.

#### CUST-004: Edit customer
- **Preconditions:** Customer created via API
- **Steps:**
  1. Navigate to `/portal/ecommerce/customers`
  2. Click edit on the test customer
  3. Update the last name
  4. Save and verify success
- **Expected:** Customer data is updated. Updated name shown in list.
- **Cleanup:** Delete customer.

#### CUST-005: Customer detail page
- **Preconditions:** Customer created via API
- **Steps:**
  1. Navigate to `/portal/ecommerce/customers/{id}`
  2. Verify customer info section (name, email, phone)
  3. Verify tabs or sections (orders, addresses, activity)
- **Expected:** Customer detail page shows all sections with correct data.
- **Cleanup:** Delete customer.

#### CUST-006: Delete customer
- **Preconditions:** Customer created via API
- **Steps:**
  1. Navigate to customer list
  2. Click delete on test customer
  3. Confirm deletion
  4. Verify customer removed from list
- **Expected:** Soft-delete removes customer. Confirmation dialog prevents accidental deletion.

#### CUST-007: Customer search and filter
- **Preconditions:** Multiple customers exist (seed via API)
- **Steps:**
  1. Navigate to `/portal/ecommerce/customers`
  2. Type a search query matching one customer
  3. Verify filtered results
  4. Clear search, verify all customers shown
- **Expected:** Search filters the list correctly.
- **Cleanup:** Delete seeded customers.

---

### 3.6 E-commerce: Customer Groups

| ID | Flow | Priority | Tags | Est. |
|----|------|----------|------|------|
| CGRP-001 | Customer group list loads | P0 | @smoke | 10s |
| CGRP-002 | Customer group CRUD | P1 | @regression | 25s |
| CGRP-003 | Customer group validation errors | P1 | @regression | 10s |

#### CGRP-001: Customer group list loads
- **Preconditions:** Logged in
- **Steps:**
  1. Navigate to `/portal/ecommerce/customer-groups`
  2. Verify page loads without errors
- **Expected:** Customer groups list page renders.

#### CGRP-002: Customer group CRUD
- **Preconditions:** Logged in as tenant admin
- **Steps:**
  1. Navigate to customer groups page
  2. Create a group: `E2E TestGroup {timestamp}`
  3. Verify it appears in list
  4. Edit the group name
  5. Save and verify
  6. Delete the group with confirmation
  7. Verify removed from list
- **Expected:** Full CRUD lifecycle works for customer groups.

#### CGRP-003: Customer group validation errors
- **Preconditions:** Logged in
- **Steps:**
  1. Open create customer group form
  2. Submit without filling name
- **Expected:** Validation error for required name field.

---

### 3.7 E-commerce: Reviews

| ID | Flow | Priority | Tags | Est. |
|----|------|----------|------|------|
| REV-001 | Review list loads | P0 | @smoke | 10s |
| REV-002 | Approve pending review | P1 | @regression | 20s |
| REV-003 | Reject pending review | P1 | @regression | 20s |
| REV-004 | Delete review | P1 | @regression | 15s |
| REV-005 | Filter reviews by status | P1 | @regression | 10s |

#### REV-001: Review list loads
- **Preconditions:** Logged in. Reviews module accessible.
- **Steps:**
  1. Navigate to `/portal/ecommerce/reviews`
  2. Verify review moderation page loads
- **Expected:** Reviews list page renders without errors.

#### REV-002: Approve pending review
- **Preconditions:** Product and review created via API (review in Pending status)
- **Steps:**
  1. Navigate to reviews page
  2. Find the pending review row
  3. Click "Approve" button
  4. Verify success toast
- **Expected:** Review is approved. Status badge updates.
- **Cleanup:** Delete review and product.

#### REV-003: Reject pending review
- **Preconditions:** Product and review created via API
- **Steps:**
  1. Navigate to reviews page
  2. Find the pending review
  3. Click "Reject"
  4. Fill reason if dialog appears
  5. Confirm
- **Expected:** Review is rejected with reason. Status badge updates.
- **Cleanup:** Delete review and product.

#### REV-004: Delete review
- **Preconditions:** Product and review created via API
- **Steps:**
  1. Navigate to reviews page
  2. Click delete on the test review
  3. Confirm deletion
  4. Verify review removed from list
- **Expected:** Review is deleted. Confirmation dialog prevents accidental deletion.
- **Cleanup:** Delete product.

#### REV-005: Filter reviews by status
- **Preconditions:** Logged in
- **Steps:**
  1. Navigate to reviews page
  2. Filter by "Pending" status
  3. Verify page loads without errors
- **Expected:** Status filter narrows results.

---

### 3.8 Marketing: Promotions

| ID | Flow | Priority | Tags | Est. |
|----|------|----------|------|------|
| PROMO-001 | Promotion list loads | P0 | @smoke | 10s |
| PROMO-002 | Create promotion via UI | P0 | @smoke | 20s |
| PROMO-003 | Edit promotion | P1 | @regression | 20s |
| PROMO-004 | Delete promotion | P1 | @regression | 15s |
| PROMO-005 | Promotion validation errors | P1 | @regression | 10s |
| PROMO-006 | Promotion status display | P1 | @regression | 15s |

#### PROMO-001: Promotion list loads
- **Preconditions:** Logged in. Marketing module accessible.
- **Steps:**
  1. Navigate to `/portal/marketing/promotions`
  2. Verify promotions table loads
- **Expected:** Promotions page renders.

#### PROMO-002: Create promotion via UI
- **Preconditions:** Logged in as tenant admin
- **Steps:**
  1. Navigate to promotions page
  2. Click "Create" button
  3. Fill in: Name, Code, Discount Value
  4. Save
  5. Verify success toast
  6. Verify promotion appears in list
- **Expected:** Promotion is created with correct code and discount.
- **Cleanup:** Delete promotion via API.

#### PROMO-003: Edit promotion
- **Preconditions:** Promotion created via API
- **Steps:**
  1. Navigate to promotions page
  2. Click edit on the test promotion
  3. Update the name
  4. Save and verify success
- **Expected:** Promotion data is updated.
- **Cleanup:** Delete promotion.

#### PROMO-004: Delete promotion
- **Preconditions:** Promotion created via API
- **Steps:**
  1. Navigate to promotions page
  2. Click delete on the test promotion
  3. Confirm deletion
  4. Verify removed from list
- **Expected:** Promotion is deleted. Confirmation dialog prevents accidental deletion.

#### PROMO-005: Promotion validation errors
- **Preconditions:** Logged in
- **Steps:**
  1. Open create promotion form
  2. Submit without filling required fields
- **Expected:** Validation errors for required fields.

#### PROMO-006: Promotion status display
- **Preconditions:** Promotion created via API
- **Steps:**
  1. Navigate to promotions page
  2. Verify the promotion row shows status badge (Active/Inactive)
- **Expected:** Promotion status badge displays correctly.
- **Cleanup:** Delete promotion.

---

### 3.9 Notifications

| ID | Flow | Priority | Tags | Est. |
|----|------|----------|------|------|
| NOTIF-001 | Notifications page loads | P1 | @regression | 10s |
| NOTIF-002 | Notification settings page | P1 | @regression | 10s |

#### NOTIF-001: Notifications page loads
- **Preconditions:** Logged in
- **Steps:**
  1. Navigate to `/portal/notifications`
  2. Verify page loads without errors
- **Expected:** Notifications page renders.

#### NOTIF-002: Notification settings page
- **Preconditions:** Logged in
- **Steps:**
  1. Navigate to `/portal/settings/notifications`
  2. Verify settings page loads with toggle switches or checkboxes
- **Expected:** Notification preferences page renders with configurable options.

---

### 3.10 CRM

| ID | Flow | Priority | Tags | Est. |
|----|------|----------|------|------|
| CRM-001 | Contact CRUD | P0 | @smoke | 20s |
| CRM-002 | Contact validation errors | P1 | @regression | 10s |
| CRM-003 | Company CRUD | P1 | @regression | 20s |
| CRM-004 | Pipeline kanban loads | P0 | @smoke | 15s |
| CRM-005 | Lead lifecycle (create to win) | P0 | @smoke | 25s |
| CRM-006 | Lead lose flow | P1 | @regression | 15s |
| CRM-007 | Activity log on contact | P1 | @regression | 15s |
| CRM-008 | Pipeline management | P2 | @nightly | 20s |
| CRM-009 | Contact detail page | P1 | @regression | 15s |

#### CRM-001: Contact CRUD
- **Preconditions:** Logged in as tenant admin
- **Steps:**
  1. Navigate to `/portal/crm/contacts`
  2. Click "Create Contact"
  3. Fill in: First Name `E2E`, Last Name `Contact {timestamp}`, Email `e2e-{timestamp}@test.com`
  4. Save
  5. Verify contact appears in list
  6. Click on the contact to open detail
  7. Edit the contact (change last name)
  8. Save and verify update
  9. Navigate back to list
  10. Delete the contact (with confirmation)
- **Expected:** Full CRUD works. Contact appears in list after create, disappears after delete.

#### CRM-002: Contact validation errors
- **Preconditions:** Logged in
- **Steps:**
  1. Navigate to `/portal/crm/contacts`
  2. Click "Create Contact"
  3. Leave required fields empty
  4. Click Save
- **Expected:** Validation errors for required fields (first name, last name, email). Form not submitted.

#### CRM-003: Company CRUD
- **Preconditions:** Logged in as tenant admin
- **Steps:**
  1. Navigate to `/portal/crm/companies`
  2. Create a company: `E2E Company {timestamp}`
  3. Verify it appears in list
  4. Click to view detail at `/portal/crm/companies/{id}`
  5. Edit company name
  6. Save and verify
  7. Delete the company
- **Expected:** Company CRUD lifecycle works.

#### CRM-004: Pipeline kanban loads
- **Preconditions:** Logged in. Default pipeline exists.
- **Steps:**
  1. Navigate to `/portal/crm/pipeline`
  2. Verify kanban board renders with stage columns
  3. Verify column headers match pipeline stages (e.g., "New", "Qualified", "Proposal", "Won", "Lost")
  4. Check that existing leads (if any) are displayed as cards
- **Expected:** Kanban board loads with correct stage columns. Cards are draggable (visual check).

#### CRM-005: Lead lifecycle (create to win)
- **Preconditions:** Contact created via API. Default pipeline exists.
- **Steps:**
  1. Navigate to `/portal/crm/pipeline`
  2. Create a new lead: title `E2E Deal {timestamp}`, associate with the test contact, value `500000`
  3. Verify lead card appears in the first stage column
  4. Click on the lead card to open deal detail
  5. Progress the lead through stages (move to "Qualified", then "Proposal")
  6. Mark the lead as "Won"
  7. Verify lead moves to "Won" column
  8. Verify that a Customer is auto-created from the contact data
- **Expected:** Lead progresses through pipeline. Winning auto-creates a customer.
- **Cleanup:** Delete lead and contact. Verify customer was created (see DATA-LINK-002).

#### CRM-006: Lead lose flow
- **Preconditions:** Contact and lead created via API
- **Steps:**
  1. Navigate to `/portal/crm/pipeline`
  2. Find the test lead
  3. Open lead detail
  4. Mark the lead as "Lost" with a reason
  5. Verify lead moves to "Lost" column
- **Expected:** Lead is marked lost. Lost reason is saved. No customer auto-created.
- **Cleanup:** Delete lead and contact.

#### CRM-007: Activity log on contact
- **Preconditions:** Contact created via API
- **Steps:**
  1. Navigate to `/portal/crm/contacts/{id}` (contact detail)
  2. Locate the activity section
  3. Add a new activity (e.g., type: Call, note: "Follow-up call")
  4. Verify the activity appears in the activity timeline
- **Expected:** Activity is created and visible on the contact detail page.
- **Cleanup:** Delete contact.

#### CRM-008: Pipeline management
- **Preconditions:** Logged in as tenant admin
- **Steps:**
  1. Navigate to pipeline settings/management
  2. Create a new pipeline: `E2E Pipeline {timestamp}`
  3. Add stages to the pipeline
  4. Verify the pipeline appears in pipeline selector
  5. Delete the pipeline
- **Expected:** Pipeline CRUD works. Stages are configurable.

#### CRM-009: Contact detail page
- **Preconditions:** Contact created via API with associated leads and activities
- **Steps:**
  1. Navigate to `/portal/crm/contacts/{id}`
  2. Verify contact info section (name, email, phone)
  3. Verify associated leads section
  4. Verify activity timeline section
- **Expected:** Detail page shows all sections with correct data.
- **Cleanup:** Delete contact.

---

### 3.11 HR

| ID | Flow | Priority | Tags | Est. |
|----|------|----------|------|------|
| HR-001 | Employee CRUD | P0 | @smoke | 20s |
| HR-002 | Employee validation errors | P1 | @regression | 10s |
| HR-003 | Department CRUD | P1 | @regression | 20s |
| HR-004 | Org chart renders | P1 | @regression | 15s |
| HR-005 | Tag management | P1 | @regression | 20s |
| HR-006 | Bulk assign tags | P2 | @nightly | 20s |
| HR-007 | Import employees (CSV) | P2 | @nightly | 25s |
| HR-008 | Export employees (Excel) | P2 | @nightly | 15s |
| HR-009 | HR reports page | P2 | @nightly | 15s |

#### HR-001: Employee CRUD
- **Preconditions:** Logged in. At least 1 department exists (seed via API).
- **Steps:**
  1. Navigate to `/portal/hr/employees`
  2. Click "Create Employee"
  3. Fill in: First Name `E2E`, Last Name `Employee {timestamp}`, Email `e2e-emp-{timestamp}@test.com`
  4. Select a department
  5. Save
  6. Verify employee appears in list with auto-generated employee code
  7. Click to view detail at `/portal/hr/employees/{id}`
  8. Edit the employee (change department)
  9. Save and verify update
  10. Navigate back to list
  11. Delete the employee
- **Expected:** Full CRUD works. Employee code is auto-generated (e.g., EMP-0001).
- **Cleanup:** Delete employee and department.

#### HR-002: Employee validation errors
- **Preconditions:** Logged in
- **Steps:**
  1. Navigate to employee create form
  2. Leave required fields empty
  3. Submit
- **Expected:** Validation errors for first name, last name, email. Form not submitted.

#### HR-003: Department CRUD
- **Preconditions:** Logged in as tenant admin
- **Steps:**
  1. Navigate to `/portal/hr/departments`
  2. Create department: `E2E Department {timestamp}`
  3. Verify it appears in the list
  4. Edit the department name
  5. Save and verify
  6. Delete the department
- **Expected:** Department CRUD lifecycle works.

#### HR-004: Org chart renders
- **Preconditions:** At least 2 employees exist with manager/report relationship (seed via API)
- **Steps:**
  1. Navigate to `/portal/hr/org-chart`
  2. Wait for d3-org-chart to render
  3. Verify at least one node (card) is visible
  4. Verify hierarchy is displayed (parent-child relationship)
- **Expected:** Org chart renders with employee cards. Hierarchy is visually correct.
- **Cleanup:** Delete seeded employees.

#### HR-005: Tag management
- **Preconditions:** Logged in as tenant admin
- **Steps:**
  1. Navigate to `/portal/hr/tags`
  2. Create a new tag: Name `E2E Tag {timestamp}`, Category: `Skill`, Color: `#3B82F6`
  3. Verify tag appears in the list
  4. Edit the tag name
  5. Delete the tag
- **Expected:** Tag CRUD works. Color and category are displayed correctly.

#### HR-006: Bulk assign tags
- **Preconditions:** Multiple employees and at least 1 tag exist (seed via API)
- **Steps:**
  1. Navigate to `/portal/hr/employees`
  2. Select multiple employees via checkboxes
  3. Click bulk action "Assign Tags"
  4. Select a tag from the dropdown
  5. Confirm
  6. Verify selected employees now have the tag
- **Expected:** Bulk tag assignment applies to all selected employees.
- **Cleanup:** Delete employees and tag.

#### HR-007: Import employees (CSV)
- **Preconditions:** Logged in. A valid CSV file is prepared with employee data.
- **Steps:**
  1. Navigate to `/portal/hr/employees`
  2. Click "Import" button
  3. Upload a CSV file with 2-3 employee records
  4. Verify import preview/summary
  5. Confirm import
  6. Verify employees appear in the list
- **Expected:** CSV import creates employees with correct data. Errors reported per row if any.
- **Cleanup:** Delete imported employees.

#### HR-008: Export employees (Excel)
- **Preconditions:** At least 1 employee exists
- **Steps:**
  1. Navigate to `/portal/hr/employees`
  2. Click "Export" button
  3. Select format: Excel
  4. Verify file download starts
  5. Verify downloaded file has content (non-zero size)
- **Expected:** Excel file downloads with employee data.

#### HR-009: HR reports page
- **Preconditions:** Some employee data exists
- **Steps:**
  1. Navigate to `/portal/hr/reports`
  2. Verify report sections load (headcount, by department, by employment type)
  3. Check that numbers are non-negative
- **Expected:** Report page renders with aggregate statistics.

---

### 3.12 Admin

| ID | Flow | Priority | Tags | Est. |
|----|------|----------|------|------|
| ADMIN-001 | User list loads | P0 | @smoke | 10s |
| ADMIN-002 | Create user | P1 | @regression | 20s |
| ADMIN-003 | Edit user role | P1 | @regression | 15s |
| ADMIN-004 | Role CRUD | P1 | @regression | 20s |
| ADMIN-005 | Permission enforcement (UI) | P1 | @regression | 20s |
| ADMIN-006 | Feature management toggle | P1 | @regression | 20s |
| ADMIN-007 | Tenant settings page | P0 | @smoke | 15s |
| ADMIN-008 | Personal settings | P1 | @regression | 15s |
| ADMIN-009 | Global search (Cmd+K) | P0 | @smoke | 15s |
| ADMIN-010 | Media manager CRUD | P1 | @regression | 25s |
| ADMIN-011 | Activity timeline page | P2 | @nightly | 15s |
| ADMIN-012 | Email templates list | P2 | @nightly | 15s |

#### ADMIN-001: User list loads
- **Preconditions:** Logged in as tenant admin
- **Steps:**
  1. Navigate to `/portal/admin/users`
  2. Verify user table loads with at least the admin user
  3. Check columns: Name, Email, Role, Status
- **Expected:** User list renders with correct data.

#### ADMIN-002: Create user
- **Preconditions:** Logged in as tenant admin
- **Steps:**
  1. Navigate to `/portal/admin/users`
  2. Click "Create User"
  3. Fill in: First Name `E2E`, Last Name `User`, Email `e2e-user-{timestamp}@noir.local`, Password `Test123!@#`
  4. Assign a role
  5. Save
  6. Verify user appears in list
- **Expected:** User is created with correct role assignment.
- **Cleanup:** Delete user via API.

#### ADMIN-003: Edit user role
- **Preconditions:** User created via API
- **Steps:**
  1. Navigate to `/portal/admin/users`
  2. Click edit on the test user
  3. Change the assigned role
  4. Save
  5. Verify role is updated in the list
- **Expected:** User role is updated.
- **Cleanup:** Delete user.

#### ADMIN-004: Role CRUD
- **Preconditions:** Logged in as tenant admin
- **Steps:**
  1. Navigate to `/portal/admin/roles`
  2. Click "Create Role"
  3. Fill in name: `E2E Role {timestamp}`
  4. Select a few permissions
  5. Save
  6. Verify role appears in list
  7. Edit the role (change name, toggle permissions)
  8. Save and verify
  9. Delete the role (with confirmation)
- **Expected:** Role CRUD works. Permissions are toggleable.

#### ADMIN-005: Permission enforcement (UI)
- **Preconditions:** A role with limited permissions exists (e.g., only "Products Read"). A user assigned to that role.
- **Steps:**
  1. Login as the limited user
  2. Navigate to `/portal`
  3. Verify sidebar only shows permitted menu items
  4. Try navigating directly to `/portal/admin/users` (no permission)
  5. Verify access denied or redirect
- **Expected:** Sidebar reflects permissions. Direct URL access to unpermitted pages is blocked.
- **Cleanup:** Delete user and role.

#### ADMIN-006: Feature management toggle
- **Preconditions:** Logged in as platform admin
- **Steps:**
  1. Navigate to `/portal/admin/platform-settings`
  2. Find the Modules/Features tab
  3. Toggle a module off (e.g., Blog)
  4. Save
  5. Navigate to sidebar
  6. Verify the Blog menu item is hidden
  7. Toggle the module back on
  8. Verify Blog menu item reappears
- **Expected:** Feature toggles hide/show sidebar items and gate access to module pages.

#### ADMIN-007: Tenant settings page
- **Preconditions:** Logged in as tenant admin
- **Steps:**
  1. Navigate to `/portal/admin/tenant-settings`
  2. Verify page loads with tabs (General, Shipping, Payment Gateways, etc.)
  3. Switch between tabs
  4. Verify each tab loads content without errors
- **Expected:** Tenant settings page loads all tabs correctly.

#### ADMIN-008: Personal settings
- **Preconditions:** Logged in
- **Steps:**
  1. Navigate to `/portal/settings`
  2. Verify personal settings form loads (name, email, theme, language)
  3. Change theme (e.g., light to dark)
  4. Verify theme changes immediately
  5. Change language to Vietnamese
  6. Verify UI text changes to Vietnamese
  7. Revert to English
- **Expected:** Personal settings (theme, language) apply immediately.

#### ADMIN-009: Global search (Cmd+K)
- **Preconditions:** Logged in. Some data exists.
- **Steps:**
  1. Press `Cmd+K` (or `Ctrl+K` on Windows)
  2. Verify command palette opens
  3. Type a search query (e.g., "products")
  4. Verify search results appear (pages and/or content results)
  5. Click on a result
  6. Verify navigation to the correct page
  7. Press Escape to close palette
- **Expected:** Command palette opens, searches work, results navigate correctly.

#### ADMIN-010: Media manager CRUD
- **Preconditions:** Logged in with Media permissions
- **Steps:**
  1. Navigate to `/portal/media`
  2. Verify media library page loads
  3. Upload a small test image
  4. Verify the image appears in the library
  5. Click to preview the image
  6. Delete the image (with confirmation)
  7. Verify it is removed
- **Expected:** Media upload, preview, and delete work correctly.

#### ADMIN-011: Activity timeline page
- **Preconditions:** Logged in. Some mutations have been performed (creating entities).
- **Steps:**
  1. Navigate to `/portal/activity-timeline`
  2. Verify timeline loads with recent activities
  3. Verify each entry shows: action, entity, user, timestamp
- **Expected:** Activity timeline shows recent audit entries.

#### ADMIN-012: Email templates list
- **Preconditions:** Logged in as tenant admin
- **Steps:**
  1. Navigate to `/portal/admin/tenant-settings`
  2. Switch to email templates tab
  3. Verify template list loads
  4. Click on a template to edit
  5. Verify redirect to `/portal/email-templates/{id}`
  6. Verify editor loads with template content
- **Expected:** Email template list and editor load correctly.

---

### 3.13 Blog CMS

| ID | Flow | Priority | Tags | Est. |
|----|------|----------|------|------|
| BLOG-001 | Blog post list loads | P0 | @smoke | 10s |
| BLOG-002 | Create blog post | P0 | @smoke | 25s |
| BLOG-003 | Edit blog post with rich content | P1 | @regression | 25s |
| BLOG-004 | Blog categories CRUD | P1 | @regression | 15s |
| BLOG-005 | Blog tags CRUD | P1 | @regression | 15s |
| BLOG-006 | Delete blog post | P1 | @regression | 15s |

#### BLOG-001: Blog post list loads
- **Preconditions:** Logged in. Blog module enabled.
- **Steps:**
  1. Navigate to `/portal/blog/posts`
  2. Verify post table loads
  3. Check columns: Title, Status, Author, Date
- **Expected:** Blog post list renders correctly.

#### BLOG-002: Create blog post
- **Preconditions:** Logged in. At least 1 blog category exists.
- **Steps:**
  1. Navigate to `/portal/blog/posts`
  2. Click "New Post"
  3. Verify redirect to `/portal/blog/posts/new`
  4. Fill in Title: `E2E Blog Post {timestamp}`
  5. Wait for Tiptap editor to initialize (contenteditable becomes true)
  6. Type body content in the editor
  7. Select a category
  8. Set status to Draft
  9. Click "Save" / "Publish"
  10. Verify success toast
- **Expected:** Blog post is created. Appears in post list.
- **Cleanup:** Delete post via API.

#### BLOG-003: Edit blog post with rich content
- **Preconditions:** Blog post created via API
- **Steps:**
  1. Navigate to `/portal/blog/posts/{id}/edit`
  2. Wait for Tiptap editor to load with existing content
  3. Modify the title
  4. Add formatted content (bold, heading, link) in the editor
  5. Save
  6. Verify changes are persisted
- **Expected:** Rich content is saved and loaded correctly on re-edit.
- **Cleanup:** Delete post.

#### BLOG-004: Blog categories CRUD
- **Preconditions:** Logged in
- **Steps:**
  1. Navigate to `/portal/blog/categories`
  2. Create category: `E2E Blog Cat {timestamp}`
  3. Verify it appears in list
  4. Edit the category
  5. Delete the category
- **Expected:** Blog category CRUD works.

#### BLOG-005: Blog tags CRUD
- **Preconditions:** Logged in
- **Steps:**
  1. Navigate to `/portal/blog/tags`
  2. Create tag: `E2E Blog Tag {timestamp}`
  3. Verify it appears in list
  4. Delete the tag
- **Expected:** Blog tag CRUD works.

#### BLOG-006: Delete blog post
- **Preconditions:** Blog post created via API
- **Steps:**
  1. Navigate to `/portal/blog/posts`
  2. Click delete on the test post
  3. Confirm deletion dialog
  4. Verify post removed from list
- **Expected:** Post is soft-deleted. Confirmation dialog prevents accidental deletion.

---

### 3.14 Inventory

| ID | Flow | Priority | Tags | Est. |
|----|------|----------|------|------|
| INV-001 | Create inventory receipt | P1 | @regression | 20s |
| INV-002 | Confirm inventory receipt | P1 | @regression | 15s |
| INV-003 | Cancel inventory receipt | P2 | @nightly | 15s |

#### INV-001: Create inventory receipt
- **Preconditions:** Logged in. At least 1 product variant exists (seed via API).
- **Steps:**
  1. Navigate to `/portal/ecommerce/inventory`
  2. Click "Create Receipt"
  3. Select type: Stock In (RCV-)
  4. Add a line item: select product variant, quantity 10
  5. Save as Draft
  6. Verify receipt appears in list with "Draft" status
- **Expected:** Inventory receipt created with auto-generated code (RCV-*).
- **Cleanup:** Delete receipt.

#### INV-002: Confirm inventory receipt
- **Preconditions:** Draft inventory receipt exists
- **Steps:**
  1. Navigate to receipt detail
  2. Click "Confirm"
  3. Verify status changes from "Draft" to "Confirmed"
  4. Verify inventory quantities are updated
- **Expected:** Confirming a receipt changes status and adjusts inventory.
- **Cleanup:** Delete or reverse receipt.

#### INV-003: Cancel inventory receipt
- **Preconditions:** Draft inventory receipt exists
- **Steps:**
  1. Navigate to receipt detail
  2. Click "Cancel"
  3. Confirm cancellation
  4. Verify status changes to "Cancelled"
- **Expected:** Receipt is cancelled. No inventory impact.

---

### 3.15 Project Management

| ID | Flow | Priority | Tags | Est. |
|----|------|----------|------|------|
| PM-001 | Project list loads | P1 | @regression | 10s |
| PM-002 | Create project | P1 | @regression | 15s |
| PM-003 | Project detail and task board | P1 | @regression | 20s |

#### PM-001: Project list loads
- **Preconditions:** Logged in. PM module enabled.
- **Steps:**
  1. Navigate to `/portal/projects`
  2. Verify project list/grid loads
- **Expected:** Projects page renders.

#### PM-002: Create project
- **Preconditions:** Logged in
- **Steps:**
  1. Navigate to `/portal/projects`
  2. Click "Create Project"
  3. Fill in Name: `E2E Project {timestamp}`
  4. Save
  5. Verify project appears in list
- **Expected:** Project is created.
- **Cleanup:** Delete project.

#### PM-003: Project detail and task board
- **Preconditions:** Project created via API
- **Steps:**
  1. Navigate to `/portal/projects/{id}`
  2. Verify project detail loads with task board / task list
  3. Create a new task within the project
  4. Verify task appears on the board
- **Expected:** Project detail shows tasks. Task creation works.
- **Cleanup:** Delete project.

---

## 4. Data Linking Tests (Cross-Module)

These tests verify that data created in one module is correctly reflected in another.

| ID | Flow | Priority | Tags | Est. |
|----|------|----------|------|------|
| DATA-LINK-001 | Customer order history | P0 | @smoke | 30s |
| DATA-LINK-002 | Win lead auto-creates customer | P0 | @smoke | 30s |
| DATA-LINK-003 | Product in order affects inventory | P1 | @regression | 30s |
| DATA-LINK-004 | Employee department org chart | P1 | @regression | 25s |
| DATA-LINK-005 | Role permissions control UI access | P1 | @regression | 25s |
| DATA-LINK-006 | Feature toggle gates module access | P1 | @regression | 20s |
| DATA-LINK-007 | Category → Product association | P1 | @regression | 25s |
| DATA-LINK-008 | Promotion → Order discount applied | P1 | @regression | 30s |
| DATA-LINK-009 | Review → Product name linked | P1 | @regression | 25s |
| DATA-LINK-010 | Customer group membership | P2 | @nightly | 25s |

#### DATA-LINK-001: Customer order history
- **Preconditions:** None (creates all data)
- **Steps:**
  1. Create a customer via API: `E2E DataLink Customer`
  2. Create a product via API: `E2E DataLink Product`
  3. Create an order via API associated with the customer
  4. Navigate to `/portal/ecommerce/customers/{customerId}`
  5. Verify the customer detail page loads
  6. Switch to orders tab or section
  7. Verify the created order appears in the customer's order history
- **Expected:** Customer detail page shows the order. Order total and status are correct.
- **Cleanup:** Delete order, product, customer.

#### DATA-LINK-002: Win lead auto-creates customer
- **Preconditions:** None (creates all data)
- **Steps:**
  1. Create a CRM contact via API: `E2E DataLink Contact`, email `datalink-{ts}@test.com`
  2. Create a lead via API associated with the contact
  3. Navigate to the lead detail page
  4. Mark the lead as "Won"
  5. Navigate to `/portal/ecommerce/customers`
  6. Search for the contact's email
  7. Verify a customer was auto-created with matching name and email
- **Expected:** Winning a lead auto-creates a Customer entity. Customer appears in the customers list.
- **Cleanup:** Delete customer, lead, contact.

#### DATA-LINK-003: Product in order affects inventory
- **Preconditions:** Product with variant exists. Inventory receipt confirmed with stock.
- **Steps:**
  1. Create a product via API
  2. Create and confirm an inventory receipt (Stock In, qty 50) via API
  3. Note the current stock level
  4. Create an order with the product (qty 5)
  5. Confirm the order
  6. Navigate to inventory or product detail
  7. Verify stock level decreased by 5
- **Expected:** Order confirmation reduces available inventory for the product.
- **Cleanup:** Delete order, receipt, product.

#### DATA-LINK-004: Employee department org chart
- **Preconditions:** None (creates all data)
- **Steps:**
  1. Create a department via API: `E2E DataLink Dept`
  2. Create an employee via API assigned to the department, with role "Manager"
  3. Create a second employee via API in the same department, with manager = first employee
  4. Navigate to `/portal/hr/org-chart`
  5. Verify both employees appear in the chart
  6. Verify the second employee is shown as reporting to the first
- **Expected:** Org chart reflects the manager-report hierarchy within the department.
- **Cleanup:** Delete employees, department.

#### DATA-LINK-005: Role permissions control UI access
- **Preconditions:** None (creates all data)
- **Steps:**
  1. Create a role via API with ONLY "Products Read" permission
  2. Create a user via API assigned to that role
  3. Logout current admin session
  4. Login as the new user
  5. Verify sidebar shows only Products
  6. Navigate to `/portal/ecommerce/products` — verify access
  7. Navigate to `/portal/admin/users` — verify access denied
  8. Navigate to `/portal/crm/contacts` — verify access denied or hidden
- **Expected:** Role permissions correctly restrict sidebar visibility and page access.
- **Cleanup:** Delete user, role. Re-login as admin.

#### DATA-LINK-006: Feature toggle gates module access
- **Preconditions:** Logged in as platform admin
- **Steps:**
  1. Navigate to platform settings
  2. Disable the CRM module
  3. Login as tenant admin
  4. Verify CRM menu items are hidden from sidebar
  5. Navigate directly to `/portal/crm/contacts`
  6. Verify access is denied or page is empty
  7. Re-enable CRM module as platform admin
- **Expected:** Feature toggle controls module availability end-to-end.

#### DATA-LINK-007: Category → Product association
- **Preconditions:** None (creates all data)
- **Steps:**
  1. Create a product category via API: `E2E DataLink Category`
  2. Create a product via API assigned to that category
  3. Navigate to `/portal/ecommerce/categories`
  4. Verify the category shows the correct product count
  5. Navigate to the product detail
  6. Verify the product shows its category assignment
- **Expected:** Products and categories are correctly linked. Category product count reflects reality.
- **Cleanup:** Delete product, category.

#### DATA-LINK-008: Promotion → Order discount applied
- **Preconditions:** None (creates all data)
- **Steps:**
  1. Create a promotion via API: code `E2E-PROMO-{ts}`, 10% discount
  2. Create a customer and product via API
  3. Create an order linked to the customer with the promotion code
  4. Navigate to `/portal/ecommerce/orders/{id}`
  5. Verify order detail shows the applied promotion/discount
- **Expected:** Order detail page reflects the promotion discount. Order total is reduced.
- **Cleanup:** Delete order, promotion, product, customer.

#### DATA-LINK-009: Review → Product name linked
- **Preconditions:** None (creates all data)
- **Steps:**
  1. Create a product via API
  2. Create a review for that product via API
  3. Navigate to `/portal/ecommerce/reviews`
  4. Find the review row
  5. Verify the product name is shown in the review row
- **Expected:** Reviews list shows the linked product name for each review.
- **Cleanup:** Delete review, product.

#### DATA-LINK-010: Customer group membership
- **Preconditions:** None (creates all data)
- **Steps:**
  1. Create a customer group via API
  2. Create a customer via API
  3. Assign the customer to the group (via API or UI)
  4. Navigate to the customer group detail or customer detail
  5. Verify the membership is reflected
- **Expected:** Customer-group relationship is visible in the UI.
- **Cleanup:** Delete customer, customer group.

---

## 5. Test Data Strategy

### Naming Convention
All test-created entities use the prefix `E2E-` or `E2E ` followed by a timestamp to ensure uniqueness:
- Products: `E2E Product 1709312345`
- Contacts: `e2e-contact-1709312345@test.com`
- Departments: `E2E Dept 1709312345`

### Seeding via API
Use `ApiHelper` for fast data setup:
```typescript
const api = new ApiHelper(request);
const product = await api.createProduct({ name: `E2E Product ${Date.now()}`, sku: `E2E-${Date.now()}` });
// ... run tests ...
await api.deleteProduct(product.id);
```

### Cleanup
- `afterAll` blocks delete all created entities
- Use `try/finally` to ensure cleanup runs even on test failure
- Cleanup order respects FK constraints (delete orders before customers, leads before contacts)

---

## 6. Summary Statistics

| Metric | Value |
|--------|-------|
| **Total planned test cases** | 102 |
| **Total test() calls in specs** | 120 (includes additional sub-scenarios) |
| **Spec files** | 21 |
| **P0 (Critical)** | 25 |
| **P1 (Important)** | 55 |
| **P2 (Nice-to-have)** | 22 |
| **@smoke tests** | ~25 (~5 min) |
| **@regression tests** | ~80 (~18 min) |
| **@nightly tests** | 120 (~30 min) |
| **Modules covered** | 15 (Auth, Dashboard, Products, Orders, Customers, Customer Groups, Reviews, Promotions, Notifications, CRM, HR, Admin, Blog, Inventory, PM) |
| **Cross-module tests** | 10 |

### Run Commands

```bash
# Run smoke tests only
npx playwright test --grep @smoke

# Run regression suite
npx playwright test --grep "@smoke|@regression"

# Run full nightly suite (all browsers)
NIGHTLY=true npx playwright test

# Run a specific module
npx playwright test --grep "ECOM-PROD"

# Run with UI mode (debugging)
npx playwright test --ui

# Run with sharding for CI (4 shards)
npx playwright test --shard=1/4
npx playwright test --shard=2/4
npx playwright test --shard=3/4
npx playwright test --shard=4/4
```
