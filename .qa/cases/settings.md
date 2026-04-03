# Settings — Test Cases

> Pages: /portal/settings, /portal/settings/platform, /portal/users, /portal/roles, /portal/tenants, /portal/activity-timeline, /portal/developer-logs, /portal/notifications, /portal/email-templates/:id, /portal/legal-pages/:id | Last updated: 2026-04-03 | Git ref: f6f5cd3
> Total: 113 cases | P0: 5 | P1: 49 | P2: 44 | P3: 15

---

## Page: Tenant Settings — Branding Tab (`/portal/settings?tab=branding`)

### Happy Path

#### TC-SET-001: Branding tab loads with current settings [P1] [smoke]
- **Pre**: Logged in as tenant admin with TenantSettingsRead permission
- **Steps**:
  1. Navigate to `/portal/settings`
  2. Default tab "Branding" is active
- **Expected**: Branding form loads with current tenant name, logo, colors. Form fields pre-populated.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI
- **Data**: ☐ Tenant name shown | ☐ Logo preview if set

#### TC-SET-002: Update branding settings [P1]
- **Pre**: On branding tab with TenantSettingsUpdate permission
- **Steps**:
  1. Change tenant name
  2. Upload new logo
  3. Modify brand colors
  4. Click Save
- **Expected**: Success toast. Settings persisted. Page reload shows new values. Logo/name reflected in sidebar/header if applicable.

#### TC-SET-003: Branding form read-only without update permission [P2] [security]
- **Pre**: User with TenantSettingsRead but NOT TenantSettingsUpdate
- **Steps**:
  1. Navigate to branding tab
- **Expected**: Form fields visible but disabled/read-only. No Save button.

---

## Page: Tenant Settings — Contact Tab (`/portal/settings?tab=contact`)

### Happy Path

#### TC-SET-004: Contact settings load and save [P1]
- **Pre**: On contact tab
- **Steps**:
  1. Enter/modify phone, address, support email
  2. Click Save
- **Expected**: Success toast. Settings saved.
- **Data**: ☐ All fields persisted on reload

---

## Page: Tenant Settings — Regional Tab (`/portal/settings?tab=regional`)

### Happy Path

#### TC-SET-005: Regional settings load with current values [P1] [smoke]
- **Pre**: On regional tab
- **Steps**:
  1. View date format, timezone, currency, language settings
- **Expected**: Current tenant regional settings displayed. Date format, timezone, currency symbol visible.

#### TC-SET-006: Change date format and verify across app [P1] [data-consistency]
- **Pre**: On regional tab
- **Steps**:
  1. Change date format (e.g., DD/MM/YYYY to MM/DD/YYYY)
  2. Save
  3. Navigate to any list page with date columns (e.g., Users, Orders)
- **Expected**: All date displays across the application use the new format via `useRegionalSettings().formatDateTime()`.
- **Data**: ☐ Date format consistent on Users page | ☐ Date format consistent on Orders page

#### TC-SET-007: Change timezone setting [P2]
- **Pre**: On regional tab
- **Steps**:
  1. Change timezone
  2. Save
  3. Check Activity Timeline timestamps
- **Expected**: Timestamps reflect new timezone across all pages.

---

## Page: Tenant Settings — Payment Gateways Tab (`/portal/settings?tab=paymentGateways`)

### Happy Path

#### TC-SET-008: Payment gateways card grid loads [P1]
- **Pre**: On payment gateways tab
- **Steps**:
  1. View available payment gateways
- **Expected**: Gateway cards displayed (Stripe, etc.) with status indicators. Configure button on each.

#### TC-SET-009: Configure a payment gateway [P2]
- **Pre**: On payment gateways tab
- **Steps**:
  1. Click "Configure" on a gateway card
  2. ConfigureGatewayDialog opens
  3. Enter API keys/settings
  4. Save
- **Expected**: Gateway configured. Status updates to active/configured.

---

## Page: Tenant Settings — Shipping Providers Tab (`/portal/settings?tab=shippingProviders`)

### Happy Path

#### TC-SET-010: Shipping providers card grid loads [P1]
- **Pre**: On shipping providers tab
- **Steps**:
  1. View available shipping providers
- **Expected**: Provider cards displayed with status. Configure button available.

#### TC-SET-011: Configure a shipping provider [P2]
- **Pre**: On shipping providers tab
- **Steps**:
  1. Click "Configure" on a provider card
  2. ConfigureProviderDialog opens
  3. Enter provider settings
  4. Save
- **Expected**: Provider configured. Status updated.

---

## Page: Tenant Settings — SMTP Tab (`/portal/settings?tab=smtp`)

### Happy Path

#### TC-SET-012: SMTP settings load [P1] [smoke]
- **Pre**: On SMTP tab
- **Steps**:
  1. View SMTP form: host, port, username, password, sender email, sender name, use SSL switch
