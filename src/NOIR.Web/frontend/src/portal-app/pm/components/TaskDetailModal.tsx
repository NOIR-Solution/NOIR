import { useState, useEffect, useRef, useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import { toast } from 'sonner'
import {
  X,
  Calendar,
  Clock,
  MessageSquare,
  Send,
  ChevronRight,
  Trash2,
  UserCheck,
  Loader2,
  CheckSquare,
  Plus,
  Tag,
  Pencil,
  ExternalLink,
  ArrowDown,
  Minus,
  ArrowUp,
  AlertTriangle,
  User,
  Check,
  Search,
  ChevronDown,
  Users,
  Layers,
  CornerDownRight,
} from 'lucide-react'
import {
  Avatar,
  Badge,
  Button,
  Calendar as CalendarPicker,
  Dialog,
  DialogContent,
  DialogClose,
  DialogTitle,
  Popover,
  PopoverContent,
  PopoverTrigger,
  ScrollArea,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Skeleton,
  Textarea,
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from '@uikit'
import { getStatusBadgeClasses } from '@/utils/statusBadge'
import { useRegionalSettings } from '@/contexts/RegionalSettingsContext'
import { ViewTransitionLink } from '@/components/navigation/ViewTransitionLink'
import {
  useTaskQuery,
  useUpdateTask,
  useChangeTaskStatus,
  useAddComment,
  useDeleteComment,
  useArchiveTask,
  useCreateTask,
  useProjectLabelsQuery,
  useAddLabelToTask,
  useRemoveLabelFromTask,
  useCreateLabel,
} from '@/portal-app/pm/queries'
import type { ProjectTaskStatus, TaskPriority, ProjectMemberDto } from '@/types/pm'

// ─── Status mapping ──────────────────────────────────────────────────────────

const statusColorMap: Record<ProjectTaskStatus, 'gray' | 'blue' | 'purple' | 'green' | 'red'> = {
  Todo: 'gray',
  InProgress: 'blue',
  InReview: 'purple',
  Done: 'green',
  Cancelled: 'red',
}

// ─── Priority config ─────────────────────────────────────────────────────────

const priorityConfig: Record<TaskPriority, { icon: React.ElementType; class: string }> = {
  Low: { icon: ArrowDown, class: 'text-slate-500' },
  Medium: { icon: Minus, class: 'text-blue-500' },
  High: { icon: ArrowUp, class: 'text-orange-500' },
  Urgent: { icon: AlertTriangle, class: 'text-red-500' },
}

// Trello-inspired label colors for new label creation
const LABEL_COLORS = [
  '#61bd4f', '#f2d600', '#ff9f1a', '#eb5a46', '#c377e0',
  '#0079bf', '#00c2e0', '#51e898', '#ff78cb', '#344563',
]

// ─── LabelPickerPopover ───────────────────────────────────────────────────────

interface LabelPickerPopoverProps {
  activeIds: string[]
  projectLabels: { id: string; name: string; color: string }[]
  projectId: string
  taskId: string
  onToggle: (labelId: string) => void
}

const LabelPickerPopover = ({ activeIds, projectLabels, projectId, taskId: _taskId, onToggle }: LabelPickerPopoverProps) => {
  const { t } = useTranslation('common')
  const [search, setSearch] = useState('')
  const [creating, setCreating] = useState(false)
  const [newName, setNewName] = useState('')
  const [newColor, setNewColor] = useState(LABEL_COLORS[0])
  const createLabelMutation = useCreateLabel()

  const filtered = useMemo(
    () => projectLabels.filter(l => l.name.toLowerCase().includes(search.toLowerCase())),
    [projectLabels, search],
  )

  const handleCreate = () => {
    if (!newName.trim()) return
    createLabelMutation.mutate(
      { projectId, request: { name: newName.trim(), color: newColor } },
      {
        onSuccess: () => { setCreating(false); setNewName(''); setNewColor(LABEL_COLORS[0]) },
        onError: (err) => toast.error(err instanceof Error ? err.message : t('errors.unknown')),
      },
    )
  }

  return (
    <div className="w-60 p-2 space-y-1">
      <p className="text-xs font-semibold text-muted-foreground px-1 pb-1 border-b border-border/40">
        {t('pm.labels', { defaultValue: 'Labels' })}
      </p>

      {/* Search */}
      <div className="relative">
        <Search className="absolute left-2 top-1/2 -translate-y-1/2 h-3 w-3 text-muted-foreground" />
        <input
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder={t('pm.searchLabels', { defaultValue: 'Search labels...' })}
          className="w-full pl-6 pr-2 py-1.5 text-xs bg-muted/50 border border-input rounded-md focus:outline-none focus:ring-1 focus:ring-ring"
        />
      </div>

      {/* Label list */}
      <ScrollArea className="max-h-48">
        <div className="space-y-0.5">
          {filtered.map(label => {
            const isActive = activeIds.includes(label.id)
            return (
              <button
                key={label.id}
                onClick={() => onToggle(label.id)}
                className="w-full flex items-center gap-2.5 px-2 py-1.5 rounded-md hover:bg-muted/60 cursor-pointer transition-colors"
              >
                {/* Color chip */}
                <span
                  className="flex-1 h-7 rounded-md flex items-center px-3 text-white text-xs font-medium"
                  style={{ backgroundColor: label.color }}
                >
                  {label.name}
                </span>
                {/* Check indicator */}
                <span className={`h-4 w-4 rounded border-2 flex items-center justify-center flex-shrink-0 transition-colors ${
                  isActive ? 'bg-primary border-primary' : 'border-border'
                }`}>
                  {isActive && <Check className="h-2.5 w-2.5 text-primary-foreground" />}
                </span>
              </button>
            )
          })}
          {filtered.length === 0 && (
            <p className="text-xs text-muted-foreground text-center py-3">
              {t('pm.noLabelsFound', { defaultValue: 'No labels found' })}
            </p>
          )}
        </div>
      </ScrollArea>

      {/* Create label */}
      <div className="border-t border-border/40 pt-1">
        {!creating ? (
          <button
            onClick={() => setCreating(true)}
            className="w-full flex items-center gap-2 px-2 py-1.5 text-xs text-muted-foreground hover:text-foreground hover:bg-muted/50 rounded-md cursor-pointer transition-colors"
          >
            <Plus className="h-3 w-3" />
            {t('pm.createLabel', { defaultValue: 'Create a new label' })}
          </button>
        ) : (
          <div className="space-y-2 px-1">
            <input
              autoFocus
              value={newName}
              onChange={(e) => setNewName(e.target.value)}
              onKeyDown={(e) => { if (e.key === 'Enter') handleCreate(); if (e.key === 'Escape') setCreating(false) }}
              placeholder={t('pm.labelName', { defaultValue: 'Label name...' })}
              className="w-full text-xs bg-background border border-input rounded px-2 py-1.5 focus:outline-none focus:ring-1 focus:ring-ring"
            />
            <div className="flex flex-wrap gap-1.5">
              {LABEL_COLORS.map(c => (
                <button
                  key={c}
                  onClick={() => setNewColor(c)}
                  className={`h-6 w-8 rounded transition-transform cursor-pointer ${newColor === c ? 'ring-2 ring-offset-1 ring-foreground scale-110' : 'hover:scale-105'}`}
                  style={{ backgroundColor: c }}
                  aria-label={c}
                />
              ))}
            </div>
            <div className="flex gap-1.5">
              <Button
                size="sm"
                className="flex-1 h-7 text-xs cursor-pointer"
                disabled={createLabelMutation.isPending || !newName.trim()}
                onClick={handleCreate}
              >
                {createLabelMutation.isPending ? <Loader2 className="h-3 w-3 animate-spin" /> : t('buttons.create', { defaultValue: 'Create' })}
              </Button>
              <Button variant="ghost" size="sm" className="h-7 text-xs cursor-pointer" onClick={() => setCreating(false)}>
                {t('buttons.cancel')}
              </Button>
            </div>
          </div>
        )}
      </div>
    </div>
  )
}

// ─── DueDateButton ────────────────────────────────────────────────────────────

interface DueDateButtonProps {
  dueDate: string | null
  taskStatus: ProjectTaskStatus
  onChange: (date: string | undefined) => void
}

const DueDateButton = ({ dueDate, taskStatus, onChange }: DueDateButtonProps) => {
  const { t } = useTranslation('common')
  const { formatDate } = useRegionalSettings()
  const [open, setOpen] = useState(false)

  const isComplete = taskStatus === 'Done' || taskStatus === 'Cancelled'
  const diffDays = dueDate ? Math.ceil((new Date(dueDate).getTime() - Date.now()) / 86400000) : null
  const isOverdue = !isComplete && diffDays !== null && diffDays < 0
  const isDueToday = !isComplete && diffDays === 0
  const isDueSoon = !isComplete && diffDays !== null && diffDays > 0 && diffDays <= 2

  const chipClass = isComplete
    ? 'bg-green-100 text-green-700 border-green-200 dark:bg-green-950/50 dark:text-green-300 dark:border-green-800'
    : isOverdue
      ? 'bg-red-100 text-red-600 border-red-200 dark:bg-red-950/50 dark:text-red-300 dark:border-red-800'
      : isDueToday
        ? 'bg-amber-100 text-amber-600 border-amber-200 dark:bg-amber-950/50 dark:text-amber-300 dark:border-amber-800'
        : isDueSoon
          ? 'bg-orange-50 text-orange-500 border-orange-200 dark:bg-orange-950/30 dark:text-orange-300 dark:border-orange-800'
          : 'bg-muted text-muted-foreground border-border hover:bg-muted/80'

  return (
    <div className="flex gap-1.5">
      <Popover open={open} onOpenChange={setOpen}>
        <PopoverTrigger asChild>
          <button
            className={`flex-1 flex items-center gap-1.5 rounded-md px-2.5 h-8 text-xs font-medium border cursor-pointer transition-colors ${
              dueDate ? chipClass : 'bg-muted/50 text-muted-foreground border-border hover:bg-muted'
            }`}
            aria-label={t('pm.dueDate', { defaultValue: 'Due date' })}
          >
            <Calendar className="h-3.5 w-3.5 flex-shrink-0" />
            {dueDate ? (
              <span className="flex-1 text-left">{formatDate(dueDate)}</span>
            ) : (
              <span className="flex-1 text-left">{t('pm.addDueDate', { defaultValue: 'Add due date' })}</span>
            )}
            {isOverdue && <span className="font-semibold">{t('pm.overdue', { defaultValue: 'Overdue' })}</span>}
            {isDueToday && <span className="font-semibold">{t('pm.dueToday', { defaultValue: 'Today' })}</span>}
          </button>
        </PopoverTrigger>
        <PopoverContent className="w-auto p-0" align="start">
          <CalendarPicker
            mode="single"
            selected={dueDate ? new Date(dueDate) : undefined}
            onSelect={(date) => {
              if (date) {
                const yyyy = date.getFullYear()
                const mm = String(date.getMonth() + 1).padStart(2, '0')
                const dd = String(date.getDate()).padStart(2, '0')
                onChange(`${yyyy}-${mm}-${dd}`)
              }
              setOpen(false)
            }}
            initialFocus
          />
        </PopoverContent>
      </Popover>
      {dueDate && (
        <button
          onClick={() => onChange(undefined)}
          className="h-8 w-8 flex-shrink-0 flex items-center justify-center rounded-md border border-border hover:bg-destructive/10 hover:text-destructive hover:border-destructive/30 transition-colors cursor-pointer text-muted-foreground"
          aria-label={t('buttons.remove', { defaultValue: 'Remove due date' })}
        >
          <X className="h-3 w-3" />
        </button>
      )}
    </div>
  )
}

// ─── MemberPickerPopover ──────────────────────────────────────────────────────

interface MemberPickerProps {
  assigneeId: string | null
  assigneeName: string | null
  members: ProjectMemberDto[]
  onAssign: (employeeId: string | null) => void
}

const MemberPickerPopover = ({ assigneeId, assigneeName, members, onAssign }: MemberPickerProps) => {
  const { t } = useTranslation('common')
  const [open, setOpen] = useState(false)
  const [search, setSearch] = useState('')

  const filtered = members.filter(m => m.employeeName.toLowerCase().includes(search.toLowerCase()))

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <button className="flex items-center gap-2 w-full hover:bg-muted/50 rounded-md px-2 py-1.5 transition-colors cursor-pointer group">
          {assigneeName ? (
            <>
              <Avatar
                src={undefined}
                alt={assigneeName}
                fallback={assigneeName}
                size="sm"
                className="h-6 w-6 text-[10px] flex-shrink-0"
              />
              <span className="text-xs text-foreground flex-1 text-left truncate">{assigneeName}</span>
            </>
          ) : (
            <>
              <div className="h-6 w-6 rounded-full border-2 border-dashed border-border flex items-center justify-center flex-shrink-0">
                <User className="h-3 w-3 text-muted-foreground" />
              </div>
              <span className="text-xs text-muted-foreground flex-1 text-left">
                {t('pm.unassigned', { defaultValue: 'Unassigned' })}
              </span>
            </>
          )}
          <ChevronDown className="h-3 w-3 text-muted-foreground opacity-0 group-hover:opacity-100 transition-opacity flex-shrink-0" />
        </button>
      </PopoverTrigger>
      <PopoverContent className="w-56 p-2 space-y-1" align="start">
        <p className="text-xs font-semibold text-muted-foreground px-1 pb-1 border-b border-border/40">
          {t('pm.assignee', { defaultValue: 'Assignee' })}
        </p>
        {members.length > 4 && (
          <div className="relative">
            <Search className="absolute left-2 top-1/2 -translate-y-1/2 h-3 w-3 text-muted-foreground" />
            <input
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder={t('pm.searchMembers', { defaultValue: 'Search members...' })}
              className="w-full pl-6 pr-2 py-1.5 text-xs bg-muted/50 border border-input rounded-md focus:outline-none focus:ring-1 focus:ring-ring"
            />
          </div>
        )}
        <ScrollArea className="max-h-48">
          <div className="space-y-0.5">
            <button
              onClick={() => { onAssign(null); setOpen(false) }}
              className="w-full flex items-center gap-2.5 px-2 py-1.5 rounded-md hover:bg-muted/60 cursor-pointer transition-colors"
            >
              <div className="h-6 w-6 rounded-full border-2 border-dashed border-border flex items-center justify-center flex-shrink-0">
                <User className="h-3 w-3 text-muted-foreground" />
              </div>
              <span className="text-xs text-muted-foreground flex-1 text-left">{t('pm.unassigned', { defaultValue: 'Unassigned' })}</span>
              {!assigneeId && <Check className="h-3.5 w-3.5 text-primary flex-shrink-0" />}
            </button>
            {filtered.map(member => (
              <button
                key={member.employeeId}
                onClick={() => { onAssign(member.employeeId); setOpen(false) }}
                className="w-full flex items-center gap-2.5 px-2 py-1.5 rounded-md hover:bg-muted/60 cursor-pointer transition-colors"
              >
                <Avatar
                  src={member.avatarUrl ?? undefined}
                  alt={member.employeeName}
                  fallback={member.employeeName}
                  size="sm"
                  className="h-6 w-6 text-[10px] flex-shrink-0"
                />
                <span className="text-xs flex-1 text-left truncate">{member.employeeName}</span>
                {assigneeId === member.employeeId && <Check className="h-3.5 w-3.5 text-primary flex-shrink-0" />}
              </button>
            ))}
          </div>
        </ScrollArea>
      </PopoverContent>
    </Popover>
  )
}

// ─── Component props ─────────────────────────────────────────────────────────

interface TaskDetailModalProps {
  taskId: string | null
  open: boolean
  onOpenChange: (open: boolean) => void
  projectMembers?: ProjectMemberDto[]
  onNavigateToTask?: (taskNumber: string) => void
}

// ─── Sidebar section header ───────────────────────────────────────────────────


// ─── Status dot colors ────────────────────────────────────────────────────────
const statusDot: Record<string, string> = {
  Todo: 'bg-slate-400',
  InProgress: 'bg-blue-500',
  InReview: 'bg-violet-500',
  Done: 'bg-green-500',
  Cancelled: 'bg-red-500',
}

// ─── Avatar color palette (hashed from name) ─────────────────────────────────
const AVATAR_COLORS = [
  'bg-red-100 text-red-700 dark:bg-red-900/50 dark:text-red-300',
  'bg-orange-100 text-orange-700 dark:bg-orange-900/50 dark:text-orange-300',
  'bg-amber-100 text-amber-700 dark:bg-amber-900/50 dark:text-amber-300',
  'bg-green-100 text-green-700 dark:bg-green-900/50 dark:text-green-300',
  'bg-blue-100 text-blue-700 dark:bg-blue-900/50 dark:text-blue-300',
  'bg-violet-100 text-violet-700 dark:bg-violet-900/50 dark:text-violet-300',
  'bg-pink-100 text-pink-700 dark:bg-pink-900/50 dark:text-pink-300',
  'bg-cyan-100 text-cyan-700 dark:bg-cyan-900/50 dark:text-cyan-300',
]
const getAvatarColorClass = (name: string) =>
  AVATAR_COLORS[name.split('').reduce((acc, c) => acc + c.charCodeAt(0), 0) % AVATAR_COLORS.length]

const SidebarSection = ({ icon: Icon, label, children }: { icon: React.ElementType; label: string; children: React.ReactNode }) => (
  <div className="space-y-1.5">
    <div className="flex items-center gap-1.5">
      <Icon className="h-3 w-3 text-muted-foreground/60" />
      <span className="text-[10px] font-bold text-muted-foreground/70 uppercase tracking-widest">{label}</span>
    </div>
    {children}
  </div>
)

// ─── TaskDetailModal ─────────────────────────────────────────────────────────

export const TaskDetailModal = ({ taskId, open, onOpenChange, projectMembers, onNavigateToTask }: TaskDetailModalProps) => {
  const { t } = useTranslation('common')
  const navigate = useNavigate()
  const { formatDate, formatRelativeTime } = useRegionalSettings()

  const { data: task, isLoading, refetch } = useTaskQuery(taskId ?? undefined)

  const updateTaskMutation = useUpdateTask()
  const changeStatusMutation = useChangeTaskStatus()
  const addCommentMutation = useAddComment()
  const deleteCommentMutation = useDeleteComment()
  const archiveTaskMutation = useArchiveTask()
  const createTaskMutation = useCreateTask()
  const addLabelMutation = useAddLabelToTask()
  const removeLabelMutation = useRemoveLabelFromTask()

  const { data: projectLabels } = useProjectLabelsQuery(task?.projectId)

  // ── Inline title ──
  const [editingTitle, setEditingTitle] = useState(false)
  const [titleValue, setTitleValue] = useState(task?.title ?? '')
  const titleInputRef = useRef<HTMLInputElement>(null)

  useEffect(() => { if (task?.title) setTitleValue(task.title) }, [task?.title])
  useEffect(() => { if (editingTitle) titleInputRef.current?.focus() }, [editingTitle])

  // ── Description ──
  const [descValue, setDescValue] = useState(task?.description ?? '')
  const descOrigRef = useRef(task?.description ?? '')

  useEffect(() => {
    const v = task?.description ?? ''
    setDescValue(v)
    descOrigRef.current = v
  }, [task?.description])

  // ── Comment ──
  const [commentText, setCommentText] = useState('')

  // ── Subtask add ──
  const [addSubtaskOpen, setAddSubtaskOpen] = useState(false)
  const [subtaskTitle, setSubtaskTitle] = useState('')


  useEffect(() => {
    if (!open) {
      setEditingTitle(false)
      setAddSubtaskOpen(false)
      setSubtaskTitle('')
      setCommentText('')
    }
  }, [open])

  // ── Handlers ──

  const handleTitleSave = () => {
    if (!task || !titleValue.trim()) {
      setEditingTitle(false)
      setTitleValue(task?.title ?? '')
      return
    }
    if (titleValue.trim() === task.title) { setEditingTitle(false); return }
    updateTaskMutation.mutate(
      { id: task.id, request: { title: titleValue.trim() } },
      {
        onSuccess: () => setEditingTitle(false),
        onError: (err) => {
          toast.error(err instanceof Error ? err.message : t('errors.unknown'))
          setTitleValue(task.title)
          setEditingTitle(false)
        },
      },
    )
  }

  const handleDescriptionBlur = () => {
    if (!task || descValue === descOrigRef.current) return
    updateTaskMutation.mutate(
      { id: task.id, request: { description: descValue } },
      {
        onSuccess: () => { descOrigRef.current = descValue },
        onError: (err) => {
          toast.error(err instanceof Error ? err.message : t('errors.unknown'))
          setDescValue(descOrigRef.current)
        },
      },
    )
  }

  const handleStatusChange = (status: string) => {
    if (!task) return
    changeStatusMutation.mutate({ id: task.id, status }, {
      onError: (err) => toast.error(err instanceof Error ? err.message : t('errors.unknown')),
    })
  }

  const handlePriorityChange = (priority: string) => {
    if (!task) return
    updateTaskMutation.mutate({ id: task.id, request: { priority: priority as TaskPriority } }, {
      onError: (err) => toast.error(err instanceof Error ? err.message : t('errors.unknown')),
    })
  }

  const handleAssigneeChange = (employeeId: string | null) => {
    if (!task) return
    updateTaskMutation.mutate({ id: task.id, request: { assigneeId: employeeId ?? '' } }, {
      onError: (err) => toast.error(err instanceof Error ? err.message : t('errors.unknown')),
    })
  }

  const handleDueDateChange = (date: string | undefined) => {
    if (!task) return
    updateTaskMutation.mutate({ id: task.id, request: { dueDate: date } }, {
      onError: (err) => toast.error(err instanceof Error ? err.message : t('errors.unknown')),
    })
  }

  const handleAddComment = () => {
    if (!task || !commentText.trim()) return
    addCommentMutation.mutate(
      { taskId: task.id, request: { content: commentText } },
      {
        onSuccess: () => setCommentText(''),
        onError: (err) => toast.error(err instanceof Error ? err.message : t('errors.unknown')),
      },
    )
  }

  const handleDeleteComment = (commentId: string) => {
    if (!task) return
    deleteCommentMutation.mutate({ taskId: task.id, commentId }, {
      onError: (err) => toast.error(err instanceof Error ? err.message : t('errors.unknown')),
    })
  }

  const handleAddSubtask = () => {
    if (!task || !subtaskTitle.trim()) return
    createTaskMutation.mutate(
      { projectId: task.projectId, title: subtaskTitle.trim(), parentTaskId: task.id, columnId: task.columnId ?? undefined },
      {
        onSuccess: () => {
          setSubtaskTitle('')
          setAddSubtaskOpen(false)
          void refetch()
        },
        onError: (err) => toast.error(err instanceof Error ? err.message : t('errors.unknown')),
      },
    )
  }

  const handleArchiveTask = () => {
    if (!task) return
    archiveTaskMutation.mutate(task.id, {
      onSuccess: () => { toast.success(t('pm.taskArchived', { defaultValue: 'Task archived' })); onOpenChange(false) },
      onError: (err) => toast.error(err instanceof Error ? err.message : t('errors.unknown')),
    })
  }

  const handleToggleLabel = (labelId: string) => {
    if (!task) return
    const hasLabel = task.labels.some((l) => l.id === labelId)
    if (hasLabel) {
      removeLabelMutation.mutate({ taskId: task.id, labelId })
    } else {
      addLabelMutation.mutate({ taskId: task.id, labelId })
    }
  }

  const handleOpenFullPage = () => {
    if (taskId) { onOpenChange(false); navigate(`/portal/tasks/${taskId}`) }
  }

  // ── Render ──

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-4xl w-full p-0 gap-0 overflow-hidden max-h-[90vh] flex flex-col [&>button:last-child]:hidden">

        {/* ── Top bar — Jira-style breadcrumb ── */}
        <div className="flex items-center gap-2 px-4 py-2.5 border-b border-border/60 bg-muted/40 dark:bg-muted/25 flex-shrink-0">
          {/* Breadcrumb */}
          <nav className="flex items-center gap-1 flex-1 min-w-0 text-xs text-muted-foreground overflow-hidden">
            {task?.projectName && (
              <>
                <button
                  onClick={() => { onOpenChange(false); navigate(`/portal/projects/${task.projectCode ?? task.projectId}`) }}
                  className="hover:text-foreground transition-colors cursor-pointer truncate max-w-[120px] flex-shrink-0"
                >
                  {task.projectName}
                </button>
                <ChevronRight className="h-3 w-3 flex-shrink-0 opacity-40" />
              </>
            )}
            {task?.parentTaskNumber && task?.parentTaskId && (
              <>
                <button
                  onClick={() => onNavigateToTask?.(task.parentTaskNumber!)}
                  className="hover:text-foreground transition-colors cursor-pointer font-mono flex-shrink-0"
                >
                  #{task.parentTaskNumber!.split('-').pop()}
                </button>
                <ChevronRight className="h-3 w-3 flex-shrink-0 opacity-40" />
              </>
            )}
            {task && (
              <span className="font-mono text-foreground font-medium flex-shrink-0">#{task.taskNumber.split('-').pop()}</span>
            )}
            {task && task.subtasks.length > 0 && !task.parentTaskId && (
              <span className="inline-flex items-center gap-0.5 rounded-full px-1.5 py-0.5 text-[10px] font-medium leading-[1.1] border bg-sky-50 text-sky-600 border-sky-200 dark:bg-sky-950/40 dark:text-sky-300 dark:border-sky-800 flex-shrink-0">
                <Layers className="h-2.5 w-2.5" />
                {t('pm.parentTask', { defaultValue: 'Parent' })}
              </span>
            )}
            {task && task.parentTaskId && (
              <span className="inline-flex items-center gap-0.5 rounded-full px-1.5 py-0.5 text-[10px] font-medium leading-[1.1] border bg-violet-50 text-violet-600 border-violet-200 dark:bg-violet-950/40 dark:text-violet-300 dark:border-violet-800 flex-shrink-0">
                <CornerDownRight className="h-2.5 w-2.5" />
                {t('pm.subtask', { defaultValue: 'Subtask' })}
              </span>
            )}
            {task && (
              <Badge
                variant="outline"
                className={`text-xs flex-shrink-0 ml-1.5 ${getStatusBadgeClasses(statusColorMap[task.status])}`}
              >
                {t(`statuses.${task.status.toLowerCase()}`, { defaultValue: task.status })}
              </Badge>
            )}
          </nav>
          <div className="flex-shrink-0" />
          <Button
            variant="ghost" size="icon"
            className="h-7 w-7 cursor-pointer text-muted-foreground hover:text-foreground flex-shrink-0"
            onClick={handleOpenFullPage}
            aria-label={t('pm.openFullPage', { defaultValue: 'Open full page' })}
          >
            <ExternalLink className="h-4 w-4" />
          </Button>
          <DialogClose asChild>
            <Button
              variant="ghost" size="icon"
              className="h-7 w-7 cursor-pointer text-muted-foreground hover:text-foreground flex-shrink-0"
              aria-label={t('buttons.close', { defaultValue: 'Close' })}
            >
              <X className="h-4 w-4" />
            </Button>
          </DialogClose>
        </div>


        <DialogTitle className="sr-only">
          {task?.title ?? t('pm.taskDetails', { defaultValue: 'Task Details' })}
        </DialogTitle>

        {/* ── Loading ── */}
        {isLoading && (
          <div className="flex-1 overflow-y-auto p-5">
            <div className="grid grid-cols-1 md:grid-cols-[1fr_220px] h-full gap-0">
              <div className="p-5 space-y-5 border-r border-border/30">
                <div className="flex gap-2">
                  <Skeleton className="h-5 w-16 rounded-full" />
                  <Skeleton className="h-5 w-20 rounded-full" />
                </div>
                <Skeleton className="h-7 w-3/4" />
                <Skeleton className="h-24 w-full" />
                <Skeleton className="h-4 w-1/3" />
                <div className="space-y-2">
                  <Skeleton className="h-10 w-full" />
                  <Skeleton className="h-10 w-full" />
                </div>
              </div>
              <div className="p-4 space-y-5">
                {[...Array(6)].map((_, i) => (
                  <div key={i} className="space-y-1.5">
                    <Skeleton className="h-3 w-16" />
                    <Skeleton className="h-8 w-full" />
                  </div>
                ))}
              </div>
            </div>
          </div>
        )}

        {/* ── Body ── */}
        {!isLoading && task && (
          <div className="flex-1 overflow-y-auto min-h-0">
            <div className="grid grid-cols-1 md:grid-cols-[1fr_220px] h-full">

              {/* ── Left panel ── */}
              <div className="p-5 space-y-5 overflow-y-auto border-r border-border/30 min-h-0">

                {/* Active labels — shown above title like Trello */}
                {task.labels.length > 0 && (
                  <div className="flex flex-wrap gap-1.5">
                    {task.labels.map((label) => (
                      <Popover key={label.id}>
                        <PopoverTrigger asChild>
                          <button
                            className="inline-flex items-center rounded px-3 py-1.5 text-[11px] font-semibold cursor-pointer hover:opacity-80 transition-opacity text-white"
                            style={{ backgroundColor: label.color }}
                            aria-label={label.name}
                          >
                            {label.name}
                          </button>
                        </PopoverTrigger>
                        <PopoverContent className="p-0 w-60" align="start">
                          <LabelPickerPopover
                            activeIds={task.labels.map(l => l.id)}
                            projectLabels={projectLabels ?? []}
                            projectId={task.projectId}
                            taskId={task.id}
                            onToggle={handleToggleLabel}
                          />
                        </PopoverContent>
                      </Popover>
                    ))}
                  </div>
                )}

                {/* Complete toggle + Inline editable title */}
                <div className="flex items-center gap-3">
                  <Tooltip>
                    <TooltipTrigger asChild>
                      <button
                        className={`flex-shrink-0 h-6 w-6 rounded-full border-2 flex items-center justify-center transition-all cursor-pointer ${
                          task.status === 'Done'
                            ? 'bg-green-500 border-green-500 hover:bg-green-600 hover:border-green-600'
                            : 'border-green-500/60 hover:border-green-500 hover:bg-green-500/10'
                        }`}
                        onClick={() => handleStatusChange(task.status === 'Done' ? 'Todo' : 'Done')}
                        aria-label={task.status === 'Done' ? t('pm.markTodo', { defaultValue: 'Mark as todo' }) : t('pm.quickComplete', { defaultValue: 'Mark as done' })}
                      >
                        {task.status === 'Done'
                          ? <Check className="h-3.5 w-3.5 text-white" />
                          : <Check className="h-3.5 w-3.5 text-green-500/40 group-hover:text-green-500" />
                        }
                      </button>
                    </TooltipTrigger>
                    <TooltipContent>
                      {task.status === 'Done'
                        ? t('pm.markTodo', { defaultValue: 'Mark as todo' })
                        : t('pm.markComplete', { defaultValue: 'Mark as complete' })
                      }
                    </TooltipContent>
                  </Tooltip>
                  <div className="flex-1 min-w-0">
                    {editingTitle ? (
                      <input
                        ref={titleInputRef}
                        value={titleValue}
                        onChange={(e) => setTitleValue(e.target.value)}
                        onBlur={handleTitleSave}
                        onKeyDown={(e) => {
                          if (e.key === 'Enter') { e.preventDefault(); handleTitleSave() }
                          if (e.key === 'Escape') { setEditingTitle(false); setTitleValue(task.title) }
                        }}
                        className={`text-xl font-bold w-full bg-transparent border-none outline-none focus:ring-2 focus:ring-ring rounded px-1 -mx-1 ${task.status === 'Done' ? 'line-through text-muted-foreground' : ''}`}
                      />
                    ) : (
                      <h2
                        className={`text-xl font-bold cursor-text hover:bg-muted/30 rounded px-1 -mx-1 transition-colors group flex items-center gap-2 leading-snug ${task.status === 'Done' ? 'line-through text-muted-foreground' : ''}`}
                        onClick={() => setEditingTitle(true)}
                      >
                        <span className="flex-1">{task.title}</span>
                        <Pencil className="h-3.5 w-3.5 text-muted-foreground opacity-0 group-hover:opacity-100 transition-opacity flex-shrink-0" />
                      </h2>
                    )}
                  </div>
                </div>

                {/* Description */}
                <div>
                  <p className="text-[10px] font-bold text-muted-foreground/70 uppercase tracking-widest mb-2">
                    {t('pm.taskDescription', { defaultValue: 'Description' })}
                  </p>
                  <Textarea
                    value={descValue}
                    onChange={(e) => setDescValue(e.target.value)}
                    onBlur={handleDescriptionBlur}
                    placeholder={t('pm.descriptionPlaceholder', { defaultValue: 'Add a description…' })}
                    rows={4}
                    className="resize-none text-sm"
                  />
                </div>

                {/* Subtasks */}
                <div>
                  <div className="flex items-center gap-2 mb-3">
                    <CheckSquare className="h-4 w-4 text-green-500" />
                    <h3 className="text-sm font-semibold">
                      {t('pm.subtasks', { defaultValue: 'Subtasks' })}
                      {task.subtasks.length > 0 && (
                        <span className="text-muted-foreground ml-1.5 font-normal text-xs">
                          ({task.subtasks.filter((s) => s.status === 'Done').length}/{task.subtasks.length})
                        </span>
                      )}
                    </h3>
                    {task.subtasks.length > 0 && (
                      <div className="flex-1 h-1.5 bg-muted rounded-full overflow-hidden">
                        <div
                          className="h-full bg-green-500 rounded-full transition-all duration-300"
                          style={{ width: `${Math.round((task.subtasks.filter((s) => s.status === 'Done').length / task.subtasks.length) * 100)}%` }}
                        />
                      </div>
                    )}
                    <Button
                      variant="ghost" size="sm"
                      className="ml-auto cursor-pointer h-7 text-xs flex-shrink-0"
                      onClick={() => setAddSubtaskOpen(true)}
                    >
                      <Plus className="h-3.5 w-3.5 mr-1" />
                      {t('pm.addSubtask', { defaultValue: 'Add' })}
                    </Button>
                  </div>

                  {task.subtasks.length > 0 && (
                    <div className="space-y-1.5 mb-3">
                      {task.subtasks.map((subtask) => (
                        <ViewTransitionLink
                          key={subtask.id}
                          to={`/portal/tasks/${subtask.id}`}
                          onClick={() => onOpenChange(false)}
                          className="flex items-center gap-2.5 p-2.5 rounded-lg border border-border/50 hover:border-primary/20 hover:bg-primary/5 transition-all group/subtask"
                        >
                          <Badge
                            variant="outline"
                            className={`text-[10px] px-1.5 py-0 flex-shrink-0 ${getStatusBadgeClasses(statusColorMap[subtask.status])}`}
                          >
                            {subtask.status}
                          </Badge>
                          <span className="text-xs font-mono text-muted-foreground flex-shrink-0">#{subtask.taskNumber.split('-').pop()}</span>
                          <span className="text-sm flex-1 truncate">{subtask.title}</span>
                          {subtask.assigneeName && (
                            <span className="text-xs text-muted-foreground hidden group-hover/subtask:inline flex-shrink-0">
                              {subtask.assigneeName}
                            </span>
                          )}
                        </ViewTransitionLink>
                      ))}
                    </div>
                  )}

                  {addSubtaskOpen && (
                    <div className="mt-2 p-3 border rounded-lg bg-muted/20 space-y-2">
                      <input
                        autoFocus
                        value={subtaskTitle}
                        onChange={(e) => setSubtaskTitle(e.target.value)}
                        onKeyDown={(e) => {
                          if (e.key === 'Enter') handleAddSubtask()
                          if (e.key === 'Escape') { setAddSubtaskOpen(false); setSubtaskTitle('') }
                        }}
                        placeholder={t('pm.taskTitle', { defaultValue: 'Subtask title…' })}
                        className="w-full text-sm bg-background border border-input rounded-md px-3 py-2 focus:outline-none focus:ring-2 focus:ring-ring"
                      />
                      <div className="flex gap-2">
                        <Button
                          size="sm"
                          className="cursor-pointer h-7 text-xs"
                          onClick={handleAddSubtask}
                          disabled={createTaskMutation.isPending || !subtaskTitle.trim()}
                        >
                          {createTaskMutation.isPending && <Loader2 className="mr-1 h-3 w-3 animate-spin" />}
                          {t('buttons.add', { defaultValue: 'Add' })}
                        </Button>
                        <Button
                          variant="ghost" size="sm"
                          className="cursor-pointer h-7 text-xs"
                          onClick={() => { setAddSubtaskOpen(false); setSubtaskTitle('') }}
                        >
                          {t('buttons.cancel')}
                        </Button>
                      </div>
                    </div>
                  )}
                </div>

                {/* Comments */}
                <div>
                  <div className="flex items-center gap-2 mb-3">
                    <MessageSquare className="h-4 w-4 text-blue-500" />
                    <h3 className="text-sm font-semibold">
                      {t('pm.comments', { defaultValue: 'Comments' })}
                      {task.comments.length > 0 && (
                        <span className="text-muted-foreground ml-1.5 font-normal text-xs">({task.comments.length})</span>
                      )}
                    </h3>
                  </div>

                  <div className="space-y-2 mb-4">
                    <Textarea
                      value={commentText}
                      onChange={(e) => setCommentText(e.target.value)}
                      onKeyDown={(e) => {
                        if (e.key === 'Enter' && (e.ctrlKey || e.metaKey)) {
                          e.preventDefault()
                          handleAddComment()
                        }
                      }}
                      placeholder={t('pm.commentPlaceholder', { defaultValue: 'Write a comment… (Ctrl+Enter)' })}
                      rows={2}
                      className="resize-none text-sm"
                    />
                    {commentText.trim() && (
                      <div className="flex justify-end">
                        <Button
                          size="sm"
                          className="cursor-pointer"
                          onClick={handleAddComment}
                          disabled={addCommentMutation.isPending}
                        >
                          {addCommentMutation.isPending ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : <Send className="h-3.5 w-3.5 mr-1.5" />}
                          {t('pm.addComment', { defaultValue: 'Comment' })}
                        </Button>
                      </div>
                    )}
                  </div>

                  {task.comments.length > 0 && (
                    <div className="space-y-3">
                      {task.comments.map((comment) => (
                        <div key={comment.id} className="flex gap-2.5 group/comment">
                          <div className={`h-7 w-7 rounded-full flex items-center justify-center text-[11px] font-bold flex-shrink-0 mt-0.5 ${getAvatarColorClass(comment.authorName)}`}>
                            {comment.authorName.charAt(0).toUpperCase()}
                          </div>
                          <div className="flex-1 min-w-0">
                            <div className="flex items-baseline gap-2 mb-1">
                              <span className="text-sm font-semibold">{comment.authorName}</span>
                              <span className="text-xs text-muted-foreground">{formatRelativeTime(comment.createdAt)}</span>
                              {comment.isEdited && <span className="text-xs text-muted-foreground italic">(edited)</span>}
                              <Button
                                variant="ghost" size="icon"
                                className="h-5 w-5 text-muted-foreground hover:text-red-500 cursor-pointer opacity-0 group-hover/comment:opacity-100 transition-opacity ml-auto"
                                onClick={() => handleDeleteComment(comment.id)}
                                disabled={deleteCommentMutation.isPending}
                                aria-label={`${t('pm.deleteComment', { defaultValue: 'Delete comment' })} — ${comment.authorName}`}
                              >
                                <Trash2 className="h-3 w-3" />
                              </Button>
                            </div>
                            <div className="bg-muted/50 border border-border/40 rounded-lg px-3 py-2">
                              <p className="text-sm whitespace-pre-wrap text-foreground/90">{comment.content}</p>
                            </div>
                          </div>
                        </div>
                      ))}
                    </div>
                  )}

                  {task.comments.length === 0 && (
                    <p className="text-xs text-muted-foreground text-center py-3">
                      {t('pm.noComments', { defaultValue: 'No comments yet. Be the first to comment!' })}
                    </p>
                  )}
                </div>
              </div>

              {/* ── Right sidebar ── */}
              <div className="p-4 bg-muted/40 dark:bg-muted/20 space-y-4 overflow-y-auto border-l border-border/60">

                {/* Status */}
                <SidebarSection icon={CheckSquare} label={t('pm.status', { defaultValue: 'Status' })}>
                  <Select value={task.status} onValueChange={handleStatusChange}>
                    <SelectTrigger className="cursor-pointer h-8 text-xs" aria-label={t('pm.status')}>
                      <SelectValue>
                        <span className="flex items-center gap-2">
                          <span className={`h-2 w-2 rounded-full flex-shrink-0 ${statusDot[task.status] ?? 'bg-slate-400'}`} />
                          {t(`statuses.${task.status.toLowerCase()}`, { defaultValue: task.status })}
                        </span>
                      </SelectValue>
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="Todo" className="cursor-pointer text-xs">
                        <span className="flex items-center gap-2"><span className="h-2 w-2 rounded-full bg-slate-400" />{t('statuses.todo', { defaultValue: 'To Do' })}</span>
                      </SelectItem>
                      <SelectItem value="InProgress" className="cursor-pointer text-xs">
                        <span className="flex items-center gap-2"><span className="h-2 w-2 rounded-full bg-blue-500" />{t('statuses.inProgress', { defaultValue: 'In Progress' })}</span>
                      </SelectItem>
                      <SelectItem value="InReview" className="cursor-pointer text-xs">
                        <span className="flex items-center gap-2"><span className="h-2 w-2 rounded-full bg-violet-500" />{t('statuses.inReview', { defaultValue: 'In Review' })}</span>
                      </SelectItem>
                      <SelectItem value="Done" className="cursor-pointer text-xs">
                        <span className="flex items-center gap-2"><span className="h-2 w-2 rounded-full bg-green-500" />{t('statuses.done', { defaultValue: 'Done' })}</span>
                      </SelectItem>
                      <SelectItem value="Cancelled" className="cursor-pointer text-xs">
                        <span className="flex items-center gap-2"><span className="h-2 w-2 rounded-full bg-red-500" />{t('statuses.cancelled', { defaultValue: 'Cancelled' })}</span>
                      </SelectItem>
                    </SelectContent>
                  </Select>
                </SidebarSection>

                {/* Priority */}
                <SidebarSection icon={ArrowUp} label={t('pm.priority', { defaultValue: 'Priority' })}>
                  <Select value={task.priority} onValueChange={handlePriorityChange}>
                    <SelectTrigger className="cursor-pointer h-8 text-xs" aria-label={t('pm.priority')}>
                      <SelectValue>
                        {(() => {
                          const { icon: PriorityIcon, class: cls } = priorityConfig[task.priority]
                          return (
                            <span className={`flex items-center gap-1.5 ${cls}`}>
                              <PriorityIcon className="h-3.5 w-3.5" />
                              {t(`priorities.${task.priority.toLowerCase()}`, { defaultValue: task.priority })}
                            </span>
                          )
                        })()}
                      </SelectValue>
                    </SelectTrigger>
                    <SelectContent>
                      {(Object.entries(priorityConfig) as [TaskPriority, { icon: React.ElementType; class: string }][]).map(([p, { icon: Icon, class: cls }]) => (
                        <SelectItem key={p} value={p} className="cursor-pointer text-xs">
                          <span className={`flex items-center gap-1.5 ${cls}`}>
                            <Icon className="h-3.5 w-3.5" />{t(`priorities.${p.toLowerCase()}`, { defaultValue: p })}
                          </span>
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </SidebarSection>

                {/* Assignee — popover member picker */}
                <SidebarSection icon={Users} label={t('pm.assignee', { defaultValue: 'Members' })}>
                  <MemberPickerPopover
                    assigneeId={task.assigneeId}
                    assigneeName={task.assigneeName}
                    members={projectMembers ?? []}
                    onAssign={handleAssigneeChange}
                  />
                </SidebarSection>

                {/* Due date — custom popover chip */}
                <SidebarSection icon={Calendar} label={t('pm.dueDate', { defaultValue: 'Due date' })}>
                  <DueDateButton
                    dueDate={task.dueDate}
                    taskStatus={task.status}
                    onChange={handleDueDateChange}
                  />
                </SidebarSection>

                {/* Labels — popover picker */}
                <SidebarSection icon={Tag} label={t('pm.labels', { defaultValue: 'Labels' })}>
                  <Popover>
                    <PopoverTrigger asChild>
                      <button
                        className="w-full flex items-center gap-2 hover:bg-muted/50 rounded-md px-2 py-1.5 transition-colors cursor-pointer group text-left"
                        aria-label={t('pm.labels', { defaultValue: 'Labels' })}
                      >
                        {task.labels.length > 0 ? (
                          <div className="flex flex-wrap gap-1 flex-1">
                            {task.labels.map(l => (
                              <span
                                key={l.id}
                                className="inline-flex items-center rounded px-2 py-0.5 text-[10px] font-semibold text-white"
                                style={{ backgroundColor: l.color }}
                              >
                                {l.name}
                              </span>
                            ))}
                          </div>
                        ) : (
                          <span className="text-xs text-muted-foreground flex-1">
                            {t('pm.noLabels', { defaultValue: 'None' })}
                          </span>
                        )}
                        <ChevronDown className="h-3 w-3 text-muted-foreground opacity-0 group-hover:opacity-100 transition-opacity flex-shrink-0" />
                      </button>
                    </PopoverTrigger>
                    <PopoverContent className="p-0 w-60" align="end" side="bottom">
                      <LabelPickerPopover
                        activeIds={task.labels.map(l => l.id)}
                        projectLabels={projectLabels ?? []}
                        projectId={task.projectId}
                        taskId={task.id}
                        onToggle={handleToggleLabel}
                      />
                    </PopoverContent>
                  </Popover>
                </SidebarSection>

                <div className="border-t border-border/30" />

                {/* Reporter */}
                <SidebarSection icon={UserCheck} label={t('pm.reporter', { defaultValue: 'Reporter' })}>
                  <p className="text-xs px-2">{task.reporterName || '—'}</p>
                </SidebarSection>

                {/* Estimated / Actual hours */}
                {(task.estimatedHours != null || task.actualHours != null) && (
                  <SidebarSection icon={Clock} label={`${t('pm.estimatedHours', { defaultValue: 'Est.' })} / ${t('pm.actualHours', { defaultValue: 'Actual' })}`}>
                    <p className="text-xs font-medium px-2">
                      {task.estimatedHours ?? '—'}h / {task.actualHours ?? '—'}h
                    </p>
                  </SidebarSection>
                )}


                {/* Column */}
                {task.columnName && (
                  <SidebarSection icon={MessageSquare} label={t('pm.columns', { defaultValue: 'Column' })}>
                    <p className="text-xs font-medium px-2">{task.columnName}</p>
                  </SidebarSection>
                )}

                <div className="border-t border-border/30" />

                {/* Timestamps */}
                <div className="space-y-1.5 text-[10px] text-muted-foreground px-2">
                  <div className="flex items-center gap-1.5">
                    <Calendar className="h-2.5 w-2.5" />
                    <span>{t('labels.created', { defaultValue: 'Created' })}: {formatRelativeTime(task.createdAt)}</span>
                  </div>
                  {task.modifiedAt && (
                    <div className="flex items-center gap-1.5">
                      <Clock className="h-2.5 w-2.5" />
                      <span>{t('labels.updatedAt', { defaultValue: 'Updated' })}: {formatRelativeTime(task.modifiedAt)}</span>
                    </div>
                  )}
                  {task.completedAt && (
                    <div className="flex items-center gap-1.5">
                      <CheckSquare className="h-2.5 w-2.5 text-green-500" />
                      <span className="text-green-600 dark:text-green-400">
                        {t('pm.completedAt', { defaultValue: 'Completed' })}: {formatDate(task.completedAt)}
                      </span>
                    </div>
                  )}
                </div>

                {/* Archive */}
                <div className="border-t border-border/30 pt-3">
                  <button
                    className="w-full flex items-center justify-center gap-1.5 text-xs text-muted-foreground hover:text-amber-600 cursor-pointer transition-colors py-1 rounded-md hover:bg-amber-500/10 disabled:opacity-50"
                    onClick={handleArchiveTask}
                    disabled={archiveTaskMutation.isPending}
                  >
                    {archiveTaskMutation.isPending
                      ? <Loader2 className="h-3 w-3 animate-spin" />
                      : <Trash2 className="h-3 w-3" />
                    }
                    {t('pm.archiveTask', { defaultValue: 'Archive task' })}
                  </button>
                </div>
              </div>
            </div>
          </div>
        )}

        {/* ── Not found ── */}
        {!isLoading && !task && taskId && (
          <div className="flex-1 flex items-center justify-center p-12 text-center">
            <div>
              <p className="text-muted-foreground text-sm">{t('pm.noTasksFound', { defaultValue: 'Task not found.' })}</p>
              <Button variant="ghost" size="sm" className="mt-3 cursor-pointer" onClick={() => onOpenChange(false)}>
                {t('buttons.close', { defaultValue: 'Close' })}
              </Button>
            </div>
          </div>
        )}
      </DialogContent>
    </Dialog>
  )
}
