# HR Domain — Test Cases

> Pages: /portal/hr/employees, /portal/hr/employees/:id, /portal/hr/departments, /portal/hr/tags, /portal/hr/org-chart, /portal/hr/reports | Last updated: 2026-04-03 | Git ref: f6f5cd3
> Total: 78 cases | P0: 4 | P1: 38 | P2: 28 | P3: 8

---

## Page: Employees (`/portal/hr/employees`)

### Happy Path

#### TC-HR-001: View employee list with pagination [P1] [smoke]
- **Pre**: At least 25 employees exist
- **Steps**:
  1. Navigate to /portal/hr/employees
  2. Observe DataTable with columns: Name, Employee Code, Email, Department, Position, Tags, Status, Employment Type, audit columns
  3. Check "Showing X of Y items" in CardDescription
  4. Click page 2 in pagination
- **Expected**: Table renders with correct data, pagination updates URL params, page 2 shows next set
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px
- **Data**: ☐ Employee count matches API response | ☐ Audit columns visible (Created At, Creator)

#### TC-HR-002: Create employee via dialog [P1] [smoke]
- **Pre**: At least one department exists
- **Steps**:
  1. Click "Create Employee" button
  2. URL should include `?dialog=create-employee`
  3. Fill: firstName, lastName, email, phone, departmentId, position, joinDate, employmentType
  4. Check "Create user account" checkbox
  5. Submit
- **Expected**: Dialog closes, toast success, new employee appears in list with auto-generated EMP-xxx code
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px
- **Data**: ☐ Employee code starts with EMP- | ☐ Department assignment correct | ☐ User account created if checked

#### TC-HR-003: Edit employee via actions dropdown [P1] [smoke]
- **Pre**: Employee "John Doe" exists
- **Steps**:
  1. Click EllipsisVertical (actions) on John Doe's row
  2. Click "Edit"
  3. URL should include `?edit=<employeeId>`
  4. Change position to "Senior Developer"
  5. Submit
- **Expected**: Dialog pre-populated with current data, saves successfully, table updates in real-time via SignalR
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px
- **Data**: ☐ Position updated in list | ☐ ModifiedAt timestamp updated

#### TC-HR-004: Search employees [P1] [smoke]
- **Pre**: Employees with various names exist
- **Steps**:
  1. Type "John" in search input
  2. Wait for debounce
- **Expected**: Table filters to show only matching employees, "Showing X of Y" updates, opacity transition during search
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px
- **Data**: ☐ Only matching employees shown | ☐ Count accurate

#### TC-HR-005: Filter by department, status, and employment type [P1] [regression]
- **Pre**: Employees across multiple departments, statuses, types
- **Steps**:
  1. Select "Engineering" from department filter
  2. Select "Active" from status filter
  3. Select "FullTime" from employment type filter
- **Expected**: Filters stack (AND logic), table updates with transition, counts update
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px
- **Data**: ☐ Only Active FullTime Engineering employees shown

#### TC-HR-006: Deactivate employee with confirmation dialog [P1] [regression]
- **Pre**: Active employee exists
- **Steps**:
  1. Click actions on Active employee
  2. Click "Deactivate Employee"
  3. Confirmation dialog appears with destructive border
  4. Click "Deactivate Employee" button
- **Expected**: Dialog has `border-destructive/30`, employee status changes to Resigned, toast success
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px
- **Data**: ☐ Status changed to Resigned | ☐ Actions now show "Reactivate"

#### TC-HR-007: Reactivate employee [P1] [regression]
- **Pre**: Resigned/Terminated employee exists
- **Steps**:
  1. Click actions on non-Active employee
  2. Click "Reactivate Employee"
- **Expected**: No confirmation needed (direct action), status changes to Active, toast success
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px
- **Data**: ☐ Status badge turns green "Active"

#### TC-HR-008: Group by department/status/employmentType [P2] [regression]
- **Pre**: Multiple employees across groups
- **Steps**:
  1. Use grouping control in toolbar
  2. Select "Department" grouping
  3. Expand/collapse group rows
  4. Switch to "Status" grouping
