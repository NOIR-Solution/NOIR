# CRM Domain — Test Cases

> Pages: /portal/crm/contacts, /portal/crm/contacts/:id, /portal/crm/companies, /portal/crm/companies/:id, /portal/crm/pipeline, /portal/crm/pipeline/deals/:id | Last updated: 2026-04-03 | Git ref: f6f5cd3
> Total: 82 cases | P0: 4 | P1: 40 | P2: 30 | P3: 8

---

## Page: Contacts (`/portal/crm/contacts`)

### Happy Path

#### TC-CRM-001: View contacts list with pagination [P1] [smoke]
- **Pre**: At least 25 contacts exist
- **Steps**:
  1. Navigate to /portal/crm/contacts
  2. Verify columns: Name, Email, Phone, Company, Source, Leads Count, audit columns
  3. Check "Showing X of Y items" in CardDescription
  4. Navigate to page 2
- **Expected**: DataTable renders with correct data, pagination works, source badges colored
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px
- **Data**: ☐ Contact count matches API | ☐ Lead count badge accurate

#### TC-CRM-002: Create contact via dialog [P1] [smoke]
- **Pre**: Companies exist for linking
- **Steps**:
  1. Click "Create Contact" button (requires CrmContactsCreate permission)
  2. URL includes `?dialog=create-crm-contact`
  3. Fill: firstName, lastName, email, phone, jobTitle, companyId, source, notes
  4. Submit
- **Expected**: Contact created, appears in list, toast success
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px
- **Data**: ☐ Company name shown in table | ☐ Source badge correct

#### TC-CRM-003: Edit contact via actions dropdown [P1] [smoke]
- **Pre**: Contact exists, user has CrmContactsUpdate permission
- **Steps**:
  1. Click EllipsisVertical on contact row
  2. Click "Edit"
  3. URL includes `?edit=<contactId>`
  4. Change phone number
  5. Submit
- **Expected**: Dialog pre-populated, saves successfully, table row updates
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CRM-004: Delete contact with confirmation [P1] [regression]
- **Pre**: Contact exists, user has CrmContactsDelete permission
- **Steps**:
  1. Click actions > Delete
  2. Confirmation dialog with destructive border appears
  3. Click Delete button (Loader2 spinner while pending)
- **Expected**: Row fades out (fadeOutRow), contact deleted, toast success
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CRM-005: Search contacts [P1] [smoke]
- **Pre**: Multiple contacts exist
- **Steps**:
  1. Type contact name in search input (full-width, flex-1)
  2. Wait for debounce
- **Expected**: Server-side search, opacity transition during loading, count updates
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CRM-006: Filter by source [P1] [regression]
- **Pre**: Contacts with different sources (Web, Referral, Social, Cold, Event, Other)
- **Steps**:
  1. Select "Referral" from source filter dropdown
- **Expected**: Only Referral contacts shown, filter persisted in URL params
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px
- **Data**: ☐ Only Referral sources shown

#### TC-CRM-007: Source badges use correct colors [P3] [visual]
- **Pre**: Contacts with all 6 sources
- **Steps**:
  1. Verify: Web=blue, Referral=green, Social=purple, Cold=gray, Event=amber, Other=gray
- **Expected**: All badges use `variant="outline"` + `getStatusBadgeClasses()`
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CRM-008: Navigate to contact detail from row click [P1] [smoke]
- **Pre**: Contact exists
- **Steps**:
  1. Click on contact row (not on actions)
- **Expected**: Navigates to /portal/crm/contacts/:id
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

### Edge Cases

#### TC-CRM-009: Empty state when no contacts [P2] [edge-case]
- **Pre**: No contacts exist
- **Steps**:
  1. Navigate to contacts page
- **Expected**: EmptyState with Contact icon, create action if user has permission
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CRM-010: Actions column hidden without permissions [P2] [security]
- **Pre**: User without CrmContactsUpdate AND CrmContactsDelete
- **Steps**:
  1. Navigate to contacts page
