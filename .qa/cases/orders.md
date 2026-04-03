# Orders — Test Cases

> Pages: /portal/orders, /portal/orders/:id, /portal/orders/manual-create, /portal/payments, /portal/payments/:id, /portal/shipping | Last updated: 2026-04-03 | Git ref: f6f5cd3
> Total: 95 cases | P0: 10 | P1: 48 | P2: 27 | P3: 10

---

## Page: Orders List (`/portal/ecommerce/orders`)

### Happy Path

#### TC-ORD-001: View orders list with DataTable [P1] [smoke]
- **Pre**: Logged in as tenant admin, orders exist
- **Steps**:
  1. Navigate to /portal/ecommerce/orders
- **Expected**: DataTable with columns: Actions, Select (if OrdersManage), Order # (mono font), Customer (name + email), Status (colored badge), Items, Total (formatted currency), Payment Status, audit columns. CardDescription shows "Showing X of Y items".
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px
- **Data**: ☐ Order numbers display correctly | ☐ Currency formatted via formatCurrency

#### TC-ORD-002: Search orders by customer email [P1] [smoke]
- **Pre**: Orders exist for multiple customers
- **Steps**:
  1. Type customer email in search input
  2. Wait for debounce
- **Expected**: Table filters by customerEmail (params.search mapped to customerEmail). Results update with opacity transition.

#### TC-ORD-003: Filter orders by status [P1] [smoke]
- **Pre**: Orders in various statuses
- **Steps**:
  1. Select "Pending" from status filter
  2. Select "Confirmed"
  3. Select "Shipped"
  4. Select "Delivered"
  5. Select "Completed"
  6. Select "Cancelled"
  7. Select "Returned"
  8. Select "Refunded"
  9. Select "All"
- **Expected**: Each filter shows only orders of that status. Status badge color matches getOrderStatusColor. Filter uses useTransition for smooth UI.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-ORD-004: Navigate to order detail from list [P1] [smoke]
- **Pre**: Orders exist
- **Steps**:
  1. Click "View Details" (Eye icon) in actions menu
- **Expected**: Navigates to /portal/ecommerce/orders/:id. Order detail page loads.

#### TC-ORD-005: Create manual order from list page [P1] [regression]
- **Pre**: OrdersWrite permission
- **Steps**:
  1. Click "New Order" button (Plus icon)
- **Expected**: Navigates to manual create order page.

#### TC-ORD-006: Bulk confirm pending orders [P1] [regression]
- **Pre**: Multiple Pending orders selected
- **Steps**:
  1. Select multiple Pending orders via checkboxes
  2. BulkActionToolbar appears with confirm action
  3. Click "Confirm" bulk action
- **Expected**: useBulkConfirmOrders mutation called. Toast shows success/partial count. Orders transition to Confirmed.
- **Data**: ☐ Only Pending orders confirmed | ☐ Other statuses ignored

#### TC-ORD-007: Bulk cancel orders with reason [P1] [regression]
- **Pre**: Multiple cancellable orders selected (Pending/Confirmed/Processing)
- **Steps**:
  1. Select cancellable orders
  2. Click "Cancel" bulk action
  3. Bulk cancel confirmation dialog appears (showBulkCancelConfirm)
  4. Enter cancel reason in Textarea
  5. Confirm
- **Expected**: useBulkCancelOrders mutation called. Toast shows result. Orders transition to Cancelled. Inventory returned for confirmed orders.
- **Data**: ☐ Cancel reason stored | ☐ Inventory restored

#### TC-ORD-008: Export orders CSV/Excel [P1] [regression]
- **Pre**: Orders exist
- **Steps**:
  1. Click ImportExportDropdown
  2. Click "Export CSV"
  3. Click "Export Excel"
- **Expected**: exportOrders service called. Files download with order data.

#### TC-ORD-009: Orders table grouping by status [P2] [regression]
- **Pre**: Orders in multiple statuses
- **Steps**:
  1. Group by Status column (enableGrouping on status column)
