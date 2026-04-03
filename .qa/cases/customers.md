# Customers Domain — Test Cases

> Pages: /portal/customers, /portal/customers/:id, /portal/customer-groups, /portal/promotions, /portal/reviews, /portal/wishlists | Last updated: 2026-04-03 | Git ref: f6f5cd3
> Total: 78 cases | P0: 4 | P1: 38 | P2: 28 | P3: 8

---

## Page: Customers List (`/portal/ecommerce/customers`)

### Happy Path

#### TC-CUS-001: View customer list with stats cards [P1] [smoke]
- **Pre**: Logged in as tenant admin with CustomersRead permission; customers exist in DB
- **Steps**:
  1. Navigate to `/portal/ecommerce/customers`
  2. Observe page header, stats cards, and DataTable
- **Expected**: Page shows 4 stats cards (Total Customers, Active Customers, VIP Customers, Avg Top Spend). DataTable shows customers with columns: Actions, Select, Name, Email, Phone, Segment, Tier, Orders, Total Spent, Points, Created At, Creator. CardDescription shows "Showing X of Y items".
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px
- **Data**: ☐ Stats counts match DB | ☐ Currency formatted correctly (VND)

#### TC-CUS-002: Search customers by name/email [P1] [smoke]
- **Pre**: Multiple customers exist
- **Steps**:
  1. Type a customer name in the search input
  2. Observe table updates
  3. Clear search, type an email fragment
- **Expected**: Table filters reactively. Search is full-width with `pl-9` icon. Content shows opacity transition during search.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CUS-003: Filter by segment [P1] [regression]
- **Pre**: Customers exist with different segments (New, Active, VIP, AtRisk, Dormant, Lost)
- **Steps**:
  1. Open Segment filter dropdown
  2. Select "VIP"
  3. Verify table shows only VIP customers
  4. Select "All" to clear filter
- **Expected**: Filter dropdown shows all 6 segments with translated labels. Table updates with opacity transition. Pagination resets to page 1 on filter change.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CUS-004: Filter by tier [P1] [regression]
- **Pre**: Customers exist with different tiers (Standard, Silver, Gold, Platinum, Diamond)
- **Steps**:
  1. Open Tier filter dropdown
  2. Select "Gold"
  3. Verify results
- **Expected**: Only Gold tier customers shown. Badge uses correct `getTierBadgeClass` color.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CUS-005: Create new customer via dialog [P0] [smoke]
- **Pre**: Has CustomersCreate permission
- **Steps**:
  1. Click "New Customer" button
  2. URL changes to `?dialog=create-customer`
  3. Fill required fields (first name, last name, email)
  4. Click Save
- **Expected**: Customer created. Toast success. Dialog closes. Table refreshes with new customer.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px
- **Data**: ☐ Customer appears in list | ☐ Created At populated

#### TC-CUS-006: Edit customer via actions menu [P1] [smoke]
- **Pre**: Has CustomersUpdate permission; customer exists
- **Steps**:
  1. Click EllipsisVertical icon on a customer row
  2. Click "Edit"
  3. URL changes to `?edit=<customerId>`
  4. Modify first name
  5. Save
- **Expected**: Customer updated. Toast success. Row reflects changes.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CUS-007: Delete customer with confirmation [P1] [smoke]
- **Pre**: Has CustomersDelete permission; customer exists
- **Steps**:
  1. Click EllipsisVertical on a customer row
  2. Click "Delete"
  3. Confirmation dialog appears with destructive border
  4. Confirm deletion
- **Expected**: Customer soft-deleted. Toast success. Row fades out via `fadeOutRow` animation.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CUS-008: Navigate to customer detail by row click [P1] [smoke]
- **Pre**: Customer exists; no rows selected
- **Steps**:
  1. Click on a customer row (not the actions button)