- **Expected**: Form shows current SMTP configuration. Override indicator if tenant overrides platform default.
- **Visual**: ☐ Light | ☐ Dark

#### TC-SET-013: Update SMTP settings [P1]
- **Pre**: On SMTP tab with update permission
- **Steps**:
  1. Change host to `smtp.example.com`
  2. Change port to `587`
  3. Enable SSL
  4. Save
- **Expected**: Success toast. Settings persisted. Override badge shown (if overriding platform).

#### TC-SET-014: Test SMTP connection [P1]
- **Pre**: SMTP configured on SMTP tab
- **Steps**:
  1. Click "Test Connection" button
- **Expected**: Test runs. Success indicator (checkmark) if connection works. Error message if connection fails.

#### TC-SET-015: Send test email [P2]
- **Pre**: SMTP configured
- **Steps**:
  1. Click "Send Test Email" (TestEmailDialog opens)
  2. Enter recipient email
  3. Send
- **Expected**: Test email sent. Success toast. Email received in inbox.

#### TC-SET-016: Revert SMTP to platform defaults [P2]
- **Pre**: Tenant has custom SMTP override
- **Steps**:
  1. Click "Revert to Platform Defaults" button
  2. Confirm
- **Expected**: SMTP settings cleared. Platform defaults used. Override badge removed.

### Edge Cases

#### TC-SET-017: Invalid SMTP host [P2] [edge-case]
- **Pre**: On SMTP tab
- **Steps**:
  1. Enter empty host
  2. Try to save
- **Expected**: Validation error "Host is required". Form not submitted.

#### TC-SET-018: Invalid SMTP port [P2] [edge-case]
- **Pre**: On SMTP tab
- **Steps**:
  1. Enter port `99999` (> 65535)
  2. Try to save
- **Expected**: Validation error on port field (must be 1-65535).

---

## Page: Tenant Settings — Email Templates Tab (`/portal/settings?tab=emailTemplates`)

### Happy Path

#### TC-SET-019: Email templates list loads [P1] [smoke]
- **Pre**: On email templates tab
- **Steps**:
  1. View email template cards
- **Expected**: Template cards displayed with name, subject, type. Edit button navigates to `/portal/email-templates/:id`.

#### TC-SET-020: Navigate to email template editor [P1]
- **Pre**: On email templates tab
- **Steps**:
  1. Click "Edit" on a template card
- **Expected**: Navigates to `/portal/email-templates/:id?from=tenant`. Editor page loads with template content.

---

## Page: Email Template Editor (`/portal/email-templates/:id`)

### Happy Path

#### TC-SET-021: Email template editor loads [P1]
- **Pre**: Navigate from tenant settings email templates tab
- **Steps**:
  1. Page loads with template data
- **Expected**: Subject field, RichTextEditor with template body, variable placeholders panel, preview button, save button. Back button returns to settings.
- **Data**: ☐ Subject pre-populated | ☐ Body content loaded in Tiptap editor

#### TC-SET-022: Edit and save email template [P1]
- **Pre**: On email template editor
- **Steps**:
  1. Modify subject line
  2. Edit body content in RichTextEditor
  3. Click Save
- **Expected**: Success toast. Template updated. Changes visible on reload.

#### TC-SET-023: Preview email template [P2]
- **Pre**: On email template editor
- **Steps**:
  1. Click "Preview" button
  2. EmailPreviewDialog opens
- **Expected**: Rendered preview with sample data replacing variable placeholders (e.g., `{{UserName}}` replaced).

#### TC-SET-024: Insert variable placeholder [P2]
- **Pre**: On email template editor
- **Steps**:
  1. Click a variable from the variables panel
- **Expected**: Variable placeholder inserted into editor at cursor position.

#### TC-SET-025: Revert template to platform default [P2]
- **Pre**: Template has been customized from platform default
- **Steps**:
  1. Click revert/reset button
  2. Confirm
- **Expected**: Template content reverted to platform default. Override indicator removed.

### Edge Cases

#### TC-SET-026: Entity conflict detection on email template [P2] [edge-case]
- **Pre**: Two users editing same template
- **Steps**:
  1. User A opens template
  2. User B saves a change to same template
  3. SignalR notifies User A
- **Expected**: EntityConflictDialog shown to User A with options to reload or continue editing.

---

## Page: Tenant Settings — Legal Pages Tab (`/portal/settings?tab=legalPages`)

### Happy Path

#### TC-SET-027: Legal pages list loads [P1]
- **Pre**: On legal pages tab
- **Steps**:
  1. View legal page cards (Terms, Privacy, etc.)
- **Expected**: Cards with page title, status (Active/Draft), last modified date. Edit button present.

#### TC-SET-028: Navigate to legal page editor [P1]
- **Pre**: On legal pages tab
- **Steps**:
  1. Click "Edit" on a legal page