- **Expected**: Orders grouped by status. Group headers use groupValueFormatter for i18n translation of status values.

#### TC-ORD-010: Orders pagination and sorting [P1] [regression]
- **Pre**: 50+ orders
- **Steps**:
  1. Navigate pages
  2. Sort by Order #, Customer, Status, Total
  3. Change page size
- **Expected**: Server-side pagination and sorting. Page size persists via localStorage (tableKey: 'orders').

### Edge Cases

#### TC-ORD-011: Orders empty state [P2] [edge-case]
- **Pre**: No orders exist
- **Expected**: EmptyState with ShoppingCart icon, title, description.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-ORD-012: Bulk cancel with non-cancellable orders in selection [P2] [edge-case]
- **Pre**: Select mix of Pending and Delivered orders
- **Steps**:
  1. Click bulk cancel
- **Expected**: Only Pending/Confirmed/Processing orders cancelled. CANCELLABLE_STATUSES enforced.

### Security

#### TC-ORD-013: Orders page without manage permission [P1] [security]
- **Pre**: User without OrdersManage
- **Steps**:
  1. Navigate to orders
- **Expected**: Select column hidden. Bulk actions not available. View Details still accessible with OrdersRead.

---

## Page: Order Detail (`/portal/ecommerce/orders/:id`)

### Happy Path — Status Transitions

#### TC-ORD-014: View order detail page [P1] [smoke]
- **Pre**: Order exists
- **Steps**:
  1. Navigate to /portal/ecommerce/orders/:id
- **Expected**: Page shows: status timeline (6 steps), order items table with images (FilePreviewTrigger), order summary (subtotal, shipping, tax, discount, total), billing/shipping addresses (AddressCard), OrderNotes, OrderPaymentInfo, OrderShipmentTracking.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px
- **Data**: ☐ Timeline steps colored correctly | ☐ Currency formatted | ☐ Addresses complete

#### TC-ORD-015: Status timeline visualization [P1] [visual]
- **Pre**: Order in Processing status
- **Steps**:
  1. View status timeline
- **Expected**: Pending, Confirmed, Processing steps highlighted with distinct colors (amber, blue, indigo). Future steps (Shipped, Delivered, Completed) dimmed. Timeline connector lines show gradient colors.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-ORD-016: Confirm pending order [P0] [smoke]
- **Pre**: Order with status = Pending, OrdersWrite permission
- **Steps**:
  1. Click "Confirm Order" button (Check icon)
- **Expected**: confirmMutation called. Status changes to Confirmed. Timeline updates. Toast: "Order confirmed successfully". confirmedAt timestamp set.
- **Data**: ☐ Status = Confirmed | ☐ confirmedAt populated

#### TC-ORD-017: Ship confirmed/processing order [P0] [smoke]
- **Pre**: Order with status = Confirmed or Processing
- **Steps**:
  1. Click "Ship" button
  2. Ship dialog opens (showShipDialog)
  3. Enter Tracking Number
  4. Enter Shipping Carrier
  5. Click Ship
- **Expected**: shipMutation called with trackingNumber and carrier. Status changes to Shipped. Dialog closes. Toast success.
- **Data**: ☐ Status = Shipped | ☐ shippedAt set | ☐ Tracking number stored

#### TC-ORD-018: Mark order as delivered [P0] [smoke]
- **Pre**: Order with status = Shipped
- **Steps**:
  1. Click "Delivered" button
- **Expected**: deliverMutation called. Status changes to Delivered. deliveredAt timestamp set.
- **Data**: ☐ Status = Delivered | ☐ deliveredAt populated

#### TC-ORD-019: Complete delivered order [P0] [smoke]
- **Pre**: Order with status = Delivered
- **Steps**:
  1. Click "Complete" button
- **Expected**: completeMutation called. Status changes to Completed. completedAt set. All timeline steps filled.
- **Data**: ☐ Status = Completed | ☐ completedAt set | ☐ Timeline fully highlighted