- **Expected**: Navigates to `/portal/ecommerce/customers/<id>`. When rows are selected, row click does NOT navigate.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CUS-009: Sort by column headers [P2] [regression]
- **Pre**: Multiple customers exist
- **Steps**:
  1. Click "Total Spent" column header
  2. Verify ascending sort
  3. Click again for descending
- **Expected**: Rows reorder by total spent. Sort indicator visible on column header.

#### TC-CUS-010: Pagination controls [P1] [regression]
- **Pre**: More than 20 customers exist
- **Steps**:
  1. Verify page 1 shows 20 items (defaultPageSize)
  2. Navigate to page 2
  3. Change page size to 50
- **Expected**: DataTablePagination shows correct page info. PageSizeSelector works. URL params update.

#### TC-CUS-011: Column visibility toggle [P2] [regression]
- **Pre**: Customer list loaded
- **Steps**:
  1. Open Columns dropdown in toolbar
  2. Hide "Phone" column
  3. Verify column hidden
  4. Click "Reset to default" if visible
- **Expected**: Column hidden/shown. Settings persisted in localStorage under `customers` key.

#### TC-CUS-012: Group by segment [P2] [regression]
- **Pre**: Customers with different segments exist
- **Steps**:
  1. Open grouping dropdown in toolbar
  2. Enable "Segment" grouping
- **Expected**: Rows grouped by segment. Group headers show translated segment names via `groupValueFormatter`. Aggregation cells show sum for Orders/TotalSpent/Points.

#### TC-CUS-013: Group by tier [P2] [regression]
- **Pre**: Customers with different tiers exist
- **Steps**:
  1. Enable "Tier" grouping
- **Expected**: Rows grouped by tier with translated labels.

### Bulk Operations

#### TC-CUS-014: Bulk activate inactive customers [P1] [regression]
- **Pre**: Has CustomersManage permission; inactive customers exist
- **Steps**:
  1. Select 3 inactive customers via checkboxes
  2. BulkActionToolbar shows "Activate (3)" button
  3. Click Activate
- **Expected**: Toast success with count. Selection cleared. Customers now active.
- **Data**: ☐ Only inactive customers counted for activate button

#### TC-CUS-015: Bulk deactivate active customers [P1] [regression]
- **Pre**: Has CustomersManage permission; active customers exist
- **Steps**:
  1. Select 2 active customers
  2. Click "Deactivate (2)"
- **Expected**: Toast success. Customers deactivated.

#### TC-CUS-016: Bulk delete with confirmation dialog [P1] [regression]
- **Pre**: Has CustomersDelete permission; customers exist
- **Steps**:
  1. Select 3 customers
  2. Click "Delete (3)"
  3. Confirmation dialog appears with destructive styling
  4. Confirm
- **Expected**: Dialog shows count and warning. On confirm, toast with results. Selection cleared.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CUS-017: Bulk activate shows warning when no inactive selected [P2] [edge-case]
- **Pre**: Select only active customers
- **Steps**:
  1. Select 2 active customers
  2. Observe BulkActionToolbar
- **Expected**: "Activate" button NOT shown (selectedInactiveCount === 0). Only "Deactivate" and "Delete" visible.

### Import/Export

#### TC-CUS-018: Import customers via CSV [P1] [regression]
- **Pre**: Has CustomersCreate permission
- **Steps**:
  1. Click ImportExportDropdown
  2. Select "Import CSV"
  3. Upload valid CSV file
- **Expected**: Import progress dialog shows. Success toast with count. Table refreshes.

#### TC-CUS-019: Export customers to CSV [P2] [regression]
- **Pre**: Customers exist
- **Steps**:
  1. Click ImportExportDropdown
  2. Select "Export CSV"
- **Expected**: CSV file downloaded with customer data. Badge shows total count.

#### TC-CUS-020: Download import template [P2] [regression]
- **Pre**: N/A
- **Steps**:
  1. Click ImportExportDropdown
  2. Select "Download Template"
- **Expected**: Template CSV downloaded with expected column headers.