- **Expected**: No actions column rendered, Create button hidden without CrmContactsCreate
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CRM-011: Column visibility and reorder persistence [P2] [regression]
- **Pre**: Contacts page loaded
- **Steps**:
  1. Hide Phone column
  2. Reorder columns
  3. Refresh page
- **Expected**: Settings persisted in localStorage under 'crm-contacts' key
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

---

## Page: Contact Detail (`/portal/crm/contacts/:id`)

### Happy Path

#### TC-CRM-012: View contact detail with tabs [P1] [smoke]
- **Pre**: Contact with linked company, leads, and activities exists
- **Steps**:
  1. Navigate to /portal/crm/contacts/:id
  2. Verify header: full name, email, Edit button (if permission)
  3. Overview tab: email, phone, jobTitle, company link, owner, source badge, linked customer, created date
  4. Deals tab: list of leads with title, stage, value (currency formatted), status badge
  5. Activities tab: ActivityTimeline component
- **Expected**: All tabs render, URL synced via `useUrlTab` (default: overview)
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px
- **Data**: ☐ Company link navigates to /portal/crm/companies/:id | ☐ Lead values formatted with Intl.NumberFormat

#### TC-CRM-013: Edit contact from detail page [P1] [regression]
- **Pre**: Contact detail, user has CrmContactsUpdate
- **Steps**:
  1. Click "Edit" button
  2. ContactDialog opens pre-populated
  3. Modify and submit
- **Expected**: Contact updated, page refreshes
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CRM-014: Navigate to linked company [P1] [regression]
- **Pre**: Contact linked to company
- **Steps**:
  1. In overview tab, click company name link (has ExternalLink icon)
- **Expected**: Navigates to /portal/crm/companies/:companyId
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CRM-015: Navigate to linked deal from deals tab [P1] [regression]
- **Pre**: Contact has associated leads
- **Steps**:
  1. Go to "Deals" tab
  2. Click on a deal row
- **Expected**: Navigates to /portal/crm/pipeline/deals/:leadId
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CRM-016: Lead status badges on deals tab [P2] [visual]
- **Pre**: Contact with Active, Won, Lost deals
- **Steps**:
  1. Go to Deals tab
  2. Verify: Active=blue, Won=green, Lost=red
- **Expected**: Correct badge colors on each deal
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CRM-017: Navigate to linked customer [P2] [cross-feature]
- **Pre**: Contact with linked customerId
- **Steps**:
  1. In overview tab, click "View Customer" link
- **Expected**: Navigates to /portal/ecommerce/customers/:customerId
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CRM-018: Activity timeline on contact [P1] [regression]
- **Pre**: Contact with CRM activities
- **Steps**:
  1. Click "Activities" tab
- **Expected**: ActivityTimeline renders with contactId, shows activity entries
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CRM-019: Empty deals state [P2] [edge-case]
- **Pre**: Contact with no leads
- **Steps**:
  1. Go to "Deals" tab
- **Expected**: EmptyState with Contact icon
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CRM-020: Entity conflict on concurrent edit [P2] [edge-case]
- **Pre**: Two sessions on same contact
- **Steps**:
  1. Session B edits contact
  2. Session A receives SignalR update
- **Expected**: EntityConflictDialog appears
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CRM-021: Contact not found [P2] [edge-case]
- **Pre**: Navigate to /portal/crm/contacts/nonexistent-uuid
- **Steps**:
  1. Page loads
- **Expected**: EmptyState with "Not Found" message
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

---

## Page: Companies (`/portal/crm/companies`)

### Happy Path

#### TC-CRM-022: View companies list [P1] [smoke]
- **Pre**: Companies exist
- **Steps**:
  1. Navigate to /portal/crm/companies
  2. Verify columns: Name, Domain, Industry, Owner, Contacts Count, audit columns
  3. Check "Showing X of Y items"
