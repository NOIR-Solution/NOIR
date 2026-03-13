import { useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams, useNavigate } from 'react-router-dom'
import { useEntityUpdateSignal } from '@/hooks/useEntityUpdateSignal'
import { OfflineBanner } from '@/components/OfflineBanner'
import { EntityConflictDialog } from '@/components/EntityConflictDialog'
import { EntityDeletedDialog } from '@/components/EntityDeletedDialog'
import { ViewTransitionLink } from '@/components/navigation/ViewTransitionLink'
import { toast } from 'sonner'
import {
  Calendar,
  Clock,
  MessageSquare,
  Send,
  Trash2,
  UserCheck,
  User,
  Loader2,
  CheckSquare,
  Pencil,
  Plus,
  EllipsisVertical,
  FolderKanban,
  Tag,
} from 'lucide-react'
import {
  Badge,
  Button,
  Card,
  CardContent,
  Credenza,
  EmptyState,
  CredenzaContent,
  CredenzaDescription,
  CredenzaFooter,
  CredenzaHeader,
  CredenzaTitle,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Skeleton,
  Textarea,
} from '@uikit'
import { getStatusBadgeClasses } from '@/utils/statusBadge'
import { useRegionalSettings } from '@/contexts/RegionalSettingsContext'
import { usePageContext } from '@/hooks/usePageContext'
import {
  useTaskQuery,
  useUpdateTask,
  useChangeTaskStatus,
  useAddComment,
  useDeleteComment,
  useDeleteTask,
  useCreateTask,
  useProjectQuery,
} from '@/portal-app/pm/queries'
import type { ProjectTaskStatus, TaskPriority } from '@/types/pm'

const statusColorMap: Record<ProjectTaskStatus, 'gray' | 'blue' | 'purple' | 'green' | 'red'> = {
  Todo: 'gray',
  InProgress: 'blue',
  InReview: 'purple',
  Done: 'green',
  Cancelled: 'red',
}