- **Expected**: Rows grouped correctly, group headers show translated values (i18n), aggregation counts shown
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px
- **Data**: ☐ Group counts match actual data

#### TC-HR-009: Navigate to employee detail from row click [P1] [smoke]
- **Pre**: Employee exists
- **Steps**:
  1. Click on employee row (not on actions)
- **Expected**: Navigates to /portal/hr/employees/:id
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

### Bulk Operations

#### TC-HR-010: Bulk assign tags to selected employees [P1] [regression]
- **Pre**: Multiple employees and tags exist
- **Steps**:
  1. Select 3 employees via checkboxes
  2. BulkActionToolbar appears showing "3 selected"
  3. Click "Assign Tags"
  4. Select 2 tags in dialog (badge click toggles selection)
  5. Click "Assign Tags" button
- **Expected**: Tags assigned to all 3 employees, selection cleared, toast success
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px
- **Data**: ☐ Tags visible on employee rows via TagChips

#### TC-HR-011: Bulk change department [P1] [regression]
- **Pre**: Multiple employees and departments exist
- **Steps**:
  1. Select 2 employees
  2. Click "Change Department" in BulkActionToolbar
  3. Select new department from dropdown
  4. Click "Change Department"
- **Expected**: Department updated for both employees, selection cleared, toast success
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px
- **Data**: ☐ Department column updated for both

### Import/Export

#### TC-HR-012: Export employees to CSV [P0] [smoke]
- **Pre**: Employees exist
- **Steps**:
  1. Click ImportExportDropdown
  2. Click "Export CSV"
- **Expected**: CSV file downloads with correct employee data, respects current filters
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px
- **Data**: ☐ CSV headers match expected fields | ☐ Row count matches filtered total

#### TC-HR-013: Export employees to Excel [P1] [regression]
- **Pre**: Employees exist
- **Steps**:
  1. Click ImportExportDropdown
  2. Click "Export Excel"
- **Expected**: Excel file downloads
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-HR-014: Import employees from CSV [P0] [smoke]
- **Pre**: Valid CSV template downloaded
- **Steps**:
  1. Click ImportExportDropdown > "Download Template"
  2. Fill template with 3 new employees
  3. Click "Import CSV" and select file
  4. ImportProgressDialog appears
- **Expected**: Progress dialog shows success/error counts, new employees appear in list with EMP-xxx codes
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px
- **Data**: ☐ 3 employees created | ☐ Auto-generated codes | ☐ Departments assigned correctly

#### TC-HR-015: Import CSV with errors [P2] [edge-case]
- **Pre**: CSV with invalid data (missing required fields, duplicate emails)
- **Steps**:
  1. Import CSV with mixed valid/invalid rows
- **Expected**: ImportProgressDialog shows partial success, error rows listed with row numbers and messages
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px
- **Data**: ☐ Valid rows imported | ☐ Invalid rows reported with line numbers

### Edge Cases

#### TC-HR-016: Empty state when no employees [P2] [edge-case]
- **Pre**: No employees exist (or filter yields 0 results)
- **Steps**:
  1. Navigate to employees page (or apply filter with no results)
- **Expected**: EmptyState component with Users icon, title, description, and "Create Employee" action button
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-HR-017: Column visibility toggle [P2] [regression]
- **Pre**: Employees page loaded
- **Steps**:
  1. Open column visibility in DataTableToolbar
  2. Hide "Position" column
  3. Refresh page
- **Expected**: Position column hidden, persisted in localStorage under 'employees' key
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-HR-018: Status badges use correct colors [P3] [visual]
- **Pre**: Employees with all 4 statuses exist
- **Steps**:
  1. Verify Active = green, Suspended = yellow, Resigned = gray, Terminated = red
  2. Verify FullTime = blue, PartTime = purple, Contract = orange, Intern = cyan