- **Expected**: DataTable renders, contact count badges accurate
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px
- **Data**: ☐ Contact counts match actual linked contacts

#### TC-CRM-023: Create company [P1] [smoke]
- **Pre**: CrmCompaniesCreate permission
- **Steps**:
  1. Click "Create Company"
  2. URL includes `?dialog=create-crm-company`
  3. Fill: name, domain, industry, website, phone, address, taxId, employeeCount, owner, notes
  4. Submit
- **Expected**: Company created, appears in list
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CRM-024: Edit company via actions [P1] [regression]
- **Pre**: Company exists, CrmCompaniesUpdate permission
- **Steps**:
  1. Click actions > Edit
  2. Modify industry
  3. Submit
- **Expected**: Company updated in list
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CRM-025: Delete company with confirmation [P1] [regression]
- **Pre**: Company exists, CrmCompaniesDelete permission
- **Steps**:
  1. Click actions > Delete
  2. Confirmation dialog with destructive border
  3. Confirm
- **Expected**: Row fades out, company deleted, toast success
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CRM-026: Search companies [P1] [regression]
- **Pre**: Multiple companies
- **Steps**:
  1. Type company name in search
- **Expected**: Server-side search, table filters
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CRM-027: Navigate to company detail from row click [P1] [smoke]
- **Pre**: Company exists
- **Steps**:
  1. Click on company row
- **Expected**: Navigates to /portal/crm/companies/:id
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

### Edge Cases

#### TC-CRM-028: Empty state when no companies [P2] [edge-case]
- **Pre**: No companies exist
- **Steps**:
  1. Navigate to companies page
- **Expected**: EmptyState with Building2 icon, create action if permitted
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CRM-029: Permission-gated UI on companies [P2] [security]
- **Pre**: User without CRM company permissions
- **Steps**:
  1. Verify Create button, Edit/Delete actions visibility based on permissions
- **Expected**: UI elements hidden per permission
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

---

## Page: Company Detail (`/portal/crm/companies/:id`)

### Happy Path

#### TC-CRM-030: View company detail with contacts [P1] [smoke]
- **Pre**: Company with linked contacts
- **Steps**:
  1. Navigate to /portal/crm/companies/:id
  2. Verify header: company name, industry subtitle, Edit button
  3. Details card: domain, phone, website (external link), address, owner, taxId, employeeCount, created date
  4. Notes card (if notes exist)
  5. Contacts list: avatar, name, email, job title, lead count badge
- **Expected**: All data renders, contacts clickable
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px
- **Data**: ☐ Website link opens in new tab | ☐ Contact count matches list

#### TC-CRM-031: Edit company from detail page [P1] [regression]
- **Pre**: Company detail, CrmCompaniesUpdate permission
- **Steps**:
  1. Click "Edit" button
  2. CompanyDialog opens pre-populated
  3. Modify and submit
- **Expected**: Company updated, page refreshes
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CRM-032: Navigate to contact from company contacts list [P1] [regression]
- **Pre**: Company with linked contacts
- **Steps**:
  1. Click on a contact row in the contacts list
- **Expected**: Navigates to /portal/crm/contacts/:contactId
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CRM-033: Empty contacts list [P2] [edge-case]
- **Pre**: Company with no linked contacts
- **Steps**:
  1. View contacts section
- **Expected**: EmptyState with Contact icon
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CRM-034: Entity conflict dialog [P2] [edge-case]
- **Pre**: Two sessions on same company
- **Steps**:
  1. Session B edits company
  2. Session A receives SignalR update
- **Expected**: EntityConflictDialog appears
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CRM-035: Company not found [P2] [edge-case]
- **Pre**: Invalid company ID
- **Steps**:
  1. Navigate to /portal/crm/companies/nonexistent-uuid
- **Expected**: EmptyState with "Not Found"
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

---

## Page: Pipeline/Kanban (`/portal/crm/pipeline`)

### Happy Path

