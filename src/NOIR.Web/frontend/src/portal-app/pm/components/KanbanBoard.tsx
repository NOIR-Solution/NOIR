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
  useDroppable,
  useDraggable,
  type DragEndEvent,
  type DragStartEvent,
  type DragOverEvent,
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
import { Plus, Kanban, Search, X, ArrowDown, Minus, ArrowUp, AlertTriangle, Loader2, MoreHorizontal, Pencil, Trash2, UserCheck, UserX } from 'lucide-react'
import {
  Avatar, Button, EmptyState, Skeleton,
  DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuSeparator, DropdownMenuTrigger,
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
} from '@/portal-app/pm/queries'
import type { TaskCardDto, KanbanColumnDto, ProjectMemberDto, TaskLabelBriefDto } from '@/types/pm'
import { TaskCard } from './TaskCard'
import { TaskDetailModal } from './TaskDetailModal'
import { ColumnSettingsDialog } from './ColumnSettingsDialog'
import { TaskFilterPopover, matchDueDate, type DueDateFilter } from './TaskFilterPopover'

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
      className={`space-y-2 p-2 min-h-[100px] rounded-b-lg transition-all duration-150 ${
        isCardOver ? 'bg-primary/5 ring-1 ring-inset ring-primary/25' : ''
      }`}
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
      className={`min-w-[280px] max-w-[320px] flex-shrink-0 group/col ${isDragging ? 'opacity-40' : ''}`}
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
      className={`inline-flex items-center gap-1.5 rounded-full px-2 py-1 text-xs font-medium leading-[1.1] border cursor-grab active:cursor-grabbing transition-all select-none ${
        active ? 'bg-primary text-primary-foreground border-primary' : 'bg-background border-border hover:bg-muted'
      }`}
      title={`${member.employeeName} — drag to assign`}
      onClick={(e) => { e.stopPropagation(); onFilter() }}
    >
      <Avatar src={member.avatarUrl ?? undefined} alt={member.employeeName} fallback={member.employeeName} size="sm" className="h-4 w-4 text-[8px]" />
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

  // ── Optimistic local state ─────────────────────────────────────────────────
  const [localColumns, setLocalColumns] = useState<KanbanColumnDto[]>([])
  const isDraggingRef = useRef(false)

  // ── Figma-style board pan ─────────────────────────────────────────────────
  const boardScrollRef = useRef<HTMLDivElement>(null)
  const panRef = useRef<{ pointerId: number; startX: number; scrollLeft: number } | null>(null)

  const handleBoardPointerDown = useCallback((e: React.PointerEvent<HTMLDivElement>) => {
    if (e.button !== 0) return
    const target = e.target as HTMLElement
    // Skip pan if clicking interactive elements or dnd-kit draggable items
    if (target.closest('button, input, textarea, a, select, [role="button"], [role="option"]')) return
    if (target.closest('.cursor-grab')) return
    panRef.current = { pointerId: e.pointerId, startX: e.clientX, scrollLeft: boardScrollRef.current?.scrollLeft ?? 0 }
    ;(e.currentTarget as HTMLElement).setPointerCapture(e.pointerId)
    ;(e.currentTarget as HTMLElement).style.cursor = 'grabbing'
  }, [])

  const handleBoardPointerMove = useCallback((e: React.PointerEvent<HTMLDivElement>) => {
    if (!panRef.current || !boardScrollRef.current) return
    const dx = e.clientX - panRef.current.startX
    boardScrollRef.current.scrollLeft = panRef.current.scrollLeft - dx
  }, [])

  const handleBoardPointerUp = useCallback((e: React.PointerEvent<HTMLDivElement>) => {
    if (!panRef.current) return
    panRef.current = null
    ;(e.currentTarget as HTMLElement).style.cursor = ''
  }, [])

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
  const boardPriorities = searchParams.get('board-priorities')?.split(',').filter(Boolean) ?? []
  const boardLabels = searchParams.get('board-labels')?.split(',').filter(Boolean) ?? []
  const boardDue = (searchParams.get('board-due') ?? '') as DueDateFilter

  const advancedFilterCount = boardLabels.length + (boardDue ? 1 : 0)
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
      next.delete('board-priorities')
      next.delete('board-task-type')
      next.delete('board-labels')
      next.delete('board-due')
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
  const filteredColumns = useMemo(() => localColumns.map(col => ({
    ...col,
    tasks: col.tasks.filter(task => {
      const matchSearch = !boardSearch || task.title.toLowerCase().includes(boardSearch.toLowerCase())
      const matchAssignee =
        boardAssignees.length === 0 ||
        (boardAssignees.includes('__unassigned__') && !task.assigneeName) ||
        (task.assigneeName != null &&
          boardAssignees.some(a => a !== '__unassigned__' && task.assigneeName!.toLowerCase().includes(a.toLowerCase())))
      const matchPriority = boardPriorities.length === 0 || boardPriorities.includes(task.priority)
      const matchTaskType =
        boardTaskType === 'all' ||
        (boardTaskType === 'tasks' && !task.parentTaskId) ||
        (boardTaskType === 'subtasks' && Boolean(task.parentTaskId))
      const matchLabel =
        boardLabels.length === 0 ||
        (boardLabels.includes('__no-label__') && task.labels.length === 0) ||
        task.labels.some(l => boardLabels.includes(l.id))
      const matchDue = matchDueDate(task.dueDate, boardDue)
      return matchSearch && matchAssignee && matchPriority && matchTaskType && matchLabel && matchDue
    }),
  })), [localColumns, boardSearch, boardAssignees, boardPriorities, boardTaskType, boardLabels, boardDue])

  // ── DnD sensors ────────────────────────────────────────────────────────────
  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 8 } }),
    useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates }),
  )

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
    const overId = String(over.id)
    if (activeId === overId) return

    if (currentType === 'column') {
      // Commit column order — localColumns already updated optimistically in onDragOver
      const fromIdx = localColumns.findIndex(c => c.id === activeId)
      const toIdx = localColumns.findIndex(c => c.id === overId)
      const finalCols = fromIdx !== -1 && toIdx !== -1 && fromIdx !== toIdx
        ? arrayMove(localColumns, fromIdx, toIdx)
        : localColumns

      reorderColumnsMutation.mutate(
        { projectId, request: { columnIds: finalCols.map(c => c.id) } },
        {
          onError: (err) => {
            setLocalColumns(board.columns)
            toast.error(err instanceof Error ? err.message : t('errors.unknown'))
          },
        },
      )
      return
    }

    // Card: find its final position in localColumns and calculate sort order
    const targetColIdx = localColumns.findIndex(col => col.tasks.some(t => t.id === activeId))
    if (targetColIdx === -1) { setLocalColumns(board.columns); return }

    const targetCol = localColumns[targetColIdx]
    const taskIdx = targetCol.tasks.findIndex(t => t.id === activeId)
    const tasks = targetCol.tasks

    // Calculate sort order using only the neighbors' values (ignoring the task's own stale sort order).
    // This correctly handles cross-column moves where the task still carries its source-column sort order.
    const prevTask = taskIdx > 0 ? tasks[taskIdx - 1] : null
    const nextTask = taskIdx < tasks.length - 1 ? tasks[taskIdx + 1] : null

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
        onError: (err) => {
          setLocalColumns(board.columns)
          toast.error(err instanceof Error ? err.message : t('errors.unknown'))
        },
      },
    )
  }, [dragType, localColumns, board, projectId, moveTaskMutation, reorderColumnsMutation, t])

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
      <div className="flex flex-wrap items-center gap-3">
        <div className="relative min-w-[200px]">
          <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-muted-foreground" />
          <input
            value={boardSearch}
            onChange={(e) => setFilter('board-search', e.target.value)}
            placeholder={t('pm.searchTasks', { defaultValue: 'Search tasks...' })}
            className="w-full pl-8 pr-8 py-1.5 text-sm bg-background border border-input rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
          />
          {boardSearch && (
            <button
              className="absolute right-2 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground cursor-pointer"
              onClick={() => setFilter('board-search', '')}
              aria-label={t('buttons.clear', { defaultValue: 'Clear' })}
            >
              <X className="h-3.5 w-3.5" />
            </button>
          )}
        </div>

        {/* Task type filter */}
        <div className="flex gap-1">
          {([
            { key: 'all', label: t('pm.filterAll', { defaultValue: 'All' }) },
            { key: 'tasks', label: t('pm.filterTasks', { defaultValue: 'Tasks' }) },
            { key: 'subtasks', label: t('pm.filterSubtasks', { defaultValue: 'Subtasks' }) },
          ] as const).map(({ key, label }) => (
            <button
              key={key}
              onClick={() => setFilter('board-task-type', key === 'all' ? '' : key)}
              className={`inline-flex items-center gap-1 rounded-full px-2.5 py-1 text-xs font-medium leading-[1.1] border cursor-pointer transition-all ${
                boardTaskType === key ? 'bg-primary text-primary-foreground border-primary' : 'bg-background border-border hover:bg-muted'
              }`}
            >
              {label}
            </button>
          ))}
        </div>

        <div className="flex flex-wrap gap-1.5">
          {(['Low', 'Medium', 'High', 'Urgent'] as const).map(p => {
            const active = boardPriorities.includes(p)
            const icons = { Low: ArrowDown, Medium: Minus, High: ArrowUp, Urgent: AlertTriangle }
            const Icon = icons[p]
            return (
              <button
                key={p}
                onClick={() => {
                  const next = active ? boardPriorities.filter(x => x !== p) : [...boardPriorities, p]
                  setFilter('board-priorities', next.join(','))
                }}
                className={(() => {
                  const c = { Low: { a: 'bg-slate-500 text-white border-slate-500', i: 'hover:bg-slate-50 hover:border-slate-300 hover:text-slate-600' }, Medium: { a: 'bg-blue-500 text-white border-blue-500', i: 'hover:bg-blue-50 hover:border-blue-300 hover:text-blue-600' }, High: { a: 'bg-orange-500 text-white border-orange-500', i: 'hover:bg-orange-50 hover:border-orange-300 hover:text-orange-600' }, Urgent: { a: 'bg-red-500 text-white border-red-500', i: 'hover:bg-red-50 hover:border-red-300 hover:text-red-600' } }[p]
                  return `inline-flex items-center gap-1 rounded-full px-2.5 py-1 text-xs font-medium leading-[1.1] border cursor-pointer transition-all ${active ? c.a : `bg-background border-border text-foreground ${c.i}`}`
                })()}
              >
                <Icon className="h-3 w-3" />{p}
              </button>
            )
          })}
        </div>

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
              className={`inline-flex items-center gap-1 rounded-full px-2 py-1 text-xs font-medium leading-[1.1] border cursor-pointer transition-all ${
                boardAssignees.includes('__unassigned__')
                  ? 'bg-slate-600 text-white border-slate-600'
                  : 'bg-background border-border hover:bg-muted'
              }`}
              title={t('pm.filterNoAssignee', { defaultValue: 'No assignee' })}
            >
              <UserX className="h-3 w-3" />
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
          showLabels
          showDueDate
          availableLabels={availableLabels}
          selectedLabels={boardLabels}
          selectedDueDate={boardDue}
          onLabelsChange={(v) => setFilter('board-labels', v.join(','))}
          onDueDateChange={(v) => setFilter('board-due', v)}
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
      </div>

      {/* ── Kanban board ── */}
      <DndContext
        sensors={sensors}
        collisionDetection={closestCorners}
        onDragStart={handleDragStart}
        onDragOver={handleDragOver}
        onDragEnd={handleDragEnd}
      >
        {/* Outer context for column drag */}
        <SortableContext items={localColumns.map(c => c.id)} strategy={horizontalListSortingStrategy}>
          <div
            ref={boardScrollRef}
            className="flex gap-4 overflow-x-auto pb-1 cursor-default scrollbar-none select-none"
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

              return (
                <SortableColumnWrapper key={column.id} id={column.id}>
                  {({ dragHandleProps, isDragging }) => (
                    <div className={`bg-muted/30 rounded-lg border transition-all duration-150 ${
                      isDragging ? 'border-primary/40 shadow-lg' : 'border-border/50'
                    }`}>
                      {/* Column color stripe */}
                      {column.color && (
                        <div className="h-1 rounded-t-lg" style={{ backgroundColor: column.color }} />
                      )}
                      {/* Column header — entire left area is draggable, title is inline editable */}
                      <div className={`flex items-center justify-between px-2 py-2.5 border-b border-border/30 ${!column.color ? 'rounded-t-lg' : ''} ${
                        wipExceeded ? 'bg-red-50 dark:bg-red-950/30' : ''
                      }`}>
                        <div
                          {...dragHandleProps}
                          className="flex items-center gap-1.5 min-w-0 flex-1 cursor-grab active:cursor-grabbing"
                        >
                          {column.color && (
                            <span className="h-2.5 w-2.5 rounded-full flex-shrink-0" style={{ backgroundColor: column.color }} />
                          )}
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
                                    <MoreHorizontal className="h-4 w-4" />
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
                                <MoreHorizontal className="h-3.5 w-3.5" />
                                {t('pm.editColumn', { defaultValue: 'Edit column' })}
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

                      {/* Cards */}
                      <SortableContext items={column.tasks.map(t => t.id)} strategy={verticalListSortingStrategy}>
                        <DroppableColumnBody id={column.id} isCardOver={isCardOver}>
                          {column.tasks.map((task) => (
                            <TaskCard
                              key={task.id}
                              task={task}
                              onClick={handleTaskClick}
                              isDraggable
                              onComplete={handleQuickComplete}
                              onUnassign={handleUnassignTask}
                              isMemberDragTarget={dragType === 'member' && overTaskId === task.id}
                            />
                          ))}
                          {column.tasks.length === 0 && (
                            <div className={`flex flex-col items-center justify-center py-6 text-center border-2 border-dashed rounded-lg m-2 transition-all duration-150 ${
                              isCardOver ? 'border-primary/60 bg-primary/5' : 'border-border/30'
                            }`}>
                              <p className="text-xs text-muted-foreground/50">
                                {t('pm.dropHere', { defaultValue: 'Drop tasks here' })}
                              </p>
                            </div>
                          )}
                        </DroppableColumnBody>
                      </SortableContext>

                      {/* Quick-add card */}
                      {quickAddColumnId === column.id ? (
                        <div className="p-2" onClick={(e) => e.stopPropagation()}>
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
                          className="w-full flex items-center gap-1.5 px-3 py-2 text-xs text-muted-foreground/60 hover:text-muted-foreground hover:bg-muted/30 transition-colors cursor-pointer rounded-b-lg"
                          onClick={() => setQuickAddColumnId(column.id)}
                        >
                          <Plus className="h-3.5 w-3.5" />
                          {t('pm.addCard', { defaultValue: 'Add card' })}
                        </button>
                      )}
                    </div>
                  )}
                </SortableColumnWrapper>
              )
            })}

            {/* Inline add column */}
            {isAddingColumn ? (
              <div className="min-w-[240px] max-w-[280px] flex-shrink-0 bg-muted/30 rounded-lg border border-border/50 p-3 space-y-2">
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
                className="min-w-[200px] flex-shrink-0 h-12 flex items-center justify-center gap-2 rounded-lg border-2 border-dashed border-border/40 text-muted-foreground hover:border-primary/50 hover:text-primary hover:bg-primary/5 transition-all cursor-pointer text-sm"
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
            <div className="min-w-[280px] max-w-[320px] bg-muted/30 rounded-lg border border-primary/40 shadow-2xl rotate-1 overflow-hidden">
              <div className="flex items-center gap-2 px-3 py-2.5 border-b border-border/30 bg-muted/50">
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

      {/* Trello-style centered task modal */}
      <TaskDetailModal
        taskId={selectedTaskId}
        open={taskModalOpen}
        onOpenChange={(open) => { if (!open) setSelectedTaskId(null) }}
        projectMembers={members}
        onNavigateToTask={(taskId) => setSelectedTaskId(taskId)}
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