### Edge Cases

#### TC-CUS-021: Empty state when no customers [P2] [edge-case]
- **Pre**: No customers in DB (or filtered to empty)
- **Steps**:
  1. Navigate to customers page
- **Expected**: EmptyState component shows with Users icon, "No customers found" title, description, and "Add Customer" action button (if has create permission).
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CUS-022: Real-time update via SignalR [P2] [edge-case]
- **Pre**: Customer list open in browser A
- **Steps**:
  1. In browser B, create a new customer
  2. Observe browser A
- **Expected**: Browser A auto-refreshes list via `useEntityUpdateSignal` (onCollectionUpdate).

#### TC-CUS-023: Offline banner on disconnect [P3] [visual]
- **Pre**: Customer list loaded
- **Steps**:
  1. Simulate network disconnect
  2. Observe OfflineBanner
- **Expected**: OfflineBanner appears at top when `isReconnecting` is true.

### Security

#### TC-CUS-024: No create button without permission [P1] [security]
- **Pre**: User without CustomersCreate permission
- **Steps**:
  1. Navigate to customers page
- **Expected**: "New Customer" button NOT rendered. Import/Export still visible if has read.

#### TC-CUS-025: No edit/delete in actions menu without permission [P1] [security]
- **Pre**: User with only CustomersRead permission
- **Steps**:
  1. Click actions menu on a customer row
- **Expected**: Only "View Details" shown. No Edit or Delete options.

---

## Page: Customer Detail (`/portal/ecommerce/customers/:id`)

### Happy Path

#### TC-CUS-026: View customer detail page [P1] [smoke]
- **Pre**: Customer exists with orders and addresses
- **Steps**:
  1. Navigate to `/portal/ecommerce/customers/<id>`
  2. Observe layout: header with back button, stats cards, tabs, sidebar
- **Expected**: Header shows full name + email. Segment and Tier badges in header action area. Stats: Total Orders, Total Spent, Avg Order, Points. Tabs: Orders, Addresses. Sidebar: Customer Info, Segment Management (if canManage), Loyalty Points.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CUS-027: Orders tab with pagination [P1] [smoke]
- **Pre**: Customer has >10 orders
- **Steps**:
  1. On detail page, verify "Orders" tab is active (default)
  2. See order history table: Eye button, Order #, Status, Items, Total, Date
  3. Navigate to page 2
- **Expected**: Orders paginated (10/page). Each order has Eye icon button for navigation. Status badges use `getOrderStatusColor`. Currency formatted. Date uses `formatDateTime`.
- **Data**: ☐ Order count matches CardDescription | ☐ Currency correct

#### TC-CUS-028: Navigate to order detail from orders tab [P1] [cross-feature]
- **Pre**: Customer has at least one order
- **Steps**:
  1. Click Eye button on an order row
- **Expected**: Navigates to `/portal/ecommerce/orders/<orderId>`.

#### TC-CUS-029: Addresses tab — view addresses [P1] [regression]
- **Pre**: Customer has addresses (shipping + billing)
- **Steps**:
  1. Click "Addresses" tab
  2. URL changes to `?tab=addresses`
- **Expected**: Address cards show: type badge (Shipping/Billing), default badge, full name, phone, address lines, ward/district/province, postal code. Card layout: 2 columns on md+.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CUS-030: Add new address [P1] [regression]
- **Pre**: Has CustomersUpdate permission; on Addresses tab
- **Steps**:
  1. Click "Add Address" button
  2. Fill address form (type, full name, phone, address line 1, etc.)
  3. Save
- **Expected**: AddressFormDialog opens. Address created. Card appears in list.

#### TC-CUS-031: Edit existing address [P2] [regression]
- **Pre**: Has CustomersUpdate permission; address exists
- **Steps**:
  1. Click pencil icon on address card
  2. Modify phone number
  3. Save
- **Expected**: Address updated. Card reflects change.