- **Expected**: All badges use `variant="outline"` + `getStatusBadgeClasses()`
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-HR-019: Real-time updates via SignalR [P2] [regression]
- **Pre**: Page open in browser
- **Steps**:
  1. In another tab/session, create a new employee
  2. Observe the employees list
- **Expected**: List auto-refreshes via `useEntityUpdateSignal`, OfflineBanner shows if disconnected
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

---

## Page: Employee Detail (`/portal/hr/employees/:id`)

### Happy Path

#### TC-HR-020: View employee detail with all tabs [P1] [smoke]
- **Pre**: Employee with direct reports and tags exists
- **Steps**:
  1. Navigate to /portal/hr/employees/:id
  2. Verify header: full name, EMP-xxx code, department, status badge, employment type badge
  3. Click "Overview" tab - shows employee info grid (name, code, email, phone, department, position, manager, joinDate, notes, tags)
  4. Click "Direct Reports" tab - shows table of reports
  5. Click "Activity" tab
- **Expected**: All tabs render correctly, URL synced via `useUrlTab`
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px
- **Data**: ☐ All fields populated | ☐ Tags shown via TagChips | ☐ Direct reports clickable

#### TC-HR-021: Edit employee from detail page [P1] [regression]
- **Pre**: Employee detail page open
- **Steps**:
  1. Click "Edit Employee" in right sidebar Actions card
  2. Modify firstName
  3. Submit
- **Expected**: EmployeeFormDialog opens pre-populated, saves and refreshes detail
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-HR-022: Manage tags via TagSelector [P1] [regression]
- **Pre**: Employee detail page, user has HrTagsManage permission
- **Steps**:
  1. In Overview tab, click "Manage Tags" button
  2. TagSelector dialog opens with tags grouped by category (7 categories)
  3. Toggle tags on/off
  4. Save
- **Expected**: Tags updated, TagChips reflect changes
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px
- **Data**: ☐ Tags grouped by Team/Skill/Project/Location/Seniority/Employment/Custom

#### TC-HR-023: Deactivate from detail page [P1] [regression]
- **Pre**: Active employee detail page
- **Steps**:
  1. Click "Deactivate Employee" in Actions card
  2. Confirm in dialog
- **Expected**: Status badge changes, action button changes to "Reactivate"
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-HR-024: Navigate to direct report detail [P2] [regression]
- **Pre**: Employee with direct reports
- **Steps**:
  1. Go to "Direct Reports" tab
  2. Click on a report row
- **Expected**: Navigates to /portal/hr/employees/:reportId
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-HR-025: Back navigation [P2] [regression]
- **Pre**: On employee detail page
- **Steps**:
  1. Click back arrow button
- **Expected**: Navigates to /portal/hr/employees
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-HR-026: Entity conflict dialog on concurrent edit [P2] [edge-case]
- **Pre**: Two sessions viewing same employee
- **Steps**:
  1. Session B edits employee
  2. Session A receives SignalR update
- **Expected**: EntityConflictDialog appears with options to continue editing or reload
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-HR-027: Account info card shows user account status [P3] [visual]
- **Pre**: Employee with linked user account
- **Steps**:
  1. View Account Information card in right sidebar
- **Expected**: "User Account" shows green "Yes" or gray "No" badge, Created At and Last Modified shown with formatDateTime
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-HR-028: Employee not found [P2] [edge-case]
- **Pre**: Navigate to /portal/hr/employees/nonexistent-uuid
- **Steps**:
  1. Page loads
- **Expected**: Error message displayed with "Back to Employees" button
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

---

## Page: Departments (`/portal/hr/departments`)

### Happy Path

#### TC-HR-029: View departments in tree view (default) [P1] [smoke]
- **Pre**: Department hierarchy exists (parent with children)
- **Steps**:
  1. Navigate to /portal/hr/departments
  2. Verify default view is "Tree"
  3. Expand/collapse department nodes