export const TaskDetailPage = () => {
  const { t } = useTranslation('common')
  const { id } = useParams<{ id: string }>()
  const { formatDate, formatRelativeTime } = useRegionalSettings()
  usePageContext('TaskDetailPage')

  const navigate = useNavigate()
  const { data: task, isLoading, refetch } = useTaskQuery(id)
  const updateTaskMutation = useUpdateTask()
  const changeStatusMutation = useChangeTaskStatus()
  const addCommentMutation = useAddComment()
  const deleteCommentMutation = useDeleteComment()
  const deleteTaskMutation = useDeleteTask()
  const createTaskMutation = useCreateTask()

  const { data: project } = useProjectQuery(task?.projectId)

  const { conflictSignal, deletedSignal, dismissConflict, reloadAndRestart, isReconnecting } = useEntityUpdateSignal({
    entityType: 'ProjectTask',
    entityId: id,
    onAutoReload: refetch,
    onNavigateAway: () => navigate('/portal/projects'),
  })

  const [commentText, setCommentText] = useState('')
  const [deleteConfirmOpen, setDeleteConfirmOpen] = useState(false)
  const [deleteCommentId, setDeleteCommentId] = useState<string | null>(null)

  // Inline title editing
  const [editingTitle, setEditingTitle] = useState(false)
  const [titleValue, setTitleValue] = useState(task?.title ?? '')

  useEffect(() => {
    if (task?.title) setTitleValue(task.title)
  }, [task?.title])

  // Add subtask inline form
  const [addSubtaskOpen, setAddSubtaskOpen] = useState(false)
  const [subtaskTitle, setSubtaskTitle] = useState('')

  const handleTitleSave = () => {
    if (!task || !titleValue.trim()) {
      setEditingTitle(false)
      setTitleValue(task?.title ?? '')
      return
    }
    if (titleValue.trim() === task.title) {
      setEditingTitle(false)
      return
    }
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

  const handleStatusChange = (status: string) => {
    if (!task) return
    changeStatusMutation.mutate(
      { id: task.id, status },
      {
        onError: (err) => {
          toast.error(err instanceof Error ? err.message : t('errors.unknown'))
        },
      },
    )
  }

  const handlePriorityChange = (priority: string) => {
    if (!task) return
    updateTaskMutation.mutate(
      { id: task.id, request: { priority: priority as TaskPriority } },
      {
        onError: (err) => {
          toast.error(err instanceof Error ? err.message : t('errors.unknown'))
        },
      },
    )
  }

  const handleAssigneeChange = (value: string) => {
    if (!task) return
    updateTaskMutation.mutate(
      { id: task.id, request: { assigneeId: value === 'unassigned' ? '' : value } },
      {
        onError: (err) => {
          toast.error(err instanceof Error ? err.message : t('errors.unknown'))
        },
      },
    )
  }

  const handleAddComment = () => {
    if (!task || !commentText.trim()) return
    addCommentMutation.mutate(
      { taskId: task.id, request: { content: commentText } },
      {
        onSuccess: () => {
          setCommentText('')
        },
        onError: (err) => {
          toast.error(err instanceof Error ? err.message : t('errors.unknown'))
        },
      },
    )
  }

  const handleDeleteComment = () => {
    if (!task || !deleteCommentId) return
    deleteCommentMutation.mutate(
      { taskId: task.id, commentId: deleteCommentId },
      {
        onSuccess: () => {
          setDeleteCommentId(null)
        },
        onError: (err) => {
          toast.error(err instanceof Error ? err.message : t('errors.unknown'))
        },
      },
    )
  }

  const handleDeleteTask = () => {
    if (!task) return
    deleteTaskMutation.mutate(task.id, {
      onSuccess: () => {
        window.history.back()
      },
      onError: (err) => {
        toast.error(err instanceof Error ? err.message : t('errors.unknown'))
      },
    })
  }

  const handleAddSubtask = () => {
    if (!task || !subtaskTitle.trim()) return
    createTaskMutation.mutate(
      {
        projectId: task.projectId,
        title: subtaskTitle.trim(),
        parentTaskId: task.id,
        columnId: task.columnId ?? undefined,
      },
      {
        onSuccess: () => {
          setSubtaskTitle('')
          setAddSubtaskOpen(false)
          void refetch()
        },
        onError: (err) => {
          toast.error(err instanceof Error ? err.message : t('errors.unknown'))
        },
      },
    )
  }

  if (isLoading) {
    return (
      <div className="space-y-6">
        <Skeleton className="h-8 w-64" />
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          <div className="lg:col-span-2 space-y-4">
            <Skeleton className="h-12 w-full" />
            <Skeleton className="h-48 w-full" />
          </div>
          <Skeleton className="h-96 w-full" />
        </div>
      </div>
    )
  }

  if (!task) {
    return (
      <EmptyState icon={CheckSquare} title={t('pm.noTasksFound')} description={t('pm.taskNotFoundDescription', { defaultValue: 'The task may have been deleted or you do not have access.' })} />
    )
  }

  return (
    <div className="space-y-6">
      <OfflineBanner visible={isReconnecting} />
      <EntityConflictDialog signal={conflictSignal} onContinueEditing={dismissConflict} onReloadAndRestart={reloadAndRestart} />
      <EntityDeletedDialog signal={deletedSignal} onGoBack={() => navigate('/portal/projects')} />

      {/* Breadcrumb + Header actions */}
      <div className="flex items-center justify-between flex-wrap gap-2">
        <nav className="text-sm text-muted-foreground flex items-center flex-wrap gap-1">
          <ViewTransitionLink to="/portal/projects" className="hover:text-foreground transition-colors">
            {t('pm.projects')}
          </ViewTransitionLink>
          <span>/</span>
          <ViewTransitionLink
            to={`/portal/projects/${task.projectCode ?? task.projectId}`}
            className="hover:text-foreground transition-colors flex items-center gap-1"
          >
            <FolderKanban className="h-3.5 w-3.5" />
            {project?.name ?? t('pm.projectDetails')}
          </ViewTransitionLink>
          <span>/</span>
          <span className="text-foreground font-mono text-xs">{task.taskNumber}</span>
        </nav>

        {/* Overflow actions menu */}
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button
              variant="ghost"
              size="icon"
              className="h-8 w-8 cursor-pointer"
              aria-label={t('labels.moreActions', { defaultValue: 'More actions' })}
            >
              <EllipsisVertical className="h-4 w-4" />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            <DropdownMenuItem
              className="cursor-pointer text-destructive focus:text-destructive focus:bg-destructive/10"
              onClick={() => setDeleteConfirmOpen(true)}
            >
              <Trash2 className="h-4 w-4 mr-2" />
              {t('pm.deleteTask')}
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>

      {/* Two-column layout */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Main content (left 2/3) */}
        <div className="lg:col-span-2 space-y-6">
          {/* Inline editable title */}
          {editingTitle ? (
            <input
              autoFocus
              value={titleValue}
              onChange={(e) => setTitleValue(e.target.value)}
              onBlur={handleTitleSave}
              onKeyDown={(e) => {
                if (e.key === 'Enter') {
                  e.preventDefault()
                  handleTitleSave()
                }
                if (e.key === 'Escape') {
                  setEditingTitle(false)
                  setTitleValue(task.title)
                }
              }}
              className="text-2xl font-bold tracking-tight w-full bg-transparent border-none outline-none focus:ring-2 focus:ring-ring rounded px-1 -mx-1"
            />
          ) : (
            <h1
              className="text-2xl font-bold tracking-tight cursor-text hover:bg-muted/30 rounded px-1 -mx-1 transition-colors group flex items-center gap-2"
              onClick={() => setEditingTitle(true)}
              title={t('labels.clickToEdit')}
            >
              {task.title}
              <Pencil className="h-3.5 w-3.5 text-muted-foreground opacity-0 group-hover:opacity-100 transition-opacity" />
            </h1>
          )}

          {/* Description */}
          {task.description && (
            <div>
              <h3 className="text-sm font-semibold mb-2">{t('pm.taskDescription')}</h3>
              <div className="prose prose-sm dark:prose-invert max-w-none">
                <p className="text-sm text-muted-foreground whitespace-pre-wrap">{task.description}</p>
              </div>
            </div>
          )}

          {/* Subtasks */}
          <div>
            <h3 className="text-sm font-semibold mb-3 flex items-center gap-2">
              <CheckSquare className="h-4 w-4" />
              {t('pm.subtasks')} ({task.subtasks.filter((s) => s.status === 'Done').length}/{task.subtasks.length})
              <Button
                variant="ghost"
                size="sm"
                className="ml-auto cursor-pointer h-7 text-xs"
                onClick={() => setAddSubtaskOpen(true)}
              >
                <Plus className="h-3.5 w-3.5 mr-1" />
                {t('pm.addSubtask', { defaultValue: 'Add Subtask' })}
              </Button>
            </h3>

            {task.subtasks.length > 0 && (
              <div className="space-y-2">
                {task.subtasks.map((subtask) => (
                  <ViewTransitionLink
                    key={subtask.id}
                    to={`/portal/tasks/${subtask.id}`}
                    className="flex items-center gap-3 p-2 rounded-lg border hover:bg-muted/50 transition-colors"
                  >
                    <Badge variant="outline" className={getStatusBadgeClasses(statusColorMap[subtask.status])}>
                      {subtask.status}
                    </Badge>
                    <span className="text-xs font-mono text-muted-foreground">{subtask.taskNumber}</span>
                    <span className="text-sm flex-1 truncate">{subtask.title}</span>
                    {subtask.assigneeName && (
                      <span className="text-xs text-muted-foreground">{subtask.assigneeName}</span>
                    )}
                  </ViewTransitionLink>
                ))}
              </div>
            )}

            {/* Inline add subtask form */}
            {addSubtaskOpen && (
              <div className="mt-2 p-3 border rounded-lg bg-muted/20 space-y-2">
                <input
                  autoFocus
                  value={subtaskTitle}
                  onChange={(e) => setSubtaskTitle(e.target.value)}
                  onKeyDown={(e) => {
                    if (e.key === 'Enter') handleAddSubtask()
                    if (e.key === 'Escape') {
                      setAddSubtaskOpen(false)
                      setSubtaskTitle('')
                    }
                  }}
                  placeholder={t('pm.taskTitle')}
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
                    {t('buttons.add')}
                  </Button>
                  <Button
                    variant="ghost"
                    size="sm"
                    className="cursor-pointer h-7 text-xs"
                    onClick={() => {
                      setAddSubtaskOpen(false)
                      setSubtaskTitle('')
                    }}
                  >
                    {t('buttons.cancel')}
                  </Button>
                </div>
              </div>
            )}
          </div>

          {/* Comments */}
          <div>
            <h3 className="text-sm font-semibold mb-3 flex items-center gap-2">
              <MessageSquare className="h-4 w-4" />
              {t('pm.comments')} ({task.comments.length})
            </h3>

            {/* Add comment */}
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
                placeholder={t('pm.commentPlaceholder')}
                rows={3}
                className="resize-none"
              />
              <div className="flex items-center justify-between">
                <p className="text-xs text-muted-foreground">
                  {t('pm.commentHint', { defaultValue: 'Ctrl+Enter to submit' })}
                </p>
                <Button
                  size="sm"
                  className="cursor-pointer"
                  onClick={handleAddComment}
                  disabled={addCommentMutation.isPending || !commentText.trim()}
                >
                  {addCommentMutation.isPending ? (
                    <Loader2 className="h-3.5 w-3.5 animate-spin" />
                  ) : (
                    <Send className="h-3.5 w-3.5 mr-1.5" />
                  )}
                  {t('pm.addComment')}
                </Button>
              </div>
            </div>

            {/* Comment list */}
            <div className="space-y-3">
              {task.comments.map((comment) => (
                <div key={comment.id} className="p-3 rounded-lg border">
                  <div className="flex items-center justify-between mb-1">
                    <div className="flex items-center gap-2">
                      <span className="text-sm font-medium">{comment.authorName}</span>
                      <span className="text-xs text-muted-foreground">{formatRelativeTime(comment.createdAt)}</span>
                      {comment.isEdited && (
                        <span className="text-xs text-muted-foreground italic">
                          ({t('pm.editComment', { defaultValue: 'edited' })})
                        </span>
                      )}
                    </div>
                    <Button
                      variant="ghost"
                      size="icon"
                      className="h-6 w-6 text-muted-foreground hover:text-red-500 cursor-pointer"
                      onClick={() => setDeleteCommentId(comment.id)}
                      aria-label={`${t('pm.deleteComment')} - ${comment.authorName}`}
                    >
                      <Trash2 className="h-3 w-3" />
                    </Button>
                  </div>
                  <p className="text-sm whitespace-pre-wrap">{comment.content}</p>
                </div>
              ))}
            </div>
          </div>
        </div>

        {/* Sidebar (right 1/3) */}
        <div className="space-y-4">
          <Card>
            <CardContent className="p-4 space-y-0">
              {/* Status + Priority group */}
              <div className="space-y-3 pb-4 border-b border-border/30">
                {/* Status */}
                <div>
                  <label className="text-xs font-medium text-muted-foreground">{t('pm.status')}</label>
                  <Select value={task.status} onValueChange={handleStatusChange}>
                    <SelectTrigger className="mt-1 cursor-pointer" aria-label={t('pm.status')}>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="Todo" className="cursor-pointer">{t('statuses.todo')}</SelectItem>
                      <SelectItem value="InProgress" className="cursor-pointer">{t('statuses.inProgress')}</SelectItem>
                      <SelectItem value="InReview" className="cursor-pointer">{t('statuses.inReview')}</SelectItem>
                      <SelectItem value="Done" className="cursor-pointer">{t('statuses.done')}</SelectItem>
                      <SelectItem value="Cancelled" className="cursor-pointer">{t('statuses.cancelled')}</SelectItem>
                    </SelectContent>
                  </Select>
                </div>

                {/* Priority */}
                <div>
                  <label className="text-xs font-medium text-muted-foreground">{t('pm.priority')}</label>
                  <Select value={task.priority} onValueChange={handlePriorityChange}>
                    <SelectTrigger className="mt-1 cursor-pointer" aria-label={t('pm.priority')}>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="Low" className="cursor-pointer">{t('priorities.low')}</SelectItem>
                      <SelectItem value="Medium" className="cursor-pointer">{t('priorities.medium')}</SelectItem>
                      <SelectItem value="High" className="cursor-pointer">{t('priorities.high')}</SelectItem>
                      <SelectItem value="Urgent" className="cursor-pointer">{t('priorities.urgent')}</SelectItem>
                    </SelectContent>
                  </Select>
                </div>
              </div>

              {/* People group */}
              <div className="space-y-3 py-4 border-b border-border/30">
                {/* Assignee — dropdown for reassignment */}
                <div>
                  <label className="text-xs font-medium text-muted-foreground">{t('pm.assignee')}</label>
                  <Select
                    value={task.assigneeId ?? 'unassigned'}
                    onValueChange={handleAssigneeChange}
                  >
                    <SelectTrigger className="mt-1 cursor-pointer" aria-label={t('pm.assignee')}>
                      <SelectValue>
                        {task.assigneeName ? (
                          <div className="flex items-center gap-2">
                            <div className="h-5 w-5 rounded-full bg-primary/10 flex items-center justify-center text-[10px] font-medium text-primary flex-shrink-0">
                              {task.assigneeName.charAt(0).toUpperCase()}
                            </div>
                            <span className="truncate">{task.assigneeName}</span>
                          </div>
                        ) : (
                          <span className="text-muted-foreground">
                            {t('pm.unassigned', { defaultValue: 'Unassigned' })}
                          </span>
                        )}
                      </SelectValue>
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="unassigned" className="cursor-pointer">
                        <span className="text-muted-foreground">
                          {t('pm.unassigned', { defaultValue: 'Unassigned' })}
                        </span>
                      </SelectItem>
                      {project?.members.map((member) => (
                        <SelectItem key={member.employeeId} value={member.employeeId} className="cursor-pointer">
                          <div className="flex items-center gap-2">
                            <div className="h-5 w-5 rounded-full bg-primary/10 flex items-center justify-center text-[10px] font-medium text-primary flex-shrink-0">
                              {member.employeeName.charAt(0).toUpperCase()}
                            </div>
                            {member.employeeName}
                          </div>
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>

                {/* Reporter */}
                <div className="flex items-center gap-2">
                  <UserCheck className="h-4 w-4 text-muted-foreground flex-shrink-0" />
                  <div>
                    <p className="text-xs text-muted-foreground">{t('pm.reporter')}</p>
                    <p className="text-sm">{task.reporterName || '-'}</p>
                  </div>
                </div>
              </div>

              {/* Dates & Time group */}
              <div className="space-y-3 py-4 border-b border-border/30">
                {/* Due date */}
                {task.dueDate ? (
                  <div className="flex items-center gap-2">
                    <Calendar className="h-4 w-4 text-muted-foreground flex-shrink-0" />
                    <div>
                      <p className="text-xs text-muted-foreground">{t('pm.dueDate')}</p>
                      <p className={`text-sm ${new Date(task.dueDate) < new Date() ? 'text-red-500' : ''}`}>
                        {formatDate(task.dueDate)}
                      </p>
                    </div>
                  </div>
                ) : (
                  <div className="flex items-center gap-2 text-muted-foreground">
                    <Calendar className="h-4 w-4 flex-shrink-0" />
                    <div>
                      <p className="text-xs">{t('pm.dueDate')}</p>
                      <p className="text-sm">-</p>
                    </div>
                  </div>
                )}

                {/* Hours */}
                {(task.estimatedHours != null || task.actualHours != null) && (
                  <div className="flex items-center gap-2">
                    <Clock className="h-4 w-4 text-muted-foreground flex-shrink-0" />
                    <div>
                      <p className="text-xs text-muted-foreground">
                        {t('pm.estimatedHours')} / {t('pm.actualHours')}
                      </p>
                      <p className="text-sm">
                        {task.estimatedHours ?? '-'}h / {task.actualHours ?? '-'}h
                      </p>
                    </div>
                  </div>
                )}
              </div>

              {/* Metadata group */}
              <div className="space-y-3 py-4 border-b border-border/30">
                {/* Labels */}
                <div>
                  <p className="text-xs font-medium text-muted-foreground mb-1.5 flex items-center justify-between">
                    <span className="flex items-center gap-1">
                      <Tag className="h-3 w-3" />
                      {t('pm.labels')}
                    </span>
                    <ViewTransitionLink
                      to={`/portal/projects/${task.projectCode ?? task.projectId}?tab=settings`}
                      className="text-[10px] hover:text-primary transition-colors cursor-pointer"
                    >
                      {t('pm.editLabel')}
                    </ViewTransitionLink>
                  </p>
                  <div className="flex flex-wrap gap-1.5">
                    {task.labels.length > 0 ? (
                      task.labels.map((label) => (
                        <Badge
                          key={label.id}
                          variant="outline"
                          style={{ borderColor: label.color, color: label.color }}
                          className="text-xs"
                        >
                          {label.name}
                        </Badge>
                      ))
                    ) : (
                      <span className="text-xs text-muted-foreground">{t('pm.noLabels')}</span>
                    )}
                  </div>
                </div>

                {/* Parent task */}
                {task.parentTaskNumber && (
                  <div>
                    <p className="text-xs text-muted-foreground">{t('pm.parentTask')}</p>
                    <ViewTransitionLink
                      to={`/portal/tasks/${task.parentTaskId}`}
                      className="text-sm text-primary hover:underline"
                    >
                      {task.parentTaskNumber}
                    </ViewTransitionLink>
                  </div>
                )}

                {/* Column */}
                {task.columnName && (
                  <div>
                    <p className="text-xs text-muted-foreground">{t('pm.columns')}</p>
                    <p className="text-sm">{task.columnName}</p>
                  </div>
                )}
              </div>

              {/* Timestamps */}
              <div className="pt-3 text-xs text-muted-foreground space-y-1">
                <div className="flex items-center gap-1">
                  <User className="h-3 w-3" />
                  <span>{t('labels.created')}: {formatRelativeTime(task.createdAt)}</span>
                </div>
                {task.modifiedAt && (
                  <div className="flex items-center gap-1">
                    <User className="h-3 w-3" />
                    <span>{t('labels.updatedAt')}: {formatRelativeTime(task.modifiedAt)}</span>
                  </div>
                )}
              </div>
            </CardContent>
          </Card>
        </div>
      </div>

      {/* Delete task confirmation */}
      <Credenza open={deleteConfirmOpen} onOpenChange={setDeleteConfirmOpen}>
        <CredenzaContent className="border-destructive/30">
          <CredenzaHeader>
            <CredenzaTitle>{t('pm.deleteTask')}</CredenzaTitle>
            <CredenzaDescription>{t('pm.deleteTaskConfirmation')}</CredenzaDescription>
          </CredenzaHeader>
          <CredenzaFooter>
            <Button variant="outline" onClick={() => setDeleteConfirmOpen(false)} className="cursor-pointer">
              {t('buttons.cancel')}
            </Button>
            <Button
              variant="destructive"
              className="cursor-pointer bg-destructive/10 text-destructive border border-destructive/30 hover:bg-destructive hover:text-destructive-foreground transition-colors"
              onClick={handleDeleteTask}
              disabled={deleteTaskMutation.isPending}
            >
              {deleteTaskMutation.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              {t('pm.deleteTask')}
            </Button>
          </CredenzaFooter>
        </CredenzaContent>
      </Credenza>

      {/* Delete comment confirmation */}
      <Credenza open={!!deleteCommentId} onOpenChange={(open) => !open && setDeleteCommentId(null)}>
        <CredenzaContent className="border-destructive/30">
          <CredenzaHeader>
            <CredenzaTitle>{t('pm.deleteComment')}</CredenzaTitle>
            <CredenzaDescription>{t('pm.deleteComment')}</CredenzaDescription>
          </CredenzaHeader>
          <CredenzaFooter>
            <Button variant="outline" onClick={() => setDeleteCommentId(null)} className="cursor-pointer">
              {t('buttons.cancel')}
            </Button>
            <Button
              variant="destructive"
              className="cursor-pointer bg-destructive/10 text-destructive border border-destructive/30 hover:bg-destructive hover:text-destructive-foreground transition-colors"
              onClick={handleDeleteComment}
              disabled={deleteCommentMutation.isPending}
            >
              {deleteCommentMutation.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              {t('pm.deleteComment')}
            </Button>
          </CredenzaFooter>
        </CredenzaContent>
      </Credenza>
    </div>
  )
}

export default TaskDetailPage