- **Expected**: Navigates to `/portal/legal-pages/:id?from=tenant`. RichTextEditor loads with page content.

---

## Page: Legal Page Editor (`/portal/legal-pages/:id`)

### Happy Path

#### TC-SET-029: Legal page editor loads and saves [P1]
- **Pre**: Navigate from legal pages tab
- **Steps**:
  1. Modify content in RichTextEditor
  2. Toggle Active/Inactive switch
  3. Click Save
- **Expected**: Success toast. Content updated. Active state persisted.
- **Data**: ☐ Content saved | ☐ Active toggle persisted

#### TC-SET-030: Revert legal page to platform default [P2]
- **Pre**: Legal page has tenant customization
- **Steps**:
  1. Click revert button
  2. Confirm in dialog
- **Expected**: Content reverted to platform default.

---

## Page: Tenant Settings — Modules Tab (`/portal/settings?tab=modules`)

### Happy Path

#### TC-SET-031: Modules list loads with toggles [P1] [smoke]
- **Pre**: On modules tab
- **Steps**:
  1. View module list with enable/disable switches
- **Expected**: All available modules listed (those marked IsAvailable by platform). Core modules locked. Toggleable modules have switches.
- **Data**: ☐ Core modules shown as locked | ☐ Toggle state matches current config

#### TC-SET-032: Enable a module [P1] [data-consistency]
- **Pre**: A toggleable module is disabled (e.g., CRM)
- **Steps**:
  1. Toggle CRM module ON
  2. Save
  3. Check sidebar navigation
- **Expected**: Module enabled. CRM sidebar items appear. CRM routes accessible.
- **Data**: ☐ Sidebar updated | ☐ CRM pages accessible | ☐ Module API endpoints work

#### TC-SET-033: Disable a module [P1] [data-consistency]
- **Pre**: A toggleable module is enabled
- **Steps**:
  1. Toggle module OFF
  2. Save
  3. Check sidebar
- **Expected**: Module disabled. Related sidebar items hidden. Routes return feature-not-enabled response.
- **Data**: ☐ Sidebar items removed | ☐ Data preserved (not deleted)

---

## Page: Tenant Settings — Webhooks Tab (`/portal/settings?tab=webhooks`)

### Happy Path

#### TC-SET-034: Webhooks list loads [P1]
- **Pre**: On webhooks tab
- **Steps**:
  1. View webhooks table
- **Expected**: Table with webhook URL, events, status (Active/Inactive), last delivery status. Create button.
- **Data**: ☐ Empty state if no webhooks | ☐ Status badges correct

#### TC-SET-035: Create webhook subscription [P1]
- **Pre**: On webhooks tab
- **Steps**:
  1. Click "Create Webhook"
  2. Credenza dialog opens
  3. Enter URL, select events, optional secret
  4. Submit
- **Expected**: Webhook created. Appears in table with "Active" status.
- **Data**: ☐ Webhook in list | ☐ Events shown correctly

#### TC-SET-036: Edit webhook [P2]
- **Pre**: Webhook exists
- **Steps**:
  1. Click edit on a webhook
  2. Change URL or events
  3. Save
- **Expected**: Webhook updated. Changes reflected in table.

#### TC-SET-037: Delete webhook [P2]
- **Pre**: Webhook exists
- **Steps**:
  1. Click delete on a webhook
  2. Confirm deletion
- **Expected**: Webhook removed from table. Confirmation dialog required.

#### TC-SET-038: Enable/disable webhook [P2]
- **Pre**: Webhook exists
- **Steps**:
  1. Toggle webhook active/inactive
- **Expected**: Status badge updates. Inactive webhooks stop receiving deliveries.

#### TC-SET-039: Test webhook delivery [P2]
- **Pre**: Active webhook exists
- **Steps**:
  1. Click "Test" / ping button
- **Expected**: Test event sent. Delivery log shows result (success/failure with status code).

### Edge Cases

#### TC-SET-040: Webhook URL validation [P2] [edge-case]
- **Pre**: Creating/editing a webhook
- **Steps**:
  1. Enter invalid URL (not https)
- **Expected**: Validation error on URL field.

---

## Page: Tenant Settings — Tab Navigation

### Happy Path

#### TC-SET-041: All 10 tabs accessible and URL-synced [P1] [smoke]
- **Pre**: On tenant settings page
- **Steps**:
  1. Click each tab: Branding, Contact, Regional, Payment Gateways, Shipping Providers, SMTP, Email Templates, Legal Pages, Modules, Webhooks
- **Expected**: Each tab loads its content. URL updates with `?tab=` parameter. Active tab highlighted. Opacity transition during tab switch.
- **Visual**: ☐ Light | ☐ Dark

#### TC-SET-042: Direct URL to specific tab [P2]
- **Pre**: Logged in
- **Steps**:
  1. Navigate to `/portal/settings?tab=smtp`