#### TC-CRM-036: View pipeline Kanban board [P0] [smoke]
- **Pre**: Default pipeline with stages and leads exists
- **Steps**:
  1. Navigate to /portal/crm/pipeline
  2. Verify KanbanBoard renders with stage columns
  3. Each column shows StageColumnHeader with stage name and lead count
  4. Each card shows LeadCard with title, contact name, value, owner
- **Expected**: All stages visible, cards positioned in correct stages, system columns (Won/Lost) at edges
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px
- **Data**: ☐ Lead values formatted correctly | ☐ Card counts per stage accurate

#### TC-CRM-037: Drag lead between stages [P0] [smoke]
- **Pre**: Active lead in "Qualification" stage
- **Steps**:
  1. Drag lead card from "Qualification" to "Negotiation"
  2. Release card
- **Expected**: `useMoveLeadStage` mutation fires, card moves to new column, no page reload
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px
- **Data**: ☐ Lead stage updated in backend | ☐ Stage counts update

#### TC-CRM-038: Drag lead to Won system column [P0] [smoke]
- **Pre**: Active lead exists, Won system column visible
- **Steps**:
  1. Drag lead to Won column
  2. Confirmation dialog appears
  3. Confirm win
- **Expected**: `useWinLead` mutation fires, lead status changes to Won, card moves to Won column
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px
- **Data**: ☐ Lead status = Won | ☐ wonAt timestamp set

#### TC-CRM-039: Drag lead to Lost system column with reason [P1] [regression]
- **Pre**: Active lead exists, Lost system column visible
- **Steps**:
  1. Drag lead to Lost column
  2. Confirmation dialog appears with Textarea for reason
  3. Enter lost reason
  4. Confirm
- **Expected**: `useLoseLead` mutation fires with reason, lead in Lost column
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px
- **Data**: ☐ Lead status = Lost | ☐ lostAt timestamp set | ☐ lostReason saved

#### TC-CRM-040: Create lead from pipeline page [P1] [smoke]
- **Pre**: Pipeline selected, CrmLeadsCreate permission
- **Steps**:
  1. Click "Create Lead" button
  2. URL includes `?dialog=create-crm-lead`
  3. Fill: title, contactId, value, currency, expectedCloseDate, notes
  4. Submit
- **Expected**: Lead created in first stage of selected pipeline, card appears on board
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px
- **Data**: ☐ Lead in correct pipeline | ☐ Value and currency correct

#### TC-CRM-041: Search leads on Kanban board [P1] [regression]
- **Pre**: Multiple leads exist
- **Steps**:
  1. Type lead title or contact name in search input
- **Expected**: Cards filtered across all columns (client-side via `filterCards`), non-matching cards hidden
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CRM-042: Switch between pipelines [P1] [regression]
- **Pre**: Multiple pipelines exist
- **Steps**:
  1. Verify pipeline selector dropdown visible (only when >1 pipeline)
  2. Select second pipeline
- **Expected**: Board reloads with new pipeline stages and leads, default pipeline marked
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CRM-043: Click lead card opens detail modal [P1] [regression]
- **Pre**: Lead card visible on board
- **Steps**:
  1. Click on a lead card
  2. LeadDetailModal opens
- **Expected**: Modal shows lead details without full page navigation
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

### Edge Cases

#### TC-CRM-044: Empty pipeline (no leads) [P2] [edge-case]
- **Pre**: Pipeline with stages but no leads
- **Steps**:
  1. View Kanban board
- **Expected**: Empty columns displayed, EmptyState shown if no stages exist
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CRM-045: No pipelines exist [P2] [edge-case]
- **Pre**: No pipelines configured
- **Steps**:
  1. Navigate to pipeline page
- **Expected**: Appropriate empty state or prompt to create pipeline
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CRM-046: Pipeline selector hidden with single pipeline [P3] [visual]
- **Pre**: Only one pipeline exists
- **Steps**:
  1. Check if pipeline selector visible
