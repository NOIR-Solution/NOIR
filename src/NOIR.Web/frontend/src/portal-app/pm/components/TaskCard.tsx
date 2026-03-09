import { useSortable } from '@dnd-kit/sortable'
import { CSS } from '@dnd-kit/utilities'
import { Calendar, MessageSquare, CheckSquare, ArrowDown, Minus, ArrowUp, AlertTriangle, Check, X, CornerDownRight, Layers } from 'lucide-react'
import { Avatar, Card, CardContent } from '@uikit'
import { useRegionalSettings } from '@/contexts/RegionalSettingsContext'
import { useTranslation } from 'react-i18next'
import type { TaskCardDto, TaskPriority } from '@/types/pm'

const priorityConfig: Record<TaskPriority, { icon: React.ElementType; badgeClass: string }> = {
  Low: {
    icon: ArrowDown,
    badgeClass: 'bg-slate-100 text-slate-600 border-slate-200 dark:bg-slate-800/50 dark:text-slate-300 dark:border-slate-700',
  },
  Medium: {
    icon: Minus,
    badgeClass: 'bg-blue-50 text-blue-600 border-blue-200 dark:bg-blue-950/50 dark:text-blue-300 dark:border-blue-800',
  },
  High: {
    icon: ArrowUp,
    badgeClass: 'bg-orange-50 text-orange-600 border-orange-200 dark:bg-orange-950/50 dark:text-orange-300 dark:border-orange-800',
  },
  Urgent: {
    icon: AlertTriangle,
    badgeClass: 'bg-red-50 text-red-600 border-red-200 dark:bg-red-950/50 dark:text-red-300 dark:border-red-800',
  },
}

const getDueDateClasses = (dueDate: string | null, status: string) => {
  if (!dueDate || status === 'Done' || status === 'Cancelled') return 'text-muted-foreground'
  const diffDays = Math.ceil((new Date(dueDate).getTime() - Date.now()) / 86400000)
  if (diffDays < 0) return 'text-red-500 font-medium'
  if (diffDays === 0) return 'text-amber-500 font-medium'
  if (diffDays <= 2) return 'text-amber-400'
  return 'text-muted-foreground'
}

/** Extracts just the numeric suffix: "TEST-PROJECT-IVZXMJ-004" → "#004" */
const shortNum = (taskNumber: string) => `#${taskNumber.split('-').pop()}`

interface TaskCardProps {
  task: TaskCardDto
  onClick: (task: TaskCardDto) => void
  isDraggable?: boolean
  onComplete?: (task: TaskCardDto) => void
  onUnassign?: (task: TaskCardDto) => void
  isMemberDragTarget?: boolean
}