- **Expected**: SMTP tab active on page load.

#### TC-SET-043: Tab overflow scroll on narrow viewport [P2] [responsive]
- **Pre**: On tenant settings, viewport < 1024px
- **Steps**:
  1. Check tab list behavior
- **Expected**: Tabs horizontally scrollable (`overflow-x-auto flex-nowrap`). All 10 tabs accessible via scroll.
- **Visual**: ☐ 768px

---

## Page: Platform Settings (`/portal/settings/platform`)

### Happy Path

#### TC-SET-044: Platform settings accessible only to platform admin [P0] [security] [smoke]
- **Pre**: Logged in as platform admin (`platform@noir.local`)
- **Steps**:
  1. Navigate to `/portal/settings/platform`
- **Expected**: Page loads with 4 tabs: SMTP, Email Templates, Legal Pages, Modules.

#### TC-SET-045: Platform settings blocked for tenant admin [P0] [security]
- **Pre**: Logged in as tenant admin (`admin@noir.local`)
- **Steps**:
  1. Navigate to `/portal/settings/platform`
- **Expected**: Access denied or redirect. Page not accessible to tenant admins.

#### TC-SET-046: Platform SMTP settings [P1]
- **Pre**: Platform admin on SMTP tab
- **Steps**:
  1. Configure platform-wide SMTP defaults
  2. Save
- **Expected**: Platform SMTP saved. Tenants without custom SMTP inherit these settings.

#### TC-SET-047: Platform modules overview [P1]
- **Pre**: Platform admin on Modules tab
- **Steps**:
  1. View module availability grid
  2. Toggle module platform availability
- **Expected**: Shows all 35 modules with IsAvailable toggles. Disabling a module at platform level hides it from all tenants. Container width expands for modules tab (`max-w-full`).
- **Data**: ☐ Core modules non-toggleable | ☐ Changes affect tenant module visibility

#### TC-SET-048: Platform email templates [P1]
- **Pre**: Platform admin on Email Templates tab
- **Steps**:
  1. View/edit platform default email templates
- **Expected**: Template list loads. Edit navigates to `/portal/email-templates/:id?from=platform`.

#### TC-SET-049: Platform legal pages [P1]
- **Pre**: Platform admin on Legal Pages tab
- **Steps**:
  1. View/edit platform default legal pages
- **Expected**: Legal page list loads. Edit navigates to `/portal/legal-pages/:id?from=platform`.

---

## Page: Users (`/portal/users`)

### Happy Path

#### TC-SET-050: Users list page loads with DataTable [P1] [smoke]
- **Pre**: Logged in with UsersRead permission
- **Steps**:
  1. Navigate to `/portal/users`
- **Expected**: DataTable with columns: Actions (44px sticky), Name, Email, Roles (badges), Status, Created At, Creator, Modified At (hidden), Editor (hidden). CardDescription shows "Showing X of Y items". Search input full-width.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI
- **Data**: ☐ Users listed | ☐ Pagination works | ☐ Sort by columns

#### TC-SET-051: Create user [P1] [smoke]
- **Pre**: Has UsersCreate permission
- **Steps**:
  1. Click "Create User" button (opens via `?dialog=create-user` URL)
  2. Fill in email, first name, last name, password
  3. Submit
- **Expected**: User created. Table refreshes. New user appears. Success toast. Dialog closes.
- **Data**: ☐ User in list | ☐ URL param cleared

#### TC-SET-052: Edit user [P1]
- **Pre**: Has UsersUpdate permission
- **Steps**:
  1. Click actions menu (EllipsisVertical) on a user row
  2. Click "Edit"
  3. EditUserDialog opens
  4. Modify name/email
  5. Save
- **Expected**: User updated. Row refreshes with new data.

#### TC-SET-053: Delete user [P1]
- **Pre**: Has UsersDelete permission
- **Steps**:
  1. Click actions > "Delete" on a user
  2. Confirmation dialog appears (DeleteUserDialog)
  3. Confirm deletion
- **Expected**: User soft-deleted. Row fades out. Table refreshes. Toast confirmation.

#### TC-SET-054: Assign roles to user [P1]
- **Pre**: Has UsersManageRoles permission
- **Steps**:
  1. Click actions > "Assign Roles" on a user
  2. AssignRolesDialog opens with role checkboxes
  3. Toggle roles
  4. Save
- **Expected**: Roles updated. User row shows new role badges.
- **Data**: ☐ Role badges updated | ☐ Permission changes take effect

#### TC-SET-055: Lock user account [P2]
- **Pre**: Has appropriate permission
- **Steps**:
  1. Click actions > "Lock" on an unlocked user
- **Expected**: User locked. Status badge changes. Locked user cannot login.

#### TC-SET-056: Unlock user account [P2]
- **Pre**: Locked user exists
- **Steps**:
  1. Click actions > "Unlock" on a locked user
