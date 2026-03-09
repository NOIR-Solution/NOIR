import { useState, useCallback } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams, useNavigate } from 'react-router-dom'
import {
  Users,
  Calendar,
  Lock,
  Globe,
  Building2,
  Info,
  Archive,
  Loader2,
  MoreHorizontal,
  UserPlus,
  Tag,
  Pencil,
  KanbanSquare,
  LayoutList,
} from 'lucide-react'
import { useEntityUpdateSignal } from '@/hooks/useEntityUpdateSignal'
import { OfflineBanner } from '@/components/OfflineBanner'
import { EntityConflictDialog } from '@/components/EntityConflictDialog'
import { EntityDeletedDialog } from '@/components/EntityDeletedDialog'
import { ViewTransitionLink } from '@/components/navigation/ViewTransitionLink'
import { useUrlTab } from '@/hooks/useUrlTab'
import { usePageContext } from '@/hooks/usePageContext'
import {
  Badge,
  Button,
  Credenza,
  CredenzaContent,
  CredenzaDescription,
  CredenzaFooter,
  CredenzaHeader,
  CredenzaTitle,
  CredenzaBody,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
  Skeleton,
  Tabs,
  TabsContent,
  TabsList,
  TabsTrigger,
} from '@uikit'
import { getStatusBadgeClasses } from '@/utils/statusBadge'
import { useProjectByCodeQuery, useProjectQuery, useArchiveProject } from '@/portal-app/pm/queries'
import { KanbanBoard } from '@/portal-app/pm/components/KanbanBoard'
import { TaskDialog } from '@/portal-app/pm/components/TaskDialog'
import { MembersManager } from '@/portal-app/pm/components/MembersManager'
import { LabelManager } from '@/portal-app/pm/components/LabelManager'
import { ProjectMemberAvatars } from '@/portal-app/pm/components/ProjectMemberAvatars'
import { ProjectDialog } from '@/portal-app/pm/components/ProjectDialog'
import { TaskListView } from './TaskListView'
import { ArchivedTasksPanel } from '@/portal-app/pm/components/ArchivedTasksPanel'
import { TaskDetailModal } from '@/portal-app/pm/components/TaskDetailModal'
import { toast } from 'sonner'
import type { ProjectStatus } from '@/types/pm'

const statusColorMap: Record<ProjectStatus, 'green' | 'blue' | 'gray' | 'yellow'> = {
  Active: 'green',
  Completed: 'blue',
  Archived: 'gray',
  OnHold: 'yellow',
}

