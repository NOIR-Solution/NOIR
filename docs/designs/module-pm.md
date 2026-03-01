# Module: Project Management

> Priority: **Phase 2** (after HR). Complexity: Medium. Depends on: HR (Employee as assignee).

---

## Why This Module

Developers and SME teams are NOIR's primary users. A PM module is immediately useful and showcases the platform's ERP capabilities. Task assignment references Employee from HR module.

---

## Entities

```
Project (TenantAggregateRoot<Guid>)
├── Id, Name, Slug, Description (rich text)
├── Status (Active/Completed/Archived/OnHold)
├── StartDate, EndDate, DueDate
├── OwnerId (FK → Employee), TenantId
├── Budget, Currency (default VND)
├── Color (hex), Icon (Lucide icon name)
├── Visibility (Private/Internal/Public)
├── ProjectMembers[]
└── Metadata (JSON: custom fields)

ProjectMember (TenantEntity)
├── Id, ProjectId, EmployeeId (FK → Employee)
├── Role (Owner/Manager/Member/Viewer)
├── JoinedAt
└── TenantId

ProjectTask (TenantAggregateRoot<Guid>)
├── Id, ProjectId (FK), TaskNumber (auto: PROJ-001)
├── Title, Description (rich text via TinyMCE)
├── Status (Todo/InProgress/InReview/Done/Cancelled)
├── Priority (Low/Medium/High/Urgent)
├── AssigneeId (FK → Employee), ReporterId (FK → Employee)
├── DueDate, EstimatedHours, ActualHours
├── ParentTaskId (FK → ProjectTask, self-ref for subtasks)
├── ColumnId (FK → ProjectColumn), SortOrder (for Kanban drag)
├── CompletedAt, TenantId
├── TaskLabels[] (many-to-many via ProjectTaskLabel)
└── TaskComments[]

ProjectColumn (TenantEntity)
├── Id, ProjectId (FK)
├── Name, SortOrder, Color
├── StatusMapping[] (which TaskStatus values map to this column)
├── WipLimit (optional: max tasks in column)
└── TenantId

TaskLabel (TenantEntity)
├── Id, ProjectId (FK)
├── Name, Color (hex)
└── TenantId

TaskComment (TenantEntity)
├── Id, TaskId (FK), AuthorId (FK → Employee)
├── Content (rich text), IsEdited
├── CreatedAt, UpdatedAt
└── TenantId

TimeEntry (TenantEntity)
├── Id, TaskId (FK), EmployeeId (FK)
├── StartTime, EndTime, Duration (TimeSpan)
├── Description
├── Billable (bool), BillableRate (optional)
└── TenantId
```

**Naming note**: Entity is `ProjectTask` (not `Task`) to avoid collision with `System.Threading.Tasks.Task`.

---

## Features (Commands + Queries)

### Project CRUD
| Command/Query | Description |
|---------------|-------------|
| `CreateProjectCommand` | Create project, add creator as Owner member |
| `UpdateProjectCommand` | Update name, description, dates, budget |
| `ArchiveProjectCommand` | Set status to Archived |
| `DeleteProjectCommand` | Soft delete project and all tasks |
| `GetProjectsQuery` | Paginated list, filter by status/owner |
| `GetProjectByIdQuery` | Full detail with members, column config |

### Project Members
| Command/Query | Description |
|---------------|-------------|
| `AddProjectMemberCommand` | Add employee to project with role |
| `RemoveProjectMemberCommand` | Remove member (fail if Owner) |
| `ChangeProjectMemberRoleCommand` | Change member role |
| `GetProjectMembersQuery` | List members with roles |

### Task CRUD
| Command/Query | Description |
|---------------|-------------|
| `CreateTaskCommand` | Create task in project, assign to column |
| `UpdateTaskCommand` | Update title, description, priority, dates |
| `MoveTaskCommand` | Change column/status (Kanban drag) |
| `ReorderTaskCommand` | Change SortOrder within column |
| `BulkUpdateTasksCommand` | Update status/assignee for multiple tasks |
| `DeleteTaskCommand` | Soft delete task and subtasks |
| `GetTasksQuery` | Paginated, filter by status/assignee/priority/label |
| `GetTaskByIdQuery` | Full detail with comments, subtasks, time entries |
| `GetKanbanBoardQuery` | Columns with tasks, optimized for board view |

### Task Relations
| Command/Query | Description |
|---------------|-------------|
| `AddSubtaskCommand` | Create task with ParentTaskId |
| `AddTaskCommentCommand` | Add comment to task |
| `UpdateTaskCommentCommand` | Edit comment |
| `DeleteTaskCommentCommand` | Soft delete comment |
| `AddTaskLabelCommand` | Attach label to task |
| `RemoveTaskLabelCommand` | Detach label from task |

### Time Tracking
| Command/Query | Description |
|---------------|-------------|
| `StartTimerCommand` | Start time entry for task |
| `StopTimerCommand` | Stop running timer, calculate duration |
| `AddManualTimeEntryCommand` | Log time manually |
| `DeleteTimeEntryCommand` | Remove time entry |
| `GetTimeEntriesQuery` | Filter by task/employee/date range |
| `GetTaskTimeTotalQuery` | Aggregated time per task |
| `GetProjectTimeReportQuery` | Time report grouped by member/task |