#### TC-ORD-020: Cancel order with reason [P0] [smoke]
- **Pre**: Order with status = Pending, Confirmed, or Processing
- **Steps**:
  1. Click "Cancel" button
  2. Cancel dialog opens (showCancelDialog)
  3. Enter reason in Textarea
  4. Confirm cancellation
- **Expected**: cancelMutation called. Status = Cancelled. Dialog closes. Inventory returned for cancelled items.
- **Data**: ☐ Status = Cancelled | ☐ Cancel reason stored | ☐ Product stock restored

#### TC-ORD-021: Return delivered order [P1] [smoke]
- **Pre**: Order with status = Delivered, OrdersManage permission
- **Steps**:
  1. Click "Return" button
  2. Return dialog opens (showReturnDialog)
  3. Enter reason (REQUIRED — returnReason.trim() check)
  4. Confirm
- **Expected**: returnMutation called. Status = Returned. Product stock restored. Toast success.
- **Data**: ☐ Status = Returned | ☐ Reason stored | ☐ Inventory restored

#### TC-ORD-022: Full lifecycle — Pending → Confirmed → Processing → Shipped → Delivered → Completed [P0] [smoke]
- **Pre**: New pending order
- **Steps**:
  1. Confirm order
  2. Ship with tracking number
  3. Mark delivered
  4. Complete order
- **Expected**: Each transition succeeds. Timeline fills progressively. Timestamps set at each step. All 6 STATUS_STEPS highlighted at completion.
- **Data**: ☐ All timestamps populated | ☐ Timeline fully green

#### TC-ORD-023: Order items display with images [P1] [visual]
- **Pre**: Order with product items that have images
- **Steps**:
  1. View order items table
- **Expected**: Table shows product image (FilePreviewTrigger), name, variant, SKU, quantity, unit price, total. Images clickable for preview.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-ORD-024: Order notes section [P2] [regression]
- **Pre**: Order exists
- **Steps**:
  1. Scroll to OrderNotes section
  2. Add a note
- **Expected**: Notes added and displayed with timestamps.

#### TC-ORD-025: Order payment info section [P1] [regression]
- **Pre**: Order with payment
- **Steps**:
  1. Scroll to OrderPaymentInfo section
- **Expected**: Payment status, method, amount displayed. Links to payment detail page if applicable.

#### TC-ORD-026: Order shipment tracking section [P1] [regression]
- **Pre**: Shipped order with tracking
- **Steps**:
  1. Scroll to OrderShipmentTracking section
- **Expected**: Tracking number, carrier, shipment status, tracking events timeline displayed.

### Edge Cases

#### TC-ORD-027: Action buttons hidden for invalid transitions [P1] [edge-case]
- **Pre**: Order in various statuses
- **Steps**:
  1. View Completed order — no action buttons
  2. View Cancelled order — no action buttons (terminal status)
  3. View Pending order — only Confirm and Cancel available
  4. View Shipped order — only Deliver available
  5. View Delivered order — Complete and Return available
- **Expected**: canConfirm/canShip/canDeliver/canComplete/canCancel/canReturn flags match: Pending(confirm,cancel), Confirmed(ship,cancel), Processing(ship,cancel), Shipped(deliver), Delivered(complete,return), Completed/Cancelled/Returned/Refunded(none).

#### TC-ORD-028: Concurrent edit — conflict dialog [P1] [edge-case]
- **Pre**: Two users viewing same order
- **Steps**:
  1. User A confirms order
  2. User B receives SignalR update
- **Expected**: EntityConflictDialog with reload option. Or auto-reload via onAutoReload.

#### TC-ORD-029: Order deleted while viewing [P1] [edge-case]
- **Pre**: Viewing order detail
- **Steps**:
  1. Order soft-deleted by another user
- **Expected**: EntityDeletedDialog appears. Navigate back to orders list.

#### TC-ORD-030: Ship without tracking number [P2] [edge-case]
- **Pre**: Confirmed order
- **Steps**:
  1. Open ship dialog
  2. Leave tracking number and carrier empty
  3. Click Ship
- **Expected**: Ship succeeds (trackingNumber and carrier are optional per `|| undefined`).