export const ProjectDetailPage = () => {
  const { t } = useTranslation('common')
  const { id: projectParam } = useParams<{ id: string }>()

  const { activeTab, handleTabChange, isPending } = useUrlTab({ defaultTab: 'board' })
  usePageContext('ProjectDetailPage')

  const navigate = useNavigate()

  // Support both old GUID URLs (bookmarks) and new project-code URLs
  const isGuid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(projectParam ?? '')
  const byGuidQuery = useProjectQuery(isGuid ? projectParam : undefined)
  const byCodeQuery = useProjectByCodeQuery(!isGuid ? projectParam : undefined)
  const { data: project, isLoading, refetch } = isGuid ? byGuidQuery : byCodeQuery

  const { conflictSignal, deletedSignal, dismissConflict, reloadAndRestart, isReconnecting } = useEntityUpdateSignal({
    entityType: 'Project',
    entityId: project?.id,
    onAutoReload: refetch,
    onNavigateAway: () => navigate('/portal/projects'),
  })

  const [taskDialogOpen, setTaskDialogOpen] = useState(false)
  const [defaultColumnId, setDefaultColumnId] = useState<string>('')
  const [editDialogOpen, setEditDialogOpen] = useState(false)
  const [archiveConfirmOpen, setArchiveConfirmOpen] = useState(false)
  const [membersDialogOpen, setMembersDialogOpen] = useState(false)
  const [labelsDialogOpen, setLabelsDialogOpen] = useState(false)
  const [listDetailTaskId, setListDetailTaskId] = useState<string | null>(null)
  const [archivedDetailTaskId, setArchivedDetailTaskId] = useState<string | null>(null)

  const archiveMutation = useArchiveProject()

  const handleCreateTask = useCallback((columnId: string) => {
    setDefaultColumnId(columnId)
    setTaskDialogOpen(true)
  }, [])

  const handleArchive = () => {
    if (!project) return
    archiveMutation.mutate(project.id, {
      onSuccess: () => {
        toast.success(t('pm.archiveProject'))
        navigate('/portal/projects')
      },
      onError: (err) => toast.error(err instanceof Error ? err.message : t('errors.unknown', { defaultValue: 'Something went wrong' })),
    })
  }

  if (isLoading) {
    return (
      <div className="space-y-6">
        <Skeleton className="h-8 w-64" />
        <Skeleton className="h-96 w-full" />
      </div>
    )
  }

  if (!project) {
    return (
      <div className="text-center py-12">
        <p className="text-muted-foreground">{t('pm.noProjectsFound')}</p>
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
        <span className="text-foreground">{project.name}</span>
      </nav>

      {/* Header */}
      <div className="flex items-center justify-between flex-wrap gap-4">
        <div className="flex items-center gap-3">
          <div
            className="h-10 w-10 rounded-xl flex-shrink-0 flex items-center justify-center text-white font-bold text-lg select-none shadow-sm"
            style={{
              background: `linear-gradient(135deg, ${project.color ?? '#6366f1'} 0%, ${project.color ? project.color + 'cc' : '#4f46e5'} 100%)`,
            }}
            aria-hidden="true"
          >
            {project.name.charAt(0).toUpperCase()}
          </div>
          <h1 className="text-2xl font-bold tracking-tight">{project.name}</h1>
          <Badge variant="outline" className={getStatusBadgeClasses(statusColorMap[project.status])}>
            {t(`statuses.${project.status.toLowerCase()}`, { defaultValue: project.status })}
          </Badge>
        </div>
        <div className="flex items-center gap-2">
          {project.members.length > 0 && (
            <ProjectMemberAvatars
              members={project.members}
              onClickMore={() => setMembersDialogOpen(true)}
            />
          )}
          <Button
            variant="outline"
            size="sm"
            className="cursor-pointer gap-1.5"
            onClick={() => setMembersDialogOpen(true)}
          >
            <UserPlus className="h-3.5 w-3.5" />
            {t('pm.share', { defaultValue: 'Share' })}
          </Button>
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="outline" size="icon" className="h-8 w-8 cursor-pointer" aria-label={t('pm.boardMenu', { defaultValue: 'Board menu' })}>
                <MoreHorizontal className="h-4 w-4" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem className="cursor-pointer gap-2" onClick={() => setEditDialogOpen(true)}>
                <Pencil className="h-3.5 w-3.5" />
                {t('pm.editProject')}
              </DropdownMenuItem>
              <DropdownMenuItem className="cursor-pointer gap-2" onClick={() => setLabelsDialogOpen(true)}>
                <Tag className="h-3.5 w-3.5" />
                {t('pm.labels', { defaultValue: 'Labels' })}
              </DropdownMenuItem>
              <DropdownMenuSeparator />
              <DropdownMenuItem
                className="cursor-pointer gap-2 text-destructive focus:text-destructive"
                onClick={() => setArchiveConfirmOpen(true)}
              >
                <Archive className="h-3.5 w-3.5" />
                {t('pm.archiveProject', { defaultValue: 'Archive Project' })}
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      </div>

      {/* Stats bar */}
      <div className="flex flex-wrap items-center gap-x-6 gap-y-1.5 text-sm text-muted-foreground py-2 border-b border-border/30">
        <span className="flex items-center gap-1.5">
          <Users className="h-3.5 w-3.5" />
          {project.members.length} {t('pm.members', { defaultValue: 'members' }).toLowerCase()}
        </span>
        {project.dueDate && (
          <span
            className={`flex items-center gap-1.5 ${
              new Date(project.dueDate) < new Date() ? 'text-red-500 font-medium' : ''
            }`}
          >
            <Calendar className="h-3.5 w-3.5" />
            {new Date(project.dueDate).toLocaleDateString()}
          </span>
        )}
        {project.description && (
          <span className="text-xs text-muted-foreground/80 truncate max-w-xs hidden md:flex items-center gap-1.5">
            <Info className="h-3.5 w-3.5 flex-shrink-0" />
            {project.description.slice(0, 80)}
            {project.description.length > 80 ? '...' : ''}
          </span>
        )}
        <span className="flex items-center gap-1.5 ml-auto">
          {project.visibility === 'Private' ? (
            <Lock className="h-3.5 w-3.5" />
          ) : project.visibility === 'Public' ? (
            <Globe className="h-3.5 w-3.5" />
          ) : (
            <Building2 className="h-3.5 w-3.5" />
          )}
          <span className="text-xs">
            {t(`pm.visibility${project.visibility}`, { defaultValue: project.visibility ?? '' })}
          </span>
        </span>
      </div>

      {/* Tabs */}
      <Tabs value={activeTab} onValueChange={handleTabChange}>
        <TabsList>
          <TabsTrigger value="board" className="cursor-pointer flex items-center gap-1.5">
            <KanbanSquare className="h-3.5 w-3.5" />
            {t('pm.board')}
          </TabsTrigger>
          <TabsTrigger value="list" className="cursor-pointer flex items-center gap-1.5">
            <LayoutList className="h-3.5 w-3.5" />
            {t('pm.listView')}
          </TabsTrigger>
          <TabsTrigger value="archived" className="cursor-pointer flex items-center gap-1.5">
            <Archive className="h-3.5 w-3.5" />
            {t('pm.archived', { defaultValue: 'Archived' })}
          </TabsTrigger>
        </TabsList>

        <div style={{ opacity: isPending ? 0.7 : 1, transition: 'opacity 200ms' }}>
          <TabsContent value="board" className="mt-6">
            <KanbanBoard
              projectId={project.id}
              members={project.members}
              onCreateTask={handleCreateTask}
            />
          </TabsContent>

          <TabsContent value="list" className="mt-6">
            <TaskListView projectId={project.id} members={project.members} onTaskClick={(id) => setListDetailTaskId(id)} />
          </TabsContent>

          <TabsContent value="archived" className="mt-6">
            <ArchivedTasksPanel
              projectId={project.id}
              onViewDetail={(taskId) => setArchivedDetailTaskId(taskId)}
            />
          </TabsContent>
        </div>
      </Tabs>

      {/* List view task detail modal */}
      <TaskDetailModal
        taskId={listDetailTaskId}
        open={!!listDetailTaskId}
        onOpenChange={(open) => { if (!open) setListDetailTaskId(null) }}
      />

      {/* Archived task detail modal */}
      <TaskDetailModal
        taskId={archivedDetailTaskId}
        open={!!archivedDetailTaskId}
        onOpenChange={(open) => { if (!open) setArchivedDetailTaskId(null) }}
      />

      {/* Task create dialog */}
      <TaskDialog
        open={taskDialogOpen}
        onOpenChange={setTaskDialogOpen}
        projectId={project.id}
        columns={project.columns}
        members={project.members}
        defaultColumnId={defaultColumnId}
      />

      {/* Project edit dialog */}
      <ProjectDialog
        open={editDialogOpen}
        onOpenChange={setEditDialogOpen}
        project={project}
      />

      {/* Members dialog */}
      <Credenza open={membersDialogOpen} onOpenChange={setMembersDialogOpen}>
        <CredenzaContent>
          <CredenzaHeader>
            <CredenzaTitle className="flex items-center gap-2">
              <Users className="h-4 w-4" />
              {t('pm.members', { defaultValue: 'Members' })}
            </CredenzaTitle>
            <CredenzaDescription>{project.name}</CredenzaDescription>
          </CredenzaHeader>
          <CredenzaBody>
            <MembersManager projectId={project.id} members={project.members} />
          </CredenzaBody>
        </CredenzaContent>
      </Credenza>

      {/* Labels dialog */}
      <Credenza open={labelsDialogOpen} onOpenChange={setLabelsDialogOpen}>
        <CredenzaContent>
          <CredenzaHeader>
            <CredenzaTitle className="flex items-center gap-2">
              <Tag className="h-4 w-4" />
              {t('pm.labels', { defaultValue: 'Labels' })}
            </CredenzaTitle>
            <CredenzaDescription>{project.name}</CredenzaDescription>
          </CredenzaHeader>
          <CredenzaBody>
            <LabelManager projectId={project.id} />
          </CredenzaBody>
        </CredenzaContent>
      </Credenza>

      {/* Archive confirmation dialog */}
      <Credenza open={archiveConfirmOpen} onOpenChange={setArchiveConfirmOpen}>
        <CredenzaContent className="border-destructive/30">
          <CredenzaHeader>
            <CredenzaTitle>{t('pm.archiveProject', { defaultValue: 'Archive Project' })}</CredenzaTitle>
            <CredenzaDescription>
              {t('pm.archiveConfirmation', { defaultValue: 'Archiving will make this project read-only.' })}
            </CredenzaDescription>
          </CredenzaHeader>
          <CredenzaFooter>
            <Button
              variant="outline"
              onClick={() => setArchiveConfirmOpen(false)}
              className="cursor-pointer"
            >
              {t('buttons.cancel', { defaultValue: 'Cancel' })}
            </Button>
            <Button
              variant="destructive"
              className="cursor-pointer bg-destructive/10 text-destructive border border-destructive/30 hover:bg-destructive hover:text-destructive-foreground transition-colors"
              onClick={handleArchive}
              disabled={archiveMutation.isPending}
            >
              {archiveMutation.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              <Archive className="h-4 w-4 mr-1.5" />
              {t('pm.archiveProject', { defaultValue: 'Archive Project' })}
            </Button>
          </CredenzaFooter>
        </CredenzaContent>
      </Credenza>
    </div>
  )
}

export default ProjectDetailPage
