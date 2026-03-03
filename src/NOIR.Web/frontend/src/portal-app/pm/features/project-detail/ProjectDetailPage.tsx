import { useState, useCallback } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams, useNavigate } from 'react-router-dom'
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
  Skeleton,
  Tabs,
  TabsContent,
  TabsList,
  TabsTrigger,
} from '@uikit'
import { getStatusBadgeClasses } from '@/utils/statusBadge'
import { useProjectQuery } from '@/portal-app/pm/queries'
import { KanbanBoard } from '@/portal-app/pm/components/KanbanBoard'
import { TaskDialog } from '@/portal-app/pm/components/TaskDialog'
import { MembersManager } from '@/portal-app/pm/components/MembersManager'
import { ColumnManager } from '@/portal-app/pm/components/ColumnManager'
import { LabelManager } from '@/portal-app/pm/components/LabelManager'
import { ProjectMemberAvatars } from '@/portal-app/pm/components/ProjectMemberAvatars'
import { ProjectDialog } from '@/portal-app/pm/components/ProjectDialog'
import { TaskListView } from './TaskListView'
import type { ProjectStatus } from '@/types/pm'

const statusColorMap: Record<ProjectStatus, 'green' | 'blue' | 'gray' | 'yellow'> = {
  Active: 'green',
  Completed: 'blue',
  Archived: 'gray',
  OnHold: 'yellow',
}

export const ProjectDetailPage = () => {
  const { t } = useTranslation('common')
  const { id } = useParams<{ id: string }>()
  const { activeTab, handleTabChange, isPending } = useUrlTab({ defaultTab: 'board' })
  usePageContext('ProjectDetailPage')

  const navigate = useNavigate()
  const { data: project, isLoading, refetch } = useProjectQuery(id)

  const { conflictSignal, deletedSignal, dismissConflict, reloadAndRestart, isReconnecting } = useEntityUpdateSignal({
    entityType: 'Project',
    entityId: id,
    onAutoReload: refetch,
    onNavigateAway: () => navigate('/portal/projects'),
  })

  const [taskDialogOpen, setTaskDialogOpen] = useState(false)
  const [defaultColumnId, setDefaultColumnId] = useState<string>('')
  const [editDialogOpen, setEditDialogOpen] = useState(false)

  const handleCreateTask = useCallback((columnId: string) => {
    setDefaultColumnId(columnId)
    setTaskDialogOpen(true)
  }, [])

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
          <span
            className="h-4 w-4 rounded-full"
            style={{ backgroundColor: project.color ?? '#6366f1' }}
          />
          <h1 className="text-2xl font-bold tracking-tight">{project.name}</h1>
          <Badge variant="outline" className={getStatusBadgeClasses(statusColorMap[project.status])}>
            {t(`statuses.${project.status.toLowerCase()}`, { defaultValue: project.status })}
          </Badge>
        </div>
        <div className="flex items-center gap-4">
          {project.members.length > 0 && (
            <ProjectMemberAvatars
              members={project.members}
              onClickMore={() => handleTabChange('settings')}
            />
          )}
          <Button
            variant="outline"
            className="cursor-pointer"
            onClick={() => setEditDialogOpen(true)}
          >
            {t('pm.editProject')}
          </Button>
        </div>
      </div>

      {/* Tabs */}
      <Tabs value={activeTab} onValueChange={handleTabChange}>
        <TabsList>
          <TabsTrigger value="board" className="cursor-pointer">{t('pm.board')}</TabsTrigger>
          <TabsTrigger value="list" className="cursor-pointer">{t('pm.listView')}</TabsTrigger>
          <TabsTrigger value="settings" className="cursor-pointer">{t('pm.settings')}</TabsTrigger>
        </TabsList>

        <div style={{ opacity: isPending ? 0.7 : 1, transition: 'opacity 200ms' }}>
          <TabsContent value="board" className="mt-6">
            <KanbanBoard
              projectId={project.id}
              onCreateTask={handleCreateTask}
            />
          </TabsContent>

          <TabsContent value="list" className="mt-6">
            <TaskListView projectId={project.id} />
          </TabsContent>

          <TabsContent value="settings" className="mt-6 space-y-8">
            {/* Project info */}
            {project.description && (
              <div>
                <h3 className="text-sm font-semibold mb-2">{t('pm.projectDescription')}</h3>
                <p className="text-sm text-muted-foreground">{project.description}</p>
              </div>
            )}

            {/* Members */}
            <MembersManager projectId={project.id} members={project.members} />

            {/* Columns */}
            <ColumnManager projectId={project.id} columns={project.columns} />

            {/* Labels */}
            <LabelManager projectId={project.id} />
          </TabsContent>
        </div>
      </Tabs>

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
    </div>
  )
}

export default ProjectDetailPage