#### TC-ORD-031: Return without reason [P2] [edge-case]
- **Pre**: Delivered order
- **Steps**:
  1. Open return dialog
  2. Leave reason empty
  3. Click confirm
- **Expected**: Return blocked — `returnReason.trim()` check prevents empty reason.

#### TC-ORD-032: Order not found [P2] [edge-case]
- **Pre**: Invalid order ID in URL
- **Steps**:
  1. Navigate to /portal/ecommerce/orders/invalid-id
- **Expected**: "Order not found" error message with "Back to Orders" button.

#### TC-ORD-033: Terminal status timeline display [P2] [visual]
- **Pre**: Cancelled order
- **Steps**:
  1. View order detail
- **Expected**: Timeline shows terminal status indicator. isTerminalStatus = true (STATUS_ORDER < 0). Normal timeline steps may be dimmed.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

### Security

#### TC-ORD-034: Status actions without write permission [P1] [security]
- **Pre**: User with OrdersRead only (no OrdersWrite)
- **Steps**:
  1. View order detail
- **Expected**: All status action buttons hidden. View-only mode. canConfirm/canShip/etc all false.

#### TC-ORD-035: Return requires manage permission [P1] [security]
- **Pre**: User with OrdersWrite but not OrdersManage
- **Steps**:
  1. View delivered order
- **Expected**: Return button hidden (canReturn = canManageOrders && status === 'Delivered').

---

## Page: Manual Create Order (`/portal/ecommerce/orders/manual-create`)

### Happy Path

#### TC-ORD-036: Create manual order — full flow [P0] [smoke]
- **Pre**: OrdersWrite permission, products and customers exist
- **Steps**:
  1. Navigate to manual create order page
  2. Search and select a customer (by email)
  3. Search products via ProductSearchTypeahead
  4. Add product variants to order
  5. Set quantities
  6. Set shipping amount, discount, tax
  7. Optionally enter coupon code
  8. Enter customer notes, internal notes
  9. Select payment method
  10. Click "Create Order" (Pending status)
- **Expected**: useManualCreateOrderMutation called. Order created in Pending status. Redirects to order detail page. Toast success.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px
- **Data**: ☐ All line items correct | ☐ Totals calculated | ☐ Customer linked

#### TC-ORD-037: Create and complete order in one step [P1] [regression]
- **Pre**: Same as TC-ORD-036
- **Steps**:
  1. Fill order details
  2. Click "Create & Complete" button
- **Expected**: useManualCreateAndCompleteOrderMutation called. Order created in Completed status directly.

#### TC-ORD-038: Product search typeahead [P1] [smoke]
- **Pre**: Products with variants exist
- **Steps**:
  1. Type product name in ProductSearchTypeahead
  2. Dropdown shows matching variants with images
  3. Click to select a variant
- **Expected**: Deferred search (useDeferredValue). Results show product name, variant name, SKU, price, stock. Selected variant added to order items.
- **Data**: ☐ Already-added variants marked (existingVariantIds) | ☐ Stock shown

#### TC-ORD-039: Adjust item quantities and custom prices [P1] [regression]
- **Pre**: Items added to order
- **Steps**:
  1. Change quantity for an item
  2. Set custom price for an item
  3. Observe total recalculates
- **Expected**: Order total updates dynamically. Custom price overrides original price. Discount per item calculated.

#### TC-ORD-040: Remove item from order [P1] [regression]
- **Pre**: Multiple items in order
- **Steps**:
  1. Click remove (Trash2 icon) on an item
- **Expected**: Item removed from list. Total recalculates.

#### TC-ORD-041: Customer search and selection [P1] [regression]
- **Pre**: Customers exist
- **Steps**:
  1. Type customer email
  2. Search results show (getCustomers service)
  3. Select customer
- **Expected**: Customer email, name, phone auto-filled. Can manually enter if no match.

#### TC-ORD-042: Apply coupon code [P2] [regression]
- **Pre**: Valid promotion with coupon code
- **Steps**:
  1. Enter coupon code
  2. Validate (validatePromoCode service)
