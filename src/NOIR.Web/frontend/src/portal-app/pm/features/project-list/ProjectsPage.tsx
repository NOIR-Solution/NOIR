import { useState, useDeferredValue } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import { FolderKanban, Plus, Search, X } from 'lucide-react'
import { usePageContext } from '@/hooks/usePageContext'
import { useEntityUpdateSignal } from '@/hooks/useEntityUpdateSignal'
import { OfflineBanner } from '@/components/OfflineBanner'
import { useUrlDialog } from '@/hooks/useUrlDialog'
import { usePermissions, Permissions } from '@/hooks/usePermissions'
import {
  Badge,
  Button,
  Card,
  CardContent,
  EmptyState,
  Input,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Skeleton,
} from '@uikit'
import { getStatusBadgeClasses } from '@/utils/statusBadge'
import { useProjectsQuery } from '@/portal-app/pm/queries'
import { ProjectDialog } from '@/portal-app/pm/components/ProjectDialog'
import type { ProjectListDto, ProjectStatus } from '@/types/pm'

const statusColorMap: Record<ProjectStatus, 'green' | 'blue' | 'gray' | 'yellow'> = {
  Active: 'green',
  Completed: 'blue',
  Archived: 'gray',
  OnHold: 'yellow',
}

export const ProjectsPage = () => {
  const { t } = useTranslation('common')
  const navigate = useNavigate()
  const { hasPermission } = usePermissions()
  usePageContext('ProjectsPage')

  const canCreate = hasPermission(Permissions.PmProjectsCreate)

  const { isOpen: isCreateOpen, open: openCreate, onOpenChange: onCreateOpenChange } = useUrlDialog({ paramValue: 'create-project' })

  const [search, setSearch] = useState('')
  const [statusFilter, setStatusFilter] = useState<string>('')
  const [page, setPage] = useState(1)
  const pageSize = 12
  const deferredSearch = useDeferredValue(search)

  const { data, isLoading, refetch } = useProjectsQuery({
    page,
    pageSize,
    search: deferredSearch || undefined,
    status: (statusFilter || undefined) as ProjectStatus | undefined,
  })

  const { isReconnecting } = useEntityUpdateSignal({
    entityType: 'Project',
    onCollectionUpdate: refetch,
  })

  const handleProjectClick = (project: ProjectListDto) => {
    navigate(`/portal/projects/${project.id}`)
  }

  return (
    <div className="space-y-6">
      <OfflineBanner visible={isReconnecting} />
      {/* Header */}
      <div className="flex items-center justify-between flex-wrap gap-4">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">{t('pm.projects')}</h1>
          <p className="text-muted-foreground">{t('pm.projectDescription', { defaultValue: 'Manage your projects and tasks' })}</p>
        </div>
        {canCreate && (
          <Button className="group transition-all duration-300 cursor-pointer" onClick={openCreate}>
            <Plus className="h-4 w-4 mr-2 transition-transform group-hover:rotate-90 duration-300" />
            {t('pm.createProject')}
          </Button>
        )}
      </div>

      {/* Filters */}
      <div className="flex items-center gap-3 flex-wrap">
        <div className="relative flex-1 min-w-[200px] max-w-sm">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground pointer-events-none" />
          <Input
            value={search}
            onChange={(e) => { setSearch(e.target.value); setPage(1) }}
            placeholder={t('pm.searchProjects', { defaultValue: 'Search projects...' })}
            className="pl-9 pr-8"
          />
          {search && (
            <Button
              variant="ghost"
              size="icon"
              className="absolute right-1 top-1/2 -translate-y-1/2 h-6 w-6 cursor-pointer"
              onClick={() => { setSearch(''); setPage(1) }}
              aria-label={t('labels.clearSearch', { defaultValue: 'Clear search' })}
            >
              <X className="h-3.5 w-3.5" />
            </Button>
          )}
        </div>
        <Select value={statusFilter} onValueChange={(v) => { setStatusFilter(v === 'all' ? '' : v); setPage(1) }}>
          <SelectTrigger className="w-[150px] cursor-pointer" aria-label={t('pm.status')}>
            <SelectValue placeholder={t('pm.status')} />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all" className="cursor-pointer">{t('labels.all', { defaultValue: 'All' })}</SelectItem>
            <SelectItem value="Active" className="cursor-pointer">{t('statuses.active', { defaultValue: 'Active' })}</SelectItem>
            <SelectItem value="Completed" className="cursor-pointer">{t('statuses.completed', { defaultValue: 'Completed' })}</SelectItem>
            <SelectItem value="OnHold" className="cursor-pointer">{t('statuses.onHold', { defaultValue: 'On Hold' })}</SelectItem>
            <SelectItem value="Archived" className="cursor-pointer">{t('statuses.archived', { defaultValue: 'Archived' })}</SelectItem>
          </SelectContent>
        </Select>
      </div>

      {/* Project grid */}
      {isLoading ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {[...Array(6)].map((_, i) => (
            <Skeleton key={i} className="h-48 rounded-lg" />
          ))}
        </div>
      ) : !data || data.items.length === 0 ? (
        <EmptyState
          icon={FolderKanban}
          title={t('pm.noProjectsFound')}
          description={t('pm.createProject')}
        />
      ) : (
        <>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {data.items.map((project) => {
              const progress = project.taskCount > 0
                ? Math.round((project.completedTaskCount / project.taskCount) * 100)
                : 0

              return (
                <Card
                  key={project.id}
                  className="shadow-sm hover:shadow-lg transition-all duration-300 cursor-pointer"
                  onClick={() => handleProjectClick(project)}
                >
                  <CardContent className="p-5 space-y-3">
                    {/* Header: color dot + name + status */}
                    <div className="flex items-start justify-between gap-2">
                      <div className="flex items-center gap-2 min-w-0">
                        <span
                          className="h-3 w-3 rounded-full flex-shrink-0"
                          style={{ backgroundColor: project.color ?? '#6366f1' }}
                        />
                        <h3 className="text-sm font-semibold truncate">{project.name}</h3>
                      </div>
                      <Badge variant="outline" className={getStatusBadgeClasses(statusColorMap[project.status])}>
                        {t(`statuses.${project.status.toLowerCase()}`, { defaultValue: project.status })}
                      </Badge>
                    </div>

                    {/* Progress bar */}
                    <div className="space-y-1">
                      <div className="flex items-center justify-between text-xs text-muted-foreground">
                        <span>{t('pm.progress')}</span>
                        <span>{progress}%</span>
                      </div>
                      <div className="h-1.5 bg-muted rounded-full overflow-hidden">
                        <div
                          className="h-full bg-primary rounded-full transition-all duration-500"
                          style={{ width: `${progress}%` }}
                        />
                      </div>
                    </div>

                    {/* Stats row */}
                    <div className="flex items-center justify-between text-xs text-muted-foreground">
                      <span>{project.taskCount} {t('pm.taskTitle', { defaultValue: 'tasks' }).toLowerCase()}</span>
                      <span>{project.memberCount} {t('pm.members').toLowerCase()}</span>
                    </div>

                    {/* Owner + due date */}
                    <div className="flex items-center justify-between text-xs text-muted-foreground">
                      {project.ownerName && <span>{project.ownerName}</span>}
                      {project.dueDate && (
                        <span className={new Date(project.dueDate) < new Date() ? 'text-red-500' : ''}>
                          {new Date(project.dueDate).toLocaleDateString()}
                        </span>
                      )}
                    </div>
                  </CardContent>
                </Card>
              )
            })}
          </div>

          {/* Pagination */}
          {data.totalPages > 1 && (
            <div className="flex items-center justify-center gap-2">
              <Button
                variant="outline"
                size="sm"
                disabled={page <= 1}
                onClick={() => setPage(p => p - 1)}
                className="cursor-pointer"
              >
                {t('buttons.previous', { defaultValue: 'Previous' })}
              </Button>
              <span className="text-sm text-muted-foreground">
                {page} / {data.totalPages}
              </span>
              <Button
                variant="outline"
                size="sm"
                disabled={page >= data.totalPages}
                onClick={() => setPage(p => p + 1)}
                className="cursor-pointer"
              >
                {t('buttons.next', { defaultValue: 'Next' })}
              </Button>
            </div>
          )}
        </>
      )}

      {/* Create dialog */}
      <ProjectDialog
        open={isCreateOpen}
        onOpenChange={onCreateOpenChange}
      />
    </div>
  )
}

export default ProjectsPage