### Column Management
| Command/Query | Description |
|---------------|-------------|
| `CreateColumnCommand` | Add column to project board |
| `UpdateColumnCommand` | Rename, recolor, set WIP limit |
| `ReorderColumnsCommand` | Change column order |
| `DeleteColumnCommand` | Remove column (move tasks to another) |

---

## Frontend Pages

| Route | Page | Features |
|-------|------|----------|
| `/portal/projects` | Project list | Grid + list toggle, status filter, create project dialog |
| `/portal/projects/:id` | Project detail | Tab layout: Board, List, Timeline, Settings |
| `/portal/projects/:id/board` | Kanban board | Drag-and-drop columns + task cards (dnd-kit) |
| `/portal/projects/:id/list` | List view | Table with sorting, filtering, inline status change |
| `/portal/projects/:id/timeline` | Timeline | Read-only Gantt chart (date range bars) |
| `/portal/projects/:id/settings` | Settings | Members, labels, columns config |
| `/portal/tasks/:id` | Task detail | Full task view: description, comments, subtasks, time log, labels |

### Key UI Components
- **KanbanBoard**: Columns with drag-and-drop cards (use `@dnd-kit/core`)
- **TaskCard**: Compact card showing title, assignee avatar, priority badge, label chips, due date
- **TaskDetailPanel**: Slide-over or page with full task info, comment thread, time log
- **TimeTracker**: Start/stop button with running timer display in header
- **ProjectMemberAvatars**: Stacked avatars showing project members

---

## Integration Points

| Module | Integration |
|--------|-------------|
| **HR** | Employee as assignee/reporter/member. Department for project scoping. |
| **Users** | Fallback: if HR not enabled, use User directly as assignee. |
| **Notifications** | Task assigned, due date reminder (1 day before), comment @mention |
| **Activity Timeline** | Task status changes, member added/removed |
| **Webhooks** | task.created, task.updated, task.completed, project.archived |
| **Calendar** | Task due dates shown on calendar (Phase 5+) |
| **Documents** | Attach documents to tasks (Phase 5+) |

---

## Phased Implementation

### Phase 1 — MVP (Project + Task + Kanban)
```
Backend:
├── Domain: Project, ProjectMember, ProjectTask, ProjectColumn, TaskLabel, TaskComment
├── Application: Project CRUD, Task CRUD, Move/Reorder, Comments, Labels
├── Infrastructure: EF configs, repositories, migration
├── Endpoints: ProjectEndpoints, TaskEndpoints
├── Module: PmModuleDefinition (Features: Pm.Projects, Pm.Tasks)
├── Permissions: pm:projects:*, pm:tasks:*
└── Seed: Default columns (Todo, In Progress, In Review, Done) per new project

Frontend:
├── Pages: Project list, Kanban board, Task detail
├── Components: KanbanBoard, TaskCard, TaskDetailPanel
├── Sidebar: Projects section
├── i18n: EN + VI
└── Hooks: useProjects, useTasks, useKanbanBoard
```

### Phase 2 — Time Tracking + List View + Labels
```
├── Domain: TimeEntry
├── Commands: Timer start/stop, manual entry, time reports
├── Frontend: List view (table), TimeTracker component, Labels management
└── Reports: Project time report by member
```

### Phase 3 — Timeline + Reports + Advanced
```
├── Timeline: Read-only Gantt chart (task date ranges as horizontal bars)
├── Reports: Project progress, overdue tasks, member workload
├── Bulk operations: Bulk status change, bulk assign
├── Subtasks: Nested task hierarchy with progress roll-up
└── Project templates: Clone project structure as template
```

---

## Architecture Notes

### Module Definition
```csharp
// Application/Modules/Erp/PmModuleDefinition.cs
public sealed class PmModuleDefinition : IModuleDefinition, ISingletonService
{
    public string Name => ModuleNames.Erp.Pm;
    public string DisplayNameKey => "modules.erp.pm";
    public bool IsCore => false;
    public bool DefaultEnabled => true;
    public IReadOnlyList<FeatureDefinition> Features =>
    [
        new(ModuleNames.Erp.Pm + ".Projects", "modules.erp.pm.projects", "..."),
        new(ModuleNames.Erp.Pm + ".Tasks", "modules.erp.pm.tasks", "..."),
        new(ModuleNames.Erp.Pm + ".TimeTracking", "modules.erp.pm.timetracking", "..."),
    ];
}
```

### Kanban Implementation Notes
- **Drag-and-drop**: Use `@dnd-kit/core` + `@dnd-kit/sortable` (already popular in React ecosystem)
- **Optimistic updates**: Move task immediately on drag, rollback on API error
- **SortOrder**: Float-based ordering (allows inserting between items without reindexing all)
- **Real-time**: Optional SignalR for multi-user board sync (future enhancement)

### Default Project Setup
When creating a project, auto-seed 4 columns:
1. Todo (status: Todo)
2. In Progress (status: InProgress)
3. In Review (status: InReview)
4. Done (status: Done)

Columns are customizable per project (add, rename, reorder, delete).