- **Expected**: Coupon validated. Discount amount applied to order total.

#### TC-ORD-043: Form validation [P1] [regression]
- **Pre**: Empty form
- **Steps**:
  1. Try to submit without customer email
  2. Enter invalid email format
  3. Set negative shipping amount
- **Expected**: Zod validation: customerEmail required + email format. shippingAmount/discountAmount/taxAmount >= 0. FormErrorBanner for server errors.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

### Edge Cases

#### TC-ORD-044: Add duplicate variant [P2] [edge-case]
- **Pre**: Variant already in order
- **Steps**:
  1. Search and try to add same variant again
- **Expected**: Already-added variants indicated in search results (existingVariantIds set).

#### TC-ORD-045: Order with zero items [P2] [edge-case]
- **Pre**: Form filled but no items
- **Steps**:
  1. Try to create order without items
- **Expected**: Validation prevents creation. Warning or error message.

#### TC-ORD-046: Product out of stock in search results [P2] [edge-case]
- **Pre**: Product variant with 0 stock
- **Steps**:
  1. Search for the product
  2. Add to order
- **Expected**: Stock quantity shown in search results. Item can still be added (manual order override). Warning icon if stock insufficient.

---

## Page: Payments List (`/portal/payments`)

### Happy Path

#### TC-ORD-047: View payments list [P1] [smoke]
- **Pre**: Payment transactions exist
- **Steps**:
  1. Navigate to /portal/payments
- **Expected**: DataTable with columns: Actions, Transaction ID, Order #, Amount (formatCurrency), Payment Method, Status (colored badge via paymentStatusColors), audit columns. 12 payment statuses supported.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-ORD-048: Filter payments by status [P1] [regression]
- **Pre**: Payments in various statuses
- **Steps**:
  1. Filter by each status: Pending, Processing, RequiresAction, Authorized, Paid, Failed, Cancelled, Expired, Refunded, PartialRefund, CodPending, CodCollected
- **Expected**: Correct filtering. Status badges use paymentStatusColors mapping.

#### TC-ORD-049: Filter payments by method [P1] [regression]
- **Pre**: Payments with various methods
- **Steps**:
  1. Filter by: EWallet, QRCode, BankTransfer, CreditCard, DebitCard, Installment, COD, BuyNowPayLater
- **Expected**: Correct filtering by payment method.

#### TC-ORD-050: Record manual payment [P1] [regression]
- **Pre**: PaymentsWrite permission
- **Steps**:
  1. Click "Record Payment" button (Plus icon)
  2. RecordManualPaymentDialog opens (recordDialogOpen state)
  3. Fill in order reference, amount, method
  4. Save
- **Expected**: Manual payment recorded. Appears in payments list.

#### TC-ORD-051: Navigate to payment detail [P1] [regression]
- **Pre**: Payment exists
- **Steps**:
  1. Click "View Details" in actions menu
- **Expected**: Navigates to /portal/payments/:id.

### Edge Cases

#### TC-ORD-052: Payments empty state [P2] [edge-case]
- **Pre**: No payments exist
- **Expected**: EmptyState with CreditCard icon.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

---

## Page: Payment Detail (`/portal/payments/:id`)

### Happy Path

#### TC-ORD-053: View payment detail with tabs [P1] [smoke]
- **Pre**: Payment transaction exists
- **Steps**:
  1. Navigate to /portal/payments/:id
- **Expected**: Page shows payment summary (status badge, amount, method, order reference). Tabs: Overview, Timeline, Raw Data. Tab state URL-synced via useUrlTab.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-ORD-054: Payment timeline tab [P1] [visual]
- **Pre**: Payment with timeline events
- **Steps**:
  1. Click Timeline tab
- **Expected**: usePaymentTimelineQuery loads events. Each event has icon (getTimelineEventIcon: StatusChange=Clock, ApiCall=Zap, Webhook=Webhook). Timestamps formatted via formatDateTime.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-ORD-055: Refresh payment status [P1] [regression]
- **Pre**: Non-terminal payment (not in Paid/Failed/Cancelled/Expired/Refunded)
- **Steps**:
  1. Click Refresh button (RefreshCw icon)