#### TC-CUS-032: Customer info sidebar [P1] [smoke]
- **Pre**: Customer loaded
- **Steps**:
  1. Observe right sidebar: name, email, phone, status, created at, last order date, tags, notes
- **Expected**: All fields display correctly. Status badge uses green/gray for active/inactive. Dates formatted via `formatDateTime`. Tags split by comma and shown as badges.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CUS-033: Change customer segment [P1] [regression]
- **Pre**: Has CustomersManage permission
- **Steps**:
  1. In Segment Management card, select a different segment from dropdown
- **Expected**: Toast "Customer segment updated". Segment badge in header updates.
- **Data**: ☐ Segment persisted in DB

#### TC-CUS-034: Add loyalty points [P1] [regression]
- **Pre**: Has CustomersManage permission
- **Steps**:
  1. In Loyalty Points card, click "Add"
  2. LoyaltyPointsDialog opens in "add" mode
  3. Enter points amount
  4. Confirm
- **Expected**: Points added. Current Points and Lifetime Points updated.
- **Data**: ☐ Points balance correct

#### TC-CUS-035: Redeem loyalty points [P1] [regression]
- **Pre**: Has CustomersManage permission; customer has points > 0
- **Steps**:
  1. Click "Redeem" button
  2. Enter points to redeem
  3. Confirm
- **Expected**: Points deducted. Current points decrease. Lifetime stays same.
- **Data**: ☐ Cannot redeem more than current balance

#### TC-CUS-036: Redeem button disabled when 0 points [P2] [edge-case]
- **Pre**: Customer has 0 loyalty points
- **Steps**:
  1. Observe "Redeem" button
- **Expected**: Button is disabled (`customer.loyaltyPoints <= 0`).

#### TC-CUS-037: Edit customer from detail page [P1] [regression]
- **Pre**: Has CustomersUpdate permission
- **Steps**:
  1. Click pencil icon in Customer Info card header
  2. Edit dialog opens with pre-filled data
  3. Modify last name
  4. Save
- **Expected**: Customer updated. Header name updates.

#### TC-CUS-038: Delete customer from detail page [P1] [regression]
- **Pre**: Has CustomersDelete permission
- **Steps**:
  1. Click trash icon in Customer Info card header
  2. DeleteCustomerDialog confirms
  3. Confirm deletion
- **Expected**: Customer deleted. Navigates back to `/portal/ecommerce/customers`.

### Edge Cases

#### TC-CUS-039: Customer not found [P2] [edge-case]
- **Pre**: Navigate to `/portal/ecommerce/customers/nonexistent-id`
- **Steps**:
  1. Page loads
- **Expected**: Error message "Customer not found" with back button to customer list.

#### TC-CUS-040: Orders tab empty state [P2] [edge-case]
- **Pre**: Customer has no orders
- **Steps**:
  1. View Orders tab
- **Expected**: EmptyState with ShoppingCart icon, "No orders yet" title and description.

#### TC-CUS-041: Addresses tab empty state [P2] [edge-case]
- **Pre**: Customer has no addresses
- **Steps**:
  1. View Addresses tab
- **Expected**: EmptyState with MapPin icon, "No addresses" title, and "Add Address" action button.

#### TC-CUS-042: Entity conflict dialog on concurrent edit [P2] [edge-case]
- **Pre**: Customer detail open in browser A and B
- **Steps**:
  1. Edit customer in browser B and save
  2. Observe browser A
- **Expected**: EntityConflictDialog appears offering "Continue Editing" or "Reload".

#### TC-CUS-043: Entity deleted dialog [P2] [edge-case]
- **Pre**: Customer detail open; another user deletes the customer
- **Steps**:
  1. Observe browser
- **Expected**: EntityDeletedDialog appears with "Go Back" button navigating to customer list.

### i18n

#### TC-CUS-044: Vietnamese locale on customer detail [P2] [i18n]
- **Pre**: UI set to Vietnamese
- **Steps**:
  1. Navigate to customer detail
  2. Verify all labels, tabs, buttons translated
