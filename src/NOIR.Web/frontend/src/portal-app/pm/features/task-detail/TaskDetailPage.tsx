import { useState } from 'react'
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
  User,
  Loader2,
  CheckSquare,
} from 'lucide-react'
import {
  Badge,
  Button,
  Card,
  CardContent,
  Credenza,
  CredenzaContent,
  CredenzaDescription,
  CredenzaFooter,
  CredenzaHeader,
  CredenzaTitle,
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

  const { conflictSignal, deletedSignal, dismissConflict, reloadAndRestart, isReconnecting } = useEntityUpdateSignal({
    entityType: 'ProjectTask',
    entityId: id,
    onAutoReload: refetch,
    onNavigateAway: () => navigate('/portal/projects'),
  })

  const [commentText, setCommentText] = useState('')
  const [deleteConfirmOpen, setDeleteConfirmOpen] = useState(false)
  const [deleteCommentId, setDeleteCommentId] = useState<string | null>(null)

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
        toast.success(t('pm.deleteTask'))
        window.history.back()
      },
      onError: (err) => {
        toast.error(err instanceof Error ? err.message : t('errors.unknown'))
      },
    })
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
      <div className="text-center py-12">
        <p className="text-muted-foreground">{t('pm.noTasksFound')}</p>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <OfflineBanner visible={isReconnecting} />
      <EntityConflictDialog signal={conflictSignal} onContinueEditing={dismissConflict} onReloadAndRestart={reloadAndRestart} />
      <EntityDeletedDialog signal={deletedSignal} onGoBack={() => navigate('/portal/projects')} />

      {/* Breadcrumb */}
      <nav className="text-sm text-muted-foreground">
        <ViewTransitionLink to="/portal/projects" className="hover:text-foreground transition-colors">
          {t('pm.projects')}
        </ViewTransitionLink>
        <span className="mx-2">/</span>
        <ViewTransitionLink to={`/portal/projects/${task.projectId}`} className="hover:text-foreground transition-colors">
          {t('pm.projectDetails')}
        </ViewTransitionLink>
        <span className="mx-2">/</span>
        <span className="text-foreground">{task.taskNumber}</span>
      </nav>

      {/* Two-column layout */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Main content (left 2/3) */}
        <div className="lg:col-span-2 space-y-6">
          {/* Title */}
          <h1 className="text-2xl font-bold tracking-tight">{task.title}</h1>

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
          {task.subtasks.length > 0 && (
            <div>
              <h3 className="text-sm font-semibold mb-3 flex items-center gap-2">
                <CheckSquare className="h-4 w-4" />
                {t('pm.subtasks')} ({task.subtasks.filter(s => s.status === 'Done').length}/{task.subtasks.length})
              </h3>
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
            </div>
          )}

          {/* Comments */}
          <div>
            <h3 className="text-sm font-semibold mb-3 flex items-center gap-2">
              <MessageSquare className="h-4 w-4" />
              {t('pm.comments')} ({task.comments.length})
            </h3>

            {/* Add comment */}
            <div className="flex gap-2 mb-4">
              <Textarea
                value={commentText}
                onChange={(e) => setCommentText(e.target.value)}
                placeholder={t('pm.commentPlaceholder')}
                rows={2}
                className="flex-1"
              />
              <Button
                size="icon"
                className="cursor-pointer self-end"
                onClick={handleAddComment}
                disabled={addCommentMutation.isPending || !commentText.trim()}
                aria-label={t('pm.addComment')}
              >
                {addCommentMutation.isPending ? (
                  <Loader2 className="h-4 w-4 animate-spin" />
                ) : (
                  <Send className="h-4 w-4" />
                )}
              </Button>
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
                        <span className="text-xs text-muted-foreground italic">({t('pm.editComment', { defaultValue: 'edited' })})</span>
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
            <CardContent className="p-4 space-y-4">
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

              {/* Assignee */}
              <div className="flex items-center gap-2">
                <User className="h-4 w-4 text-muted-foreground" />
                <div>
                  <p className="text-xs text-muted-foreground">{t('pm.assignee')}</p>
                  <p className="text-sm">{task.assigneeName || '-'}</p>
                </div>
              </div>

              {/* Reporter */}
              <div className="flex items-center gap-2">
                <User className="h-4 w-4 text-muted-foreground" />
                <div>
                  <p className="text-xs text-muted-foreground">{t('pm.reporter')}</p>
                  <p className="text-sm">{task.reporterName || '-'}</p>
                </div>
              </div>

              {/* Due date */}
              {task.dueDate && (
                <div className="flex items-center gap-2">
                  <Calendar className="h-4 w-4 text-muted-foreground" />
                  <div>
                    <p className="text-xs text-muted-foreground">{t('pm.dueDate')}</p>
                    <p className={`text-sm ${new Date(task.dueDate) < new Date() ? 'text-red-500' : ''}`}>
                      {formatDate(task.dueDate)}
                    </p>
                  </div>
                </div>
              )}

              {/* Hours */}
              {(task.estimatedHours != null || task.actualHours != null) && (
                <div className="flex items-center gap-2">
                  <Clock className="h-4 w-4 text-muted-foreground" />
                  <div>
                    <p className="text-xs text-muted-foreground">{t('pm.estimatedHours')} / {t('pm.actualHours')}</p>
                    <p className="text-sm">{task.estimatedHours ?? '-'}h / {task.actualHours ?? '-'}h</p>
                  </div>
                </div>
              )}

              {/* Labels */}
              {task.labels.length > 0 && (
                <div>
                  <p className="text-xs text-muted-foreground mb-1">{t('pm.labels')}</p>
                  <div className="flex flex-wrap gap-1">
                    {task.labels.map((label) => (
                      <Badge
                        key={label.id}
                        variant="outline"
                        style={{ borderColor: label.color, color: label.color }}
                      >
                        {label.name}
                      </Badge>
                    ))}
                  </div>
                </div>
              )}

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

              {/* Delete button */}
              <Button
                variant="destructive"
                className="w-full cursor-pointer bg-destructive/10 text-destructive border border-destructive/30 hover:bg-destructive hover:text-destructive-foreground transition-colors"
                onClick={() => setDeleteConfirmOpen(true)}
              >
                <Trash2 className="h-4 w-4 mr-2" />
                {t('pm.deleteTask')}
              </Button>
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