export const TaskCard = ({ task, onClick, isDraggable = true, onComplete, onUnassign, isMemberDragTarget }: TaskCardProps) => {
  const { t } = useTranslation('common')
  const { formatDate } = useRegionalSettings()

  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    transition,
    isDragging,
  } = useSortable({
    id: task.id,
    disabled: !isDraggable,
  })

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
    opacity: isDragging ? 0.5 : 1,
  }

  const { icon: PriorityIcon, badgeClass } = priorityConfig[task.priority]
  const visibleLabels = task.labels.slice(0, 2)
  const extraLabels = task.labels.length - 2
  const isParent = task.subtaskCount > 0 && !task.parentTaskId
  const isSubtask = Boolean(task.parentTaskId && task.parentTaskNumber)

  return (
    <div
      ref={setNodeRef}
      style={style}
      {...attributes}
      {...(isDraggable ? listeners : {})}
      className={`group ${isDraggable ? 'cursor-grab active:cursor-grabbing' : ''}`}
    >
      <Card
        className={`shadow-sm hover:shadow-md transition-all duration-200 cursor-pointer border-border/50 py-0 gap-0 ${
          isMemberDragTarget ? 'ring-2 ring-primary/60 shadow-md bg-primary/5' : ''
        } ${task.status === 'Done' || task.status === 'Cancelled' ? 'opacity-60' : ''}`}
        onClick={() => onClick(task)}
      >
        <CardContent className="p-2.5 space-y-1">

          {/* ── Row 1: #Number · badges (left) | Done button (right) ── */}
          <div className="flex items-center justify-between gap-2">
            <div className="flex items-center gap-1.5 flex-wrap min-w-0">
              {/* Short task number */}
              <span className="text-[11px] text-muted-foreground font-mono font-semibold">{shortNum(task.taskNumber)}</span>

              {/* Priority badge */}
              <span className={`inline-flex items-center gap-0.5 rounded-full px-1.5 py-0.5 text-[10px] font-medium leading-[1.1] border ${badgeClass}`}>
                <PriorityIcon className="h-2.5 w-2.5" />
                {task.priority}
              </span>

              {/* Parent task badge */}
              {isParent && (
                <span className="inline-flex items-center gap-0.5 rounded-full px-1.5 py-0.5 text-[10px] font-medium leading-[1.1] border bg-sky-50 text-sky-600 border-sky-200 dark:bg-sky-950/40 dark:text-sky-300 dark:border-sky-800">
                  <Layers className="h-2.5 w-2.5" />
                  {t('pm.parentTask', { defaultValue: 'Parent' })}
                </span>
              )}

              {/* Subtask badge: ↳ #004 */}
              {isSubtask && (
                <span className="inline-flex items-center gap-0.5 rounded-full px-1.5 py-0.5 text-[10px] font-medium leading-[1.1] border bg-violet-50 text-violet-600 border-violet-200 dark:bg-violet-950/40 dark:text-violet-300 dark:border-violet-800">
                  <CornerDownRight className="h-2.5 w-2.5" />
                  {shortNum(task.parentTaskNumber!)}
                </span>
              )}
            </div>

            {/* Done checkbox — always visible when Done, hover-only otherwise */}
            {onComplete && (
              <button
                className={`flex-shrink-0 h-5 w-5 rounded-full border-2 flex items-center justify-center transition-all cursor-pointer ${
                  task.status === 'Done'
                    ? 'bg-green-500 border-green-500 hover:bg-green-600 hover:border-green-600'
                    : 'opacity-0 group-hover:opacity-100 border-muted-foreground/30 hover:border-green-500 hover:bg-green-500/10'
                }`}
                onClick={(e) => { e.stopPropagation(); onComplete(task) }}
                aria-label={task.status === 'Done' ? t('pm.markTodo', { defaultValue: 'Mark as todo' }) : t('pm.quickComplete', { defaultValue: 'Mark as done' })}
              >
                <Check className={`h-3 w-3 ${task.status === 'Done' ? 'text-white' : 'opacity-0 group-hover:opacity-60 text-green-600'}`} />
              </button>
            )}
          </div>

          {/* ── Row 2: Title ── */}
          <p className={`text-sm font-medium line-clamp-2 ${task.status === 'Done' ? 'line-through text-muted-foreground' : ''}`}>{task.title}</p>

          {/* ── Row 3: metadata — subtasks · comments · due date · labels · assignee ── */}
          <div className="flex items-center gap-2 flex-wrap text-xs text-muted-foreground">
            {/* Subtask count */}
            {task.subtaskCount > 0 && (
              <div className="flex items-center gap-1">
                <CheckSquare className="h-3 w-3 flex-shrink-0" />
                <span className={task.completedSubtaskCount === task.subtaskCount ? 'text-green-600 font-medium' : ''}>
                  {task.completedSubtaskCount}/{task.subtaskCount}
                </span>
              </div>
            )}

            {/* Comment count */}
            {task.commentCount > 0 && (
              <div className="flex items-center gap-1">
                <MessageSquare className="h-3 w-3 flex-shrink-0" />
                <span>{task.commentCount}</span>
              </div>
            )}

            {/* Due date */}
            {task.dueDate && (
              <div className={`flex items-center gap-1 ${getDueDateClasses(task.dueDate, task.status)}`}>
                <Calendar className="h-3 w-3 flex-shrink-0" />
                <span>{formatDate(task.dueDate)}</span>
              </div>
            )}

            {/* Labels */}
            {task.labels.length > 0 && (
              <div className="flex items-center gap-1 flex-wrap">
                {visibleLabels.map((label) => (
                  <span
                    key={label.id}
                    className="inline-flex items-center rounded-full px-1.5 py-0.5 text-[10px] font-medium leading-[1.1] border"
                    style={{
                      backgroundColor: `${label.color}20`,
                      borderColor: `${label.color}60`,
                      color: label.color,
                    }}
                  >
                    {label.name}
                  </span>
                ))}
                {extraLabels > 0 && (
                  <span className="inline-flex items-center rounded-full px-1.5 py-0.5 text-[10px] font-medium leading-[1.1] border bg-muted text-muted-foreground border-border">
                    +{extraLabels}
                  </span>
                )}
              </div>
            )}

            {/* Assignee */}
            {task.assigneeName && (
              <div
                className="flex items-center gap-1 ml-auto group/assignee"
                onClick={(e) => e.stopPropagation()}
              >
                <Avatar
                  src={task.assigneeAvatarUrl ?? undefined}
                  alt={task.assigneeName}
                  fallback={task.assigneeName}
                  size="sm"
                  className="h-5 w-5 flex-shrink-0 text-[9px]"
                />
                {onUnassign && (
                  <button
                    className="opacity-0 group-hover/assignee:opacity-100 h-3.5 w-3.5 rounded-full bg-muted-foreground/20 hover:bg-red-500/20 hover:text-red-500 flex items-center justify-center flex-shrink-0 transition-all cursor-pointer"
                    onClick={(e) => { e.stopPropagation(); onUnassign(task) }}
                    aria-label={t('pm.unassign', { defaultValue: 'Unassign' })}
                  >
                    <X className="h-2.5 w-2.5" />
                  </button>
                )}
              </div>
            )}
          </div>

        </CardContent>
      </Card>
    </div>
  )
}