- **Expected**: useRefreshPaymentMutation called. Payment status updated from gateway. Timeline updated.

#### TC-ORD-056: Confirm COD collection [P1] [regression]
- **Pre**: Payment with status = CodPending
- **Steps**:
  1. Click "Confirm COD Collection"
- **Expected**: useConfirmCodCollectionMutation called. Status changes to CodCollected. Toast success.

#### TC-ORD-057: Request refund [P1] [regression]
- **Pre**: Paid payment
- **Steps**:
  1. Click "Request Refund"
  2. Dialog opens with refund reason selection (RefundReason type)
  3. Enter amount, reason
  4. Submit
- **Expected**: useRequestRefundMutation called. Refund requested.

#### TC-ORD-058: Approve refund [P1] [regression]
- **Pre**: Refund pending approval
- **Steps**:
  1. Click "Approve Refund" (ThumbsUp icon)
- **Expected**: useApproveRefundMutation called. Refund processed.

#### TC-ORD-059: Reject refund [P1] [regression]
- **Pre**: Refund pending approval
- **Steps**:
  1. Click "Reject Refund" (ThumbsDown icon)
- **Expected**: useRejectRefundMutation called. Refund rejected.

#### TC-ORD-060: Raw data tab — JSON viewer [P2] [visual]
- **Pre**: Payment with gateway response data
- **Steps**:
  1. Click Raw Data tab
- **Expected**: JsonViewer component renders payment gateway response. Syntax highlighted.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

### Edge Cases

#### TC-ORD-061: Refresh on terminal payment [P2] [edge-case]
- **Pre**: Payment in terminal status (Paid/Failed/Cancelled/Expired/Refunded)
- **Steps**:
  1. Observe page
- **Expected**: Refresh button hidden or disabled for TERMINAL_STATUSES.

#### TC-ORD-062: Payment not found [P2] [edge-case]
- **Pre**: Invalid payment ID
- **Steps**:
  1. Navigate to /portal/payments/invalid-id
- **Expected**: Error state with back navigation.

#### TC-ORD-063: Concurrent payment update [P1] [edge-case]
- **Pre**: Viewing payment detail
- **Steps**:
  1. Webhook updates payment status externally
- **Expected**: EntityConflictDialog or auto-reload via SignalR useEntityUpdateSignal.

---

## Page: Shipping Management (`/portal/shipping`)

### Happy Path

#### TC-ORD-064: View shipping page with tabs [P1] [smoke]
- **Pre**: Navigate to /portal/shipping
- **Steps**:
  1. Page loads with Providers and Shipments tabs
- **Expected**: URL-synced tabs via useUrlTab (defaultTab: 'providers'). PageHeader with Truck icon.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-ORD-065: Providers tab — list providers [P1] [smoke]
- **Pre**: Shipping providers configured
- **Steps**:
  1. View Providers tab (default)
- **Expected**: ProviderList component renders. Each provider shows name, type, status, configuration.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-ORD-066: Create shipping provider [P1] [regression]
- **Pre**: Admin permissions
- **Steps**:
  1. Click "Add Provider"
  2. ProviderFormDialog opens
  3. Fill in provider name, type, API key/config
  4. Save
- **Expected**: Provider created. Appears in list.

#### TC-ORD-067: Edit shipping provider [P1] [regression]
- **Pre**: Provider exists
- **Steps**:
  1. Click edit on provider
  2. Modify configuration
  3. Save
- **Expected**: Provider updated.

#### TC-ORD-068: Delete shipping provider [P1] [regression]
- **Pre**: Provider exists
- **Steps**:
  1. Click delete on provider
  2. Confirm
- **Expected**: Provider removed.

#### TC-ORD-069: Shipments tab — lookup shipment [P1] [regression]
- **Pre**: Shipped orders exist
- **Steps**:
  1. Click "Shipments" tab
  2. ShipmentLookup component loads
  3. Search by tracking number or order number
