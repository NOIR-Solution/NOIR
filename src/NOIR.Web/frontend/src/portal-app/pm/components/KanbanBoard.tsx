import { useCallback, useState, useEffect, useRef, useMemo, type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { useSearchParams } from 'react-router-dom'
import {
  DndContext,
  DragOverlay,
  PointerSensor,
  KeyboardSensor,
  useSensor,
  useSensors,
  closestCorners,
  pointerWithin,
  rectIntersection,
  useDroppable,
  useDraggable,
  type DragEndEvent,
  type DragStartEvent,
  type DragOverEvent,
  type CollisionDetection,
} from '@dnd-kit/core'
import {
  SortableContext,
  verticalListSortingStrategy,
  horizontalListSortingStrategy,
  sortableKeyboardCoordinates,
  useSortable,
  arrayMove,
} from '@dnd-kit/sortable'
import { CSS } from '@dnd-kit/utilities'
import { toast } from 'sonner'
import { Plus, Kanban, X, ArrowDown, Minus, ArrowUp, AlertTriangle, Loader2, EllipsisVertical, Pencil, Trash2, UserCheck, UserX, ChevronDown, Layers, ArrowUpDown, Check, ChevronsUpDown, Archive, Copy, Hand, MousePointer2, Square, CheckSquare } from 'lucide-react'
import {
  Avatar, Button, EmptyState, Skeleton,
  DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuSeparator, DropdownMenuTrigger, DropdownMenuCheckboxItem,
  DropdownMenuSub, DropdownMenuSubContent, DropdownMenuSubTrigger,
  Credenza, CredenzaContent, CredenzaHeader, CredenzaTitle, CredenzaDescription, CredenzaFooter, CredenzaBody,
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue,
  Tooltip, TooltipContent, TooltipTrigger,
} from '@uikit'
import {
  useKanbanBoardQuery,
  useMoveTask,
  useCreateTask,
  useUpdateTask,
  useCreateColumn,
  useUpdateColumn,
  useDeleteColumn,
  useChangeTaskStatus,
  useReorderColumns,
  useMoveAllColumnTasks,
  useDuplicateColumn,
  useArchiveTask,
  useBulkArchiveTasks,
  useBulkChangeTaskStatus,
} from '@/portal-app/pm/queries'
import type { TaskCardDto, KanbanColumnDto, ProjectMemberDto, TaskLabelBriefDto, TaskPriority } from '@/types/pm'
import { TaskCard } from './TaskCard'
import { TaskDetailModal } from './TaskDetailModal'
import { ColumnSettingsDialog } from './ColumnSettingsDialog'
import { TaskFilterPopover, matchDueDate, matchCompletion, type DueDateFilter, type CompletionFilter } from './TaskFilterPopover'
import { TaskSearchInput } from './TaskSearchInput'

// ─── Column ↔ Status mapping + default colors ──────────────────────────────

/** Map column name (lowercase) → backend status enum value */
const COLUMN_STATUS_MAP: Record<string, string> = {
  'todo': 'Todo', 'in progress': 'InProgress', 'in review': 'InReview', 'done': 'Done',
}

/** Default colors for known columns when no custom color is set */
const DEFAULT_COLUMN_COLORS: Record<string, string> = {
  'todo': '#94a3b8', 'in progress': '#3b82f6', 'in review': '#8b5cf6', 'done': '#22c55e',
}

/** Get display color for a column: custom color > default by name > gray fallback */
const getColumnColor = (column: { name: string; color: string | null }): string =>
  column.color ?? DEFAULT_COLUMN_COLORS[column.name.toLowerCase()] ?? '#94a3b8'

// ─── Droppable body: makes empty columns valid drop targets ──────────────────

const DroppableColumnBody = ({
  id,
  isCardOver,
  children,
}: {
  id: string
  isCardOver: boolean
  children: ReactNode
}) => {
  const { setNodeRef } = useDroppable({ id })
  return (
    <div
      ref={setNodeRef}
      className={`flex-1 min-h-0 overflow-y-auto space-y-2 p-2 transition-all duration-150 ${
        isCardOver ? 'bg-primary/5 ring-1 ring-inset ring-primary/25' : ''
      }`}
      style={{ scrollbarWidth: 'thin', scrollbarColor: 'color-mix(in oklch, var(--border) 80%, transparent) transparent' } as React.CSSProperties}
    >
      {children}
    </div>
  )
}

// ─── Sortable column wrapper: makes each column draggable ────────────────────

const SortableColumnWrapper = ({
  id,
  children,
}: {
  id: string
  children: (props: {
    dragHandleProps: Record<string, unknown>
    isDragging: boolean
  }) => ReactNode
}) => {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({ id })
  const style = {
    transform: CSS.Transform.toString(transform),
    transition: isDragging ? 'none' : transition,
  }
  return (
    <div
      ref={setNodeRef}
      style={style}
      className={`min-w-[280px] max-w-[320px] flex-shrink-0 group/col flex flex-col h-full ${isDragging ? 'opacity-40' : ''}`}
    >
      {children({ dragHandleProps: { ...listeners, ...attributes }, isDragging })}
    </div>
  )
}

// ─── Draggable member pill ────────────────────────────────────────────────────

const DraggableMemberPill = ({
  member,
  active,
  onFilter,
}: {
  member: ProjectMemberDto
  active: boolean
  onFilter: () => void
}) => {
  const { attributes, listeners, setNodeRef, isDragging } = useDraggable({
    id: `member-${member.employeeId}`,
    data: { type: 'member', employeeId: member.employeeId, memberName: member.employeeName, avatarUrl: member.avatarUrl },
  })
  const firstName = member.employeeName.split(' ')[0]

  return (
    <div
      ref={setNodeRef}
      {...listeners}
      {...attributes}
      style={{ opacity: isDragging ? 0.4 : 1, touchAction: 'none' }}
      className={`inline-flex items-center gap-1.5 rounded-full px-3 py-1.5 text-sm font-medium border cursor-grab active:cursor-grabbing transition-all select-none ${
        active ? 'bg-primary text-primary-foreground border-primary' : 'bg-background border-border hover:bg-muted'
      }`}
      title={`${member.employeeName} — drag to assign`}
      onClick={(e) => { e.stopPropagation(); onFilter() }}
    >
      <Avatar src={member.avatarUrl ?? undefined} alt={member.employeeName} fallback={member.employeeName} size="sm" className="h-5 w-5 text-[9px]" />
      {firstName}
    </div>
  )
}

// ─── KanbanBoard ─────────────────────────────────────────────────────────────

interface KanbanBoardProps {
  projectId: string
  members?: ProjectMemberDto[]
  onCreateTask?: (columnId: string) => void
}

export const KanbanBoard = ({ projectId, members, onCreateTask }: KanbanBoardProps) => {
  const { t } = useTranslation('common')
  const { data: board, isLoading } = useKanbanBoardQuery(projectId)
  const moveTaskMutation = useMoveTask()
  const createTaskMutation = useCreateTask()
  const updateTaskMutation = useUpdateTask()
  const createColumnMutation = useCreateColumn()
  const updateColumnMutation = useUpdateColumn()
  const deleteColumnMutation = useDeleteColumn()
  const changeStatusMutation = useChangeTaskStatus()
  const reorderColumnsMutation = useReorderColumns()
  const moveAllColumnTasksMutation = useMoveAllColumnTasks()
  const duplicateColumnMutation = useDuplicateColumn()
  const archiveTaskMutation = useArchiveTask()
  const bulkArchiveTasksMutation = useBulkArchiveTasks()
  const bulkChangeStatusMutation = useBulkChangeTaskStatus()

  // ── Optimistic local state ─────────────────────────────────────────────────
  const [localColumns, setLocalColumns] = useState<KanbanColumnDto[]>([])
  const localColumnsRef = useRef<KanbanColumnDto[]>([])
  const isDraggingRef = useRef(false)

  // Keep ref in sync so handleDragEnd always reads the latest localColumns (avoids stale closure)
  useEffect(() => { localColumnsRef.current = localColumns }, [localColumns])

  // ── Pan / Select mode ─────────────────────────────────────────────────────
  const [boardMode, setBoardMode] = useState<'pan' | 'select'>('pan')
  const [selectedTaskIds, setSelectedTaskIds] = useState<Set<string>>(new Set())

  const toggleTaskSelection = useCallback((taskId: string) => {
    setSelectedTaskIds(prev => {
      const next = new Set(prev)
      if (next.has(taskId)) next.delete(taskId)
      else next.add(taskId)
      return next
    })
  }, [])

  const clearSelection = useCallback(() => {
    setSelectedTaskIds(new Set())
  }, [])

  // When switching to pan mode, clear selection
  const switchMode = useCallback((mode: 'pan' | 'select') => {
    setBoardMode(mode)
    if (mode === 'pan') setSelectedTaskIds(new Set())
  }, [])

  // ── Figma-style board pan ─────────────────────────────────────────────────
  const boardScrollRef = useRef<HTMLDivElement>(null)
  const panRef = useRef<{ pointerId: number; startX: number; scrollLeft: number } | null>(null)

  // ── Rubber-band / lasso selection ───────────────────────────────────────────
  const [lassoRect, setLassoRect] = useState<{ startX: number; startY: number; endX: number; endY: number } | null>(null)
  const lassoRef = useRef<{ pointerId: number; startX: number; startY: number } | null>(null)

  const getIntersectingTaskIds = useCallback((screenRect: { left: number; top: number; right: number; bottom: number }): string[] => {
    const cards = boardScrollRef.current?.querySelectorAll('[data-task-id]')
    if (!cards) return []
    const ids: string[] = []
    cards.forEach(card => {
      const cr = card.getBoundingClientRect()
      if (!(cr.right < screenRect.left || cr.left > screenRect.right ||
            cr.bottom < screenRect.top || cr.top > screenRect.bottom)) {
        const id = card.getAttribute('data-task-id')
        if (id) ids.push(id)
      }
    })
    return ids
  }, [])

  const handleBoardPointerDown = useCallback((e: React.PointerEvent<HTMLDivElement>) => {
    if (e.button !== 0) return
    const target = e.target as HTMLElement
    // Skip if DOM target is outside the board (portalled menus, dialogs, etc.)
    if (!boardScrollRef.current?.contains(target)) return
    // Skip if clicking interactive elements or dnd-kit draggable items
    if (target.closest('button, input, textarea, a, select, [role="button"], [role="option"], [role="menuitem"]')) return
    if (target.closest('.cursor-grab')) return

    if (boardMode === 'pan') {
      panRef.current = { pointerId: e.pointerId, startX: e.clientX, scrollLeft: boardScrollRef.current?.scrollLeft ?? 0 }
      ;(e.currentTarget as HTMLElement).setPointerCapture(e.pointerId)
      ;(e.currentTarget as HTMLElement).style.cursor = 'grabbing'
    } else if (boardMode === 'select') {
      // Don't start lasso if clicking on a card
      if (target.closest('[data-task-id]')) return
      const board = boardScrollRef.current!
      const boardRect = board.getBoundingClientRect()
      const x = e.clientX - boardRect.left + board.scrollLeft
      const y = e.clientY - boardRect.top + board.scrollTop
      lassoRef.current = { pointerId: e.pointerId, startX: x, startY: y }
      setLassoRect({ startX: x, startY: y, endX: x, endY: y })
      ;(e.currentTarget as HTMLElement).setPointerCapture(e.pointerId)
    }
  }, [boardMode])

  const handleBoardPointerMove = useCallback((e: React.PointerEvent<HTMLDivElement>) => {
    if (panRef.current && boardScrollRef.current) {
      const dx = e.clientX - panRef.current.startX
      boardScrollRef.current.scrollLeft = panRef.current.scrollLeft - dx
      return
    }
    if (lassoRef.current && boardScrollRef.current) {
      const board = boardScrollRef.current
      const boardRect = board.getBoundingClientRect()
      const x = e.clientX - boardRect.left + board.scrollLeft
      const y = e.clientY - boardRect.top + board.scrollTop
      setLassoRect({ startX: lassoRef.current.startX, startY: lassoRef.current.startY, endX: x, endY: y })
    }
  }, [])

  const handleBoardPointerUp = useCallback((e: React.PointerEvent<HTMLDivElement>) => {
    if (panRef.current) {
      panRef.current = null
      ;(e.currentTarget as HTMLElement).style.cursor = ''
      return
    }
    if (lassoRef.current && lassoRect && boardScrollRef.current) {
      const board = boardScrollRef.current
      const boardRect = board.getBoundingClientRect()
      // Convert lasso rect (in scroll-space) to screen-space for intersection check
      const screenLeft = Math.min(lassoRect.startX, lassoRect.endX) - board.scrollLeft + boardRect.left
      const screenTop = Math.min(lassoRect.startY, lassoRect.endY) - board.scrollTop + boardRect.top
      const screenRight = Math.max(lassoRect.startX, lassoRect.endX) - board.scrollLeft + boardRect.left
      const screenBottom = Math.max(lassoRect.startY, lassoRect.endY) - board.scrollTop + boardRect.top
      // Only count as lasso if dragged more than 5px (avoid accidental micro-drags)
      if (Math.abs(lassoRect.endX - lassoRect.startX) > 5 || Math.abs(lassoRect.endY - lassoRect.startY) > 5) {
        const ids = getIntersectingTaskIds({ left: screenLeft, top: screenTop, right: screenRight, bottom: screenBottom })
        if (ids.length > 0) {
          setSelectedTaskIds(prev => {
            const next = new Set(prev)
            ids.forEach(id => next.add(id))
            return next
          })
        }
      }
      lassoRef.current = null
      setLassoRect(null)
    }
  }, [lassoRect, getIntersectingTaskIds])

  // Sync from server only when not mid-drag (prevents snap-back)
  useEffect(() => {
    if (board?.columns && !isDraggingRef.current) {
      setLocalColumns(board.columns)
    }
  }, [board?.columns])

  // ── Drag tracking ──────────────────────────────────────────────────────────
  const [activeId, setActiveId] = useState<string | null>(null)
  const [overColumnId, setOverColumnId] = useState<string | null>(null)
  const [dragType, setDragType] = useState<'card' | 'column' | 'member' | null>(null)
  const [overTaskId, setOverTaskId] = useState<string | null>(null)

  const allTasks = useMemo(() => localColumns.flatMap(c => c.tasks), [localColumns])
  const activeTask = activeId && dragType === 'card' ? allTasks.find(t => t.id === activeId) ?? null : null
  const activeColumn = activeId && dragType === 'column' ? localColumns.find(c => c.id === activeId) ?? null : null

  // ── Column sort overrides (frontend-only, ephemeral) ──────────────────────
  type SortOption = 'default' | 'priority' | 'dueDate' | 'alpha'
  const priorityOrder: Record<string, number> = { Urgent: 4, High: 3, Medium: 2, Low: 1 }
  const [columnSortOverrides, setColumnSortOverrides] = useState<Record<string, SortOption>>({})

  // ── Collapsed columns (frontend-only, ephemeral) ──────────────────────────
  const [collapsedColumns, setCollapsedColumns] = useState<Set<string>>(new Set())

  const toggleColumnCollapse = useCallback((columnId: string) => {
    setCollapsedColumns(prev => {
      const next = new Set(prev)
      if (next.has(columnId)) next.delete(columnId)
      else next.add(columnId)
      return next
    })
  }, [])

  // ── UI state ───────────────────────────────────────────────────────────────
  const [quickAddColumnId, setQuickAddColumnId] = useState<string | null>(null)
  const [quickAddTitle, setQuickAddTitle] = useState('')
  const [isAddingColumn, setIsAddingColumn] = useState(false)
  const [newColumnName, setNewColumnName] = useState('')
  const [multiPasteLines, setMultiPasteLines] = useState<string[]>([])
  const [multiPasteColumnId, setMultiPasteColumnId] = useState<string | null>(null)
  const [editingColumnId, setEditingColumnId] = useState<string | null>(null)
  const [editingColumnName, setEditingColumnName] = useState('')
  const [columnSettingsId, setColumnSettingsId] = useState<string | null>(null)
  const [deleteColumnId, setDeleteColumnId] = useState<string | null>(null)
  const [deleteColumnMoveToId, setDeleteColumnMoveToId] = useState<string>('')
  const [moveAllColumnId, setMoveAllColumnId] = useState<string | null>(null)
  const [moveAllTargetColumnId, setMoveAllTargetColumnId] = useState<string>('')
  const [archiveAllColumnId, setArchiveAllColumnId] = useState<string | null>(null)

  // ── URL-synced state ───────────────────────────────────────────────────────
  const [searchParams, setSearchParams] = useSearchParams()

  // Task modal — URL-synced via ?task=TASK-NUMBER (human-readable like Jira)
  const taskNumber = searchParams.get('task')
  const taskModalOpen = Boolean(taskNumber)
  // Resolve task number → GUID from board data
  const selectedTaskId = useMemo(() => {
    if (!taskNumber) return null
    return localColumns.flatMap(c => c.tasks).find(t => t.taskNumber === taskNumber)?.id ?? null
  }, [taskNumber, localColumns])
  const setSelectedTaskId = (taskNum: string | null) => {
    setSearchParams(prev => {
      const next = new URLSearchParams(prev)
      if (taskNum) next.set('task', taskNum)
      else next.delete('task')
      return next
    }, { replace: true })
  }

  // Task type filter — All | Tasks | Subtasks
  const boardTaskType = (searchParams.get('board-task-type') ?? 'all') as 'all' | 'tasks' | 'subtasks'

  const boardSearch = searchParams.get('board-search') ?? ''
  const boardAssignees = searchParams.get('board-assignees')?.split(',').filter(Boolean) ?? []
  const boardReporters = searchParams.get('board-reporters')?.split(',').filter(Boolean) ?? []
  const boardPriorities = searchParams.get('board-priorities')?.split(',').filter(Boolean) ?? []
  const boardLabels = searchParams.get('board-labels')?.split(',').filter(Boolean) ?? []
  const boardDue = (searchParams.get('board-due') ?? '') as DueDateFilter
  const boardDueStart = searchParams.get('board-due-start') ?? ''
  const boardDueEnd = searchParams.get('board-due-end') ?? ''
  const boardCompletion = (searchParams.get('board-completion') ?? '') as CompletionFilter

  const advancedFilterCount =
    boardLabels.length +
    (boardDue ? 1 : 0) +
    boardReporters.length +
    (boardCompletion ? 1 : 0)
  const hasActiveFilters =
    Boolean(boardSearch) ||
    boardAssignees.length > 0 ||
    boardPriorities.length > 0 ||
    boardTaskType !== 'all' ||
    advancedFilterCount > 0

  const setFilter = (key: string, value: string) => {
    setSearchParams(prev => {
      const next = new URLSearchParams(prev)
      if (value) next.set(key, value)
      else next.delete(key)
      return next
    }, { replace: true })
  }

  const clearAllFilters = () => {
    setSearchParams(prev => {
      const next = new URLSearchParams(prev)
      next.delete('board-search')
      next.delete('board-assignees')
      next.delete('board-reporters')
      next.delete('board-priorities')
      next.delete('board-task-type')
      next.delete('board-labels')
      next.delete('board-due')
      next.delete('board-due-start')
      next.delete('board-due-end')
      next.delete('board-completion')
      return next
    }, { replace: true })
  }

  // ── Available labels (unique across all tasks) ───────────────────────────────
  const availableLabels = useMemo((): TaskLabelBriefDto[] => {
    const seen = new Set<string>()
    const labels: TaskLabelBriefDto[] = []
    for (const col of localColumns) {
      for (const task of col.tasks) {
        for (const label of task.labels) {
          if (!seen.has(label.id)) { seen.add(label.id); labels.push(label) }
        }
      }
    }
    return labels
  }, [localColumns])

  // ── Filtered view (display only — DnD operates on localColumns) ────────────
  const filteredColumns = useMemo(() => localColumns.map(col => {
    const sortedTasks = [...col.tasks].sort((a, b) => {
      const sort = columnSortOverrides[col.id] ?? 'default'
      if (sort === 'priority') return (priorityOrder[b.priority] ?? 0) - (priorityOrder[a.priority] ?? 0)
      if (sort === 'dueDate') {
        if (!a.dueDate && !b.dueDate) return 0
        if (!a.dueDate) return 1
        if (!b.dueDate) return -1
        return new Date(a.dueDate).getTime() - new Date(b.dueDate).getTime()
      }
      if (sort === 'alpha') return a.title.localeCompare(b.title)
      return a.sortOrder - b.sortOrder // default
    })
    return {
    ...col,
    tasks: sortedTasks.filter(task => {
      const matchSearch = !boardSearch || task.title.toLowerCase().includes(boardSearch.toLowerCase())
      const matchAssignee =
        boardAssignees.length === 0 ||
        (boardAssignees.includes('__unassigned__') && !task.assigneeName) ||
        (task.assigneeName != null &&
          boardAssignees.some(a => a !== '__unassigned__' && task.assigneeName!.toLowerCase().includes(a.toLowerCase())))
      const matchReporter =
        boardReporters.length === 0 ||
        (boardReporters.includes('__no-reporter__') && !task.reporterName) ||
        (task.reporterName != null &&
          boardReporters.some(r => r !== '__no-reporter__' && task.reporterName!.toLowerCase().includes(r.toLowerCase())))
      const matchPriority = boardPriorities.length === 0 || boardPriorities.includes(task.priority)
      const matchTaskType =
        boardTaskType === 'all' ||
        (boardTaskType === 'tasks' && !task.parentTaskId) ||
        (boardTaskType === 'subtasks' && Boolean(task.parentTaskId))
      const matchLabel =
        boardLabels.length === 0 ||
        (boardLabels.includes('__no-label__') && task.labels.length === 0) ||
        task.labels.some(l => boardLabels.includes(l.id))
      const matchDue = matchDueDate(task.dueDate, boardDue, boardDueStart || undefined, boardDueEnd || undefined)
      const matchComp = matchCompletion(task.completedAt, boardCompletion)
      return matchSearch && matchAssignee && matchReporter && matchPriority && matchTaskType && matchLabel && matchDue && matchComp
    }),
  }
  }), [localColumns, boardSearch, boardAssignees, boardReporters, boardPriorities, boardTaskType, boardLabels, boardDue, boardDueStart, boardDueEnd, boardCompletion, columnSortOverrides])

  // ── DnD sensors — effectively disabled in select mode via huge activation distance ──
  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: boardMode === 'pan' ? 8 : 999999 } }),
    useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates }),
  )

  // ── Collision detection strategy ───────────────────────────────────────────
  const collisionDetectionStrategy: CollisionDetection = useCallback((args) => {
    const dragId = String(args.active.id)
    const colIds = new Set(localColumnsRef.current.map(c => c.id))

    // Column drag: only consider other columns — filtering out task droppables
    // prevents closestCorners from resolving to a task id, which breaks handleDragOver
    if (colIds.has(dragId)) {
      return closestCorners({
        ...args,
        droppableContainers: args.droppableContainers.filter(c => colIds.has(String(c.id))),
      })
    }

    // Member drag: only collide with task cards
    if (dragId.startsWith('member-')) {
      return closestCorners({
        ...args,
        droppableContainers: args.droppableContainers.filter(c => !colIds.has(String(c.id))),
      })
    }

    // Card drag: pointerWithin fires as soon as cursor enters target — Trello-style
    const pointerHits = pointerWithin(args)
    if (pointerHits.length > 0) {
      // Prefer a task target (precise position) over a column body (appends to end)
      const taskHit = pointerHits.find(({ id }) => !colIds.has(String(id)))
      return taskHit ? [taskHit] : [pointerHits[0]]
    }

    // Fallback: first bounding-box overlap — catches fast moves and edges
    return rectIntersection(args)
  }, [])

  // ── handleDragStart ────────────────────────────────────────────────────────
  const handleDragStart = useCallback((event: DragStartEvent) => {
    const id = String(event.active.id)
    setActiveId(id)
    isDraggingRef.current = true
    if (id.startsWith('member-')) {
      setDragType('member')
    } else {
      setDragType(localColumns.some(c => c.id === id) ? 'column' : 'card')
    }
  }, [localColumns])

  // ── handleDragOver: optimistic real-time reorder ───────────────────────────
  const handleDragOver = useCallback((event: DragOverEvent) => {
    const { active, over } = event
    if (!over) { setOverColumnId(null); setOverTaskId(null); return }

    const activeId = String(active.id)
    const overId = String(over.id)
    if (activeId === overId) return

    // Member drag: just track which task we're over for visual highlight
    if (activeId.startsWith('member-')) {
      const isTask = localColumns.some(col => col.tasks.some(t => t.id === overId))
      setOverTaskId(isTask ? overId : null)
      return
    }

    setLocalColumns(prev => {
      // ── Column drag: real-time preview ──
      const isCol = prev.some(c => c.id === activeId)
      if (isCol) {
        const fromIdx = prev.findIndex(c => c.id === activeId)
        const toIdx = prev.findIndex(c => c.id === overId)
        if (fromIdx === -1 || toIdx === -1 || fromIdx === toIdx) return prev
        return arrayMove(prev, fromIdx, toIdx)
      }

      // ── Card drag ──
      const srcIdx = prev.findIndex(col => col.tasks.some(t => t.id === activeId))
      if (srcIdx === -1) return prev

      let tgtIdx = prev.findIndex(c => c.id === overId)
      if (tgtIdx === -1) tgtIdx = prev.findIndex(col => col.tasks.some(t => t.id === overId))
      if (tgtIdx === -1) return prev

      setOverColumnId(prev[tgtIdx].id)

      const cols = prev.map(c => ({ ...c, tasks: [...c.tasks] }))
      const srcTasks = cols[srcIdx].tasks
      const tgtTasks = cols[tgtIdx].tasks
      const activeTaskIdx = srcTasks.findIndex(t => t.id === activeId)
      if (activeTaskIdx === -1) return prev

      if (srcIdx === tgtIdx) {
        // Same column — reorder
        const overTaskIdx = srcTasks.findIndex(t => t.id === overId)
        if (overTaskIdx === -1) return prev
        cols[srcIdx].tasks = arrayMove(srcTasks, activeTaskIdx, overTaskIdx)
      } else {
        // Cross-column move
        const [moved] = srcTasks.splice(activeTaskIdx, 1)
        const overTaskIdx = tgtTasks.findIndex(t => t.id === overId)
        if (overTaskIdx === -1) {
          tgtTasks.push(moved)
        } else {
          tgtTasks.splice(overTaskIdx, 0, moved)
        }
        cols[srcIdx].tasks = srcTasks
        cols[tgtIdx].tasks = tgtTasks
      }
      return cols
    })
  }, [])

  // ── handleDragEnd: commit to server ────────────────────────────────────────
  const handleDragEnd = useCallback((event: DragEndEvent) => {
    const { active, over } = event
    isDraggingRef.current = false
    setActiveId(null)
    setOverColumnId(null)
    setOverTaskId(null)
    const currentType = dragType
    setDragType(null)

    if (!over || !board) {
      if (board?.columns) setLocalColumns(board.columns)
      return
    }

    // ── Member drag: assign to task ──
    if (currentType === 'member') {
      const overId = String(over.id)
      const employeeId = (active.data.current as { employeeId: string } | undefined)?.employeeId
      const targetTask = localColumns.flatMap(c => c.tasks).find(t => t.id === overId)
      if (employeeId && targetTask) {
        updateTaskMutation.mutate(
          { id: targetTask.id, request: { assigneeId: employeeId } },
          { onError: (err) => toast.error(err instanceof Error ? err.message : t('errors.unknown')) },
        )
      }
      return
    }

    const activeId = String(active.id)

    if (currentType === 'column') {
      // handleDragOver already rearranged localColumns to the final order.
      // Just persist that order — no need to re-arrayMove.
      const latestCols = localColumnsRef.current
      const originalIds = board.columns.map(c => c.id).join(',')
      const newIds = latestCols.map(c => c.id)
      // Skip save if order hasn't changed
      if (newIds.join(',') === originalIds) return

      reorderColumnsMutation.mutate(
        { projectId, request: { columnIds: newIds } },
        {
          onError: (err) => {
            setLocalColumns(board.columns)
            toast.error(err instanceof Error ? err.message : t('errors.unknown'))
          },
        },
      )
      return
    }

    // Card: use localColumnsRef (always latest) to find where the task landed after drag-over.
    // handleDragOver already moved the card to its final position in localColumns.
    const latestCols = localColumnsRef.current
    const targetColIdx = latestCols.findIndex(col => col.tasks.some(t => t.id === activeId))
    if (targetColIdx === -1) { setLocalColumns(board.columns); return }

    const targetCol = latestCols[targetColIdx]
    const colTasks = targetCol.tasks
    const taskIdx = colTasks.findIndex(t => t.id === activeId)

    // Check if card actually moved (different column or different position)
    const origCol = board.columns.find(col => col.tasks.some(t => t.id === activeId))
    const origIdx = origCol?.tasks.findIndex(t => t.id === activeId) ?? -1
    if (origCol?.id === targetCol.id && origIdx === taskIdx) return // no change

    const prevTask = taskIdx > 0 ? colTasks[taskIdx - 1] : null
    const nextTask = taskIdx < colTasks.length - 1 ? colTasks[taskIdx + 1] : null

    let newSortOrder: number
    if (!prevTask && !nextTask) {
      // Only task in column
      newSortOrder = 1
    } else if (!prevTask) {
      // Placing at the beginning: go below the first existing task
      const nextOrder = nextTask!.sortOrder
      newSortOrder = nextOrder > 0 ? nextOrder / 2 : nextOrder - 1
    } else if (!nextTask) {
      // Placing at the end
      newSortOrder = prevTask.sortOrder + 1
    } else {
      // Placing between two tasks
      const prev = prevTask.sortOrder
      const next = nextTask.sortOrder
      newSortOrder = prev < next ? (prev + next) / 2 : prev + 1
    }

    moveTaskMutation.mutate(
      { id: activeId, request: { columnId: targetCol.id, sortOrder: newSortOrder } },
      {
        onSuccess: () => {
          // Sync task status when moving to a column that maps to a known status
          const matchedStatus = COLUMN_STATUS_MAP[targetCol.name.toLowerCase()]
          const movedTask = origCol?.tasks.find(t => t.id === activeId)
          if (matchedStatus && movedTask && movedTask.status !== matchedStatus) {
            changeStatusMutation.mutate({ id: activeId, status: matchedStatus })
          }
        },
        onError: (err) => {
          setLocalColumns(board.columns)
          toast.error(err instanceof Error ? err.message : t('errors.unknown'))
        },
      },
    )
  }, [dragType, board, projectId, moveTaskMutation, changeStatusMutation, reorderColumnsMutation, t])

  // ── Other handlers ─────────────────────────────────────────────────────────
  const handleTaskClick = useCallback((task: TaskCardDto) => {
    setSelectedTaskId(task.taskNumber)
  }, [])

  const handleQuickAddTask = useCallback((columnId: string, title: string) => {
    const lines = title.split('\n').map(l => l.trim()).filter(Boolean)
    if (lines.length > 1) {
      setMultiPasteLines(lines)
      setMultiPasteColumnId(columnId)
      setQuickAddColumnId(null)
      setQuickAddTitle('')
      return
    }
    createTaskMutation.mutate(
      { projectId, title: lines[0] ?? title, columnId },
      {
        onSuccess: () => { setQuickAddColumnId(null); setQuickAddTitle('') },
        onError: (err) => toast.error(err instanceof Error ? err.message : t('errors.unknown')),
      },
    )
  }, [createTaskMutation, projectId, t])

  const handleAddColumn = useCallback(() => {
    if (!newColumnName.trim()) return
    createColumnMutation.mutate(
      { projectId, request: { name: newColumnName.trim() } },
      {
        onSuccess: () => { setIsAddingColumn(false); setNewColumnName('') },
        onError: (err) => toast.error(err instanceof Error ? err.message : t('errors.unknown')),
      },
    )
  }, [createColumnMutation, newColumnName, projectId, t])

  const handleUnassignTask = useCallback((task: TaskCardDto) => {
    updateTaskMutation.mutate(
      { id: task.id, request: { assigneeId: undefined } },
      { onError: (err) => toast.error(err instanceof Error ? err.message : t('errors.unknown')) },
    )
  }, [updateTaskMutation, t])

  const handleQuickComplete = useCallback((task: TaskCardDto) => {
    const newStatus = task.status === 'Done' ? 'Todo' : 'Done'
    changeStatusMutation.mutate(
      { id: task.id, status: newStatus },
      { onError: (err) => toast.error(err instanceof Error ? err.message : t('errors.unknown')) },
    )
  }, [changeStatusMutation, t])

  const handleArchiveTask = useCallback((task: TaskCardDto) => {
    archiveTaskMutation.mutate(task.id, {
      onError: (err) => toast.error(err instanceof Error ? err.message : t('errors.unknown')),
    })
  }, [archiveTaskMutation, t])

  const handleMoveToColumn = useCallback((task: TaskCardDto, columnId: string) => {
    const targetColumn = board?.columns.find(c => c.id === columnId)
    if (!targetColumn) return

    // Move the task to the target column
    moveTaskMutation.mutate(
      { id: task.id, request: { columnId, sortOrder: targetColumn.tasks.length } },
      {
        onSuccess: () => {
          // Also sync status if column maps to a known status
          const matchedStatus = COLUMN_STATUS_MAP[targetColumn.name.toLowerCase()]
          if (matchedStatus && task.status !== matchedStatus) {
            changeStatusMutation.mutate({ id: task.id, status: matchedStatus })
          }
        },
        onError: (err) => toast.error(err instanceof Error ? err.message : t('errors.unknown')),
      },
    )
  }, [board?.columns, moveTaskMutation, changeStatusMutation, t])

  // When user picks a column in the detail modal, move the task + sync status
  const handleModalMoveToColumn = useCallback((taskId: string, columnId: string) => {
    if (!board?.columns) return
    const targetColumn = board.columns.find(c => c.id === columnId)
    if (!targetColumn) return
    const currentColumn = board.columns.find(c => c.tasks.some(t => t.id === taskId))
    if (currentColumn?.id === columnId) return
    moveTaskMutation.mutate(
      { id: taskId, request: { columnId, sortOrder: targetColumn.tasks.length } },
      {
        onSuccess: () => {
          // Sync status if column maps to a known status
          const matchedStatus = COLUMN_STATUS_MAP[targetColumn.name.toLowerCase()]
          const task = currentColumn?.tasks.find(t => t.id === taskId)
          if (matchedStatus && task && task.status !== matchedStatus) {
            changeStatusMutation.mutate({ id: taskId, status: matchedStatus })
          }
        },
      },
    )
  }, [board?.columns, moveTaskMutation, changeStatusMutation])

  const handleContextMenuChangePriority = useCallback((task: TaskCardDto, priority: string) => {
    updateTaskMutation.mutate(
      { id: task.id, request: { priority: priority as TaskPriority } },
      { onError: (err) => toast.error(err instanceof Error ? err.message : t('errors.unknown')) },
    )
  }, [updateTaskMutation, t])

  const handleColumnRenameStart = useCallback((column: KanbanColumnDto) => {
    setEditingColumnId(column.id)
    setEditingColumnName(column.name)
  }, [])

  const handleColumnRenameSave = useCallback((columnId: string) => {
    const original = localColumns.find(c => c.id === columnId)
    if (!editingColumnName.trim() || original?.name === editingColumnName.trim()) {
      setEditingColumnId(null)
      return
    }
    updateColumnMutation.mutate(
      { projectId, columnId, request: { name: editingColumnName.trim() } },
      {
        onSuccess: () => setEditingColumnId(null),
        onError: (err) => { toast.error(err instanceof Error ? err.message : t('errors.unknown')); setEditingColumnId(null) },
      },
    )
  }, [editingColumnName, localColumns, updateColumnMutation, projectId, t])

  // ── Loading / Empty states ─────────────────────────────────────────────────
  if (isLoading) {
    return (
      <div className="flex gap-4 overflow-x-auto pb-4">
        {[...Array(4)].map((_, i) => (
          <div key={i} className="min-w-[280px] space-y-3">
            <Skeleton className="h-12 w-full rounded-lg" />
            <Skeleton className="h-24 w-full rounded-lg" />
            <Skeleton className="h-24 w-full rounded-lg" />
          </div>
        ))}
      </div>
    )
  }

  if (!board || localColumns.length === 0) {
    return (
      <EmptyState
        icon={Kanban}
        title={t('pm.noTasksFound')}
        description={t('pm.createTask')}
      />
    )
  }

  return (
    <div className="space-y-3">
      {/* ── Filter bar ── */}
      <div className="flex flex-wrap items-center gap-2">
        {/* Search input */}
        <TaskSearchInput
          value={boardSearch}
          onChange={(v) => setFilter('board-search', v)}
        />

        {/* Task type dropdown */}
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <button className={`inline-flex items-center gap-1.5 rounded-full px-3 h-9 text-sm font-medium border cursor-pointer transition-all ${
              boardTaskType ? 'bg-primary/5 text-primary border-primary/30 hover:bg-primary/10' : 'bg-background border-border hover:bg-muted'
            }`}>
              <Layers className="h-3.5 w-3.5" />
              {boardTaskType === 'tasks'
                ? t('pm.filterTasks', { defaultValue: 'Tasks' })
                : boardTaskType === 'subtasks'
                  ? t('pm.filterSubtasks', { defaultValue: 'Subtasks' })
                  : t('pm.filterAll', { defaultValue: 'All' })}
              <ChevronDown className="h-3.5 w-3.5 opacity-60" />
            </button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="start" className="w-40">
            {([
              { key: '', label: t('pm.filterAll', { defaultValue: 'All' }) },
              { key: 'tasks', label: t('pm.filterTasks', { defaultValue: 'Tasks only' }) },
              { key: 'subtasks', label: t('pm.filterSubtasks', { defaultValue: 'Subtasks only' }) },
            ] as const).map(({ key, label }) => (
              <DropdownMenuItem
                key={key}
                onClick={() => setFilter('board-task-type', key)}
                className={`cursor-pointer ${boardTaskType === key ? 'bg-primary/5 text-primary font-medium' : ''}`}
              >
                {label}
              </DropdownMenuItem>
            ))}
          </DropdownMenuContent>
        </DropdownMenu>

        {/* Priority multi-select dropdown */}
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <button className={`inline-flex items-center gap-1.5 rounded-full px-3 h-9 text-sm font-medium border cursor-pointer transition-all ${
              boardPriorities.length > 0 ? 'bg-primary/5 text-primary border-primary/30 hover:bg-primary/10' : 'bg-background border-border hover:bg-muted'
            }`}>
              <AlertTriangle className="h-3.5 w-3.5" />
              {t('pm.priority', { defaultValue: 'Priority' })}
              {boardPriorities.length > 0 && (
                <span className="inline-flex items-center justify-center h-4 min-w-4 px-1 rounded-full text-[10px] font-bold leading-none bg-primary text-primary-foreground">
                  {boardPriorities.length}
                </span>
              )}
              <ChevronDown className="h-3.5 w-3.5 opacity-60" />
            </button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="start" className="w-44">
            {(['Low', 'Medium', 'High', 'Urgent'] as const).map(p => {
              const icons = { Low: ArrowDown, Medium: Minus, High: ArrowUp, Urgent: AlertTriangle }
              const colors = { Low: 'text-slate-500', Medium: 'text-blue-500', High: 'text-orange-500', Urgent: 'text-red-500' }
              const Icon = icons[p]
              return (
                <DropdownMenuCheckboxItem
                  key={p}
                  checked={boardPriorities.includes(p)}
                  onCheckedChange={(checked) => {
                    const next = checked ? [...boardPriorities, p] : boardPriorities.filter(x => x !== p)
                    setFilter('board-priorities', next.join(','))
                  }}
                  onSelect={(e) => e.preventDefault()}
                  className="cursor-pointer"
                >
                  <Icon className={`h-3.5 w-3.5 mr-1.5 ${colors[p]}`} />
                  {p}
                </DropdownMenuCheckboxItem>
              )
            })}
            {boardPriorities.length > 0 && (
              <>
                <DropdownMenuSeparator />
                <DropdownMenuItem
                  onClick={() => setFilter('board-priorities', '')}
                  className="cursor-pointer text-muted-foreground text-xs"
                >
                  <X className="h-3 w-3 mr-1.5" />
                  {t('buttons.clearFilters', { defaultValue: 'Clear' })}
                </DropdownMenuItem>
              </>
            )}
          </DropdownMenuContent>
        </DropdownMenu>

        {members && members.length > 0 && (
          <div className="flex flex-wrap gap-1.5 items-center">
            {/* Unassigned filter pill */}
            <button
              onClick={() => {
                const next = boardAssignees.includes('__unassigned__')
                  ? boardAssignees.filter(a => a !== '__unassigned__')
                  : [...boardAssignees, '__unassigned__']
                setFilter('board-assignees', next.join(','))
              }}
              className={`inline-flex items-center gap-1 rounded-full px-3 py-1.5 text-sm font-medium border cursor-pointer transition-all ${
                boardAssignees.includes('__unassigned__')
                  ? 'bg-slate-600 text-white border-slate-600'
                  : 'bg-background border-border hover:bg-muted'
              }`}
              title={t('pm.filterNoAssignee', { defaultValue: 'No assignee' })}
            >
              <UserX className="h-3.5 w-3.5" />
            </button>
            {members.map(member => {
              const firstName = member.employeeName.split(' ')[0]
              const active = boardAssignees.some(a => a !== '__unassigned__' && member.employeeName.toLowerCase().includes(a.toLowerCase()))
              return (
                <DraggableMemberPill
                  key={member.id}
                  member={member}
                  active={active}
                  onFilter={() => {
                    const next = active
                      ? boardAssignees.filter(a => !member.employeeName.toLowerCase().includes(a.toLowerCase()))
                      : [...boardAssignees, firstName]
                    setFilter('board-assignees', next.join(','))
                  }}
                />
              )
            })}
            {dragType === 'member' && (
              <span className="text-xs text-primary/70 animate-pulse flex items-center gap-1">
                <UserCheck className="h-3 w-3" />
                {t('pm.dragToAssign', { defaultValue: 'Drop on a card to assign' })}
              </span>
            )}
          </div>
        )}

        {/* Advanced filters: Labels + Due Date */}
        <TaskFilterPopover
          showCompletion
          showAssignees
          showReporters
          showLabels
          showDueDate
          members={members}
          availableLabels={availableLabels}
          selectedAssignees={boardAssignees}
          onAssigneesChange={(v) => setFilter('board-assignees', v.join(','))}
          selectedReporters={boardReporters}
          onReportersChange={(v) => setFilter('board-reporters', v.join(','))}
          selectedLabels={boardLabels}
          onLabelsChange={(v) => setFilter('board-labels', v.join(','))}
          selectedDueDate={boardDue}
          onDueDateChange={(v) => setFilter('board-due', v)}
          dueDateSpecificStart={boardDueStart}
          onDueDateSpecificStartChange={(v) => setFilter('board-due-start', v)}
          dueDateSpecificEnd={boardDueEnd}
          onDueDateSpecificEndChange={(v) => setFilter('board-due-end', v)}
          completionFilter={boardCompletion}
          onCompletionChange={(v) => setFilter('board-completion', v)}
          onClearAll={clearAllFilters}
          activeCount={advancedFilterCount}
        />

        {hasActiveFilters && (
          <button
            onClick={clearAllFilters}
            className="inline-flex items-center gap-1 text-xs text-muted-foreground hover:text-foreground cursor-pointer transition-colors"
          >
            <X className="h-3 w-3" />
            {t('buttons.clearFilters', { defaultValue: 'Clear filters' })}
          </button>
        )}

        {/* Pan / Select mode toggle */}
        <div className="ml-auto flex items-center rounded-full border border-border bg-muted/40 p-0.5 gap-0.5">
          <Tooltip>
            <TooltipTrigger asChild>
              <button
                className={`inline-flex items-center gap-1.5 rounded-full px-2.5 py-1.5 text-xs font-medium cursor-pointer transition-all ${
                  boardMode === 'pan' ? 'bg-background shadow-sm text-foreground' : 'text-muted-foreground hover:text-foreground'
                }`}
                onClick={() => switchMode('pan')}
                aria-label={t('pm.panMode', { defaultValue: 'Pan mode' })}
              >
                <Hand className="h-3.5 w-3.5" />
              </button>
            </TooltipTrigger>
            <TooltipContent>{t('pm.panMode', { defaultValue: 'Pan mode' })}</TooltipContent>
          </Tooltip>
          <Tooltip>
            <TooltipTrigger asChild>
              <button
                className={`inline-flex items-center gap-1.5 rounded-full px-2.5 py-1.5 text-xs font-medium cursor-pointer transition-all ${
                  boardMode === 'select' ? 'bg-background shadow-sm text-foreground' : 'text-muted-foreground hover:text-foreground'
                }`}
                onClick={() => switchMode('select')}
                aria-label={t('pm.selectMode', { defaultValue: 'Select mode' })}
              >
                <MousePointer2 className="h-3.5 w-3.5" />
              </button>
            </TooltipTrigger>
            <TooltipContent>{t('pm.selectMode', { defaultValue: 'Select mode' })}</TooltipContent>
          </Tooltip>
        </div>
      </div>

      {/* ── Bulk action bar — shown when tasks are selected in select mode ── */}
      {boardMode === 'select' && selectedTaskIds.size > 0 && (
        <div className="flex items-center gap-2 px-3 py-2 rounded-lg border border-primary/30 bg-primary/5 text-sm">
          <span className="font-medium text-primary">
            {t('pm.selectedCount', { count: selectedTaskIds.size, defaultValue: '{{count}} selected' })}
          </span>
          <div className="flex items-center gap-1 ml-2">
            {/* Bulk move to column */}
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <button className="inline-flex items-center justify-center gap-1.5 rounded-md px-2.5 h-7 text-xs font-medium border border-border bg-background hover:bg-muted cursor-pointer transition-all">
                  <Check className="h-3 w-3 shrink-0" />
                  {t('pm.bulkMoveToColumn', { defaultValue: 'Move to' })}
                  <ChevronDown className="h-3 w-3 shrink-0 opacity-60" />
                </button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="start" className="w-44">
                {filteredColumns.map(column => {
                  const status = COLUMN_STATUS_MAP[column.name.toLowerCase()]
                  return (
                    <DropdownMenuItem
                      key={column.id}
                      className="cursor-pointer gap-2"
                      onSelect={() => {
                        const ids = Array.from(selectedTaskIds)
                        if (status) {
                          // Status-mapped column: use bulk API (changes status + moves)
                          bulkChangeStatusMutation.mutate(
                            { taskIds: ids, status },
                            {
                              onSuccess: () => {
                                clearSelection()
                              },
                              onError: (err) => toast.error(err instanceof Error ? err.message : t('errors.unknown')),
                            },
                          )
                        } else {
                          // Custom column: move tasks individually
                          Promise.all(ids.map(id =>
                            moveTaskMutation.mutateAsync({ id, request: { columnId: column.id, sortOrder: 999 } })
                          )).then(() => {
                            clearSelection()
                          }).catch((err) => toast.error(err instanceof Error ? err.message : t('errors.unknown')))
                        }
                      }}
                    >
                      <span className="h-2.5 w-2.5 rounded-full flex-shrink-0" style={{ backgroundColor: getColumnColor(column) }} />
                      {column.name}
                    </DropdownMenuItem>
                  )
                })}
              </DropdownMenuContent>
            </DropdownMenu>

            {/* Bulk archive */}
            <button
              className="inline-flex items-center justify-center gap-1.5 rounded-md px-2.5 h-7 text-xs font-medium border border-border bg-background hover:bg-amber-50 hover:text-amber-600 hover:border-amber-200 cursor-pointer transition-all"
              onClick={() => {
                bulkArchiveTasksMutation.mutate(Array.from(selectedTaskIds), {
                  onSuccess: () => {
                    clearSelection()
                  },
                  onError: (err) => toast.error(err instanceof Error ? err.message : t('errors.unknown')),
                })
              }}
            >
              <Archive className="h-3 w-3 shrink-0" />
              {t('pm.bulkArchive', { defaultValue: 'Archive' })}
            </button>

            {/* Select all / Deselect all toggle */}
            {(() => {
              const allIds = filteredColumns.flatMap(c => c.tasks.map(t => t.id))
              const isAllSelected = allIds.length > 0 && allIds.every(id => selectedTaskIds.has(id))
              return (
                <button
                  className="inline-flex items-center justify-center gap-1.5 rounded-md px-2.5 h-7 text-xs font-medium border border-border bg-background hover:bg-muted cursor-pointer transition-all"
                  onClick={() => {
                    if (isAllSelected) {
                      clearSelection()
                    } else {
                      setSelectedTaskIds(new Set(allIds))
                    }
                  }}
                >
                  {isAllSelected ? <CheckSquare className="h-3 w-3 shrink-0" /> : <Square className="h-3 w-3 shrink-0" />}
                  {isAllSelected
                    ? t('pm.deselectAll', { defaultValue: 'Deselect all' })
                    : t('pm.selectAll', { defaultValue: 'Select all' })}
                </button>
              )
            })()}
          </div>

          <button
            className="ml-auto inline-flex items-center gap-1 text-xs text-muted-foreground hover:text-foreground cursor-pointer transition-colors"
            onClick={clearSelection}
          >
            <X className="h-3 w-3" />
            {t('pm.clearSelection', { defaultValue: 'Clear' })}
          </button>
        </div>
      )}

      {/* ── Kanban board ── */}
      <DndContext
        sensors={sensors}
        collisionDetection={collisionDetectionStrategy}
        onDragStart={handleDragStart}
        onDragOver={handleDragOver}
        onDragEnd={handleDragEnd}
      >
        {/* Outer context for column drag */}
        <SortableContext items={localColumns.map(c => c.id)} strategy={horizontalListSortingStrategy}>
          <div
            ref={boardScrollRef}
            className="relative flex gap-4 overflow-x-auto pb-2 cursor-default scrollbar-none select-none h-[calc(100vh-9.5rem)] min-h-[400px]"
            style={{ scrollbarWidth: 'none', msOverflowStyle: 'none' } as React.CSSProperties}
            onPointerDown={handleBoardPointerDown}
            onPointerMove={handleBoardPointerMove}
            onPointerUp={handleBoardPointerUp}
            onLostPointerCapture={handleBoardPointerUp}
          >
            {filteredColumns.map((column) => {
              const originalColumn = board.columns.find(c => c.id === column.id)
              const originalTaskCount = originalColumn?.tasks.length ?? 0
              const wipExceeded = column.wipLimit != null && originalTaskCount > column.wipLimit
              const isCardOver = overColumnId === column.id && dragType === 'card'

              const isCollapsed = collapsedColumns.has(column.id)

              return (
                <SortableColumnWrapper key={column.id} id={column.id}>
                  {({ dragHandleProps, isDragging }) => isCollapsed ? (
                    /* ── Collapsed column: thin vertical strip ── */
                    <div
                      className={`w-10 flex-shrink-0 flex flex-col items-center rounded-lg border bg-muted/40 dark:bg-muted/50 cursor-pointer transition-all duration-150 overflow-hidden ${
                        isDragging ? 'border-primary/40 shadow-lg opacity-40' : 'border-border dark:border-border/80 hover:border-primary/40'
                      }`}
                      {...dragHandleProps}
                      onClick={() => toggleColumnCollapse(column.id)}
                      title={t('pm.expandColumn', { defaultValue: 'Expand column' })}
                    >
                      <div className="h-1 w-full rounded-t-lg flex-shrink-0" style={{ backgroundColor: getColumnColor(column) }} />
                      <div className="flex-1 flex flex-col items-center justify-center gap-2 py-3 px-0 w-full overflow-hidden">
                        <span
                          className="text-[11px] font-semibold text-muted-foreground leading-none select-none max-h-32 overflow-hidden"
                          style={{ writingMode: 'vertical-lr', textOrientation: 'mixed', transform: 'rotate(180deg)' }}
                        >
                          {column.name}
                        </span>
                        <span className="inline-flex items-center justify-center h-5 min-w-5 rounded-full px-1 text-[10px] font-medium bg-muted text-muted-foreground tabular-nums">
                          {originalTaskCount}
                        </span>
                      </div>
                    </div>
                  ) : (
                    <div className={`flex flex-col h-full bg-muted/40 dark:bg-muted/50 rounded-lg border transition-all duration-150 ${
                      isDragging ? 'border-primary/40 shadow-lg' : 'border-border dark:border-border/80'
                    }`}>
                      {/* Column color stripe */}
                      <div className="h-1 rounded-t-lg" style={{ backgroundColor: getColumnColor(column) }} />
                      {/* Column header — entire left area is draggable, title is inline editable */}
                      <div className={`flex items-center justify-between px-2 py-2.5 border-b border-border/50 dark:border-border/70 ${
                        wipExceeded ? 'bg-red-50 dark:bg-red-950/30' : ''
                      }`}>
                        <div
                          {...dragHandleProps}
                          className="flex items-center gap-1.5 min-w-0 flex-1 cursor-grab active:cursor-grabbing"
                        >
                          <span className="h-2.5 w-2.5 rounded-full flex-shrink-0" style={{ backgroundColor: getColumnColor(column) }} />
                          {editingColumnId === column.id ? (
                            <input
                              autoFocus
                              value={editingColumnName}
                              onChange={(e) => setEditingColumnName(e.target.value)}
                              onBlur={() => handleColumnRenameSave(column.id)}
                              onKeyDown={(e) => {
                                if (e.key === 'Enter') { e.preventDefault(); handleColumnRenameSave(column.id) }
                                if (e.key === 'Escape') setEditingColumnId(null)
                              }}
                              onPointerDown={(e) => e.stopPropagation()}
                              onClick={(e) => e.stopPropagation()}
                              className="flex-1 text-sm font-semibold bg-background border border-input rounded px-1.5 py-0.5 focus:outline-none focus:ring-1 focus:ring-ring min-w-0 cursor-text"
                            />
                          ) : (
                            <Tooltip>
                              <TooltipTrigger asChild>
                                <h3
                                  className="text-sm font-semibold leading-5 truncate hover:bg-muted/40 rounded px-1 -mx-1 transition-colors cursor-text"
                                  onClick={(e) => { e.stopPropagation(); handleColumnRenameStart(column) }}
                                  onPointerDown={(e) => e.stopPropagation()}
                                >
                                  {column.name}
                                </h3>
                              </TooltipTrigger>
                              <TooltipContent>{t('pm.clickToRename', { defaultValue: 'Click to rename' })}</TooltipContent>
                            </Tooltip>
                          )}
                          <span className={`inline-flex items-center justify-center min-w-[1.25rem] h-5 rounded px-1.5 text-xs font-medium tabular-nums flex-shrink-0 ${
                            wipExceeded ? 'bg-red-100 text-red-600 dark:bg-red-950 dark:text-red-400 font-bold' : 'bg-muted text-muted-foreground'
                          }`}>
                            {originalTaskCount}{column.wipLimit != null && `/${column.wipLimit}`}
                          </span>
                          {wipExceeded && (
                            <span className="inline-flex items-center text-[10px] bg-red-100 text-red-600 dark:bg-red-950 dark:text-red-300 rounded-full px-1.5 py-0.5 font-medium leading-[1.1] border border-red-200 dark:border-red-800 flex-shrink-0">
                              {t('pm.wipExceeded', { defaultValue: 'WIP!' })}
                            </span>
                          )}
                        </div>
                        <div className="flex items-center gap-0.5 flex-shrink-0">
                          {onCreateTask && (
                            <Tooltip>
                              <TooltipTrigger asChild>
                                <Button
                                  variant="ghost"
                                  size="icon"
                                  className="h-6 w-6 cursor-pointer"
                                  onClick={(e) => { e.stopPropagation(); onCreateTask(column.id) }}
                                  onPointerDown={(e) => e.stopPropagation()}
                                  aria-label={`${t('pm.createTask')} - ${column.name}`}
                                >
                                  <Plus className="h-4 w-4" />
                                </Button>
                              </TooltipTrigger>
                              <TooltipContent>{t('pm.addCard', { defaultValue: 'Add a card' })}</TooltipContent>
                            </Tooltip>
                          )}
                          <DropdownMenu>
                            <Tooltip>
                              <TooltipTrigger asChild>
                                <DropdownMenuTrigger asChild>
                                  <Button
                                    variant="ghost"
                                    size="icon"
                                    className="h-6 w-6 cursor-pointer"
                                    onPointerDown={(e) => e.stopPropagation()}
                                    onClick={(e) => e.stopPropagation()}
                                    aria-label={t('pm.columnOptions', { defaultValue: 'Column options' })}
                                  >
                                    <EllipsisVertical className="h-4 w-4" />
                                  </Button>
                                </DropdownMenuTrigger>
                              </TooltipTrigger>
                              <TooltipContent>{t('pm.columnOptions', { defaultValue: 'Column options' })}</TooltipContent>
                            </Tooltip>
                            <DropdownMenuContent align="end" onClick={(e) => e.stopPropagation()}>
                              <DropdownMenuItem
                                className="cursor-pointer gap-2"
                                onClick={() => { handleColumnRenameStart(column) }}
                              >
                                <Pencil className="h-3.5 w-3.5" />
                                {t('pm.renameColumn', { defaultValue: 'Rename' })}
                              </DropdownMenuItem>
                              <DropdownMenuItem
                                className="cursor-pointer gap-2"
                                onClick={() => setColumnSettingsId(column.id)}
                              >
                                <EllipsisVertical className="h-3.5 w-3.5" />
                                {t('pm.editColumn', { defaultValue: 'Edit column' })}
                              </DropdownMenuItem>
                              <DropdownMenuSeparator />
                              {/* Sort submenu */}
                              <DropdownMenuSub>
                                <DropdownMenuSubTrigger className="cursor-pointer gap-2">
                                  <ArrowUpDown className="h-3.5 w-3.5" />
                                  {t('pm.sortCards', { defaultValue: 'Sort by' })}
                                </DropdownMenuSubTrigger>
                                <DropdownMenuSubContent>
                                  {([
                                    { key: 'default', label: t('pm.sortDefault', { defaultValue: 'Default order' }) },
                                    { key: 'priority', label: t('pm.sortByPriority', { defaultValue: 'Priority' }) },
                                    { key: 'dueDate', label: t('pm.sortByDueDate', { defaultValue: 'Due date' }) },
                                    { key: 'alpha', label: t('pm.sortByAlpha', { defaultValue: 'Alphabetical' }) },
                                  ] as const).map(({ key, label }) => (
                                    <DropdownMenuItem
                                      key={key}
                                      className="cursor-pointer gap-2"
                                      onClick={() => setColumnSortOverrides(prev => ({ ...prev, [column.id]: key }))}
                                    >
                                      <Check className={`h-3.5 w-3.5 ${(columnSortOverrides[column.id] ?? 'default') === key ? 'opacity-100' : 'opacity-0'}`} />
                                      {label}
                                    </DropdownMenuItem>
                                  ))}
                                </DropdownMenuSubContent>
                              </DropdownMenuSub>
                              {/* Collapse/expand */}
                              <DropdownMenuItem
                                className="cursor-pointer gap-2"
                                onClick={() => toggleColumnCollapse(column.id)}
                              >
                                <ChevronsUpDown className="h-3.5 w-3.5" />
                                {collapsedColumns.has(column.id)
                                  ? t('pm.expandColumn', { defaultValue: 'Expand column' })
                                  : t('pm.collapseColumn', { defaultValue: 'Collapse column' })
                                }
                              </DropdownMenuItem>
                              <DropdownMenuSeparator />
                              {/* Move all cards */}
                              <DropdownMenuItem
                                className="cursor-pointer gap-2"
                                onClick={() => {
                                  setMoveAllColumnId(column.id)
                                  setMoveAllTargetColumnId(localColumns.find(c => c.id !== column.id)?.id ?? '')
                                }}
                              >
                                <ArrowUp className="h-3.5 w-3.5" />
                                {t('pm.moveAllCards', { defaultValue: 'Move all cards to...' })}
                              </DropdownMenuItem>
                              {/* Archive all cards */}
                              <DropdownMenuItem
                                className="cursor-pointer gap-2"
                                onClick={() => setArchiveAllColumnId(column.id)}
                              >
                                <Archive className="h-3.5 w-3.5" />
                                {t('pm.archiveAllCards', { defaultValue: 'Archive all cards' })}
                              </DropdownMenuItem>
                              {/* Duplicate column */}
                              <DropdownMenuItem
                                className="cursor-pointer gap-2"
                                onClick={() => {
                                  duplicateColumnMutation.mutate(
                                    { projectId, columnId: column.id },
                                  )
                                }}
                              >
                                <Copy className="h-3.5 w-3.5" />
                                {t('pm.duplicateColumn', { defaultValue: 'Duplicate column' })}
                              </DropdownMenuItem>
                              <DropdownMenuSeparator />
                              <DropdownMenuItem
                                className="cursor-pointer gap-2 text-destructive focus:text-destructive"
                                onClick={() => {
                                  setDeleteColumnId(column.id)
                                  setDeleteColumnMoveToId(localColumns.find(c => c.id !== column.id)?.id ?? '')
                                }}
                              >
                                <Trash2 className="h-3.5 w-3.5" />
                                {t('pm.deleteColumn', { defaultValue: 'Delete column' })}
                              </DropdownMenuItem>
                            </DropdownMenuContent>
                          </DropdownMenu>
                        </div>
                      </div>

                      {/* Cards + quick-add inside the droppable body so the button sits right below the last card */}
                      <SortableContext items={column.tasks.map(t => t.id)} strategy={verticalListSortingStrategy}>
                        <DroppableColumnBody id={column.id} isCardOver={isCardOver}>
                          {column.tasks.map((task) => (
                            <TaskCard
                              key={task.id}
                              task={task}
                              onClick={handleTaskClick}
                              isDraggable={boardMode === 'pan'}
                              onComplete={boardMode === 'pan' ? handleQuickComplete : undefined}
                              onUnassign={boardMode === 'pan' ? handleUnassignTask : undefined}
                              onArchive={boardMode === 'pan' ? handleArchiveTask : undefined}
                              onChangePriority={boardMode === 'pan' ? handleContextMenuChangePriority : undefined}
                              onMoveToColumn={boardMode === 'pan' ? handleMoveToColumn : undefined}
                              columns={filteredColumns}
                              currentColumnId={column.id}
                              isMemberDragTarget={dragType === 'member' && overTaskId === task.id}
                              isSelected={selectedTaskIds.has(task.id)}
                              onSelect={boardMode === 'select' ? toggleTaskSelection : undefined}
                            />
                          ))}
                          {column.tasks.length === 0 && (
                            <div className={`flex flex-col items-center justify-center py-6 text-center border-2 border-dashed rounded-lg transition-all duration-150 ${
                              isCardOver ? 'border-primary/60 bg-primary/5' : 'border-border/50 dark:border-border/60'
                            }`}>
                              <p className="text-xs text-muted-foreground">
                                {t('pm.dropHere', { defaultValue: 'Drop tasks here' })}
                              </p>
                            </div>
                          )}

                          {/* Quick-add sits right below the last card */}
                          {quickAddColumnId === column.id ? (
                            <div onClick={(e) => e.stopPropagation()}>
                              <textarea
                                autoFocus
                                rows={2}
                                value={quickAddTitle}
                                onChange={(e) => setQuickAddTitle(e.target.value)}
                                onPaste={(e) => {
                                  const text = e.clipboardData.getData('text')
                                  const lines = text.split('\n').map(l => l.trim()).filter(Boolean)
                                  if (lines.length > 1) {
                                    e.preventDefault()
                                    setMultiPasteLines(lines)
                                    setMultiPasteColumnId(column.id)
                                    setQuickAddColumnId(null)
                                    setQuickAddTitle('')
                                  }
                                }}
                                onKeyDown={(e) => {
                                  if (e.key === 'Enter' && !e.shiftKey) {
                                    e.preventDefault()
                                    if (quickAddTitle.trim()) handleQuickAddTask(column.id, quickAddTitle.trim())
                                  }
                                  if (e.key === 'Escape') { setQuickAddColumnId(null); setQuickAddTitle('') }
                                }}
                                placeholder={t('pm.taskTitlePlaceholder', { defaultValue: 'Task title...' })}
                                className="w-full resize-none text-sm bg-background border border-input rounded-md px-3 py-2 focus:outline-none focus:ring-2 focus:ring-ring"
                              />
                              <div className="flex items-center gap-2 mt-2">
                                <Button
                                  size="sm"
                                  className="cursor-pointer h-7 text-xs bg-green-600 hover:bg-green-700 text-white border-0"
                                  disabled={createTaskMutation.isPending}
                                  onClick={() => { if (quickAddTitle.trim()) handleQuickAddTask(column.id, quickAddTitle.trim()) }}
                                >
                                  {createTaskMutation.isPending && <Loader2 className="mr-1 h-3 w-3 animate-spin" />}
                                  {t('pm.addCard', { defaultValue: 'Add card' })}
                                </Button>
                                <Button
                                  variant="ghost"
                                  size="sm"
                                  className="cursor-pointer h-7 text-xs"
                                  onClick={() => { setQuickAddColumnId(null); setQuickAddTitle('') }}
                                >
                                  {t('buttons.cancel')}
                                </Button>
                              </div>
                            </div>
                          ) : (
                            <button
                              className="w-full flex items-center gap-1.5 px-1 py-1.5 text-xs text-muted-foreground hover:text-foreground hover:bg-muted/40 rounded-md transition-colors cursor-pointer"
                              onClick={() => setQuickAddColumnId(column.id)}
                            >
                              <Plus className="h-3.5 w-3.5" />
                              {t('pm.addCard', { defaultValue: 'Add card' })}
                            </button>
                          )}
                        </DroppableColumnBody>
                      </SortableContext>
                    </div>
                  )}
                </SortableColumnWrapper>
              )
            })}

            {/* Inline add column */}
            {/* Rubber-band lasso overlay */}
            {lassoRect && (
              <div
                className="absolute bg-primary/10 border border-primary/40 rounded-sm pointer-events-none z-50"
                style={{
                  left: Math.min(lassoRect.startX, lassoRect.endX),
                  top: Math.min(lassoRect.startY, lassoRect.endY),
                  width: Math.abs(lassoRect.endX - lassoRect.startX),
                  height: Math.abs(lassoRect.endY - lassoRect.startY),
                }}
              />
            )}

            {isAddingColumn ? (
              <div className="min-w-[240px] max-w-[280px] flex-shrink-0 bg-muted/40 dark:bg-muted/50 rounded-lg border border-border dark:border-border/80 p-3 space-y-2">
                <input
                  autoFocus
                  value={newColumnName}
                  onChange={(e) => setNewColumnName(e.target.value)}
                  onKeyDown={(e) => {
                    if (e.key === 'Enter') handleAddColumn()
                    if (e.key === 'Escape') { setIsAddingColumn(false); setNewColumnName('') }
                  }}
                  placeholder={t('pm.newColumnName', { defaultValue: 'Column name...' })}
                  className="w-full text-sm bg-background border border-input rounded-md px-3 py-2 focus:outline-none focus:ring-2 focus:ring-ring"
                />
                <div className="flex gap-2">
                  <Button size="sm" className="cursor-pointer" disabled={createColumnMutation.isPending} onClick={handleAddColumn}>
                    {createColumnMutation.isPending && <Loader2 className="mr-1 h-3 w-3 animate-spin" />}
                    {t('buttons.add', { defaultValue: 'Add' })}
                  </Button>
                  <Button variant="ghost" size="sm" className="cursor-pointer" onClick={() => { setIsAddingColumn(false); setNewColumnName('') }}>
                    {t('buttons.cancel')}
                  </Button>
                </div>
              </div>
            ) : (
              <button
                className="min-w-[200px] flex-shrink-0 h-12 flex items-center justify-center gap-2 rounded-lg border-2 border-dashed border-border/50 dark:border-border/60 text-muted-foreground hover:border-primary/50 hover:text-primary hover:bg-primary/5 transition-all cursor-pointer text-sm"
                onClick={() => setIsAddingColumn(true)}
              >
                <Plus className="h-4 w-4" />
                {t('pm.addColumn')}
              </button>
            )}
          </div>
        </SortableContext>

        {/* DragOverlay */}
        <DragOverlay>
          {dragType === 'member' && activeId && (() => {
            const member = members?.find(m => `member-${m.employeeId}` === activeId)
            if (!member) return null
            return (
              <div className="flex items-center gap-2 bg-primary text-primary-foreground rounded-full px-3 py-1.5 text-xs font-medium shadow-lg border border-primary/50">
                <Avatar src={member.avatarUrl ?? undefined} alt={member.employeeName} fallback={member.employeeName} size="sm" className="h-5 w-5 text-[9px]" />
                {member.employeeName.split(' ')[0]}
                <UserCheck className="h-3.5 w-3.5 ml-0.5" />
              </div>
            )
          })()}
          {activeTask && (
            <div className="w-[280px] rotate-2 opacity-95 shadow-2xl">
              <TaskCard task={activeTask} onClick={() => {}} isDraggable={false} />
            </div>
          )}
          {activeColumn && (
            <div className="min-w-[280px] max-w-[320px] bg-muted/50 dark:bg-muted/60 rounded-lg border border-primary/40 shadow-2xl rotate-1 overflow-hidden">
              <div className="flex items-center gap-2 px-3 py-2.5 border-b border-border/50 bg-muted/60">
                {activeColumn.color && <span className="h-2.5 w-2.5 rounded-full" style={{ backgroundColor: activeColumn.color }} />}
                <h3 className="text-sm font-semibold">{activeColumn.name}</h3>
                <span className="text-xs text-muted-foreground">{activeColumn.tasks.length}</span>
              </div>
              <div className="p-2 space-y-1.5 max-h-48 overflow-hidden">
                {activeColumn.tasks.slice(0, 3).map(task => (
                  <div key={task.id} className="h-10 bg-background/60 rounded-md border border-border/40" />
                ))}
                {activeColumn.tasks.length > 3 && (
                  <p className="text-[10px] text-center text-muted-foreground py-1">+{activeColumn.tasks.length - 3} more</p>
                )}
                {activeColumn.tasks.length === 0 && (
                  <div className="h-12 border-2 border-dashed border-border/30 rounded-md" />
                )}
              </div>
            </div>
          )}
        </DragOverlay>
      </DndContext>

      {/* Column settings dialog */}
      <ColumnSettingsDialog
        open={columnSettingsId !== null}
        onOpenChange={(open) => { if (!open) setColumnSettingsId(null) }}
        projectId={projectId}
        column={localColumns.find(c => c.id === columnSettingsId) ?? null}
      />

      {/* Delete column confirmation */}
      <Credenza open={deleteColumnId !== null} onOpenChange={(open) => { if (!open) setDeleteColumnId(null) }}>
        <CredenzaContent className="border-destructive/30">
          <CredenzaHeader>
            <CredenzaTitle>{t('pm.deleteColumn', { defaultValue: 'Delete column' })}</CredenzaTitle>
            <CredenzaDescription>
              {t('pm.deleteColumnDesc', { defaultValue: 'Tasks in this column will be moved to another column.' })}
            </CredenzaDescription>
          </CredenzaHeader>
          <CredenzaBody className="space-y-3">
            <div>
              <label className="text-sm font-medium">{t('pm.moveTasksTo', { defaultValue: 'Move tasks to' })}</label>
              <Select value={deleteColumnMoveToId} onValueChange={setDeleteColumnMoveToId}>
                <SelectTrigger className="mt-1 cursor-pointer">
                  <SelectValue placeholder={t('pm.selectColumn', { defaultValue: 'Select column' })} />
                </SelectTrigger>
                <SelectContent>
                  {localColumns.filter(c => c.id !== deleteColumnId).map(c => (
                    <SelectItem key={c.id} value={c.id} className="cursor-pointer">{c.name}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </CredenzaBody>
          <CredenzaFooter>
            <Button variant="outline" className="cursor-pointer" onClick={() => setDeleteColumnId(null)}>
              {t('buttons.cancel')}
            </Button>
            <Button
              variant="destructive"
              className="cursor-pointer bg-destructive/10 text-destructive border border-destructive/30 hover:bg-destructive hover:text-destructive-foreground transition-colors"
              disabled={!deleteColumnMoveToId || deleteColumnMutation.isPending}
              onClick={() => {
                if (!deleteColumnId || !deleteColumnMoveToId) return
                deleteColumnMutation.mutate(
                  { projectId, columnId: deleteColumnId, moveToColumnId: deleteColumnMoveToId },
                  {
                    onSuccess: () => { setDeleteColumnId(null) },
                    onError: (err) => { toast.error(err instanceof Error ? err.message : t('errors.unknown')) },
                  }
                )
              }}
            >
              {deleteColumnMutation.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              <Trash2 className="h-4 w-4 mr-1.5" />
              {t('pm.deleteColumn', { defaultValue: 'Delete column' })}
            </Button>
          </CredenzaFooter>
        </CredenzaContent>
      </Credenza>

      {/* Move all cards dialog */}
      <Credenza open={moveAllColumnId !== null} onOpenChange={(open) => { if (!open) setMoveAllColumnId(null) }}>
        <CredenzaContent>
          <CredenzaHeader>
            <CredenzaTitle>{t('pm.moveAllCards', { defaultValue: 'Move all cards to...' })}</CredenzaTitle>
            <CredenzaDescription>
              {t('pm.moveAllCardsDesc', { defaultValue: 'All cards in this column will be moved to the selected column.' })}
            </CredenzaDescription>
          </CredenzaHeader>
          <CredenzaBody className="space-y-3">
            <div>
              <label className="text-sm font-medium">{t('pm.targetColumn', { defaultValue: 'Target column' })}</label>
              <Select value={moveAllTargetColumnId} onValueChange={setMoveAllTargetColumnId}>
                <SelectTrigger className="mt-1 cursor-pointer">
                  <SelectValue placeholder={t('pm.selectColumn', { defaultValue: 'Select column' })} />
                </SelectTrigger>
                <SelectContent>
                  {localColumns.filter(c => c.id !== moveAllColumnId).map(c => (
                    <SelectItem key={c.id} value={c.id} className="cursor-pointer">{c.name}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </CredenzaBody>
          <CredenzaFooter>
            <Button variant="outline" className="cursor-pointer" onClick={() => setMoveAllColumnId(null)}>
              {t('buttons.cancel')}
            </Button>
            <Button
              className="cursor-pointer"
              disabled={!moveAllTargetColumnId || moveAllColumnTasksMutation.isPending}
              onClick={() => {
                if (!moveAllColumnId || !moveAllTargetColumnId) return
                moveAllColumnTasksMutation.mutate(
                  { projectId, sourceColumnId: moveAllColumnId, targetColumnId: moveAllTargetColumnId },
                  {
                    onSuccess: () => {
                      setMoveAllColumnId(null)
                    },
                    onError: (err) => toast.error(err instanceof Error ? err.message : t('errors.unknown')),
                  }
                )
              }}
            >
              {moveAllColumnTasksMutation.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              {t('pm.moveCards', { defaultValue: 'Move cards' })}
            </Button>
          </CredenzaFooter>
        </CredenzaContent>
      </Credenza>

      {/* Archive all cards confirmation */}
      <Credenza open={archiveAllColumnId !== null} onOpenChange={(open) => { if (!open) setArchiveAllColumnId(null) }}>
        <CredenzaContent className="border-destructive/30">
          <CredenzaHeader>
            <CredenzaTitle>{t('pm.archiveAllCards', { defaultValue: 'Archive all cards' })}</CredenzaTitle>
            <CredenzaDescription>
              {t('pm.archiveAllCardsDesc', { defaultValue: 'All cards in this column will be moved to the archive. You can restore them later.' })}
            </CredenzaDescription>
          </CredenzaHeader>
          <CredenzaFooter>
            <Button variant="outline" className="cursor-pointer" onClick={() => setArchiveAllColumnId(null)}>
              {t('buttons.cancel')}
            </Button>
            <Button
              className="cursor-pointer bg-destructive/10 text-destructive border border-destructive/30 hover:bg-destructive hover:text-destructive-foreground transition-colors"
              disabled={bulkArchiveTasksMutation.isPending}
              onClick={() => {
                if (!archiveAllColumnId) return
                const column = localColumns.find(c => c.id === archiveAllColumnId)
                const taskIds = column?.tasks.map(t => t.id) ?? []
                if (taskIds.length === 0) { setArchiveAllColumnId(null); return }
                bulkArchiveTasksMutation.mutate(taskIds, {
                  onSuccess: () => {
                    setArchiveAllColumnId(null)
                  },
                  onError: (err) => toast.error(err instanceof Error ? err.message : t('errors.unknown')),
                })
              }}
            >
              {bulkArchiveTasksMutation.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              <Archive className="h-4 w-4 mr-1.5" />
              {t('pm.archiveAll', { defaultValue: 'Archive all' })}
            </Button>
          </CredenzaFooter>
        </CredenzaContent>
      </Credenza>

      {/* Trello-style centered task modal */}
      <TaskDetailModal
        taskId={selectedTaskId}
        open={taskModalOpen}
        onOpenChange={(open) => { if (!open) setSelectedTaskId(null) }}
        projectMembers={members}
        onNavigateToTask={(taskId) => {
          const task = localColumnsRef.current.flatMap(c => c.tasks).find(t => t.id === taskId)
          if (task) setSelectedTaskId(task.taskNumber)
        }}
        boardColumns={filteredColumns}
        onMoveToColumn={handleModalMoveToColumn}
      />

      {/* Multi-line paste confirmation */}
      {multiPasteLines.length > 0 && multiPasteColumnId && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <div className="bg-background border border-border rounded-xl shadow-2xl p-6 max-w-sm w-full mx-4 space-y-4">
            <div>
              <h3 className="font-semibold text-sm">{t('pm.createMultipleTasks', { defaultValue: 'Create multiple tasks?' })}</h3>
              <p className="text-xs text-muted-foreground mt-1">
                {t('pm.createMultipleTasksDesc', { count: multiPasteLines.length, defaultValue: `Create ${multiPasteLines.length} tasks from pasted text?` })}
              </p>
            </div>
            <ul className="space-y-1 max-h-48 overflow-y-auto">
              {multiPasteLines.map((line, i) => (
                <li key={i} className="text-xs bg-muted rounded px-2 py-1 truncate">{line}</li>
              ))}
            </ul>
            <div className="flex gap-2">
              <Button
                size="sm"
                className="flex-1 cursor-pointer bg-green-600 hover:bg-green-700 text-white border-0"
                disabled={createTaskMutation.isPending}
                onClick={async () => {
                  for (const line of multiPasteLines) {
                    await createTaskMutation.mutateAsync({ projectId, title: line, columnId: multiPasteColumnId })
                  }
                  setMultiPasteLines([])
                  setMultiPasteColumnId(null)
                }}
              >
                {createTaskMutation.isPending && <Loader2 className="mr-1 h-3 w-3 animate-spin" />}
                {t('pm.createAll', { defaultValue: 'Create all' })}
              </Button>
              <Button
                variant="ghost"
                size="sm"
                className="cursor-pointer"
                onClick={() => { setMultiPasteLines([]); setMultiPasteColumnId(null) }}
              >
                {t('buttons.cancel')}
              </Button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