- **Expected**: All segment labels (segment.new, segment.active, etc.), tier labels, tab labels, button text in Vietnamese. Dates formatted per tenant regional settings.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

---

## Page: Customer Groups (`/portal/customer-groups`)

### Happy Path

#### TC-CUS-045: View customer groups list [P1] [smoke]
- **Pre**: Customer groups exist
- **Steps**:
  1. Navigate to `/portal/customer-groups`
- **Expected**: DataTable with columns: Actions, Name (+ description), Slug, Status, Members, audit columns. CardDescription shows "Showing X of Y".
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CUS-046: Create customer group [P1] [smoke]
- **Pre**: Has CustomerGroupsCreate permission
- **Steps**:
  1. Click "New Group"
  2. URL changes to `?dialog=create-group`
  3. Fill name, description, slug
  4. Save
- **Expected**: Group created. Toast success. Table refreshes.
- **Data**: ☐ Slug auto-generated from name | ☐ Group appears in list

#### TC-CUS-047: Edit customer group via row click [P1] [regression]
- **Pre**: Has CustomerGroupsUpdate permission; group exists
- **Steps**:
  1. Click on a group row
  2. URL changes to `?edit=<groupId>`
  3. Edit dialog opens with pre-filled data
  4. Modify name
  5. Save
- **Expected**: Group updated. Row reflects changes.

#### TC-CUS-048: Delete customer group with confirmation [P1] [regression]
- **Pre**: Has CustomerGroupsDelete permission; group exists
- **Steps**:
  1. Click actions menu on group
  2. Click "Delete"
  3. Confirmation dialog shows group name
  4. Confirm
- **Expected**: Group soft-deleted. Row fades out. Toast success.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CUS-049: Search customer groups [P2] [regression]
- **Pre**: Multiple groups exist
- **Steps**:
  1. Type in search field
- **Expected**: Table filters by group name/description.

#### TC-CUS-050: Empty state for customer groups [P2] [edge-case]
- **Pre**: No customer groups
- **Steps**:
  1. Navigate to page
- **Expected**: EmptyState with UsersRound icon, "No customer groups found" title, "Add Group" action.

---

## Page: Promotions (`/portal/promotions`)

### Happy Path

#### TC-CUS-051: View promotions list with filters [P1] [smoke]
- **Pre**: Promotions exist with various statuses and types
- **Steps**:
  1. Navigate to `/portal/promotions`
- **Expected**: DataTable with columns: Actions, Name, Code, Type, Discount, Status, Start Date, End Date, Usage, audit columns. Status filter (Draft, Active, Scheduled, Expired, Cancelled) and Type filter (VoucherCode, FlashSale, BundleDeal, FreeShipping) visible.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px
- **Data**: ☐ Status badge colors: Active=green, Draft=gray, Scheduled=blue, Expired=orange, Cancelled=red

#### TC-CUS-052: Create promotion with date range [P0] [smoke]
- **Pre**: Has PromotionsWrite permission
- **Steps**:
  1. Click "New Promotion"
  2. URL changes to `?dialog=create-promotion`
  3. Fill: name, code, type (VoucherCode), discount type (Percentage), value (10), start date, end date
  4. Set usage limit
  5. Save
- **Expected**: Promotion created with Draft status. Toast success. Table refreshes.
- **Data**: ☐ Start/end dates stored correctly | ☐ Discount value correct

#### TC-CUS-053: Edit promotion [P1] [regression]
- **Pre**: Draft promotion exists
- **Steps**:
  1. Click on promotion row or actions > Edit
  2. URL changes to `?edit=<promoId>`
  3. Modify discount value
  4. Save
- **Expected**: Promotion updated. Changes reflected in table.

#### TC-CUS-054: Activate a draft promotion [P1] [smoke]
- **Pre**: Has PromotionsWrite permission; Draft promotion exists
- **Steps**:
  1. Click actions menu on Draft promotion
  2. Click "Activate"