- **Expected**: Selector hidden (conditional: `pipelines.length > 1`)
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CRM-047: Cancel terminate confirmation [P2] [regression]
- **Pre**: Drag lead to Won/Lost column, dialog appears
- **Steps**:
  1. Click Cancel in confirmation dialog
- **Expected**: Lead returns to original column, no mutation fired
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

---

## Page: Deal Detail (`/portal/crm/pipeline/deals/:id`)

### Happy Path

#### TC-CRM-048: View deal detail with tabs [P1] [smoke]
- **Pre**: Active lead with contact and company links
- **Steps**:
  1. Navigate to /portal/crm/pipeline/deals/:id
  2. Verify header: title, status badge (Active=blue, Won=green, Lost=red), pipeline/stage subtitle
  3. Overview tab: value (currency formatted), stage dot with stageColor, expectedCloseDate, owner, contact link, company link
  4. Activities tab: ActivityTimeline with leadId
- **Expected**: All data renders, URL tabs synced, links clickable
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px
- **Data**: ☐ Value formatted via Intl.NumberFormat | ☐ Stage color dot matches stageColor

#### TC-CRM-049: Win deal from detail page [P1] [smoke]
- **Pre**: Active deal, CrmLeadsManage permission
- **Steps**:
  1. Click "Win" button (green themed)
  2. Confirmation dialog appears
  3. Confirm
- **Expected**: Status changes to Won, wonAt timestamp shown in green, Win/Lose buttons replaced with Reopen
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px
- **Data**: ☐ Status badge = Won (green) | ☐ wonAt displayed with formatDateTime

#### TC-CRM-050: Lose deal with reason from detail page [P1] [regression]
- **Pre**: Active deal, CrmLeadsManage permission
- **Steps**:
  1. Click "Lose" button (red themed)
  2. Confirmation dialog with destructive border
  3. Enter lost reason in Textarea
  4. Confirm
- **Expected**: Status = Lost, lostAt + lostReason shown in red, Reopen button available
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px
- **Data**: ☐ lostReason saved | ☐ lostAt displayed

#### TC-CRM-051: Reopen closed deal [P1] [regression]
- **Pre**: Won or Lost deal, CrmLeadsManage permission
- **Steps**:
  1. Click "Reopen" button
  2. Confirmation dialog
  3. Confirm
- **Expected**: Status changes back to Active, Win/Lose buttons reappear
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CRM-052: Navigate to linked contact from deal [P1] [regression]
- **Pre**: Deal with contact
- **Steps**:
  1. In overview, click contact name link (ExternalLink icon)
- **Expected**: Navigates to /portal/crm/contacts/:contactId
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CRM-053: Navigate to linked company from deal [P2] [regression]
- **Pre**: Deal with company link
- **Steps**:
  1. Click company name link
- **Expected**: Navigates to /portal/crm/companies/:companyId
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CRM-054: Activity timeline on deal [P1] [regression]
- **Pre**: Deal with CRM activities
- **Steps**:
  1. Click "Activities" tab
- **Expected**: ActivityTimeline renders with leadId
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CRM-055: Deal with notes [P2] [visual]
- **Pre**: Deal with notes field filled
- **Steps**:
  1. View overview tab
- **Expected**: Notes card renders with `whitespace-pre-wrap`
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

### Edge Cases

#### TC-CRM-056: Deal not found [P2] [edge-case]
- **Pre**: Invalid deal ID
- **Steps**:
  1. Navigate to /portal/crm/pipeline/deals/nonexistent-uuid
- **Expected**: EmptyState with Kanban icon, "Not Found"
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CRM-057: Entity conflict on deal [P2] [edge-case]
- **Pre**: Two sessions on same deal
- **Steps**:
  1. Session B moves deal to new stage
  2. Session A observes
- **Expected**: EntityConflictDialog appears
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CRM-058: LeadStatusActions hidden without CrmLeadsManage [P2] [security]
- **Pre**: User without CrmLeadsManage permission
- **Steps**:
  1. View deal detail