- **Expected**: CategoryTreeView renders hierarchy, employee counts shown, ViewModeToggle visible
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px
- **Data**: ☐ Employee counts accurate | ☐ Parent-child hierarchy correct

#### TC-HR-030: Switch to table view [P1] [regression]
- **Pre**: Departments exist
- **Steps**:
  1. Click "List" in ViewModeToggle
  2. Verify columns: Name (with code badge), Manager, Parent Department, Employees, Sub-depts, audit columns
  3. Pagination visible
- **Expected**: DataTable renders flat list with all columns
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-HR-031: Create department [P1] [smoke]
- **Pre**: Page loaded
- **Steps**:
  1. Click "Create Department"
  2. Fill: name, code, manager, parent department (optional)
  3. Submit
- **Expected**: Department created, tree/table updates
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-HR-032: Create sub-department from context menu [P1] [regression]
- **Pre**: Parent department "Engineering" exists
- **Steps**:
  1. In tree view, use actions on "Engineering"
  2. Click "Add Sub-department"
  3. Fill form (parent should be pre-selected)
  4. Submit
- **Expected**: New department created as child of Engineering
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px
- **Data**: ☐ Parent correctly set | ☐ Child count incremented

#### TC-HR-033: Edit department [P1] [regression]
- **Pre**: Department exists
- **Steps**:
  1. Click Edit on a department (tree or table view)
  2. Change name
  3. Submit
- **Expected**: Department updated in both tree and table views
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-HR-034: Delete department with confirmation [P1] [regression]
- **Pre**: Department with no children/employees
- **Steps**:
  1. Click Delete on department
  2. DeleteDepartmentDialog appears
  3. Confirm deletion
- **Expected**: Department removed, toast success
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-HR-035: Reorder departments via drag-and-drop [P2] [regression]
- **Pre**: Multiple departments exist at same level
- **Steps**:
  1. In tree view, drag a department to new position
  2. Or change parent via drag
- **Expected**: `handleReorder` called with updated sortOrder and parentId, persisted via API
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-HR-036: Search departments [P2] [regression]
- **Pre**: Departments exist
- **Steps**:
  1. Type department name or code in search
- **Expected**: Both tree and table views filter by name/code
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

### Edge Cases

#### TC-HR-037: Delete department with children [P2] [edge-case]
- **Pre**: Department with sub-departments
- **Steps**:
  1. Attempt to delete parent department
- **Expected**: Error or warning about children, deletion blocked or cascaded safely
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-HR-038: Inactive department badge [P3] [visual]
- **Pre**: Inactive department exists
- **Steps**:
  1. View department in table view
- **Expected**: "Inactive" badge shown with gray status badge classes
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

---

## Page: Employee Tags (`/portal/hr/tags`)

### Happy Path

#### TC-HR-039: View tags DataTable with category grouping [P1] [smoke]
- **Pre**: Tags exist across 7 categories
- **Steps**:
  1. Navigate to /portal/hr/tags
  2. Verify columns: Name (with color dot), Category (with badge), Description, Employees, Sort Order (hidden), audit columns
  3. Enable category grouping via toolbar
- **Expected**: Tags grouped by category (Team/Skill/Project/Location/Seniority/Employment/Custom), group headers show translated category names
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px
- **Data**: ☐ All 7 categories present | ☐ Employee counts accurate

#### TC-HR-040: Create tag with color picker [P1] [smoke]
- **Pre**: HrTagsManage permission
- **Steps**:
  1. Click "Create Tag"
  2. URL includes `?dialog=create-employee-tag`
  3. Fill: name, category (select from 7), description, color (pick from 12 presets), sort order
  4. Submit
- **Expected**: Tag created with selected color, appears in list
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px
- **Data**: ☐ Color dot matches selected color | ☐ Category badge correct

#### TC-HR-041: Edit tag via row click [P1] [regression]
- **Pre**: Tag exists, user has HrTagsManage permission
- **Steps**:
  1. Click on tag row
  2. Edit dialog opens with `?edit=<tagId>`
  3. Change color via ColorPopover
  4. Submit
