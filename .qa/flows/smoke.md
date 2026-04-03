# Smoke Flow — Critical Path (~15 min)

> 140 cases tagged [smoke] | Execution order: auth → dashboard → settings → catalog → orders → customers → content → hr → crm → pm
> Last updated: 2026-04-03

## 1. Auth (8 cases)
- TC-AUTH-001: Login with valid tenant admin credentials
- TC-AUTH-002: Login with platform admin credentials
- TC-AUTH-003: Multi-tenant organization selection step
- TC-AUTH-019: Request password reset OTP
- TC-AUTH-024: Dashboard loads after login
- TC-AUTH-026: Profile tab loads with current user data
- TC-AUTH-033: Change password successfully
- TC-AUTH-047: Unauthenticated user redirected to login

## 2. Dashboard (6 cases)
- TC-DSH-001: Dashboard loads with Core widgets
- TC-DSH-002: E-commerce widget group renders when enabled
- TC-RPT-001: Reports page loads with Revenue tab
- TC-RPT-002: Revenue tab — metric cards and chart
- TC-DSH-020: Welcome page renders correctly
- TC-DSH-021: Portal CTA button navigates correctly

## 3. Settings (14 cases)
- TC-SET-001: Branding tab loads with current settings
- TC-SET-005: Regional settings load with current values
- TC-SET-012: SMTP settings load
- TC-SET-019: Email templates list loads
- TC-SET-031: Modules list loads with toggles
- TC-SET-041: All 10 tabs accessible and URL-synced
- TC-SET-044: Platform settings accessible only to platform admin
- TC-SET-050: Users list page loads with DataTable
- TC-SET-051: Create user
- TC-SET-064: Roles list page loads
- TC-SET-073: Tenants page accessible only to platform admin
- TC-SET-082: Activity timeline loads with entries
- TC-SET-091: Developer logs page loads for platform admin
- TC-SET-099: Notifications page loads

## 4. Catalog (19 cases)
- TC-CAT-001: View products list with DataTable
- TC-CAT-002: Search products by name
- TC-CAT-008: Bulk select and publish draft products
- TC-CAT-010: Bulk delete products with confirmation
- TC-CAT-026: Create new product — basic info
- TC-CAT-027: Edit product — update fields
- TC-CAT-029: Add product variant
- TC-CAT-032: Upload product images
- TC-CAT-036: Publish product from edit page
- TC-CAT-047: View categories in tree mode
- TC-CAT-049: Create new category
- TC-CAT-058: View product attributes list
- TC-CAT-059: Create new product attribute
- TC-CAT-066: View brands list
- TC-CAT-067: Create new brand
- TC-CAT-074: View inventory receipts list
- TC-CAT-077: View receipt detail
- TC-CAT-078: Confirm a StockIn receipt
- TC-CAT-079: Confirm a StockOut receipt

## 5. Orders (18 cases)
- TC-ORD-001: View orders list with DataTable
- TC-ORD-002: Search orders by customer email
- TC-ORD-003: Filter orders by status
- TC-ORD-004: Navigate to order detail from list
- TC-ORD-014: View order detail page
- TC-ORD-016: Confirm pending order
- TC-ORD-017: Ship confirmed/processing order
- TC-ORD-018: Mark order as delivered
- TC-ORD-019: Complete delivered order
- TC-ORD-020: Cancel order with reason
- TC-ORD-021: Return delivered order
- TC-ORD-022: Full lifecycle — Pending → Confirmed → Processing → Shipped → Delivered → Completed
- TC-ORD-036: Create manual order — full flow
- TC-ORD-038: Product search typeahead
- TC-ORD-047: View payments list
- TC-ORD-053: View payment detail with tabs
- TC-ORD-064: View shipping page with tabs
- TC-ORD-065: Providers tab — list providers

## 6. Customers (18 cases)
- TC-CUS-001: View customer list with stats cards
- TC-CUS-002: Search customers by name/email
- TC-CUS-005: Create new customer via dialog
- TC-CUS-006: Edit customer via actions menu
- TC-CUS-007: Delete customer with confirmation
- TC-CUS-008: Navigate to customer detail by row click
- TC-CUS-026: View customer detail page
- TC-CUS-027: Orders tab with pagination
- TC-CUS-032: Customer info sidebar
- TC-CUS-045: View customer groups list
- TC-CUS-046: Create customer group
- TC-CUS-051: View promotions list with filters
- TC-CUS-052: Create promotion with date range
- TC-CUS-054: Activate a draft promotion
- TC-CUS-064: View reviews with tab filtering
- TC-CUS-067: Approve a pending review
- TC-CUS-068: Reject a pending review with reason
- TC-CUS-075: View wishlist analytics

## 7. Content (15 cases)
- TC-CON-001: View blog posts list
- TC-CON-002: Search blog posts
- TC-CON-005: Featured image thumbnail with preview
- TC-CON-006: Navigate to edit page via row click
- TC-CON-018: Create new blog post with all fields
- TC-CON-019: Tiptap RichTextEditor — text formatting
- TC-CON-020: Tiptap RichTextEditor — image upload
- TC-CON-023: Publishing options — draft/publish/schedule
- TC-CON-040: View categories with tree/table toggle
- TC-CON-041: Create category
- TC-CON-049: View blog tags list
- TC-CON-051: Create blog tag
- TC-CON-057: View media library in grid mode (default)
- TC-CON-059: Upload media file via dialog
- TC-CON-060: FilePreviewTrigger in list view

## 8. HR (14 cases)
- TC-HR-001: View employee list with pagination
- TC-HR-002: Create employee via dialog
- TC-HR-003: Edit employee via actions dropdown
- TC-HR-004: Search employees
- TC-HR-009: Navigate to employee detail from row click
- TC-HR-012: Export employees to CSV
- TC-HR-014: Import employees from CSV
- TC-HR-020: View employee detail with all tabs
- TC-HR-029: View departments in tree view (default)
- TC-HR-031: Create department
- TC-HR-039: View tags DataTable with category grouping
- TC-HR-040: Create tag with color picker
- TC-HR-047: View org chart with ReactFlow
- TC-HR-055: View HR reports with all 4 charts

## 9. CRM (16 cases)
- TC-CRM-001: View contacts list with pagination
- TC-CRM-002: Create contact via dialog
- TC-CRM-003: Edit contact via actions dropdown
- TC-CRM-005: Search contacts
- TC-CRM-008: Navigate to contact detail from row click
- TC-CRM-012: View contact detail with tabs
- TC-CRM-022: View companies list
- TC-CRM-023: Create company
- TC-CRM-027: Navigate to company detail from row click
- TC-CRM-030: View company detail with contacts
- TC-CRM-036: View pipeline Kanban board
- TC-CRM-037: Drag lead between stages
- TC-CRM-038: Drag lead to Won system column
- TC-CRM-040: Create lead from pipeline page
- TC-CRM-048: View deal detail with tabs
- TC-CRM-049: Win deal from detail page

## 10. PM (12 cases)
- TC-PM-001: View projects in grid mode (default)
- TC-PM-002: Switch to list view
- TC-PM-003: Create a new project
- TC-PM-007: Click project card navigates to detail
- TC-PM-016: Load project detail with Kanban board
- TC-PM-017: Drag task between Kanban columns
- TC-PM-018: Quick-add task via textarea
- TC-PM-033: Task detail modal via board click
- TC-PM-045: Load task detail page
- TC-PM-047: Change task status
- TC-PM-050: Add a comment
- TC-PM-052: Add a subtask