- **Expected**: User unlocked. Status badge changes. User can login again.

#### TC-SET-057: Search users [P1]
- **Pre**: Multiple users exist
- **Steps**:
  1. Type name/email in search input
  2. Wait for debounced search
- **Expected**: Table filters to matching users. "Showing X of Y" count updates. Opacity transition during search.

#### TC-SET-058: Filter users by role [P2]
- **Pre**: Users with different roles exist
- **Steps**:
  1. Select a role from filter dropdown
- **Expected**: Table shows only users with selected role. Filter is URL-synced via `useTableParams`.

#### TC-SET-059: Sort users by column [P2]
- **Pre**: Users page with data
- **Steps**:
  1. Click column header (e.g., "Name")
  2. Click again for descending
- **Expected**: Table sorts by selected column. Sort indicator shown. Server-side sorting.

### Edge Cases

#### TC-SET-060: Cannot delete yourself [P2] [edge-case]
- **Pre**: Logged in user viewing own row
- **Steps**:
  1. Open actions menu on your own user row
- **Expected**: Delete option disabled or hidden for current user.

#### TC-SET-061: View user Activity Timeline link [P2]
- **Pre**: On users page
- **Steps**:
  1. Click actions > "Activity" on a user
- **Expected**: Navigates to Activity Timeline filtered by that user.

### Visual

#### TC-SET-062: Users page responsive layout [P2] [responsive] [visual]
- **Pre**: On users page
- **Steps**:
  1. Resize to 768px
- **Expected**: DataTable horizontally scrollable. PageHeader stacks. Search input remains full-width.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-SET-063: Users page audit columns [P2] [data-consistency]
- **Pre**: On users page, click Columns dropdown
- **Steps**:
  1. Enable "Modified At" and "Editor" columns
- **Expected**: Hidden columns appear. Dates use `formatDateTime()`. Column order: domain columns then audit columns last.

---

## Page: Roles (`/portal/roles`)

### Happy Path

#### TC-SET-064: Roles list page loads [P1] [smoke]
- **Pre**: Logged in with RolesRead permission
- **Steps**:
  1. Navigate to `/portal/roles`
- **Expected**: DataTable with Actions, Name, Description, Type (System/Custom badge), Permission count, audit columns. System roles (admin, user) have translated descriptions.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI

#### TC-SET-065: Create custom role [P1]
- **Pre**: Has RolesCreate permission
- **Steps**:
  1. Click "Create Role" (URL: `?dialog=create-role`)
  2. Enter role name, description
  3. Submit
- **Expected**: Role created with "Custom" type. Appears in table.

#### TC-SET-066: Edit role [P1]
- **Pre**: Has RolesUpdate permission, custom role exists
- **Steps**:
  1. Click actions > "Edit" on a custom role
  2. Change name/description
  3. Save
- **Expected**: Role updated. Table refreshes.

#### TC-SET-067: Delete custom role [P2]
- **Pre**: Has RolesDelete permission, custom role with no users assigned
- **Steps**:
  1. Click actions > "Delete" on a custom role
  2. Confirm in DeleteRoleDialog
- **Expected**: Role deleted. Row removed. System roles cannot be deleted.

#### TC-SET-068: Manage role permissions [P1] [data-consistency]
- **Pre**: Has RolesManagePermissions permission
- **Steps**:
  1. Click actions > "Permissions" on a role
  2. PermissionsDialog opens with PermissionPicker
  3. Expand categories, toggle permissions
  4. Save
- **Expected**: Permissions updated. Permission count in table updates. Users with this role gain/lose access immediately.
- **Data**: ☐ PermissionPicker shows all categories with icons | ☐ Search works | ☐ Select All/Clear All works | ☐ Category-level toggle works

#### TC-SET-069: Filter roles by type (System/Custom) [P2]
- **Pre**: Both system and custom roles exist
- **Steps**:
  1. Select "System" from type filter dropdown
- **Expected**: Only system roles shown. Select "Custom" shows only custom. "All" shows all.

### Edge Cases

#### TC-SET-070: Cannot delete system role [P2] [edge-case]
- **Pre**: System role (admin/user)
- **Steps**:
  1. Check actions menu on system role
- **Expected**: Delete action not available for system roles.

#### TC-SET-071: Duplicate role name [P2] [edge-case]
- **Pre**: Role "Manager" exists
- **Steps**:
  1. Create new role with name "Manager"
- **Expected**: Server error mapped to name field. FormErrorBanner or inline error.

### Visual

#### TC-SET-072: Roles page Vietnamese system role descriptions [P2] [i18n] [regression]
- **Pre**: Language set to Vietnamese
- **Steps**:
  1. View roles page
  2. Check system role descriptions
- **Expected**: System role descriptions (admin, user) show Vietnamese translations via `SYSTEM_ROLE_DESCRIPTION_KEYS` mapping. No raw English descriptions.