- **Expected**: Tag updated, color dot changes in list
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-HR-042: Delete tag with confirmation [P1] [regression]
- **Pre**: Tag with 0 employees
- **Steps**:
  1. Click actions > Delete
  2. DeleteEmployeeTagDialog shows tag preview
  3. Confirm
- **Expected**: Row fades out (fadeOutRow animation), tag deleted, toast success
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-HR-043: Search tags by name/description/category [P2] [regression]
- **Pre**: Multiple tags exist
- **Steps**:
  1. Type "Skill" in search
- **Expected**: Filters to tags matching name, description, or category (client-side)
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-HR-044: Category badges use correct colors [P3] [visual]
- **Pre**: Tags across all categories
- **Steps**:
  1. Verify: Team=blue, Skill=green, Project=purple, Location=yellow, Seniority=gray, Employment=red, Custom=gray
- **Expected**: Correct `getStatusBadgeClasses()` applied
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

### Edge Cases

#### TC-HR-045: No actions column without HrTagsManage permission [P2] [security]
- **Pre**: User without HrTagsManage permission
- **Steps**:
  1. Navigate to /portal/hr/tags
- **Expected**: No actions column, no "Create Tag" button, row click does nothing
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-HR-046: ColorPopover in name column [P3] [visual]
- **Pre**: Tags with various colors
- **Steps**:
  1. Click on color dot in Name column
- **Expected**: ColorPopover displays the tag's color
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

---

## Page: Org Chart (`/portal/hr/org-chart`)

### Happy Path

#### TC-HR-047: View org chart with ReactFlow [P1] [smoke]
- **Pre**: Employees with manager hierarchy exist
- **Steps**:
  1. Navigate to /portal/hr/org-chart
  2. Verify ReactFlow canvas with department and employee nodes
  3. Zoom, pan, controls visible
- **Expected**: Hierarchy rendered top-to-bottom (TB direction), edges connect managers to reports
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-HR-048: Filter org chart by department [P1] [regression]
- **Pre**: Multiple departments exist
- **Steps**:
  1. Select "Engineering" from department dropdown in Panel overlay
- **Expected**: Chart filters to show only Engineering hierarchy, fitView animates
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-HR-049: Search org chart [P2] [regression]
- **Pre**: Org chart loaded
- **Steps**:
  1. Type employee name in search input on Panel overlay
- **Expected**: Matching nodes highlighted/filtered
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-HR-050: Expand/Collapse all nodes [P2] [regression]
- **Pre**: Org chart with collapsible departments
- **Steps**:
  1. Click "Expand All"
  2. Click "Collapse All"
- **Expected**: All nodes expand/collapse with fitView animation (300ms)
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-HR-051: Double-click employee node navigates to detail [P1] [regression]
- **Pre**: Employee node visible
- **Steps**:
  1. Double-click an employee node
- **Expected**: Navigates to /portal/hr/employees/:id
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-HR-052: MiniMap appears for large org charts [P3] [visual]
- **Pre**: 20+ nodes in org chart (MINIMAP_THRESHOLD = 20)
- **Steps**:
  1. Load org chart with enough nodes
- **Expected**: MiniMap renders at bottom-right, pannable and zoomable
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

### Edge Cases

#### TC-HR-053: Empty org chart [P2] [edge-case]
- **Pre**: No employees or all filtered out
- **Steps**:
  1. Filter to department with no employees
- **Expected**: EmptyState component with Users icon
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-HR-054: Loading skeleton [P3] [visual]
- **Pre**: Slow network or first load
- **Steps**:
  1. Navigate to org chart
- **Expected**: Skeleton loader with 5 rows during loading
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

---

## Page: HR Reports (`/portal/hr/reports`)

### Happy Path

