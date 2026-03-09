import { useState, useDeferredValue, useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate, useSearchParams } from 'react-router-dom'
import {
  FolderKanban,
  Plus,
  Search,
  X,
  LayoutGrid,
  List,
  ArrowUpDown,
  CheckSquare,
  Users,
  Calendar,
  Lock,
  Globe,
  Building2,
  Star,
  Clock,
} from 'lucide-react'
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
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
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

/** Derive a slightly darker shade of a hex color for gradient end stop */
const darkenHex = (hex: string, amount = 30): string => {
  const h = hex.replace('#', '')
  const r = Math.max(0, parseInt(h.slice(0, 2), 16) - amount)
  const g = Math.max(0, parseInt(h.slice(2, 4), 16) - amount)
  const b = Math.max(0, parseInt(h.slice(4, 6), 16) - amount)
  return `#${r.toString(16).padStart(2, '0')}${g.toString(16).padStart(2, '0')}${b.toString(16).padStart(2, '0')}`
}

const VisibilityIcon = ({ visibility }: { visibility: ProjectListDto['visibility'] }) => {
  if (visibility === 'Private') return <Lock className="h-3 w-3" />
  if (visibility === 'Public') return <Globe className="h-3 w-3" />
  return <Building2 className="h-3 w-3" />
}

