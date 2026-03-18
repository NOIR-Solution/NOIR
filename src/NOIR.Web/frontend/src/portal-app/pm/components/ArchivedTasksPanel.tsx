import { useState, useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import {
  AlertTriangle,
  ArrowDown,
  ArrowUp,
  Archive,
  ChevronDown,
  ChevronUp,
  ChevronsUpDown,
  Loader2,
  Minus,
  RotateCcw,
  Search,
  Trash2,
} from 'lucide-react'
import { toast } from 'sonner'
import {
  Badge,
  Button,
  EmptyState,
  Skeleton,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@uikit'
import { getStatusBadgeClasses } from '@/utils/statusBadge'
import { useRegionalSettings } from '@/contexts/RegionalSettingsContext'
import {
  useArchivedTasksQuery,
  useRestoreTask,
  usePermanentDeleteTask,
  useEmptyProjectTrash,
} from '@/portal-app/pm/queries'
import type { ArchivedTaskCardDto, ProjectTaskStatus, TaskPriority } from '@/types/pm'
import { TaskSearchInput } from './TaskSearchInput'

// ─── Constants ────────────────────────────────────────────────────────────────

const statusColorMap: Record<ProjectTaskStatus, 'gray' | 'blue' | 'purple' | 'green' | 'red'> = {
  Todo: 'gray',
  InProgress: 'blue',
  InReview: 'purple',
  Done: 'green',
  Cancelled: 'gray',
}

const STATUS_I18N: Record<string, string> = {
  Todo: 'todo',
  InProgress: 'inProgress',
  InReview: 'inReview',
  Done: 'done',
  Cancelled: 'cancelled',
}

const PRIORITY_ORDER: Record<TaskPriority, number> = {
  Urgent: 4,
  High: 3,
  Medium: 2,
  Low: 1,
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

type SortField = 'taskNumber' | 'title' | 'status' | 'priority' | 'assignee' | 'dueDate' | 'archivedAt'

// ─── SortableHead ─────────────────────────────────────────────────────────────

interface SortableHeadProps {
  field: SortField
  sortField: SortField
  sortDir: 'asc' | 'desc'
  onSort: (field: SortField) => void
  children: React.ReactNode
  className?: string
}

const SortableHead = ({ field, sortField, sortDir, onSort, children, className }: SortableHeadProps) => {
  const isActive = sortField === field
  return (
    <TableHead
      className={`cursor-pointer select-none hover:bg-muted/50 transition-colors ${className ?? ''}`}
      onClick={() => onSort(field)}
    >
      <div className="flex items-center gap-1">
        {children}
        <span className="text-muted-foreground">
          {isActive ? (
            sortDir === 'asc' ? <ChevronUp className="h-3.5 w-3.5" /> : <ChevronDown className="h-3.5 w-3.5" />
          ) : (
            <ChevronsUpDown className="h-3.5 w-3.5 opacity-40" />
          )}
        </span>
      </div>
    </TableHead>
  )
}

// ─── ArchivedTaskRow ───────────────────────────────────────────────────────────

interface ArchivedTaskRowProps {
  task: ArchivedTaskCardDto
  onViewDetail: (id: string) => void
  onRestore: (task: ArchivedTaskCardDto) => void
  onDelete: (task: ArchivedTaskCardDto) => void
  isRestoring: boolean
  isDeleting: boolean
  formatDate: (date: string) => string
  t: ReturnType<typeof useTranslation>['t']
}

const ArchivedTaskRow = ({
  task, onViewDetail, onRestore, onDelete,
  isRestoring, isDeleting, formatDate, t,
}: ArchivedTaskRowProps) => {
  const PriorityIcon = PRIORITY_ICONS[task.priority]

  const dueDateNode = task.dueDate ? (
    <span className="text-sm text-muted-foreground/70 line-through">
      {formatDate(task.dueDate)}
    </span>
  ) : (
    <span className="text-sm text-muted-foreground">-</span>
  )

  return (
    <TableRow
      className="group cursor-pointer opacity-75 hover:opacity-100 transition-opacity"
      onClick={() => onViewDetail(task.id)}
    >
      {/* # */}
      <TableCell className="font-mono text-xs text-muted-foreground">
        {task.taskNumber}
      </TableCell>

      {/* Title */}
      <TableCell>
        <span className="text-sm font-medium line-through text-muted-foreground/70">{task.title}</span>
      </TableCell>

      {/* Status */}
      <TableCell>
        <Badge variant="outline" className={getStatusBadgeClasses(statusColorMap[task.status])}>
          {t(`statuses.${STATUS_I18N[task.status]}`, { defaultValue: task.status })}
        </Badge>
      </TableCell>

      {/* Priority */}
      <TableCell>
        <span className={`inline-flex items-center gap-1 rounded-full px-1.5 py-0.5 text-[10px] font-medium leading-[1.1] border ${PRIORITY_CLASSES[task.priority]}`}>
          <PriorityIcon className="h-2.5 w-2.5" />
          {t(`priorities.${task.priority.toLowerCase()}`, { defaultValue: task.priority })}
        </span>
      </TableCell>

      {/* Assignee */}
      <TableCell>
        {task.assigneeName ? (
          <div className="flex items-center gap-1.5">
            <div className="h-5 w-5 rounded-full bg-primary/10 flex items-center justify-center text-[9px] font-medium text-primary flex-shrink-0">
              {task.assigneeName.charAt(0).toUpperCase()}
            </div>
            <span className="text-sm text-muted-foreground truncate">{task.assigneeName}</span>
          </div>
        ) : (
          <span className="text-sm text-muted-foreground">-</span>
        )}
      </TableCell>

      {/* Due date */}
      <TableCell>{dueDateNode}</TableCell>

      {/* Labels */}
      <TableCell>
        <div className="flex flex-wrap gap-1">
          {task.labels.map((label) => (
            <span
              key={label.id}
              className="inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium leading-[1.1] border opacity-70"
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

      {/* Archived on */}
      <TableCell className="text-xs text-muted-foreground whitespace-nowrap">
        {task.archivedAt ? formatDate(task.archivedAt) : '-'}
      </TableCell>

      {/* Actions */}
      <TableCell onClick={(e) => e.stopPropagation()}>
        <div className="flex items-center gap-1.5 opacity-0 group-hover:opacity-100 transition-opacity">
          <Button
            size="sm"
            variant="outline"
            className="text-xs cursor-pointer gap-1 hover:border-green-500/40 hover:text-green-600 hover:bg-green-500/5 transition-colors"
            onClick={() => onRestore(task)}
            disabled={isRestoring}
            aria-label={t('pm.restoreTask', { defaultValue: 'Restore task' })}
          >
            {isRestoring ? <Loader2 className="h-3 w-3 animate-spin" /> : <RotateCcw className="h-3 w-3" />}
            {t('pm.restore', { defaultValue: 'Restore' })}
          </Button>
          <Button
            size="sm"
            variant="outline"
            className="text-xs cursor-pointer hover:border-destructive/40 hover:text-destructive hover:bg-destructive/5 transition-colors"
            onClick={() => onDelete(task)}
            disabled={isDeleting}
            aria-label={t('pm.permanentDelete', { defaultValue: 'Delete permanently' })}
          >
            {isDeleting ? <Loader2 className="h-3 w-3 animate-spin" /> : <Trash2 className="h-3 w-3" />}
          </Button>
        </div>
      </TableCell>
    </TableRow>
  )
}

// ─── Main component ────────────────────────────────────────────────────────────

interface ArchivedTasksPanelProps {
  projectId: string
  onViewDetail: (taskId: string) => void
}

export const ArchivedTasksPanel = ({ projectId, onViewDetail }: ArchivedTasksPanelProps) => {
  const { t } = useTranslation('common')
  const { formatDate } = useRegionalSettings()

  const [emptyConfirm, setEmptyConfirm] = useState(false)
  const [search, setSearch] = useState('')
  const [sortField, setSortField] = useState<SortField>('archivedAt')
  const [sortDir, setSortDir] = useState<'asc' | 'desc'>('desc')

  const { data: archivedTasks = [], isLoading } = useArchivedTasksQuery(projectId)
  const restoreMutation = useRestoreTask()
  const permanentDeleteMutation = usePermanentDeleteTask()
  const emptyTrashMutation = useEmptyProjectTrash()

  const handleSort = (field: SortField) => {
    if (sortField === field) setSortDir((d) => (d === 'asc' ? 'desc' : 'asc'))
    else { setSortField(field); setSortDir('asc') }
  }

  const handleRestore = (task: ArchivedTaskCardDto) => {
    restoreMutation.mutate(task.id, {
      onError: (err) => toast.error(err instanceof Error ? err.message : t('errors.unknown')),
    })
  }

  const handlePermanentDelete = (task: ArchivedTaskCardDto) => {
    permanentDeleteMutation.mutate(task.id, {
      onError: (err) => toast.error(err instanceof Error ? err.message : t('errors.unknown')),
    })
  }

  const handleEmptyTrash = () => {
    emptyTrashMutation.mutate(projectId, {
      onSuccess: () => {
        setEmptyConfirm(false)
      },
      onError: (err) => toast.error(err instanceof Error ? err.message : t('errors.unknown')),
    })
  }

  const filtered = useMemo(() => {
    const q = search.toLowerCase()
    return archivedTasks.filter(
      (t) => !q || t.title.toLowerCase().includes(q) || t.taskNumber.toLowerCase().includes(q),
    )
  }, [archivedTasks, search])

  const sorted = useMemo(() => {
    const items = [...filtered]
    const dir = sortDir === 'asc' ? 1 : -1
    switch (sortField) {
      case 'title': return items.sort((a, b) => a.title.localeCompare(b.title) * dir)
      case 'status': return items.sort((a, b) => a.status.localeCompare(b.status) * dir)
      case 'priority': return items.sort((a, b) => (PRIORITY_ORDER[a.priority] - PRIORITY_ORDER[b.priority]) * dir)
      case 'assignee': return items.sort((a, b) => (a.assigneeName ?? '').localeCompare(b.assigneeName ?? '') * dir)
      case 'dueDate': return items.sort((a, b) => {
        if (!a.dueDate) return 1 * dir
        if (!b.dueDate) return -1 * dir
        return (new Date(a.dueDate).getTime() - new Date(b.dueDate).getTime()) * dir
      })
      case 'archivedAt': return items.sort((a, b) => {
        if (!a.archivedAt) return 1 * dir
        if (!b.archivedAt) return -1 * dir
        return (new Date(a.archivedAt).getTime() - new Date(b.archivedAt).getTime()) * dir
      })
      default: return items.sort((a, b) => a.taskNumber.localeCompare(b.taskNumber) * dir)
    }
  }, [filtered, sortField, sortDir])

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
    <div className="space-y-4">
      {/* Toolbar */}
      <div className="flex flex-wrap items-center gap-3">
        {/* Search */}
        <TaskSearchInput value={search} onChange={setSearch} />

        {/* Task count */}
        <span className="text-xs text-muted-foreground">
          {filtered.length}
          {search && archivedTasks.length !== filtered.length && ` / ${archivedTasks.length}`}
          {' '}{t('pm.archivedTasks', { defaultValue: 'archived' }).toLowerCase()}
        </span>

        {/* Empty bin */}
        {archivedTasks.length > 0 && (
          <div className="flex items-center gap-2 ml-auto">
            {emptyConfirm ? (
              <>
                <span className="text-xs text-destructive font-medium">
                  {t('pm.confirmEmptyTrash', { defaultValue: 'Delete all permanently?' })}
                </span>
                <Button
                  size="sm"
                  variant="destructive"
                  className="text-xs cursor-pointer bg-destructive/10 text-destructive border border-destructive/30 hover:bg-destructive hover:text-destructive-foreground transition-colors"
                  onClick={handleEmptyTrash}
                  disabled={emptyTrashMutation.isPending}
                >
                  {emptyTrashMutation.isPending
                    ? <Loader2 className="h-3 w-3 animate-spin" />
                    : t('pm.emptyTrash', { defaultValue: 'Empty bin' })}
                </Button>
                <Button
                  size="sm"
                  variant="outline"
                  className="text-xs cursor-pointer"
                  onClick={() => setEmptyConfirm(false)}
                  disabled={emptyTrashMutation.isPending}
                >
                  {t('buttons.cancel', { defaultValue: 'Cancel' })}
                </Button>
              </>
            ) : (
              <Button
                size="sm"
                variant="outline"
                className="text-xs cursor-pointer text-destructive hover:text-destructive hover:border-destructive/40 hover:bg-destructive/5 transition-colors"
                onClick={() => setEmptyConfirm(true)}
              >
                <Trash2 className="h-3 w-3 mr-1" />
                {t('pm.emptyTrash', { defaultValue: 'Empty bin' })}
              </Button>
            )}
          </div>
        )}
      </div>

      {/* Warning banner */}
      {archivedTasks.length > 0 && (
        <div className="flex items-start gap-2 rounded-lg border border-amber-200 bg-amber-50 px-3 py-2 dark:border-amber-800/50 dark:bg-amber-950/20">
          <AlertTriangle className="h-3.5 w-3.5 text-amber-600 dark:text-amber-400 flex-shrink-0 mt-0.5" />
          <p className="text-xs text-amber-700 dark:text-amber-300">
            {t('pm.archivedTasksWarning', { defaultValue: 'Archived tasks are hidden from the board. Restore them to make them active again, or permanently delete them.' })}
          </p>
        </div>
      )}

      {/* Empty state */}
      {archivedTasks.length === 0 && (
        <EmptyState
          icon={Archive}
          title={t('pm.noArchivedTasks', { defaultValue: 'No archived tasks' })}
          description={t('pm.noArchivedTasksHint', { defaultValue: 'Tasks you archive will appear here' })}
        />
      )}

      {archivedTasks.length > 0 && filtered.length === 0 && (
        <EmptyState
          icon={Search}
          title={t('pm.noTasksMatchFilter', { defaultValue: 'No matching tasks' })}
          description={t('pm.tryAdjustingFilters', { defaultValue: 'Try adjusting your filters' })}
          size="sm"
        />
      )}

      {/* Table */}
      {sorted.length > 0 && (
        <div className="rounded-md border">
          <Table>
            <TableHeader>
              <TableRow>
                <SortableHead field="taskNumber" sortField={sortField} sortDir={sortDir} onSort={handleSort}>
                  {t('pm.taskNumber')}
                </SortableHead>
                <SortableHead field="title" sortField={sortField} sortDir={sortDir} onSort={handleSort}>
                  {t('pm.taskTitle')}
                </SortableHead>
                <SortableHead field="status" sortField={sortField} sortDir={sortDir} onSort={handleSort}>
                  {t('pm.status')}
                </SortableHead>
                <SortableHead field="priority" sortField={sortField} sortDir={sortDir} onSort={handleSort}>
                  {t('pm.priority')}
                </SortableHead>
                <SortableHead field="assignee" sortField={sortField} sortDir={sortDir} onSort={handleSort}>
                  {t('pm.assignee')}
                </SortableHead>
                <SortableHead field="dueDate" sortField={sortField} sortDir={sortDir} onSort={handleSort}>
                  {t('pm.dueDate')}
                </SortableHead>
                <TableHead>{t('pm.labels')}</TableHead>
                <SortableHead field="archivedAt" sortField={sortField} sortDir={sortDir} onSort={handleSort} className="whitespace-nowrap">
                  {t('pm.archivedOn', { defaultValue: 'Archived On' })}
                </SortableHead>
                <TableHead className="w-[140px]" />
              </TableRow>
            </TableHeader>
            <TableBody>
              {sorted.map((task) => (
                <ArchivedTaskRow
                  key={task.id}
                  task={task}
                  onViewDetail={onViewDetail}
                  onRestore={handleRestore}
                  onDelete={handlePermanentDelete}
                  isRestoring={restoreMutation.isPending && restoreMutation.variables === task.id}
                  isDeleting={permanentDeleteMutation.isPending && permanentDeleteMutation.variables === task.id}
                  formatDate={formatDate}
                  t={t}
                />
              ))}
            </TableBody>
          </Table>
        </div>
      )}
    </div>
  )
}