#### TC-HR-055: View HR reports with all 4 charts [P1] [smoke]
- **Pre**: Employees across departments, types, statuses with tags
- **Steps**:
  1. Navigate to /portal/hr/reports
  2. Verify summary cards: Total Active Employees, Total Departments
  3. Verify 4 charts: Headcount by Department (horizontal bar), Tag Distribution (horizontal bar with tag colors), Employment Type Breakdown (donut), Status Breakdown (donut)
- **Expected**: All charts render with correct data, Recharts tooltips themed to match card design
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px
- **Data**: ☐ Summary counts match actual | ☐ Chart values match backend

#### TC-HR-056: Chart tooltips styled correctly [P3] [visual]
- **Pre**: Reports page loaded with data
- **Steps**:
  1. Hover over bar in Headcount chart
  2. Hover over pie slice in Employment Type chart
- **Expected**: Tooltip uses card background, card foreground, border color (CHART_TOOLTIP_STYLE)
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-HR-057: Empty state when no data [P2] [edge-case]
- **Pre**: No employees exist
- **Steps**:
  1. Navigate to HR Reports
- **Expected**: EmptyState shown in each chart area, summary cards show 0
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-HR-058: Employment type labels translated [P2] [i18n]
- **Pre**: Switch to Vietnamese
- **Steps**:
  1. View Employment Type chart
  2. View Status chart
- **Expected**: Labels use `t('hr.employmentTypes.fullTime')` etc., not raw enum values
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

---

## Cross-Cutting

### Localization

#### TC-HR-059: All labels translated in Vietnamese [P1] [i18n]
- **Pre**: Switch language to VI
- **Steps**:
  1. Visit all HR pages
  2. Check all buttons, labels, tooltips, empty states, badges
- **Expected**: No English text visible (except EMP-xxx codes, email addresses)
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-HR-060: Status and employment type translations [P2] [i18n]
- **Pre**: Vietnamese locale
- **Steps**:
  1. Verify statuses: Active/Suspended/Resigned/Terminated translated
  2. Verify types: FullTime/PartTime/Contract/Intern translated
  3. Verify grouped row headers use groupValueFormatter
- **Expected**: All enum values use `t('hr.statuses.*')` and `t('hr.employmentTypes.*')`
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

### Responsive

#### TC-HR-061: Employee list at 768px [P2] [responsive]
- **Pre**: Employees exist
- **Steps**:
  1. Resize to 768px width
  2. Check table horizontal scroll, filters wrap, PageHeader responsive
- **Expected**: No content overflow, filters wrap via `flex-wrap`, responsive PageHeader collapses
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-HR-062: Employee detail at 768px [P2] [responsive]
- **Pre**: Employee detail page
- **Steps**:
  1. Resize to 768px
- **Expected**: Grid switches from 3-col to 1-col, sidebar cards stack below tabs
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

### Security

#### TC-HR-063: Unauthenticated access returns 401 [P0] [security]
- **Pre**: Not logged in
- **Steps**:
  1. Navigate to /portal/hr/employees
- **Expected**: Redirected to login
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-HR-064: Permission-gated actions [P1] [security]
- **Pre**: User without HR write permissions
- **Steps**:
  1. Navigate to employees page
  2. Check if Create button visible
  3. Check if edit/deactivate actions visible
- **Expected**: Create button hidden, action menu items hidden based on permissions
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

### Form Validation

#### TC-HR-065: Employee form required fields [P1] [regression]
- **Pre**: Open create employee dialog
- **Steps**:
  1. Leave all fields empty
  2. Click submit
  3. Fill only firstName, blur
  4. Fill valid email, blur
- **Expected**: Required asterisks auto-detected from Zod schema, errors show on blur (not on focus), errors clear on valid input
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-HR-066: Duplicate email validation [P2] [edge-case]
- **Pre**: Employee with email john@test.com exists
- **Steps**:
  1. Create new employee with john@test.com
  2. Submit
- **Expected**: Server error mapped to email field via `handleFormError`, FormErrorBanner shown
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

### Data Consistency