- **Expected**: Toast "Promotion activated successfully". Status badge changes to Active (green).
- **Data**: ☐ Status in DB is Active

#### TC-CUS-055: Deactivate an active promotion [P1] [regression]
- **Pre**: Active promotion exists
- **Steps**:
  1. Click actions menu on Active promotion
  2. Click "Deactivate"
- **Expected**: Toast success. Status changes from Active.

#### TC-CUS-056: Activate not available for expired/cancelled [P2] [edge-case]
- **Pre**: Expired and Cancelled promotions exist
- **Steps**:
  1. Open actions menu on Expired promotion
  2. Open actions menu on Cancelled promotion
- **Expected**: "Activate" option NOT shown for Expired or Cancelled status.

#### TC-CUS-057: Delete promotion with confirmation [P1] [regression]
- **Pre**: Has PromotionsDelete permission
- **Steps**:
  1. Click actions > Delete on a promotion
  2. DeletePromotionDialog appears
  3. Confirm
- **Expected**: Promotion deleted. Toast success.

#### TC-CUS-058: Filter by status [P1] [regression]
- **Pre**: Promotions with different statuses exist
- **Steps**:
  1. Select "Active" from status filter
  2. Verify only active promotions shown
  3. Clear filter
- **Expected**: Filter works. Pagination resets to page 1.

#### TC-CUS-059: Filter by type [P2] [regression]
- **Pre**: Promotions with different types exist
- **Steps**:
  1. Select "FlashSale" from type filter
- **Expected**: Only FlashSale promotions shown.

#### TC-CUS-060: Discount display formats [P1] [data-consistency]
- **Pre**: Promotions with different discount types exist
- **Steps**:
  1. Observe Discount column for each type
- **Expected**: Percentage shows "10%". FixedAmount shows "50,000" (localized number). FreeShipping shows "Free Shipping". BuyXGetY shows "Buy X Get Y".
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CUS-061: Usage column shows current/limit [P2] [data-consistency]
- **Pre**: Promotion with usage limit set
- **Steps**:
  1. Observe Usage column
- **Expected**: Shows "5/100" format (currentUsageCount/usageLimitTotal). If no limit, shows just "5".

#### TC-CUS-062: Group by type or status [P2] [regression]
- **Pre**: Multiple promotions
- **Steps**:
  1. Enable grouping by Type
  2. Verify group headers
- **Expected**: Rows grouped. Group headers show translated type/status names.

#### TC-CUS-063: Empty state for promotions [P3] [visual]
- **Pre**: No promotions
- **Steps**:
  1. Navigate to promotions page
- **Expected**: EmptyState with Percent icon, "No promotions found" title, "Add Promotion" action.

---

## Page: Reviews (`/portal/reviews`)

### Happy Path

#### TC-CUS-064: View reviews with tab filtering [P1] [smoke]
- **Pre**: Reviews exist with Pending, Approved, Rejected statuses
- **Steps**:
  1. Navigate to `/portal/reviews`
  2. Observe tabs: All, Pending, Approved, Rejected
  3. Click "Pending" tab
- **Expected**: URL updates with tab param. Table filters to pending reviews only. Pagination resets. Row selection cleared on tab change.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CUS-065: Star rating display [P1] [visual]
- **Pre**: Reviews with varying ratings exist
- **Steps**:
  1. Observe Rating column
- **Expected**: 5-star display: filled yellow for earned, muted for unearned. Correct icon sizing (h-3.5 w-3.5).
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CUS-066: Filter by rating [P2] [regression]
- **Pre**: Reviews with different ratings
- **Steps**:
  1. Select "5 stars" from rating filter
- **Expected**: Only 5-star reviews shown. Filter resets page to 1.