- **Expected**: Win/Lose/Reopen buttons not rendered
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

---

## Cross-Cutting

### Localization

#### TC-CRM-059: All CRM labels translated in Vietnamese [P1] [i18n]
- **Pre**: Switch to Vietnamese
- **Steps**:
  1. Visit all CRM pages: contacts, companies, pipeline, deal detail
  2. Check all buttons, labels, tooltips, empty states, badges
- **Expected**: No English text visible (except email addresses, URLs, currency codes)
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CRM-060: "Pipeline" translated to Vietnamese [P1] [i18n]
- **Pre**: Vietnamese locale
- **Steps**:
  1. Check sidebar label for Pipeline
  2. Check page title
  3. Check any reference to "Pipeline" in UI
- **Expected**: Must be "Quy trinh ban hang" (pure Vietnamese), NOT "Pipeline" or "Giá trị Pipeline"
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CRM-061: Contact source labels translated [P2] [i18n]
- **Pre**: Vietnamese locale
- **Steps**:
  1. Check source badges: Web, Referral, Social, Cold, Event, Other
  2. Check source filter dropdown
- **Expected**: All use `t('crm.sources.*')` translations
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CRM-062: Lead status labels translated [P2] [i18n]
- **Pre**: Vietnamese locale
- **Steps**:
  1. Verify Active/Won/Lost badges on deals tab and deal detail
- **Expected**: Use `t('crm.statuses.*')` translations
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CRM-063: No English words mixed in Vietnamese CRM UI [P1] [i18n]
- **Pre**: Vietnamese locale, all CRM pages
- **Steps**:
  1. Scan sidebar items under CRM section
  2. Scan page headers, card titles, empty states
  3. Check dashboard CRM widgets if visible
- **Expected**: Pure Vietnamese - no "Pipeline", "Leads", "Contacts" as labels. Sidebar: "Quy trinh ban hang", not "Pipeline"
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

### Responsive

#### TC-CRM-064: Contacts list at 768px [P2] [responsive]
- **Pre**: Contacts exist
- **Steps**:
  1. Resize to 768px
  2. Check table scroll, filter layout, header
- **Expected**: Table scrolls horizontally, filter wraps, responsive header
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CRM-065: Pipeline Kanban at 768px [P2] [responsive]
- **Pre**: Pipeline with 5+ stages
- **Steps**:
  1. Resize to 768px
  2. Check horizontal scrolling of Kanban columns
- **Expected**: Columns scroll horizontally, drag still functional
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CRM-066: Deal detail at 768px [P2] [responsive]
- **Pre**: Deal detail page
- **Steps**:
  1. Resize to 768px
- **Expected**: Grid switches to single column
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

### Security

#### TC-CRM-067: Unauthenticated access returns 401 [P0] [security]
- **Pre**: Not logged in
- **Steps**:
  1. Navigate to /portal/crm/contacts
- **Expected**: Redirected to login
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CRM-068: CRM module gating [P0] [security]
- **Pre**: CRM module disabled for tenant
- **Steps**:
  1. Navigate to /portal/crm/contacts
- **Expected**: Page not accessible, sidebar items hidden
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

### Dark Mode

#### TC-CRM-069: All CRM pages in dark mode [P2] [dark-mode]
- **Pre**: Dark mode enabled
- **Steps**:
  1. Visit contacts list, contact detail, companies, company detail, pipeline, deal detail
  2. Check badge contrast, card shadows, Kanban column backgrounds, LeadStatusActions button theming
- **Expected**: No contrast issues, green/red themed buttons have dark mode variants (dark:text-green-400, dark:border-green-800, etc.)
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

### Audit Columns

#### TC-CRM-070: Audit columns on contacts and companies list [P1] [regression]
- **Pre**: Contacts and companies pages
- **Steps**:
  1. Verify Created At and Creator visible by default
  2. Toggle Modified At and Editor from column visibility
  3. Dates use formatDateTime