- **Expected**: Shipment details shown with ShipmentDetail component.

#### TC-ORD-070: Shipment detail with tracking timeline [P2] [visual]
- **Pre**: Shipment with tracking events
- **Steps**:
  1. View shipment detail
- **Expected**: TrackingTimeline component shows chronological tracking events. Each event has timestamp, location, status.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

### Edge Cases

#### TC-ORD-071: Shipment not found [P2] [edge-case]
- **Pre**: Invalid tracking number
- **Steps**:
  1. Search with non-existent tracking number
- **Expected**: "No shipment found" message.

#### TC-ORD-072: Tab URL persistence [P2] [edge-case]
- **Pre**: On shipping page
- **Steps**:
  1. Click Shipments tab
  2. URL updates to ?tab=shipments
  3. Refresh page
- **Expected**: Shipments tab active after refresh (URL-synced).

---

## Cross-Feature: Order Lifecycle Data Consistency

#### TC-ORD-073: Order creation → inventory deduction [P0] [data-consistency]
- **Pre**: Product A stock = 20. Customer places order for 3 units.
- **Steps**:
  1. Create manual order with 3x Product A
  2. Confirm order
- **Expected**: Stock reserved/deducted. Product A stock reflects order.
- **Data**: ☐ Stock updated | ☐ Inventory movement recorded

#### TC-ORD-074: Order cancellation → inventory return [P0] [data-consistency]
- **Pre**: Confirmed order for 3x Product A
- **Steps**:
  1. Cancel the order with reason
  2. Check Product A stock
- **Expected**: Stock returned to previous level. Inventory receipt or movement logged.
- **Data**: ☐ Stock restored | ☐ Cancel reason in order

#### TC-ORD-075: Order return → inventory restoration [P0] [data-consistency]
- **Pre**: Delivered order for 5x Product A
- **Steps**:
  1. Process return
  2. Check Product A stock
- **Expected**: Stock returned. Return reason stored. Order status = Returned.
- **Data**: ☐ Stock restored | ☐ Return logged

#### TC-ORD-076: Order → Payment relationship [P1] [cross-feature]
- **Pre**: Order with payment
- **Steps**:
  1. View order detail — OrderPaymentInfo shows payment status
  2. Navigate to payment detail — shows order reference
  3. Update payment status
  4. Return to order — payment info updated
- **Expected**: Bidirectional link between order and payment. Status changes reflected.

#### TC-ORD-077: Order → Shipping relationship [P1] [cross-feature]
- **Pre**: Shipped order
- **Steps**:
  1. View order detail — OrderShipmentTracking shows tracking
  2. Look up shipment on shipping page — same tracking number
- **Expected**: Tracking info consistent between order detail and shipping page.

#### TC-ORD-078: Manual order with promotion code [P2] [cross-feature]
- **Pre**: Active promotion with code "SAVE10"
- **Steps**:
  1. Create manual order
  2. Enter coupon code "SAVE10"
  3. Validate code
  4. Complete order
- **Expected**: Discount applied. Promotion usage count incremented.
- **Data**: ☐ Discount amount correct | ☐ Promotion usage tracked

---

## i18n Tests

#### TC-ORD-079: Orders page in Vietnamese [P2] [i18n]
- **Pre**: Language set to VI
- **Steps**:
  1. Navigate through all order pages
- **Expected**: All labels, statuses, buttons in Vietnamese. Status translations via `orders.status.{status.toLowerCase()}`. No English leaks.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-ORD-080: Payment statuses in Vietnamese [P2] [i18n]
- **Pre**: VI language, payments exist
- **Steps**:
  1. View payments list and detail
- **Expected**: All 12 payment statuses translated. Payment methods translated.

#### TC-ORD-081: Order status timeline in Vietnamese [P2] [i18n]
- **Pre**: VI language
- **Steps**:
  1. View order detail timeline
- **Expected**: All 6 status step labels translated. Terminal status labels (Cancelled, Returned, Refunded) translated.

---

