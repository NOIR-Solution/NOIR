import { useMemo, useCallback } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate, useSearchParams } from 'react-router-dom'
import {
  AlertTriangle,
  ArrowDown,
  ArrowUp,
  ChevronDown,
  ChevronUp,
  ChevronsUpDown,
  Circle,
  Layers,
  ListTodo,
  Minus,
  Search,
  X,
} from 'lucide-react'
import {
  Avatar,
  Badge,
  DropdownMenu,
  DropdownMenuCheckboxItem,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
  EmptyState,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Skeleton,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@uikit'
import { getStatusBadgeClasses } from '@/utils/statusBadge'
import { useKanbanBoardQuery } from '@/portal-app/pm/queries'
import type { ProjectTaskStatus, TaskCardDto, TaskPriority, TaskLabelBriefDto, ProjectMemberDto } from '@/types/pm'
import { TaskFilterPopover, matchDueDate, matchCompletion, type DueDateFilter, type CompletionFilter } from '@/portal-app/pm/components/TaskFilterPopover'
import { TaskSearchInput } from '@/portal-app/pm/components/TaskSearchInput'

// ─── Constants ───────────────────────────────────────────────────────────────

const PRIORITY_VALUES = ['Low', 'Medium', 'High', 'Urgent'] as const

const PRIORITY_ORDER: Record<TaskPriority, number> = {
  Urgent: 4,
  High: 3,
  Medium: 2,
  Low: 1,
}

const statusColorMap: Record<ProjectTaskStatus, 'blue' | 'purple' | 'green' | 'gray'> = {
  Todo: 'gray',
  InProgress: 'blue',
  InReview: 'purple',
  Done: 'green',
  Cancelled: 'gray',
}

const STATUS_I18N_KEYS: Record<string, string> = {
  Todo: 'todo',
  InProgress: 'inProgress',
  InReview: 'inReview',
  Done: 'done',
  Cancelled: 'cancelled',
}

const PRIORITY_ICONS = {
  Low: ArrowDown,
  Medium: Minus,
  High: ArrowUp,
  Urgent: AlertTriangle,
} as const

const PRIORITY_CLASSES: Record<TaskPriority, string> = {
  Low: 'bg-slate-100 text-slate-600 border-slate-200',
  Medium: 'bg-blue-50 text-blue-600 border-blue-200',
  High: 'bg-orange-50 text-orange-600 border-orange-200',
  Urgent: 'bg-red-50 text-red-600 border-red-200',
}


const PRIORITY_COLOR_MAP: Record<TaskPriority, string> = {
  Low: 'text-slate-500',
  Medium: 'text-blue-500',
  High: 'text-orange-500',
  Urgent: 'text-red-500',
}

// ─── SortableHead ─────────────────────────────────────────────────────────────

interface SortableHeadProps {
  field: string
  sortField: string
  sortDir: 'asc' | 'desc'
  onSort: (field: string) => void
  children: React.ReactNode
}

const SortableHead = ({ field, sortField, sortDir, onSort, children }: SortableHeadProps) => {
  const isActive = sortField === field
  return (
    <TableHead
      className="cursor-pointer select-none hover:bg-muted/50 transition-colors"
      onClick={() => onSort(field)}
    >
      <div className="flex items-center gap-1">
        {children}
        <span className="text-muted-foreground">
          {isActive ? (
            sortDir === 'asc' ? (
              <ChevronUp className="h-3.5 w-3.5" />
            ) : (
              <ChevronDown className="h-3.5 w-3.5" />
            )
          ) : (
            <ChevronsUpDown className="h-3.5 w-3.5 opacity-40" />
          )}
        </span>
      </div>
    </TableHead>
  )
}

// ─── TaskRow ──────────────────────────────────────────────────────────────────

interface TaskRowProps {
  task: TaskCardDto
  onClick: (id: string) => void
  t: ReturnType<typeof useTranslation>['t']
  columnColor?: string | null
}

const TaskRow = ({ task, onClick, t, columnColor }: TaskRowProps) => {
  const PriorityIcon = PRIORITY_ICONS[task.priority]

  const dueDateNode = task.dueDate
    ? (() => {
        const diffDays = Math.ceil(
          (new Date(task.dueDate).getTime() - Date.now()) / 86400000,
        )
        const cls =
          diffDays < 0
            ? 'text-red-500 font-medium'
            : diffDays <= 2
              ? 'text-amber-500'
              : 'text-muted-foreground'
        return (
          <span className={`text-sm ${cls}`}>
            {new Date(task.dueDate).toLocaleDateString()}
          </span>
        )
      })()
    : <span className="text-sm text-muted-foreground">-</span>

  const isDone = task.status === 'Done' || task.status === 'Cancelled'

  return (
    <TableRow
      className={`cursor-pointer ${isDone ? 'opacity-60' : ''}`}
      onClick={() => onClick(task.id)}
    >
      <TableCell className="font-mono text-xs text-muted-foreground">
        #{task.taskNumber.split('-').pop()}
      </TableCell>
      <TableCell className={`font-medium ${isDone ? 'line-through text-muted-foreground' : ''}`}>{task.title}</TableCell>
      <TableCell>
        <Badge variant="outline" className={`gap-1.5 ${getStatusBadgeClasses(statusColorMap[task.status])}`}>
          {columnColor && (
            <span className="h-2 w-2 rounded-full flex-shrink-0" style={{ backgroundColor: columnColor }} />
          )}
          {t(`statuses.${STATUS_I18N_KEYS[task.status]}`, { defaultValue: task.status })}
        </Badge>
      </TableCell>
      <TableCell>
        <span
          className={`inline-flex items-center gap-1 rounded-full px-1.5 py-0.5 text-[10px] font-medium leading-[1.1] border ${PRIORITY_CLASSES[task.priority]}`}
        >
          <PriorityIcon className="h-2.5 w-2.5" />
          {t(`priorities.${task.priority.toLowerCase()}`, { defaultValue: task.priority })}
        </span>
      </TableCell>
      <TableCell>
        {task.assigneeName ? (
          <div className="flex items-center gap-1.5">
            <Avatar
              src={task.assigneeAvatarUrl ?? undefined}
              alt={task.assigneeName}
              fallback={task.assigneeName}
              size="sm"
              className="h-5 w-5 text-[9px] flex-shrink-0"
            />
            <span className="text-sm text-muted-foreground truncate">{task.assigneeName}</span>
          </div>
        ) : (
          <span className="text-sm text-muted-foreground">-</span>
        )}
      </TableCell>
      <TableCell>{dueDateNode}</TableCell>
      <TableCell>
        <div className="flex flex-wrap gap-1">
          {task.labels.map((label) => (
            <span
              key={label.id}
              className="inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium leading-[1.1] border"
              style={{
                backgroundColor: `${label.color}20`,
                borderColor: `${label.color}40`,
                color: label.color,
              }}
            >
              {label.name}
            </span>
          ))}
        </div>
      </TableCell>
    </TableRow>
  )
}

// ─── TaskTable ────────────────────────────────────────────────────────────────

interface TaskTableProps {
  tasks: TaskCardDto[]
  sortField: string
  sortDir: 'asc' | 'desc'
  onSort: (field: string) => void
  onRowClick: (id: string) => void
  t: ReturnType<typeof useTranslation>['t']
  columnColorMap?: Record<string, string | null>
}

const TaskTable = ({ tasks, sortField, sortDir, onSort, onRowClick, t, columnColorMap = {} }: TaskTableProps) => (
  <div className="rounded-md border">
    <Table>
      <TableHeader>
        <TableRow>
          <SortableHead field="taskNumber" sortField={sortField} sortDir={sortDir} onSort={onSort}>
            {t('pm.taskNumber')}
          </SortableHead>
          <SortableHead field="title" sortField={sortField} sortDir={sortDir} onSort={onSort}>
            {t('pm.taskTitle')}
          </SortableHead>
          <SortableHead field="status" sortField={sortField} sortDir={sortDir} onSort={onSort}>
            {t('pm.status')}
          </SortableHead>
          <SortableHead field="priority" sortField={sortField} sortDir={sortDir} onSort={onSort}>
            {t('pm.priority')}
          </SortableHead>
          <SortableHead field="assignee" sortField={sortField} sortDir={sortDir} onSort={onSort}>
            {t('pm.assignee')}
          </SortableHead>
          <SortableHead field="dueDate" sortField={sortField} sortDir={sortDir} onSort={onSort}>
            {t('pm.dueDate')}
          </SortableHead>
          <TableHead>{t('pm.labels')}</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {tasks.map((task) => (
          <TaskRow key={task.id} task={task} onClick={onRowClick} t={t} columnColor={columnColorMap[task.status]} />
        ))}
      </TableBody>
    </Table>
  </div>
)

// ─── Main component ───────────────────────────────────────────────────────────

interface TaskListViewProps {
  projectId: string
  members?: ProjectMemberDto[]
  onTaskClick?: (taskId: string) => void
}

export const TaskListView = ({ projectId, members = [], onTaskClick }: TaskListViewProps) => {
  const { t } = useTranslation('common')
  const navigate = useNavigate()
  const [searchParams, setSearchParams] = useSearchParams()
  const { data: board, isLoading } = useKanbanBoardQuery(projectId)

  // ── Raw tasks ──
  const tasks = useMemo(
    () => board?.columns.flatMap((c) => c.tasks) ?? [],
    [board],
  )

  // ── URL filter state ──
  const listSearch = searchParams.get('list-search') ?? ''
  const listStatuses = useMemo(
    () => searchParams.get('list-status')?.split(',').filter(Boolean) ?? [],
    [searchParams],
  )
  const listPriorities = useMemo(
    () => searchParams.get('list-priority')?.split(',').filter(Boolean) ?? [],
    [searchParams],
  )
  const listAssignees = useMemo(
    () => searchParams.get('list-assignees')?.split(',').filter(Boolean) ?? [],
    [searchParams],
  )
  const listReporters = useMemo(
    () => searchParams.get('list-reporters')?.split(',').filter(Boolean) ?? [],
    [searchParams],
  )
  const listLabels = useMemo(
    () => searchParams.get('list-labels')?.split(',').filter(Boolean) ?? [],
    [searchParams],
  )
  const listDue = (searchParams.get('list-due') ?? '') as DueDateFilter
  const listDueStart = searchParams.get('list-due-start') ?? ''
  const listDueEnd = searchParams.get('list-due-end') ?? ''
  const listCompletion = (searchParams.get('list-completion') ?? '') as CompletionFilter
  const sortField = searchParams.get('list-sort') ?? 'taskNumber'
  const sortDir = (searchParams.get('list-dir') ?? 'asc') as 'asc' | 'desc'
  const groupBy = searchParams.get('list-group') ?? 'none'

  const advancedFilterCount =
    listAssignees.length +
    listReporters.length +
    listLabels.length +
    (listDue ? 1 : 0) +
    (listCompletion ? 1 : 0)
  const hasActiveFilters =
    Boolean(listSearch) ||
    listStatuses.length > 0 ||
    listPriorities.length > 0 ||
    advancedFilterCount > 0

  // ── Derived data ──
  const availableLabels = useMemo((): TaskLabelBriefDto[] => {
    const seen = new Set<string>()
    const labels: TaskLabelBriefDto[] = []
    for (const task of tasks) {
      for (const label of task.labels) {
        if (!seen.has(label.id)) { seen.add(label.id); labels.push(label) }
      }
    }
    return labels
  }, [tasks])

  // ── Helpers ──
  const setFilter = useCallback(
    (key: string, value: string) => {
      setSearchParams(
        (prev) => {
          const next = new URLSearchParams(prev)
          if (value) next.set(key, value)
          else next.delete(key)
          return next
        },
        { replace: true },
      )
    },
    [setSearchParams],
  )

  const clearAllFilters = useCallback(() => {
    setSearchParams(
      (prev) => {
        const next = new URLSearchParams(prev)
        next.delete('list-search')
        next.delete('list-status')
        next.delete('list-priority')
        next.delete('list-assignees')
        next.delete('list-reporters')
        next.delete('list-labels')
        next.delete('list-due')
        next.delete('list-due-start')
        next.delete('list-due-end')
        next.delete('list-completion')
        return next
      },
      { replace: true },
    )
  }, [setSearchParams])

  const setSortField = useCallback(
    (field: string) => {
      setSearchParams(
        (prev) => {
          const next = new URLSearchParams(prev)
          if (prev.get('list-sort') === field) {
            next.set('list-dir', prev.get('list-dir') === 'asc' ? 'desc' : 'asc')
          } else {
            next.set('list-sort', field)
            next.set('list-dir', 'asc')
          }
          return next
        },
        { replace: true },
      )
    },
    [setSearchParams],
  )

  const setGroupBy = useCallback(
    (group: string) => {
      setSearchParams(
        (prev) => {
          const next = new URLSearchParams(prev)
          if (group === 'none') next.delete('list-group')
          else next.set('list-group', group)
          return next
        },
        { replace: true },
      )
    },
    [setSearchParams],
  )

  const handleRowClick = useCallback(
    (taskId: string) => {
      if (onTaskClick) onTaskClick(taskId)
      else navigate(`/portal/tasks/${taskId}`)
    },
    [onTaskClick, navigate],
  )

  // ── Filtering ──
  const filteredTasks = useMemo(() => {
    return tasks.filter((task) => {
      const matchSearch =
        !listSearch ||
        task.title.toLowerCase().includes(listSearch.toLowerCase()) ||
        task.taskNumber.toLowerCase().includes(listSearch.toLowerCase())
      const matchStatus = listStatuses.length === 0 || listStatuses.includes(task.status)
      const matchPriority = listPriorities.length === 0 || listPriorities.includes(task.priority)
      const matchAssignee =
        listAssignees.length === 0 ||
        (listAssignees.includes('__unassigned__') && !task.assigneeName) ||
        (task.assigneeName != null &&
          listAssignees.some(a => a !== '__unassigned__' && task.assigneeName!.toLowerCase().includes(a.toLowerCase())))
      const matchReporter =
        listReporters.length === 0 ||
        (listReporters.includes('__no-reporter__') && !task.reporterName) ||
        (task.reporterName != null &&
          listReporters.some(r => r !== '__no-reporter__' && task.reporterName!.toLowerCase().includes(r.toLowerCase())))
      const matchLabel =
        listLabels.length === 0 ||
        (listLabels.includes('__no-label__') && task.labels.length === 0) ||
        task.labels.some(l => listLabels.includes(l.id))
      const matchDue = matchDueDate(task.dueDate, listDue, listDueStart || undefined, listDueEnd || undefined)
      const matchComp = matchCompletion(task.completedAt, listCompletion)
      return matchSearch && matchStatus && matchPriority && matchAssignee && matchReporter && matchLabel && matchDue && matchComp
    })
  }, [tasks, listSearch, listStatuses, listPriorities, listAssignees, listReporters, listLabels, listDue, listDueStart, listDueEnd, listCompletion])

  // ── Sorting ──
  const sortedTasks = useMemo(() => {
    const items = [...filteredTasks]
    const dir = sortDir === 'asc' ? 1 : -1

    switch (sortField) {
      case 'title':
        return items.sort((a, b) => a.title.localeCompare(b.title) * dir)
      case 'status':
        return items.sort((a, b) => a.status.localeCompare(b.status) * dir)
      case 'priority':
        return items.sort(
          (a, b) => (PRIORITY_ORDER[a.priority] - PRIORITY_ORDER[b.priority]) * dir,
        )
      case 'assignee':
        return items.sort(
          (a, b) => (a.assigneeName ?? '').localeCompare(b.assigneeName ?? '') * dir,
        )
      case 'dueDate':
        return items.sort((a, b) => {
          if (!a.dueDate) return 1 * dir
          if (!b.dueDate) return -1 * dir
          return (new Date(a.dueDate).getTime() - new Date(b.dueDate).getTime()) * dir
        })
      default:
        return items.sort((a, b) => a.taskNumber.localeCompare(b.taskNumber) * dir)
    }
  }, [filteredTasks, sortField, sortDir])

  // ── Grouping ──
  // Map task status → column color for display
  // Map task status → column color for display (always has a color via defaults)
  const DEFAULT_COLUMN_COLORS: Record<string, string> = {
    'todo': '#94a3b8', 'in progress': '#3b82f6', 'in review': '#8b5cf6', 'done': '#22c55e',
  }
  const columnColorMap = useMemo(() => {
    const map: Record<string, string> = {}
    const nameToStatus: Record<string, string> = {
      'todo': 'Todo', 'in progress': 'InProgress', 'in review': 'InReview', 'done': 'Done',
    }
    for (const col of board?.columns ?? []) {
      const key = col.name.toLowerCase()
      const status = nameToStatus[key]
      if (status) map[status] = col.color ?? DEFAULT_COLUMN_COLORS[key] ?? '#94a3b8'
    }
    return map
  }, [board?.columns])

  const groupedTasks = useMemo(() => {
    if (groupBy === 'none') {
      return [{ key: 'all', label: null as string | null, tasks: sortedTasks }]
    }

    const groups: Record<string, { key: string; label: string; tasks: TaskCardDto[] }> = {}

    for (const task of sortedTasks) {
      let key: string
      let label: string

      switch (groupBy) {
        case 'status':
          key = task.status
          label = t(`statuses.${STATUS_I18N_KEYS[task.status]}`, { defaultValue: task.status })
          break
        case 'priority':
          key = task.priority
          label = t(`priorities.${task.priority.toLowerCase()}`, { defaultValue: task.priority })
          break
        case 'assignee':
          key = task.assigneeName ?? 'unassigned'
          label = task.assigneeName ?? t('pm.unassigned', { defaultValue: 'Unassigned' })
          break
        default:
          key = 'all'
          label = ''
      }

      if (!groups[key]) groups[key] = { key, label, tasks: [] }
      groups[key].tasks.push(task)
    }

    return Object.values(groups)
  }, [sortedTasks, groupBy, t])

  // ── Render ──
  if (isLoading) {
    return (
      <div className="space-y-3">
        <Skeleton className="h-10 w-full" />
        <Skeleton className="h-10 w-full" />
        <Skeleton className="h-10 w-full" />
      </div>
    )
  }

  return (
    <div className="space-y-3">
      {/* Filter bar */}
      <div className="flex flex-wrap items-center gap-2">
        {/* Search */}
        <TaskSearchInput
          value={listSearch}
          onChange={(v) => setFilter('list-search', v)}
        />

        {/* Status multi-select dropdown — uses real board columns with their colors */}
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <button className={`inline-flex items-center gap-1.5 rounded-full px-3 h-9 text-sm font-medium border cursor-pointer transition-all ${
              listStatuses.length > 0 ? 'bg-primary/5 text-primary border-primary/30 hover:bg-primary/10' : 'bg-background border-border hover:bg-muted'
            }`}>
              <Circle className="h-3.5 w-3.5" />
              {t('pm.status', { defaultValue: 'Status' })}
              {listStatuses.length > 0 && (
                <span className="inline-flex items-center justify-center h-4 min-w-4 px-1 rounded-full text-[10px] font-bold leading-none bg-primary text-primary-foreground">
                  {listStatuses.length}
                </span>
              )}
              <ChevronDown className="h-3.5 w-3.5 opacity-60" />
            </button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="start" className="w-44">
            {(board?.columns ?? []).map((column) => {
              // Map column name → status enum for filter matching
              const columnStatusMap: Record<string, string> = {
                'todo': 'Todo', 'in progress': 'InProgress', 'in review': 'InReview', 'done': 'Done',
              }
              const statusValue = columnStatusMap[column.name.toLowerCase()] ?? column.name
              return (
                <DropdownMenuCheckboxItem
                  key={column.id}
                  checked={listStatuses.includes(statusValue)}
                  onCheckedChange={(checked) => {
                    const next = checked
                      ? [...listStatuses, statusValue]
                      : listStatuses.filter((x) => x !== statusValue)
                    setFilter('list-status', next.join(','))
                  }}
                  onSelect={(e) => e.preventDefault()}
                  className="cursor-pointer"
                >
                  <span
                    className="h-2.5 w-2.5 rounded-full mr-1.5 flex-shrink-0"
                    style={{ backgroundColor: column.color ?? DEFAULT_COLUMN_COLORS[column.name.toLowerCase()] ?? '#94a3b8' }}
                  />
                  {column.name}
                </DropdownMenuCheckboxItem>
              )
            })}
            {listStatuses.length > 0 && (
              <>
                <DropdownMenuSeparator />
                <DropdownMenuItem
                  onClick={() => setFilter('list-status', '')}
                  className="cursor-pointer text-muted-foreground text-xs"
                >
                  <X className="h-3 w-3 mr-1.5" />
                  {t('buttons.clearFilters', { defaultValue: 'Clear' })}
                </DropdownMenuItem>
              </>
            )}
          </DropdownMenuContent>
        </DropdownMenu>

        {/* Priority multi-select dropdown */}
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <button className={`inline-flex items-center gap-1.5 rounded-full px-3 h-9 text-sm font-medium border cursor-pointer transition-all ${
              listPriorities.length > 0 ? 'bg-primary/5 text-primary border-primary/30 hover:bg-primary/10' : 'bg-background border-border hover:bg-muted'
            }`}>
              <AlertTriangle className="h-3.5 w-3.5" />
              {t('pm.priority', { defaultValue: 'Priority' })}
              {listPriorities.length > 0 && (
                <span className="inline-flex items-center justify-center h-4 min-w-4 px-1 rounded-full text-[10px] font-bold leading-none bg-primary text-primary-foreground">
                  {listPriorities.length}
                </span>
              )}
              <ChevronDown className="h-3.5 w-3.5 opacity-60" />
            </button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="start" className="w-44">
            {PRIORITY_VALUES.map((p) => {
              const Icon = PRIORITY_ICONS[p]
              return (
                <DropdownMenuCheckboxItem
                  key={p}
                  checked={listPriorities.includes(p)}
                  onCheckedChange={(checked) => {
                    const next = checked
                      ? [...listPriorities, p]
                      : listPriorities.filter((x) => x !== p)
                    setFilter('list-priority', next.join(','))
                  }}
                  onSelect={(e) => e.preventDefault()}
                  className="cursor-pointer"
                >
                  <Icon className={`h-3.5 w-3.5 mr-1.5 ${PRIORITY_COLOR_MAP[p]}`} />
                  {t(`priorities.${p.toLowerCase()}`, { defaultValue: p })}
                </DropdownMenuCheckboxItem>
              )
            })}
            {listPriorities.length > 0 && (
              <>
                <DropdownMenuSeparator />
                <DropdownMenuItem
                  onClick={() => setFilter('list-priority', '')}
                  className="cursor-pointer text-muted-foreground text-xs"
                >
                  <X className="h-3 w-3 mr-1.5" />
                  {t('buttons.clearFilters', { defaultValue: 'Clear' })}
                </DropdownMenuItem>
              </>
            )}
          </DropdownMenuContent>
        </DropdownMenu>

        {/* Advanced filters */}
        <TaskFilterPopover
          showCompletion
          showAssignees
          showReporters
          showLabels
          showDueDate
          members={members}
          availableLabels={availableLabels}
          selectedAssignees={listAssignees}
          onAssigneesChange={(v) => setFilter('list-assignees', v.join(','))}
          selectedReporters={listReporters}
          onReportersChange={(v) => setFilter('list-reporters', v.join(','))}
          selectedLabels={listLabels}
          onLabelsChange={(v) => setFilter('list-labels', v.join(','))}
          selectedDueDate={listDue}
          onDueDateChange={(v) => setFilter('list-due', v)}
          dueDateSpecificStart={listDueStart}
          onDueDateSpecificStartChange={(v) => setFilter('list-due-start', v)}
          dueDateSpecificEnd={listDueEnd}
          onDueDateSpecificEndChange={(v) => setFilter('list-due-end', v)}
          completionFilter={listCompletion}
          onCompletionChange={(v) => setFilter('list-completion', v)}
          onClearAll={clearAllFilters}
          activeCount={advancedFilterCount}
        />

        {/* Group by */}
        <Select value={groupBy} onValueChange={setGroupBy}>
          <SelectTrigger className="w-[140px] cursor-pointer text-xs h-8" aria-label={t('pm.groupBy', { defaultValue: 'Group by' })}>
            <Layers className="h-3.5 w-3.5 mr-1.5 text-muted-foreground" />
            <SelectValue placeholder={t('pm.groupBy', { defaultValue: 'Group by' })} />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="none" className="cursor-pointer">
              {t('pm.noGrouping', { defaultValue: 'No grouping' })}
            </SelectItem>
            <SelectItem value="status" className="cursor-pointer">
              {t('pm.groupByStatus', { defaultValue: 'Status' })}
            </SelectItem>
            <SelectItem value="priority" className="cursor-pointer">
              {t('pm.groupByPriority', { defaultValue: 'Priority' })}
            </SelectItem>
            <SelectItem value="assignee" className="cursor-pointer">
              {t('pm.groupByAssignee', { defaultValue: 'Assignee' })}
            </SelectItem>
          </SelectContent>
        </Select>

        {/* Clear filters */}
        {hasActiveFilters && (
          <button
            onClick={clearAllFilters}
            className="inline-flex items-center gap-1 text-xs text-muted-foreground hover:text-foreground cursor-pointer transition-colors"
          >
            <X className="h-3 w-3" />
            {t('buttons.clearFilters', { defaultValue: 'Clear filters' })}
          </button>
        )}

        {/* Task count */}
        <span className="text-xs text-muted-foreground ml-auto">
          {filteredTasks.length}{' '}
          {t('pm.taskTitle', { defaultValue: 'tasks' }).toLowerCase()}
          {hasActiveFilters && tasks.length !== filteredTasks.length && ` / ${tasks.length}`}
        </span>
      </div>

      {/* Empty states */}
      {tasks.length === 0 && (
        <EmptyState
          icon={ListTodo}
          title={t('pm.noTasksFound')}
          description={t('pm.createTask')}
        />
      )}

      {tasks.length > 0 && filteredTasks.length === 0 && (
        <EmptyState
          icon={Search}
          title={t('pm.noTasksMatchFilter', { defaultValue: 'No matching tasks' })}
          description={t('pm.tryAdjustingFilters', { defaultValue: 'Try adjusting your filters' })}
          size="sm"
        />
      )}

      {/* Task groups / table */}
      {filteredTasks.length > 0 && (
        <div className="space-y-4">
          {groupedTasks.map(({ key, label, tasks: groupTasks }) => (
            <div key={key}>
              {label !== null && (
                <div className="flex items-center gap-2 mb-2">
                  <span className="text-sm font-semibold">{label}</span>
                  <span className="text-xs text-muted-foreground">({groupTasks.length})</span>
                  <div className="flex-1 h-px bg-border" />
                </div>
              )}
              <TaskTable
                tasks={groupTasks}
                sortField={sortField}
                sortDir={sortDir}
                onSort={setSortField}
                onRowClick={handleRowClick}
                t={t}
                columnColorMap={columnColorMap}
              />
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
