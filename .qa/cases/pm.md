# Project Management — Test Cases

> Pages: /portal/pm/projects, /portal/projects/:code, /portal/tasks/:id | Last updated: 2026-04-03 | Git ref: f6f5cd3
> Total: 62 cases | P0: 4 | P1: 32 | P2: 19 | P3: 7

---

## Page: Projects List (`/portal/pm/projects`)

### Happy Path

#### TC-PM-001: View projects in grid mode (default) [P1] [smoke]
- **Pre**: User has PM permissions, at least 3 projects exist
- **Steps**:
  1. Navigate to `/portal/pm/projects`
  2. Observe default view mode is Grid
  3. Verify project cards show: name, slug (uppercase), status badge, task count, member count, progress bar, due date
- **Expected**: Grid of project cards with 4 columns on XL, 3 on LG, 2 on SM, 1 on mobile. Each card has gradient header with project initial.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px
- **Data**: ☐ Task count matches completedTaskCount/taskCount | ☐ Progress bar % is correct

#### TC-PM-002: Switch to list view [P1] [smoke]
- **Pre**: Projects exist, grid view active
- **Steps**:
  1. Click the List icon in ViewModeToggle
  2. Verify URL updates to `?view=list`
  3. Verify DataTable renders with columns: Actions, Name (with color icon + slug), Status, Progress, Members, Due Date, audit columns
- **Expected**: DataTable with proper column headers, sortable columns, pagination
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-PM-003: Create a new project [P1] [smoke]
- **Pre**: User has PmProjectsCreate permission
- **Steps**:
  1. Click "Create Project" button (or navigate with `?dialog=create-project`)
  2. Fill in: name, description, due date, color, visibility (Private/Public/Team)
  3. Submit
- **Expected**: Project created, appears in list. Auto-generated `PRJ-xxx` project code visible in card slug.
- **Data**: ☐ Project code follows PRJ-xxx pattern | ☐ Status defaults to Active

#### TC-PM-004: Search projects [P1] [regression]
- **Pre**: Multiple projects exist
- **Steps**:
  1. Type project name in search input
  2. Observe search is debounced (no immediate request per keystroke)
- **Expected**: List/grid filters to matching projects. Content shows opacity transition during search.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-PM-005: Filter by status [P1] [regression]
- **Pre**: Projects with different statuses (Active, Completed, OnHold, Archived)
- **Steps**:
  1. Select "On Hold" from status filter dropdown
  2. Verify only OnHold projects appear
  3. Select "All" to clear filter
- **Expected**: Filter applies correctly. Card count badge in header updates.

#### TC-PM-006: Sort projects (grid mode) [P2] [regression]
- **Pre**: Multiple projects, grid mode
- **Steps**:
  1. Select sort dropdown: Name A-Z, Progress, Due Date, Last Updated, Newest
  2. Verify sort order changes for each
- **Expected**: Projects reorder client-side. URL updates with `?sort=` param.

#### TC-PM-007: Click project card navigates to detail [P1] [smoke]
- **Pre**: At least one project exists
- **Steps**:
  1. Click a project card (grid) or row (list)
- **Expected**: Navigates to `/portal/projects/{projectCode}` (not GUID)

#### TC-PM-008: Grid pagination [P2] [regression]
- **Pre**: More than 12 projects (default page size)
- **Steps**:
  1. View grid mode, see Previous/Next buttons at bottom
  2. Click Next
  3. Verify page counter updates (e.g., "2 / 3")
- **Expected**: New page of project cards. Previous button becomes enabled.

### Edge Cases

#### TC-PM-009: Empty state — no projects [P2] [edge-case]
- **Pre**: New tenant with no projects
- **Steps**:
  1. Navigate to `/portal/pm/projects`
- **Expected**: EmptyState component with FolderKanban icon, "No projects found" title, "Create your first project to get started" description
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-PM-010: Empty state — search with no results [P2] [edge-case]
- **Pre**: Projects exist but search term matches none
- **Steps**:
  1. Type "zzz_nonexistent" in search
- **Expected**: EmptyState with "Try adjusting your search or filters" description

#### TC-PM-011: Create button hidden without permission [P1] [security]
- **Pre**: User does NOT have PmProjectsCreate permission
- **Steps**:
  1. Navigate to projects page