#### TC-CUS-067: Approve a pending review [P0] [smoke]
- **Pre**: Pending review exists
- **Steps**:
  1. Click actions menu on pending review
  2. Click "Approve"
- **Expected**: Toast "Review approved successfully". Review status changes to Approved (green badge).
- **Data**: ☐ Status in DB is Approved

#### TC-CUS-068: Reject a pending review with reason [P1] [smoke]
- **Pre**: Pending review exists
- **Steps**:
  1. Click actions menu > "Reject"
  2. RejectReviewDialog opens
  3. Enter rejection reason (optional)
  4. Confirm
- **Expected**: Review rejected. Toast success. Status changes to Rejected.

#### TC-CUS-069: View review detail dialog [P1] [regression]
- **Pre**: Review exists
- **Steps**:
  1. Click on a review row (when no rows selected)
  2. ReviewDetailDialog opens
- **Expected**: Dialog shows full review: product name, customer name, rating, title, content, verified purchase badge, status. Actions: Approve, Reject, Respond available for pending reviews.

#### TC-CUS-070: Admin response to review [P2] [regression]
- **Pre**: Review exists
- **Steps**:
  1. Click actions > "Respond"
  2. AdminResponseDialog opens
  3. Type response
  4. Submit
- **Expected**: Response saved. Dialog closes.

#### TC-CUS-071: Bulk approve selected reviews [P1] [regression]
- **Pre**: Multiple pending reviews exist
- **Steps**:
  1. Select 3 pending reviews via checkboxes
  2. BulkActionToolbar shows "Approve selected" and "Reject selected"
  3. Click "Approve selected"
- **Expected**: Toast "3 reviews approved". Selection cleared. Reviews now Approved.

#### TC-CUS-072: Bulk reject selected reviews [P1] [regression]
- **Pre**: Multiple pending reviews selected
- **Steps**:
  1. Click "Reject selected"
  2. RejectReviewDialog opens in bulk mode
  3. Enter reason
  4. Confirm
- **Expected**: Toast with count. All selected rejected.

#### TC-CUS-073: Verified purchase badge [P2] [visual]
- **Pre**: Review with `isVerifiedPurchase: true`
- **Steps**:
  1. Observe Customer column
- **Expected**: ShieldCheck icon (blue) appears next to customer name with aria-label "Verified purchase".
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CUS-074: Approve/Reject only shown for Pending [P2] [edge-case]
- **Pre**: Approved and Rejected reviews exist
- **Steps**:
  1. Open actions menu on Approved review
- **Expected**: Only "View Details" and "Respond" shown. No Approve/Reject options.

---

## Page: Wishlists (`/portal/wishlists`)

### Happy Path

#### TC-CUS-075: View wishlist analytics [P1] [smoke]
- **Pre**: Wishlists with items exist
- **Steps**:
  1. Navigate to `/portal/wishlists` (analytics page)
- **Expected**: Stats cards: Total Wishlists, Total Items, Top Products Tracked. Top Products table with FilePreviewTrigger thumbnails.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px
- **Data**: ☐ Stats match actual DB counts

#### TC-CUS-076: Wishlist page with items [P1] [regression]
- **Pre**: Wishlists exist with items having different priorities
- **Steps**:
  1. Navigate to wishlist page
  2. Observe wishlist cards with items
- **Expected**: Items show product name, price, priority badge (color-coded: High=red, Medium=amber, Low=blue, None=default). Actions: move to cart, remove, share, edit.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CUS-077: Change wishlist item priority [P2] [regression]
- **Pre**: Wishlist item exists
- **Steps**:
  1. Change priority from "None" to "High"
- **Expected**: Priority badge updates to red. Change persisted.

#### TC-CUS-078: Delete wishlist with confirmation [P1] [regression]
- **Pre**: Wishlist exists
- **Steps**:
  1. Open actions menu on wishlist
  2. Click Delete
  3. Confirmation dialog appears
  4. Confirm
- **Expected**: Wishlist deleted. Toast success.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px