---

## Page: Tenants (`/portal/tenants`) — Platform Admin Only

### Happy Path

#### TC-SET-073: Tenants page accessible only to platform admin [P0] [security] [smoke]
- **Pre**: Logged in as platform admin
- **Steps**:
  1. Navigate to `/portal/tenants`
- **Expected**: DataTable with tenant name, domain, status (Active/Suspended/etc.), subscription, created date.

#### TC-SET-074: Tenants page blocked for tenant admin [P0] [security]
- **Pre**: Logged in as tenant admin
- **Steps**:
  1. Navigate to `/portal/tenants`
- **Expected**: Access denied or redirect.

#### TC-SET-075: Create tenant [P1]
- **Pre**: Platform admin, on tenants page
- **Steps**:
  1. Click "Create Tenant" (`?dialog=create-tenant`)
  2. Fill in name, domain, admin email, admin password
  3. Submit
- **Expected**: Tenant created with admin user. Appears in table. Status "Active".
- **Data**: ☐ Tenant in list | ☐ Admin user created | ☐ Can login as new tenant admin

#### TC-SET-076: Edit tenant details [P1]
- **Pre**: Tenant exists
- **Steps**:
  1. Click actions > "Edit" on a tenant (`?edit=tenantId`)
  2. EditTenantDialog opens on "details" tab
  3. Modify name
  4. Save
- **Expected**: Tenant updated. Table refreshes.

#### TC-SET-077: Manage tenant modules from tenant dialog [P2]
- **Pre**: On edit tenant dialog
- **Steps**:
  1. Switch to "modules" tab (`?edit=tenantId&tab=modules`)
  2. Toggle modules for this tenant
  3. Save
- **Expected**: Module availability for this specific tenant updated.

#### TC-SET-078: Delete tenant [P1]
- **Pre**: Platform admin, tenant exists
- **Steps**:
  1. Click actions > "Delete"
  2. Confirm in DeleteTenantDialog
- **Expected**: Tenant soft-deleted. Row fades out.

#### TC-SET-079: Reset tenant admin password [P2]
- **Pre**: Platform admin, tenant exists
- **Steps**:
  1. Click actions > "Reset Admin Password"
  2. ResetAdminPasswordDialog opens
  3. Enter new password
  4. Confirm
- **Expected**: Admin password reset. Can login with new password.

#### TC-SET-080: Search tenants [P2]
- **Pre**: Multiple tenants exist
- **Steps**:
  1. Type in search input
- **Expected**: Table filters by tenant name/domain.

#### TC-SET-081: Tenant status badges [P3] [visual]
- **Pre**: Tenants with various statuses
- **Steps**:
  1. View status badges in table
- **Expected**: `TenantStatusBadge` component renders correct colors via `getStatusBadgeClasses()`. Active=green, Suspended=red, etc.

---

## Page: Activity Timeline (`/portal/activity-timeline`)

### Happy Path

#### TC-SET-082: Activity timeline loads with entries [P1] [smoke]
- **Pre**: Logged in, audit entries exist
- **Steps**:
  1. Navigate to `/portal/activity-timeline`
- **Expected**: Timeline entries displayed chronologically. Each entry shows: operation icon (Create/Update/Delete), translated description, user avatar, relative time, page context.
- **Visual**: ☐ Light | ☐ Dark

#### TC-SET-083: Filter by operation type [P2]
- **Pre**: On activity timeline
- **Steps**:
  1. Select "Create" from operation filter
- **Expected**: Only create operations shown. Count updates.

#### TC-SET-084: Filter by date range [P2]
- **Pre**: On activity timeline
- **Steps**:
  1. Select date range via DateRangePicker
- **Expected**: Timeline shows entries within selected range only.

#### TC-SET-085: Filter by page context [P2]
- **Pre**: On activity timeline
- **Steps**:
  1. Select a page context from dropdown (e.g., "Users", "Orders")
- **Expected**: Only entries from selected page context shown.

#### TC-SET-086: Search activity entries [P2]
- **Pre**: On activity timeline
- **Steps**:
  1. Type search term
- **Expected**: Entries filtered by description/entity name match.

#### TC-SET-087: View activity entry details [P2]
- **Pre**: On activity timeline
- **Steps**:
  1. Click on an entry
  2. ActivityDetailsDialog opens
- **Expected**: Full details: before/after state diff (for updates), entity data, timestamp, user, IP.

#### TC-SET-088: Pagination on activity timeline [P2]
- **Pre**: Many audit entries exist
- **Steps**:
  1. Scroll to bottom, click next page
- **Expected**: Pagination component works. Next page loads.

### Regression

#### TC-SET-089: Audit descriptions translated in Vietnamese [P1] [regression] [i18n]
- **Pre**: Language set to Vietnamese
- **Steps**:
  1. Navigate to activity timeline
  2. Check entry descriptions