- **Expected**: "Create Project" button is not rendered

#### TC-PM-012: URL dialog sync — direct link to create [P2] [regression]
- **Pre**: User has create permission
- **Steps**:
  1. Navigate to `/portal/pm/projects?dialog=create-project`
- **Expected**: Create project dialog opens immediately

#### TC-PM-013: Overdue project card styling [P2] [visual]
- **Pre**: Project with due date in the past
- **Steps**:
  1. View project card in grid
- **Expected**: Due date text is red (`text-red-500 font-medium`) with Clock icon instead of Calendar icon
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-PM-014: Due soon project card styling [P3] [visual]
- **Pre**: Project with due date within 3 days
- **Steps**:
  1. View project card
- **Expected**: Due date text is amber (`text-amber-500`)

#### TC-PM-015: Offline banner with SignalR reconnection [P2] [edge-case]
- **Pre**: SignalR connection drops (e.g., network toggle)
- **Steps**:
  1. Observe OfflineBanner appears
  2. Reconnect network
- **Expected**: Banner shows during disconnection, hides on reconnect. Data auto-refetches via `useEntityUpdateSignal`.

---

## Page: Project Detail / Kanban Board (`/portal/projects/:code`)

### Happy Path

#### TC-PM-016: Load project detail with Kanban board [P0] [smoke]
- **Pre**: Project exists with columns and tasks
- **Steps**:
  1. Navigate to `/portal/projects/{projectCode}`
  2. Verify header shows: breadcrumb (Projects / ProjectName), color icon, status badge, info tooltip, tabs (Board/List/Archived), Share button, menu
- **Expected**: Kanban board loads with columns. Default tab is "board". URL-synced via `useUrlTab`.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-PM-017: Drag task between Kanban columns [P0] [smoke]
- **Pre**: Board has at least 2 columns, a task in column 1
- **Steps**:
  1. Drag a task card from column 1 to column 2
  2. Release
- **Expected**: Task moves to column 2. Sort order calculated via midpoint algorithm. If target column maps to a status (e.g., "Done" column), task status auto-updates.
- **Data**: ☐ Task appears in new column after refresh | ☐ Status updated if column-status mapped

#### TC-PM-018: Quick-add task via textarea [P1] [smoke]
- **Pre**: Kanban board loaded
- **Steps**:
  1. Click "+" button on a column header
  2. Type task title in the quick-add textarea (raw textarea, accepted exception)
  3. Press Enter or click Add
- **Expected**: Task created in that column. Textarea clears. No success toast (PM silent-on-success policy).
- **Data**: ☐ Task has auto-generated task number | ☐ Task appears at bottom of column

#### TC-PM-019: Multi-line paste creates multiple tasks [P1] [regression]
- **Pre**: Quick-add open on a column
- **Steps**:
  1. Paste multi-line text (3+ lines) into quick-add
  2. Confirm multi-paste dialog
- **Expected**: Each non-empty line creates a separate task in that column.

#### TC-PM-020: Create task via full dialog [P1] [regression]
- **Pre**: Board loaded, `onCreateTask` callback wired
- **Steps**:
  1. Trigger task dialog (via column menu or create button)
  2. Fill in: title, description, assignee, priority, due date, labels
  3. Submit
- **Expected**: Task created with all fields. Appears in the specified column.
- **Data**: ☐ Assignee shows in task card | ☐ Priority indicator visible | ☐ Labels shown

#### TC-PM-021: Edit column name inline [P1] [regression]
- **Pre**: Board with columns
- **Steps**:
  1. Click column name (editable)
  2. Type new name, press Enter
- **Expected**: Column name updates. Column color preserved.

#### TC-PM-022: Add new column [P1] [regression]
- **Pre**: Kanban board loaded
- **Steps**:
  1. Click "Add Column" at end of board
  2. Enter column name, press Enter
- **Expected**: New column appears at the right end of the board.

#### TC-PM-023: Delete column with task migration [P1] [regression]
- **Pre**: Column with tasks, at least 2 columns exist
- **Steps**:
  1. Open column menu, click Delete
  2. Select target column to move tasks to
  3. Confirm