#### TC-HR-067: Employee count on department updates after employee changes [P1] [data-consistency]
- **Pre**: Department "Engineering" has 5 employees
- **Steps**:
  1. Create new employee in Engineering
  2. Navigate to Departments page
- **Expected**: Engineering employee count shows 6
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px
- **Data**: ☐ Count matches actual employees | ☐ SignalR updates count in real-time

#### TC-HR-068: Tag employee count updates after assignment [P2] [data-consistency]
- **Pre**: Tag "Senior" has 3 employees
- **Steps**:
  1. Assign "Senior" tag to 2 more employees via bulk
  2. Navigate to Tags page
- **Expected**: "Senior" tag shows 5 in employee count badge
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

### Performance

#### TC-HR-069: Large employee list loads within 3s [P2] [performance]
- **Pre**: 500+ employees in database
- **Steps**:
  1. Navigate to employees page
  2. Measure time to interactive
- **Expected**: First render under 3 seconds, skeleton shown during load
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

### Dark Mode

#### TC-HR-070: All HR pages render correctly in dark mode [P2] [dark-mode]
- **Pre**: Dark mode enabled
- **Steps**:
  1. Visit employees list, detail, departments, tags, org chart, reports
  2. Check badge contrast, card shadows, chart colors, org chart node colors
- **Expected**: No contrast issues, text readable, charts use CSS variables
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

### Audit Columns

#### TC-HR-071: Audit columns on all list pages [P1] [regression]
- **Pre**: Employees, departments, tags pages
- **Steps**:
  1. Verify Created At and Creator columns visible by default
  2. Enable Modified At and Editor columns via column toggle
  3. Verify dates use formatDateTime (not relative time)
- **Expected**: All 4 audit columns available, hidden ones toggle correctly, dates formatted per tenant settings
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

### URL State

#### TC-HR-072: URL dialog state for create/edit [P2] [regression]
- **Pre**: Employees page
- **Steps**:
  1. Click "Create Employee" - URL has `?dialog=create-employee`
  2. Close dialog - URL clean
  3. Click edit on employee - URL has `?edit=<id>`
  4. Refresh page with edit URL
- **Expected**: Dialog opens from URL, closes cleanly, edit resolves entity from list on refresh
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-HR-073: URL tab state on employee detail [P2] [regression]
- **Pre**: Employee detail page
- **Steps**:
  1. Click "Direct Reports" tab
  2. URL should show `?tab=directReports`
  3. Refresh page
- **Expected**: Correct tab active after refresh, default tab "overview" omitted from URL
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

### Page Size

#### TC-HR-074: Page size selector persistence [P2] [regression]
- **Pre**: Employees page
- **Steps**:
  1. Change page size to 50 via DataTablePagination
  2. Navigate away and back
- **Expected**: Page size persists in localStorage, shows 50 items per page
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

### Employee Form

#### TC-HR-075: Manager search with deferred value [P2] [regression]
- **Pre**: Create/edit employee dialog open
- **Steps**:
  1. Start typing manager name
  2. Select from search results
- **Expected**: Manager dropdown searches employees, uses `useDeferredValue` for smooth UX
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-HR-076: Date picker for join date [P2] [regression]
- **Pre**: Create employee dialog open
- **Steps**:
  1. Click join date DatePicker
  2. Select date
- **Expected**: DatePicker uses `formatDate` from `useRegionalSettings()`, date stored as ISO string
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

### Module Gating

#### TC-HR-077: HR pages hidden when module disabled [P0] [security]
- **Pre**: HR module disabled for tenant
- **Steps**:
  1. Try to navigate to /portal/hr/employees
- **Expected**: Page not accessible, sidebar items hidden
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-HR-078: Sorting on all sortable columns [P2] [regression]
- **Pre**: Employees page with data
- **Steps**:
  1. Click Name column header to sort asc
  2. Click again for desc
  3. Try sorting Employee Code, Email, Department, Status, Employment Type, Created At
- **Expected**: Server-side sorting works, URL params update, tags column not sortable
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px