- **Expected**: Descriptions translated via `translateAuditDescription()`. No raw PascalCase enums (e.g., "InProgress" should be "Dang lam"). No English action words in Vietnamese mode.
- **Data**: ☐ "Created" translated | ☐ "Updated" translated | ☐ "Deleted" translated | ☐ Status values translated

#### TC-SET-090: Activity timeline relative time display [P3]
- **Pre**: On activity timeline
- **Steps**:
  1. Check timestamps on entries
- **Expected**: Uses `formatRelativeTime()` (e.g., "2h ago", "Yesterday"). NOT `formatDateTime()` — relative time is correct for timeline entries per date formatting standard.

---

## Page: Developer Logs (`/portal/developer-logs`)

### Happy Path

#### TC-SET-091: Developer logs page loads for platform admin [P1] [smoke]
- **Pre**: Logged in as platform admin
- **Steps**:
  1. Navigate to `/portal/developer-logs`
- **Expected**: 4 tabs: Live Logs (default), History, Stats, Error Clusters. SignalR connection established (connection badge green). Live log entries stream in.
- **Visual**: ☐ Light | ☐ Dark

#### TC-SET-092: Live logs tab — stream and filter [P1]
- **Pre**: On live logs tab, connected
- **Steps**:
  1. Observe real-time log entries appearing
  2. Toggle "Exceptions Only" switch
  3. Search by term
- **Expected**: Entries appear in real-time. Filter reduces visible entries. Search highlights matches.

#### TC-SET-093: Pause/resume live logs [P2]
- **Pre**: On live logs tab
- **Steps**:
  1. Click pause button
  2. New entries should stop appearing
  3. Click resume
- **Expected**: Stream pauses. Buffer continues filling. Resume shows buffered entries.

#### TC-SET-094: Change server log level [P2]
- **Pre**: On live logs tab
- **Steps**:
  1. Change log level dropdown (e.g., Information -> Warning)
- **Expected**: Server log level updated. Only Warning+ entries appear.

#### TC-SET-095: History tab — browse historical logs [P2]
- **Pre**: On History tab
- **Steps**:
  1. Select date range
  2. Browse historical entries
- **Expected**: Historical log entries loaded from server. Pagination works.

#### TC-SET-096: Stats tab [P3]
- **Pre**: On Stats tab
- **Steps**:
  1. View buffer statistics
- **Expected**: Buffer stats displayed (entry count, memory usage, etc.).

#### TC-SET-097: Error clusters tab [P2]
- **Pre**: On Error Clusters tab
- **Steps**:
  1. View error groupings
- **Expected**: Errors grouped by message pattern. Count shown per cluster.

### Security

#### TC-SET-098: Developer logs not accessible to tenant admin [P1] [security]
- **Pre**: Logged in as tenant admin
- **Steps**:
  1. Navigate to `/portal/developer-logs`
- **Expected**: Access denied. SignalR auto-connect disabled for non-platform users (prevents 403).

---

## Page: Notifications (`/portal/notifications`)

### Happy Path

#### TC-SET-099: Notifications page loads [P1] [smoke]
- **Pre**: Logged in, notifications exist
- **Steps**:
  1. Navigate to `/portal/notifications`
- **Expected**: Full notification history with NotificationList. Live indicator (green pulse) if connected. Settings button links to preferences.
- **Visual**: ☐ Light | ☐ Dark

#### TC-SET-100: Real-time notification delivery [P1]
- **Pre**: On notifications page, connected
- **Steps**:
  1. Trigger an action in another tab (e.g., create an order)
  2. Check notifications page
- **Expected**: New notification appears without page refresh. NotificationBell in header shows unread count.

#### TC-SET-101: Mark notification as read [P2]
- **Pre**: Unread notification exists
- **Steps**:
  1. Click on an unread notification
- **Expected**: Notification marked as read. Visual indicator changes. Unread count decrements.

---

## Page: Notification Preferences (`/portal/settings/notifications`)

### Happy Path

#### TC-SET-102: Notification preferences load [P1]
- **Pre**: Navigate to notification preferences
- **Steps**:
  1. View category list: System, User Action, Workflow, Security, Integration
  2. Each category has in-app toggle and email frequency selector
- **Expected**: Current preferences loaded. Category icons match config (Settings2, Users, Workflow, Shield, Bell).

#### TC-SET-103: Update notification preferences [P1]
- **Pre**: On notification preferences
- **Steps**:
  1. Disable in-app notifications for a category
  2. Change email frequency from "immediate" to "daily"
  3. Click Save
- **Expected**: Preferences saved. Toast confirmation. Disabled categories stop generating in-app notifications.
- **Data**: ☐ Settings persisted on reload | ☐ Email frequency options: none, immediate, daily, weekly

---

## Cross-Cutting: Settings Localization