- **Expected**: Column deleted. Tasks moved to target column. Confirmation dialog shown (destructive styling with `border-destructive/30`).

#### TC-PM-024: Reorder columns via drag [P2] [regression]
- **Pre**: Board with 3+ columns
- **Steps**:
  1. Drag column header to reorder
- **Expected**: Columns reorder. `reorderColumnsMutation` fires with new ordered IDs.

#### TC-PM-025: Column settings dialog (color, WIP limit) [P2] [regression]
- **Pre**: Column exists
- **Steps**:
  1. Open column menu, click settings
  2. Change column color
  3. Save
- **Expected**: Column header color updates. Color is source of truth for task status dots.

#### TC-PM-026: Move all tasks from one column to another [P2] [regression]
- **Pre**: Source column has tasks, target column exists
- **Steps**:
  1. Column menu > Move all tasks
  2. Select target column
  3. Confirm
- **Expected**: All tasks moved via dedicated bulk endpoint (not individual mutate loops).

#### TC-PM-027: Duplicate column [P2] [edge-case]
- **Pre**: Column with tasks
- **Steps**:
  1. Column menu > Duplicate column
- **Expected**: New column created with same name + "(copy)" and same tasks duplicated.

#### TC-PM-028: Archive all tasks in column [P2] [regression]
- **Pre**: Column with tasks
- **Steps**:
  1. Column menu > Archive all
  2. Confirm
- **Expected**: All tasks archived via `useBulkArchiveTasks`. Tasks disappear from board.

#### TC-PM-029: Task filter popover — filter by assignee, priority, labels, due date [P1] [regression]
- **Pre**: Board with tasks having different assignees/priorities/labels
- **Steps**:
  1. Open filter popover
  2. Select assignee filter
  3. Select priority filter
  4. Observe active filter count badge
- **Expected**: Board cards filter in real-time. URL params update (`board-assignees`, `board-priorities`, etc.). Clear All resets.

#### TC-PM-030: Board search — filter tasks by title [P1] [regression]
- **Pre**: Board with tasks
- **Steps**:
  1. Type in board search input
- **Expected**: Only tasks matching search title appear in columns. Empty columns still shown.

#### TC-PM-031: Board mode — Drag / Pan / Select [P2] [regression]
- **Pre**: Kanban board loaded
- **Steps**:
  1. Toggle to Pan mode (Hand icon) — verify board scrolls on drag instead of moving cards
  2. Toggle to Select mode (Square icon) — verify lasso selection works
  3. Select multiple tasks, apply bulk action (change status)
  4. Toggle back to Drag mode
- **Expected**: Each mode works correctly. Select mode shows bulk action bar. Mode icons: MousePointer2 (drag), Hand (pan), Square (select).

#### TC-PM-032: Member drag-drop assignment [P1] [regression]
- **Pre**: Project has members, tasks on board
- **Steps**:
  1. Drag a member pill (avatar) from the member bar
  2. Drop on a task card
- **Expected**: Task gets assigned to that member. `handleCustomDrop` fires with `assigneeId`.

#### TC-PM-033: Task detail modal via board click [P1] [smoke]
- **Pre**: Tasks on board
- **Steps**:
  1. Click a task card on the board
  2. Verify URL updates with `?task={taskNumber}`
  3. Verify TaskDetailModal opens with task info
  4. Close modal
- **Expected**: Modal shows task details (title, description, status, assignee, labels, subtasks, comments). URL param clears on close.

#### TC-PM-034: Switch to List view tab [P1] [regression]
- **Pre**: Project with tasks
- **Steps**:
  1. Click "List" tab
  2. Verify URL updates with `?tab=list`
  3. Verify TaskListView renders with table
- **Expected**: Alternative table view with sorting and filtering. Click task opens TaskDetailModal.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-PM-035: Switch to Archived tab [P1] [regression]
- **Pre**: Project has archived tasks
- **Steps**:
  1. Click "Archived" tab
  2. Verify ArchivedTasksPanel renders
  3. Click a task to view detail
- **Expected**: Archived tasks shown. Can click to view detail modal.

#### TC-PM-036: Edit project via header menu [P1] [regression]
- **Pre**: Project loaded
- **Steps**:
  1. Click EllipsisVertical menu > Edit Project
  2. Modify name or description
  3. Save