## Responsive Tests

#### TC-ORD-082: Orders list at 768px [P2] [responsive]
- **Pre**: 768px viewport
- **Steps**:
  1. Navigate to orders list
- **Expected**: DataTable scrollable horizontally. Filters stack. BulkActionToolbar responsive.
- **Visual**: ☐ 768px

#### TC-ORD-083: Order detail at 768px [P2] [responsive]
- **Pre**: 768px viewport
- **Steps**:
  1. View order detail
- **Expected**: 3-column grid collapses to single column (lg:grid-cols-3 → 1). Status timeline wraps. Address cards stack.
- **Visual**: ☐ 768px

#### TC-ORD-084: Manual create order at 768px [P2] [responsive]
- **Pre**: 768px viewport
- **Steps**:
  1. Navigate to manual create order
- **Expected**: Form sections stack. Product search full width. Items table scrollable.
- **Visual**: ☐ 768px

#### TC-ORD-085: Payment detail at 768px [P2] [responsive]
- **Pre**: 768px viewport
- **Steps**:
  1. View payment detail
- **Expected**: Tabs responsive. Timeline readable. JSON viewer scrollable.
- **Visual**: ☐ 768px

---

## Dark Mode Tests

#### TC-ORD-086: Order status badges in dark mode [P3] [dark-mode]
- **Pre**: Dark mode
- **Steps**:
  1. View orders list with all statuses
- **Expected**: getOrderStatusColor provides dark mode variants. All badges readable against dark background.
- **Visual**: ☐ Dark

#### TC-ORD-087: Status timeline in dark mode [P3] [dark-mode]
- **Pre**: Dark mode
- **Steps**:
  1. View order detail timeline
- **Expected**: Each step has dark:text-* and dark:bg-* variants (amber-400, blue-400, indigo-400, violet-400, emerald-400, green-400). Connector gradients visible.
- **Visual**: ☐ Dark

#### TC-ORD-088: Payment status colors in dark mode [P3] [dark-mode]
- **Pre**: Dark mode, various payment statuses
- **Expected**: paymentStatusColors provide dark mode readable colors for all 12 statuses.
- **Visual**: ☐ Dark

#### TC-ORD-089: Cancel/Return dialog in dark mode [P3] [dark-mode]
- **Pre**: Dark mode
- **Steps**:
  1. Open cancel dialog on an order
- **Expected**: Destructive border (border-destructive/30) visible. Textarea readable.
- **Visual**: ☐ Dark

#### TC-ORD-090: Shipping tracking timeline in dark mode [P3] [dark-mode]
- **Pre**: Dark mode, shipment with tracking
- **Expected**: TrackingTimeline events readable.
- **Visual**: ☐ Dark

---

## Performance Tests

#### TC-ORD-091: Orders list with 1000+ orders [P2] [performance]
- **Pre**: 1000+ orders in database
- **Steps**:
  1. Navigate to orders page
  2. Change page size to 100
- **Expected**: Page loads within 3s. Server-side pagination prevents full load.

#### TC-ORD-092: Order detail with 50+ items [P2] [performance]
- **Pre**: Order with many line items
- **Steps**:
  1. View order detail
- **Expected**: Items table renders smoothly. FilePreviewTrigger thumbnails lazy-loaded.

#### TC-ORD-093: Product search typeahead responsiveness [P2] [performance]
- **Pre**: 10000+ product variants
- **Steps**:
  1. Type in manual create order product search
- **Expected**: useDeferredValue prevents jank. Results appear within 500ms of typing stop.

#### TC-ORD-094: Payment timeline with 100+ events [P2] [performance]
- **Pre**: Payment with extensive event history
- **Steps**:
  1. View payment timeline tab
- **Expected**: Timeline renders without lag. Scrollable if needed.

#### TC-ORD-095: Bulk operations on 50 orders [P2] [performance]
- **Pre**: 50 orders selected
- **Steps**:
  1. Bulk confirm or cancel
- **Expected**: Operation completes within 10s. useTransition prevents UI freeze during bulk mutation.