#### TC-SET-104: All tenant settings tabs in Vietnamese [P1] [i18n] [visual]
- **Pre**: Language set to Vietnamese
- **Steps**:
  1. Navigate through all 10 tenant settings tabs
- **Expected**: All tab labels, form labels, descriptions, buttons in Vietnamese. No English words except allowed acronyms (SMTP, API, SSL). Tab names: use sentence case per sidebar naming convention.
- **Visual**: ☐ Light VI | ☐ Dark VI

#### TC-SET-105: No English words in Vietnamese settings UI [P1] [regression] [i18n]
- **Pre**: Language set to Vietnamese
- **Steps**:
  1. Check all settings pages for leftover English
  2. Specifically check: module names, webhook event names, SMTP labels, permission category names
- **Expected**: Pure Vietnamese except allowed exceptions (CRM, API, SMTP, SSL). No "Pipeline", "Blog" in sidebar items (per sidebar naming convention rule).

#### TC-SET-106: Settings pages PermissionPicker translation [P2] [i18n]
- **Pre**: Vietnamese mode, on role permissions dialog
- **Steps**:
  1. Open PermissionPicker
  2. Check category headers and permission descriptions
- **Expected**: All categories translated via `permissionTranslation.ts`. No raw English category names.

---

## Cross-Cutting: Data Consistency

#### TC-SET-107: Role permission changes reflect in user access [P1] [data-consistency]
- **Pre**: User has "Manager" role with UsersRead permission
- **Steps**:
  1. Remove UsersRead from Manager role
  2. Refresh as user with Manager role
  3. Navigate to `/portal/users`
- **Expected**: Users page no longer accessible. Sidebar item hidden or page shows access denied.

#### TC-SET-108: Feature toggle hides sidebar items [P1] [data-consistency] [cross-feature]
- **Pre**: CRM module is enabled, CRM sidebar items visible
- **Steps**:
  1. Go to Tenant Settings > Modules
  2. Disable CRM module
  3. Save
  4. Refresh page
- **Expected**: CRM sidebar items (Contacts, Companies, Leads, Pipeline) disappear. CRM routes return feature-not-enabled.
- **Data**: ☐ CRM sidebar items hidden | ☐ CRM data preserved

#### TC-SET-109: Platform module disable cascades to tenants [P1] [data-consistency]
- **Pre**: Platform admin, CRM available at platform level, tenant has CRM enabled
- **Steps**:
  1. Platform admin disables CRM IsAvailable
  2. Switch to tenant admin
  3. Check modules tab
- **Expected**: CRM no longer available in tenant modules list. CRM features disabled for tenant.

---

## Regression

#### TC-SET-113: ColorPicker sr-only does not cause horizontal scroll [P1] [regression]
- **Pre**: On branding tab (`/portal/settings?tab=branding`)
- **Steps**:
  1. Open branding settings page
  2. Check for horizontal scrollbar on the page
  3. Open ColorPicker (click custom color button)
  4. Verify no horizontal overflow appears
  5. Check page width is exactly viewport width (no overflow)
- **Expected**: No horizontal scrollbar. ColorPicker native `<input type="color">` is `sr-only` and does not contribute to layout. Page fits viewport.
- **Bug ref**: 76666f7d — `<Input>` (styled) → `<input>` (native hidden) for color type input
- **Visual**: ☐ Light | ☐ Dark

---

## Cross-Cutting: Visual / Dark Mode

#### TC-SET-110: All settings tabs in dark mode [P2] [dark-mode] [visual]
- **Pre**: Dark mode enabled
- **Steps**:
  1. Navigate through all tenant settings tabs
  2. Check contrast, readability, form styling
- **Expected**: All forms, cards, badges, switches properly styled for dark mode. No white-on-white or black-on-black text.
- **Visual**: ☐ Branding | ☐ Contact | ☐ Regional | ☐ SMTP | ☐ Email Templates | ☐ Legal Pages | ☐ Modules | ☐ Webhooks

#### TC-SET-111: DataTable pages dark mode consistency [P2] [dark-mode] [visual]
- **Pre**: Dark mode enabled
- **Steps**:
  1. Check Users, Roles, Tenants pages in dark mode
- **Expected**: Card backgrounds, table rows, hover states, action menus all correctly themed. Status badges maintain readability.
- **Visual**: ☐ Users | ☐ Roles | ☐ Tenants

#### TC-SET-112: Settings pages responsive at 768px [P2] [responsive] [visual]
- **Pre**: Viewport 768px
- **Steps**:
  1. Check tenant settings tab bar overflow scroll
  2. Check form layouts stack vertically
  3. Check DataTable pages horizontal scroll
- **Expected**: Tab bar scrollable. Forms single-column. Tables scrollable with sticky action column.
- **Visual**: ☐ Tenant Settings tabs | ☐ Users page | ☐ Roles page