const ProjectCard = ({
  project,
  onClick,
  t,
}: {
  project: ProjectListDto
  onClick: () => void
  t: (key: string, opts?: Record<string, unknown>) => string
}) => {
  const progress = project.taskCount > 0
    ? Math.round((project.completedTaskCount / project.taskCount) * 100)
    : 0
  const color = project.color ?? '#6366f1'
  const darkColor = darkenHex(color, 35)
  const initial = project.name.charAt(0).toUpperCase()

  const diffDays = project.dueDate
    ? Math.ceil((new Date(project.dueDate).getTime() - Date.now()) / 86400000)
    : null
  const isOverdue = diffDays !== null && diffDays < 0
  const isDueSoon = diffDays !== null && diffDays >= 0 && diffDays <= 3

  return (
    <Card
      className="group cursor-pointer overflow-hidden hover:-translate-y-0.5 hover:shadow-lg transition-all duration-200 border-border/60 py-0 gap-0"
      onClick={onClick}
    >
      {/* Compact gradient header with large initial letter */}
      <div
        className="h-[72px] relative flex items-center justify-center select-none"
        style={{ background: `linear-gradient(135deg, ${color} 0%, ${darkColor} 100%)` }}
      >
        {/* Large initial — the card's visual identity */}
        <span className="text-[40px] font-bold text-white/25 leading-none tracking-tight pointer-events-none">
          {initial}
        </span>

        {/* Visibility — top left */}
        <div
          className="absolute top-2 left-2.5 text-white/60 flex items-center gap-1"
          title={project.visibility}
        >
          <VisibilityIcon visibility={project.visibility} />
        </div>

        {/* Star — top right, hover only */}
        <button
          className="absolute top-1.5 right-1.5 opacity-0 group-hover:opacity-100 text-white/60 hover:text-white cursor-pointer transition-all p-1 rounded hover:bg-white/10"
          onClick={(e) => e.stopPropagation()}
          aria-label={t('pm.starProject', { defaultValue: 'Star project' })}
        >
          <Star className="h-3.5 w-3.5" />
        </button>

        {/* Status badge — bottom right */}
        <div className="absolute bottom-2 right-2.5">
          <span
            className={`text-[10px] font-medium px-1.5 py-0.5 rounded-full border border-white/20 bg-black/20 text-white backdrop-blur-sm`}
          >
            {t(`statuses.${project.status.toLowerCase()}`, { defaultValue: project.status })}
          </span>
        </div>
      </div>

      {/* Card body */}
      <CardContent className="p-4 pt-3 space-y-2">
        {/* Project name */}
        <div>
          <h3 className="font-semibold text-sm leading-snug line-clamp-1 group-hover:text-primary transition-colors">
            {project.name}
          </h3>
          {project.slug && (
            <p className="text-[10px] text-muted-foreground/60 font-mono mt-0.5 tracking-wide">
              {project.slug.toUpperCase()}
            </p>
          )}
        </div>

        {/* Divider */}
        <div className="border-t border-border/40" />

        {/* Stats row */}
        <div className="flex items-center gap-3 text-xs text-muted-foreground">
          <span className="flex items-center gap-1" title={t('pm.tasks', { defaultValue: 'Tasks' })}>
            <CheckSquare className="h-3 w-3 flex-shrink-0" />
            <span className="tabular-nums">{project.completedTaskCount}/{project.taskCount}</span>
          </span>
          {project.memberCount > 0 && (
            <span className="flex items-center gap-1" title={t('pm.members', { defaultValue: 'Members' })}>
              <Users className="h-3 w-3 flex-shrink-0" />
              <span className="tabular-nums">{project.memberCount}</span>
            </span>
          )}
          {project.dueDate && (
            <span
              className={`flex items-center gap-1 ml-auto ${
                isOverdue ? 'text-red-500 font-medium' : isDueSoon ? 'text-amber-500' : ''
              }`}
              title={t('pm.dueDate', { defaultValue: 'Due Date' })}
            >
              {isOverdue ? <Clock className="h-3 w-3 flex-shrink-0" /> : <Calendar className="h-3 w-3 flex-shrink-0" />}
              {new Date(project.dueDate).toLocaleDateString()}
            </span>
          )}
        </div>

        {/* Progress bar — only when there are tasks */}
        {project.taskCount > 0 && (
          <div
            className="h-1 bg-muted rounded-full overflow-hidden"
            title={`${progress}%`}
          >
            <div
              className={`h-full rounded-full transition-all duration-500 ${
                progress === 100 ? 'bg-green-500' : 'bg-primary/60'
              }`}
              style={{ width: `${progress}%` }}
            />
          </div>
        )}
      </CardContent>
    </Card>
  )
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

  const [searchParams, setSearchParams] = useSearchParams()
  const viewMode = (searchParams.get('view') ?? 'grid') as 'grid' | 'list'
  const sortBy = searchParams.get('sort') ?? 'created'

  const setViewMode = (mode: 'grid' | 'list') => {
    setSearchParams(prev => {
      const next = new URLSearchParams(prev)
      if (mode === 'grid') next.delete('view')
      else next.set('view', mode)
      return next
    }, { replace: true })
  }

  const setSortBy = (sort: string) => {
    setSearchParams(prev => {
      const next = new URLSearchParams(prev)
      if (sort === 'created') next.delete('sort')
      else next.set('sort', sort)
      return next
    }, { replace: true })
  }

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

  const sortedItems = useMemo(() => {
    if (!data?.items) return []
    const items = [...data.items]
    switch (sortBy) {
      case 'name':
        return items.sort((a, b) => a.name.localeCompare(b.name))
      case 'progress':
        return items.sort((a, b) => {
          const aP = a.taskCount > 0 ? a.completedTaskCount / a.taskCount : 0
          const bP = b.taskCount > 0 ? b.completedTaskCount / b.taskCount : 0
          return bP - aP
        })
      case 'dueDate':
        return items.sort((a, b) => {
          if (!a.dueDate) return 1
          if (!b.dueDate) return -1
          return new Date(a.dueDate).getTime() - new Date(b.dueDate).getTime()
        })
      case 'updated':
        return items.sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
      default:
        return items.sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
    }
  }, [data?.items, sortBy])

  const handleProjectClick = (project: ProjectListDto) => {
    navigate(`/portal/projects/${project.projectCode}`)
  }

  const totalCount = data?.totalCount ?? 0

  return (
    <div className="space-y-5">
      <OfflineBanner visible={isReconnecting} />

      {/* Header */}
      <div className="flex items-center justify-between flex-wrap gap-4">
        <div>
          <h1 className="text-2xl font-bold tracking-tight flex items-center gap-2">
            {t('pm.projects')}
            {totalCount > 0 && !isLoading && (
              <span className="text-sm font-normal text-muted-foreground bg-muted px-2 py-0.5 rounded-full">
                {totalCount}
              </span>
            )}
          </h1>
          <p className="text-sm text-muted-foreground mt-0.5">
            {t('pm.projectsPageDescription', { defaultValue: 'Your workspaces and team projects' })}
          </p>
        </div>
        {canCreate && (
          <Button className="group transition-all duration-300 cursor-pointer" onClick={openCreate}>
            <Plus className="h-4 w-4 mr-2 transition-transform group-hover:rotate-90 duration-300" />
            {t('pm.createProject')}
          </Button>
        )}
      </div>

      {/* Filters + view controls */}
      <div className="flex items-center gap-2.5 flex-wrap">
        <div className="relative flex-1 min-w-[200px] max-w-[280px]">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground pointer-events-none" />
          <Input
            value={search}
            onChange={(e) => { setSearch(e.target.value); setPage(1) }}
            placeholder={t('pm.searchProjects', { defaultValue: 'Search projects...' })}
            className="pl-9 pr-8 h-8 text-sm"
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
          <SelectTrigger className="w-[130px] h-8 text-sm cursor-pointer" aria-label={t('pm.status')}>
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

        <Select value={sortBy} onValueChange={(v) => { setSortBy(v); setPage(1) }}>
          <SelectTrigger className="w-[140px] h-8 text-sm cursor-pointer">
            <ArrowUpDown className="h-3.5 w-3.5 mr-1.5 flex-shrink-0" />
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="created" className="cursor-pointer">{t('pm.sortByCreated', { defaultValue: 'Newest' })}</SelectItem>
            <SelectItem value="name" className="cursor-pointer">{t('pm.sortByName', { defaultValue: 'Name A-Z' })}</SelectItem>
            <SelectItem value="progress" className="cursor-pointer">{t('pm.sortByProgress', { defaultValue: 'Progress' })}</SelectItem>
            <SelectItem value="dueDate" className="cursor-pointer">{t('pm.sortByDueDate', { defaultValue: 'Due Date' })}</SelectItem>
            <SelectItem value="updated" className="cursor-pointer">{t('pm.sortByUpdated', { defaultValue: 'Last Updated' })}</SelectItem>
          </SelectContent>
        </Select>

        {/* View toggle */}
        <div className="flex items-center rounded-md border p-0.5 bg-muted/30 ml-auto">
          <button
            onClick={() => setViewMode('grid')}
            className={`p-1.5 rounded cursor-pointer transition-colors ${viewMode === 'grid' ? 'bg-background shadow-sm' : 'hover:bg-muted'}`}
            aria-label={t('pm.gridView', { defaultValue: 'Grid view' })}
          >
            <LayoutGrid className="h-3.5 w-3.5" />
          </button>
          <button
            onClick={() => setViewMode('list')}
            className={`p-1.5 rounded cursor-pointer transition-colors ${viewMode === 'list' ? 'bg-background shadow-sm' : 'hover:bg-muted'}`}
            aria-label={t('pm.listView', { defaultValue: 'List view' })}
          >
            <List className="h-3.5 w-3.5" />
          </button>
        </div>
      </div>

      {/* Content */}
      {isLoading ? (
        viewMode === 'list' ? (
          <div className="space-y-2">
            {[...Array(6)].map((_, i) => (
              <Skeleton key={i} className="h-12 rounded-lg" />
            ))}
          </div>
        ) : (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
            {[...Array(8)].map((_, i) => (
              <Skeleton key={i} className="h-[168px] rounded-xl" />
            ))}
          </div>
        )
      ) : !data || sortedItems.length === 0 ? (
        <EmptyState
          icon={FolderKanban}
          title={t('pm.noProjectsFound')}
          description={
            search || statusFilter
              ? t('pm.noProjectsMatchFilter', { defaultValue: 'Try adjusting your search or filters' })
              : t('pm.noProjectsDescription', { defaultValue: 'Create your first project to get started' })
          }
        />
      ) : (
        <>
          {viewMode === 'list' ? (
            <div className="rounded-lg border overflow-hidden">
              <Table>
                <TableHeader>
                  <TableRow className="bg-muted/30">
                    <TableHead className="font-semibold">{t('pm.projectName', { defaultValue: 'Project' })}</TableHead>
                    <TableHead className="font-semibold">{t('pm.status', { defaultValue: 'Status' })}</TableHead>
                    <TableHead className="font-semibold">{t('pm.progress', { defaultValue: 'Progress' })}</TableHead>
                    <TableHead className="font-semibold">{t('pm.members', { defaultValue: 'Members' })}</TableHead>
                    <TableHead className="font-semibold">{t('pm.dueDate', { defaultValue: 'Due Date' })}</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {sortedItems.map((project) => {
                    const progress = project.taskCount > 0
                      ? Math.round((project.completedTaskCount / project.taskCount) * 100)
                      : 0
                    const isOverdue = project.dueDate && new Date(project.dueDate) < new Date()
                    return (
                      <TableRow
                        key={project.id}
                        className="cursor-pointer hover:bg-muted/30 transition-colors"
                        onClick={() => handleProjectClick(project)}
                      >
                        <TableCell>
                          <div className="flex items-center gap-3">
                            {/* Color initial avatar */}
                            <div
                              className="h-8 w-8 rounded-lg flex-shrink-0 flex items-center justify-center text-white font-bold text-sm select-none"
                              style={{ background: `linear-gradient(135deg, ${project.color ?? '#6366f1'} 0%, ${darkenHex(project.color ?? '#6366f1', 35)} 100%)` }}
                            >
                              {project.name.charAt(0).toUpperCase()}
                            </div>
                            <div>
                              <span className="font-medium text-sm">{project.name}</span>
                              {project.slug && (
                                <p className="text-[10px] text-muted-foreground font-mono">
                                  {project.slug.toUpperCase()}
                                </p>
                              )}
                            </div>
                          </div>
                        </TableCell>
                        <TableCell>
                          <Badge variant="outline" className={getStatusBadgeClasses(statusColorMap[project.status])}>
                            {t(`statuses.${project.status.toLowerCase()}`, { defaultValue: project.status })}
                          </Badge>
                        </TableCell>
                        <TableCell>
                          <div className="flex items-center gap-2 min-w-[120px]">
                            <div className="flex-1 h-1.5 bg-muted rounded-full overflow-hidden">
                              <div
                                className={`h-full rounded-full ${progress === 100 ? 'bg-green-500' : 'bg-primary'}`}
                                style={{ width: `${progress}%` }}
                              />
                            </div>
                            <span className="text-xs text-muted-foreground w-8 text-right tabular-nums">{progress}%</span>
                          </div>
                        </TableCell>
                        <TableCell className="text-sm text-muted-foreground">
                          <span className="flex items-center gap-1">
                            <Users className="h-3.5 w-3.5" />
                            {project.memberCount}
                          </span>
                        </TableCell>
                        <TableCell className={`text-sm ${isOverdue ? 'text-red-500 font-medium' : 'text-muted-foreground'}`}>
                          {project.dueDate ? new Date(project.dueDate).toLocaleDateString() : '—'}
                        </TableCell>
                      </TableRow>
                    )
                  })}
                </TableBody>
              </Table>
            </div>
          ) : (
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
              {sortedItems.map((project) => (
                <ProjectCard
                  key={project.id}
                  project={project}
                  onClick={() => handleProjectClick(project)}
                  t={t}
                />
              ))}
            </div>
          )}

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
              <span className="text-sm text-muted-foreground tabular-nums">
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