- **Expected**: All 4 audit columns available on both list pages
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

### URL State

#### TC-CRM-071: URL dialog state for create/edit contacts [P2] [regression]
- **Pre**: Contacts page
- **Steps**:
  1. Click Create - URL has `?dialog=create-crm-contact`
  2. Close, click Edit - URL has `?edit=<id>`
  3. Refresh with edit URL
- **Expected**: Dialogs open/close from URL correctly
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CRM-072: URL tab state on contact detail [P2] [regression]
- **Pre**: Contact detail page
- **Steps**:
  1. Click "Deals" tab - URL has `?tab=deals`
  2. Refresh
- **Expected**: Deals tab active after refresh
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CRM-073: URL tab state on deal detail [P2] [regression]
- **Pre**: Deal detail page
- **Steps**:
  1. Click "Activities" tab - URL has `?tab=activities`
  2. Refresh
- **Expected**: Activities tab active after refresh
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

### Data Consistency

#### TC-CRM-074: Company contact count updates after contact CRUD [P1] [data-consistency]
- **Pre**: Company "Acme" has 3 contacts
- **Steps**:
  1. Create new contact linked to Acme
  2. Navigate to companies list
  3. Navigate to Acme detail
- **Expected**: Contact count badge shows 4, contacts list shows 4 items
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CRM-075: Contact lead count updates after deal changes [P1] [data-consistency]
- **Pre**: Contact has 2 leads
- **Steps**:
  1. Create new lead for this contact
  2. Check contacts list
- **Expected**: Lead count badge shows 3
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CRM-076: Pipeline view updates after lead stage change [P1] [data-consistency]
- **Pre**: Lead in "Qualification", pipeline board open
- **Steps**:
  1. In another session, move lead via API or detail page
  2. Observe Kanban board
- **Expected**: Board reflects new stage via real-time update
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

### Form Validation

#### TC-CRM-077: Contact form required fields [P1] [regression]
- **Pre**: Open create contact dialog
- **Steps**:
  1. Submit empty form
  2. Fill firstName, blur
  3. Enter invalid email, blur
  4. Enter valid email, blur
- **Expected**: Required asterisks from schema, errors on blur (not focus), errors clear on valid input
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CRM-078: Lead form required fields [P1] [regression]
- **Pre**: Open create lead dialog
- **Steps**:
  1. Submit empty
  2. Fill title, contactId, value
  3. Submit
- **Expected**: Validation follows "reward early, punish late" pattern
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

### Performance

#### TC-CRM-079: Kanban board with 100+ leads loads smoothly [P2] [performance]
- **Pre**: Pipeline with 100+ leads across stages
- **Steps**:
  1. Navigate to pipeline page
  2. Drag cards
- **Expected**: Board renders within 3s, drag-drop smooth (no lag)
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

### Real-Time Updates

#### TC-CRM-080: SignalR entity update signals on all CRM entities [P2] [regression]
- **Pre**: CRM pages open
- **Steps**:
  1. In another session, modify contact/company/lead
  2. Observe list and detail pages
- **Expected**: Lists auto-refresh via `useEntityUpdateSignal`, detail pages show conflict dialog if edited
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

### Page Size

#### TC-CRM-081: Page size persistence on contacts and companies [P2] [regression]
- **Pre**: Contacts page
- **Steps**:
  1. Change page size to 50
  2. Navigate away and back
- **Expected**: Page size persisted in localStorage under 'crm-contacts' / 'crm-companies' keys
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

### Back Navigation

#### TC-CRM-082: Back buttons on all detail pages [P2] [regression]
- **Pre**: Contact detail, company detail, deal detail pages
- **Steps**:
  1. Click back arrow on each detail page
- **Expected**: Contact -> /portal/crm/contacts, Company -> /portal/crm/companies, Deal -> /portal/crm/pipeline
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px