- **Expected**: ProjectDialog opens with existing data. Changes saved.

#### TC-PM-037: Archive project [P1] [regression]
- **Pre**: Active project
- **Steps**:
  1. Click menu > Archive Project
  2. Confirmation dialog appears with destructive styling
  3. Confirm
- **Expected**: Project archived. Navigates to `/portal/projects`.

#### TC-PM-038: Share project / manage members [P1] [regression]
- **Pre**: Project loaded
- **Steps**:
  1. Click "Share" button or member avatars
  2. MembersManager dialog opens
  3. Add/remove a member
  4. Close
- **Expected**: Members list updates. ProjectMemberAvatars reflects changes.

#### TC-PM-039: Label management [P2] [regression]
- **Pre**: Project loaded
- **Steps**:
  1. Click menu > Labels
  2. Create/edit/delete labels with colors
  3. Close
- **Expected**: Labels available for task assignment.

### Edge Cases

#### TC-PM-040: Old GUID URL redirect [P2] [edge-case]
- **Pre**: Bookmark using old GUID-based URL `/portal/projects/{guid}`
- **Steps**:
  1. Navigate to old URL
- **Expected**: `useProjectQuery` fires (GUID detected by regex), project loads correctly.

#### TC-PM-041: Project not found [P2] [edge-case]
- **Pre**: Invalid project code
- **Steps**:
  1. Navigate to `/portal/projects/INVALID-CODE`
- **Expected**: EmptyState with KanbanSquare icon, "The project may have been deleted or you do not have access."

#### TC-PM-042: Entity conflict dialog (concurrent edit) [P2] [edge-case]
- **Pre**: Two users editing same project
- **Steps**:
  1. User A edits project, User B edits same project
  2. User A saves first
  3. User B sees EntityConflictDialog
- **Expected**: Dialog offers "Continue Editing" or "Reload". Uses `useEntityUpdateSignal`.

#### TC-PM-043: Entity deleted dialog [P2] [edge-case]
- **Pre**: Project deleted by another user while viewing
- **Steps**:
  1. Another user deletes the project
  2. SignalR signal arrives
- **Expected**: EntityDeletedDialog shows, "Go Back" navigates to `/portal/projects`.

#### TC-PM-044: Collapsed column toggle [P3] [visual]
- **Pre**: Board with columns
- **Steps**:
  1. Collapse a column (if supported)
  2. Expand it again
- **Expected**: Column collapses to minimal width, expands back.

---

## Page: Task Detail (`/portal/pm/projects/:id/tasks/:taskId`)

### Happy Path

#### TC-PM-045: Load task detail page [P1] [smoke]
- **Pre**: Task exists
- **Steps**:
  1. Navigate to `/portal/tasks/{taskId}`
  2. Verify breadcrumb: Projects / {ProjectName} / {TaskNumber}
  3. Verify two-column layout: left (title, description, subtasks, comments), right sidebar (status, priority, assignee, reporter, due date, labels, timestamps)
- **Expected**: All task data renders. Status/priority dropdowns populated. Labels shown with colors.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-PM-046: Inline edit task title [P1] [regression]
- **Pre**: Task loaded
- **Steps**:
  1. Click task title (shows Pencil icon on hover)
  2. Edit text, press Enter
- **Expected**: Title updates. Raw input used (accepted exception for inline-edit).
- **Data**: ☐ New title persists after page refresh

#### TC-PM-047: Change task status [P1] [smoke]
- **Pre**: Task loaded
- **Steps**:
  1. Open Status dropdown in sidebar
  2. Select "InProgress"
- **Expected**: Status updates. Badge color changes (gray=Todo, blue=InProgress, purple=InReview, green=Done, red=Cancelled). Only toast.error on failure.

#### TC-PM-048: Change task priority [P1] [regression]
- **Pre**: Task loaded
- **Steps**:
  1. Open Priority dropdown
  2. Select "Urgent"
- **Expected**: Priority updates. Options: Low, Medium, High, Urgent.

#### TC-PM-049: Reassign task [P1] [regression]
- **Pre**: Task loaded, project has members
- **Steps**:
  1. Open Assignee dropdown
  2. Select a different member (shows avatar initial + name)
  3. Or select "Unassigned"
- **Expected**: Assignee updates. Avatar and name reflect new assignment.

#### TC-PM-050: Add a comment [P0] [smoke]
- **Pre**: Task loaded
- **Steps**:
  1. Type in comment textarea
  2. Click "Add Comment" or press Ctrl+Enter
- **Expected**: Comment appears in list with author name, relative timestamp (`formatRelativeTime`), content. Textarea clears.
- **Data**: ☐ Comment author is current user | ☐ Timestamp uses formatRelativeTime

#### TC-PM-051: Delete a comment [P1] [regression]
- **Pre**: Task with comments
- **Steps**:
  1. Click trash icon on a comment
  2. Confirmation dialog appears (destructive styling)
  3. Confirm
- **Expected**: Comment removed. Confirmation required (destructive action rule).

#### TC-PM-052: Add a subtask [P0] [smoke]
- **Pre**: Task loaded
- **Steps**:
  1. Click "Add Subtask" button
  2. Inline form appears with input field
  3. Type subtask title, press Enter
- **Expected**: Subtask created with parent reference. Appears in subtask list with status badge, task number, title, assignee. Counter updates (e.g., "0/1").
- **Data**: ☐ Subtask shows in list | ☐ Counter updates | ☐ Subtask has parentTaskId set

#### TC-PM-053: Navigate to subtask [P1] [regression]
- **Pre**: Task has subtasks
- **Steps**:
  1. Click a subtask row (ViewTransitionLink)
- **Expected**: Navigates to `/portal/tasks/{subtaskId}`. Subtask page shows parent task link.

#### TC-PM-054: View parent task link [P2] [regression]
- **Pre**: Viewing a subtask
- **Steps**:
  1. Check sidebar for "Parent Task" section
- **Expected**: Parent task number shown as link. Clicking navigates to parent task.

#### TC-PM-055: Due date display — overdue styling [P1] [visual]
- **Pre**: Task with past due date
- **Steps**:
  1. View sidebar due date
- **Expected**: Date text is red (`text-red-500`). Uses `formatDate` from `useRegionalSettings`.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-PM-056: Labels display with colors [P2] [visual]
- **Pre**: Task with assigned labels
- **Steps**:
  1. View sidebar labels section
- **Expected**: Each label shown as Badge with `borderColor` and `color` from label data. "Edit labels" link navigates to project settings.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-PM-057: Delete task [P1] [regression]
- **Pre**: Task loaded
- **Steps**:
  1. Click EllipsisVertical menu > Delete Task
  2. Confirmation dialog appears
  3. Confirm
- **Expected**: Task soft-deleted. Navigates back (`window.history.back()`).

#### TC-PM-058: Estimated/actual hours display [P3] [visual]
- **Pre**: Task with estimated and/or actual hours set
- **Steps**:
  1. View sidebar "Estimated Hours / Actual Hours" section
- **Expected**: Shows "Xh / Yh" format. Dash for null values.

### Edge Cases

#### TC-PM-059: Task not found [P2] [edge-case]
- **Pre**: Invalid task ID
- **Steps**:
  1. Navigate to `/portal/tasks/invalid-id`
- **Expected**: EmptyState with CheckSquare icon, "The task may have been deleted or you do not have access."

#### TC-PM-060: Edited comment indicator [P3] [visual]
- **Pre**: Comment that has been edited
- **Steps**:
  1. View comment
- **Expected**: "(edited)" shown in italics after timestamp.

#### TC-PM-061: Cancel inline title edit with Escape [P3] [edge-case]
- **Pre**: Title in edit mode
- **Steps**:
  1. Edit title text
  2. Press Escape
- **Expected**: Title reverts to original value. Edit mode closes.

#### TC-PM-062: i18n — status and priority translations [P1] [i18n]
- **Pre**: Switch language to Vietnamese
- **Steps**:
  1. Check status dropdown options (Todo, InProgress, etc.)
  2. Check priority dropdown options (Low, Medium, High, Urgent)
  3. Check breadcrumb, buttons, labels
- **Expected**: All text translated. PascalCase to camelCase i18n mapping works (InProgress -> `statuses.inProgress`).
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px
