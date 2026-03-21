import { useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { useDelayedLoading } from '@/hooks/useDelayedLoading'
import {
  Eye,
  FolderKanban,
  Plus,
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
import { createColumnHelper } from '@tanstack/react-table'
import type { ColumnDef, SortingState } from '@tanstack/react-table'
import { OfflineBanner } from '@/components/OfflineBanner'
import { useUrlDialog } from '@/hooks/useUrlDialog'
import { usePageContext } from '@/hooks/usePageContext'
import { useEntityUpdateSignal } from '@/hooks/useEntityUpdateSignal'
import { useTableParams } from '@/hooks/useTableParams'
import { useEnterpriseTable } from '@/hooks/useEnterpriseTable'
import { createActionsColumn, createFullAuditColumns } from '@/lib/table/columnHelpers'
import { usePermissions, Permissions } from '@/hooks/usePermissions'
import { useRegionalSettings } from '@/contexts/RegionalSettingsContext'
import { useRowHighlight } from '@/hooks/useRowHighlight'
import {
  Badge,
  Button,
  Card,
  CardContent,
  DataTable,
  DataTableColumnHeader,
  DataTablePagination,
  DataTableToolbar,
  DropdownMenuItem,
  EmptyState,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Skeleton,
  Tooltip,
  TooltipContent,
  TooltipTrigger,
  ViewModeToggle,
  type ViewModeOption,
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
  formatDate,
}: {
  project: ProjectListDto
  onClick: () => void
  t: (key: string, opts?: Record<string, unknown>) => string
  formatDate: (date: Date | string) => string
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
      <div
        className="h-[72px] relative flex items-center justify-center select-none"
        style={{ background: `linear-gradient(135deg, ${color} 0%, ${darkColor} 100%)` }}
      >
        <span className="text-[40px] font-bold text-white/25 leading-none tracking-tight pointer-events-none">
          {initial}
        </span>
        <div
          className="absolute top-2 left-2.5 text-white/60 flex items-center gap-1"
          title={t(`pm.visibility${project.visibility}`, { defaultValue: project.visibility })}
        >
          <VisibilityIcon visibility={project.visibility} />
        </div>
        <button
          className="absolute top-1.5 right-1.5 opacity-0 group-hover:opacity-100 text-white/60 hover:text-white cursor-pointer transition-all p-1 rounded hover:bg-white/10"
          onClick={(e) => e.stopPropagation()}
          aria-label={t('pm.starProject', { defaultValue: 'Star project' })}
        >
          <Star className="h-3.5 w-3.5" />
        </button>
        <div className="absolute bottom-2 right-2.5">
          <span
            className={`text-[10px] font-medium px-1.5 py-0.5 rounded-full border border-white/20 bg-black/20 text-white backdrop-blur-sm`}
          >
            {t(`statuses.${project.status.charAt(0).toLowerCase() + project.status.slice(1)}`, { defaultValue: project.status })}
          </span>
        </div>
      </div>
      <CardContent className="p-4 pt-3 space-y-2">
        <div>
          <h3 className="font-semibold text-sm leading-snug line-clamp-1 group-hover:text-primary transition-colors">
            {project.name}
          </h3>
          {project.slug && (
            <p className="text-[10px] text-muted-foreground font-mono mt-0.5 tracking-wide">
              {project.slug.toUpperCase()}
            </p>
          )}
        </div>
        <div className="border-t border-border/40" />
        <div className="flex items-center gap-3 text-xs text-muted-foreground">
          <Tooltip>
            <TooltipTrigger asChild>
              <span className="flex items-center gap-1">
                <CheckSquare className="h-3 w-3 flex-shrink-0" />
                <span className="tabular-nums">{project.completedTaskCount}/{project.taskCount}</span>
              </span>
            </TooltipTrigger>
            <TooltipContent side="bottom">{t('pm.tasks', { defaultValue: 'Tasks' })}</TooltipContent>
          </Tooltip>
          {project.memberCount > 0 && (
            <Tooltip>
              <TooltipTrigger asChild>
                <span className="flex items-center gap-1">
                  <Users className="h-3 w-3 flex-shrink-0" />
                  <span className="tabular-nums">{project.memberCount}</span>
                </span>
              </TooltipTrigger>
              <TooltipContent side="bottom">{t('pm.members', { defaultValue: 'Members' })}</TooltipContent>
            </Tooltip>
          )}
          {project.dueDate && (
            <Tooltip>
              <TooltipTrigger asChild>
                <span
                  className={`flex items-center gap-1 ml-auto ${
                    isOverdue ? 'text-red-500 font-medium' : isDueSoon ? 'text-amber-500' : ''
                  }`}
                >
                  {isOverdue ? <Clock className="h-3 w-3 flex-shrink-0" /> : <Calendar className="h-3 w-3 flex-shrink-0" />}
                  {formatDate(project.dueDate)}
                </span>
              </TooltipTrigger>
              <TooltipContent side="bottom">{t('pm.dueDate', { defaultValue: 'Due Date' })}</TooltipContent>
            </Tooltip>
          )}
        </div>
        {project.taskCount > 0 && (
          <Tooltip>
            <TooltipTrigger asChild>
              <div className="h-1 bg-muted rounded-full overflow-hidden">
                <div
                  className={`h-full rounded-full transition-all duration-500 ${
                    progress === 100 ? 'bg-green-500' : 'bg-primary/60'
                  }`}
                  style={{ width: `${progress}%` }}
                />
              </div>
            </TooltipTrigger>
            <TooltipContent side="bottom">{progress}%</TooltipContent>
          </Tooltip>
        )}
      </CardContent>
    </Card>
  )
}

const ch = createColumnHelper<ProjectListDto>()

export const ProjectsPage = () => {
  const { t } = useTranslation('common')
  const navigate = useNavigate()
  const { hasPermission } = usePermissions()
  const { formatDate, formatDateTime } = useRegionalSettings()
  usePageContext('ProjectsPage')

  const { getRowAnimationClass } = useRowHighlight()

  const canCreate = hasPermission(Permissions.PmProjectsCreate)

  const { isOpen: isCreateOpen, open: openCreate, onOpenChange: onCreateOpenChange } = useUrlDialog({ paramValue: 'create-project' })

  const [searchParams, setSearchParams] = useSearchParams()
  const viewMode = (searchParams.get('view') ?? 'grid') as 'grid' | 'list'
  const viewModeOptions: ViewModeOption<'grid' | 'list'>[] = useMemo(() => [
    { value: 'grid', label: t('labels.grid', 'Grid'), icon: LayoutGrid, ariaLabel: t('pm.gridView', { defaultValue: 'Grid view' }) },
    { value: 'list', label: t('labels.list', 'List'), icon: List, ariaLabel: t('pm.listView', { defaultValue: 'List view' }) },
  ], [t])
  const sortBy = searchParams.get('sort') ?? 'created'

  const {
    params,
    searchInput,
    setSearchInput,
    isSearchStale,
    isFilterPending,
    setFilter,
    setSorting,
    setPage,
    setPageSize,
    defaultPageSize,
  } = useTableParams<{ status?: ProjectStatus }>({ defaultPageSize: 12, tableKey: 'projects' })

  const { data, isLoading, isPlaceholderData, refetch } = useProjectsQuery({
    ...params,
    status: params.filters.status,
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
    setPage(1)
  }

  const setStatusFilter = (value: string) => setFilter('status', value === 'all' ? undefined : (value as ProjectStatus))

  const columns = useMemo((): ColumnDef<ProjectListDto, unknown>[] => [
    createActionsColumn<ProjectListDto>((project) => (
      <DropdownMenuItem className="cursor-pointer" onClick={() => handleProjectClick(project)}>
        <Eye className="h-4 w-4 mr-2" />
        {t('labels.viewDetails', 'View Details')}
      </DropdownMenuItem>
    )),
    ch.accessor('name', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('pm.projectName', { defaultValue: 'Project' })} />,
      meta: { label: t('pm.projectName', { defaultValue: 'Project' }) },
      cell: ({ row }) => (
        <div className="flex items-center gap-3">
          <div
            className="h-8 w-8 rounded-lg flex-shrink-0 flex items-center justify-center text-white font-bold text-sm select-none"
            style={{ background: `linear-gradient(135deg, ${row.original.color ?? '#6366f1'} 0%, ${darkenHex(row.original.color ?? '#6366f1', 35)} 100%)` }}
          >
            {row.original.name.charAt(0).toUpperCase()}
          </div>
          <div>
            <span className="font-medium text-sm">{row.original.name}</span>
            {row.original.slug && (
              <p className="text-[10px] text-muted-foreground font-mono">
                {row.original.slug.toUpperCase()}
              </p>
            )}
          </div>
        </div>
      ),
    }) as ColumnDef<ProjectListDto, unknown>,
    ch.accessor('status', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('pm.status', { defaultValue: 'Status' })} />,
      meta: { label: t('pm.status', { defaultValue: 'Status' }) },
      cell: ({ row }) => (
        <Badge variant="outline" className={getStatusBadgeClasses(statusColorMap[row.original.status])}>
          {t(`statuses.${row.original.status.charAt(0).toLowerCase() + row.original.status.slice(1)}`, { defaultValue: row.original.status })}
        </Badge>
      ),
    }) as ColumnDef<ProjectListDto, unknown>,
    ch.accessor((row) => row.taskCount > 0 ? Math.round((row.completedTaskCount / row.taskCount) * 100) : 0, {
      id: 'progress',
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('pm.progress', { defaultValue: 'Progress' })} />,
      meta: { label: t('pm.progress', { defaultValue: 'Progress' }) },
      enableSorting: false,
      cell: ({ row }) => {
        const progress = row.original.taskCount > 0
          ? Math.round((row.original.completedTaskCount / row.original.taskCount) * 100)
          : 0
        return (
          <div className="flex items-center gap-2 min-w-[120px]">
            <div className="flex-1 h-1.5 bg-muted rounded-full overflow-hidden">
              <div
                className={`h-full rounded-full ${progress === 100 ? 'bg-green-500' : 'bg-primary'}`}
                style={{ width: `${progress}%` }}
              />
            </div>
            <span className="text-xs text-muted-foreground w-8 text-right tabular-nums">{progress}%</span>
          </div>
        )
      },
    }) as ColumnDef<ProjectListDto, unknown>,
    ch.accessor('memberCount', {
      id: 'members',
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('pm.members', { defaultValue: 'Members' })} />,
      meta: { label: t('pm.members', { defaultValue: 'Members' }) },
      cell: ({ row }) => (
        <span className="flex items-center gap-1 text-sm text-muted-foreground">
          <Users className="h-3.5 w-3.5" />
          {row.original.memberCount}
        </span>
      ),
    }) as ColumnDef<ProjectListDto, unknown>,
    ch.accessor('dueDate', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('pm.dueDate', { defaultValue: 'Due Date' })} />,
      meta: { label: t('pm.dueDate', { defaultValue: 'Due Date' }) },
      cell: ({ row }) => {
        const isOverdue = row.original.dueDate && new Date(row.original.dueDate) < new Date()
        return (
          <span className={`text-sm ${isOverdue ? 'text-red-500 font-medium' : 'text-muted-foreground'}`}>
            {row.original.dueDate ? formatDate(row.original.dueDate) : '—'}
          </span>
        )
      },
    }) as ColumnDef<ProjectListDto, unknown>,
    ...createFullAuditColumns<ProjectListDto>(t, formatDateTime),
  // eslint-disable-next-line react-hooks/exhaustive-deps
  ], [t, formatDateTime])

  const { table, settings, isCustomized, resetToDefault } = useEnterpriseTable({
    data: sortedItems,
    columns,
    tableKey: 'projects',
    rowCount: data?.totalCount ?? 0,
    state: {
      pagination: { pageIndex: params.page - 1, pageSize: params.pageSize },
      sorting: params.sorting as SortingState,
    },
    onPaginationChange: (updater) => {
      const next = typeof updater === 'function'
        ? updater({ pageIndex: params.page - 1, pageSize: params.pageSize })
        : updater
      if (next.pageIndex !== params.page - 1) setPage(next.pageIndex + 1)
      if (next.pageSize !== params.pageSize) setPageSize(next.pageSize)
    },
    onSortingChange: setSorting,
    getRowId: (row) => row.id,
  })

  const isContentStale = useDelayedLoading(isSearchStale || isFilterPending || isPlaceholderData)
  const totalCount = data?.totalCount ?? 0

  return (
    <div className="space-y-5">
      <OfflineBanner visible={isReconnecting} />

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

      <div className="flex items-center gap-2.5 flex-wrap">
        <div className="flex-1 min-w-0 flex items-center gap-2 flex-wrap">
          <DataTableToolbar
            table={table}
            searchInput={searchInput}
            onSearchChange={setSearchInput}
            searchPlaceholder={t('pm.searchProjects', { defaultValue: 'Search projects...' })}
            isSearchStale={isSearchStale}
            columnOrder={settings.columnOrder}
            onColumnsReorder={(newOrder) => table.setColumnOrder(newOrder)}
            isCustomized={isCustomized}
            onResetSettings={resetToDefault}
            filterSlot={
              <>
                <Select value={params.filters.status ?? 'all'} onValueChange={setStatusFilter}>
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
                <Select value={sortBy} onValueChange={setSortBy}>
                  <SelectTrigger className="w-[140px] h-8 text-sm cursor-pointer" aria-label={t('pm.sortBy', { defaultValue: 'Sort by' })}>
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
              </>
            }
          />
        </div>

        <ViewModeToggle options={viewModeOptions} value={viewMode} onChange={setViewMode} />
      </div>

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
            searchInput || params.filters.status
              ? t('pm.noProjectsMatchFilter', { defaultValue: 'Try adjusting your search or filters' })
              : t('pm.noProjectsDescription', { defaultValue: 'Create your first project to get started' })
          }
        />
      ) : (
        <>
          {viewMode === 'list' ? (
            <>
              <DataTable
                table={table}
                isLoading={false}
                isStale={isContentStale}
                onRowClick={handleProjectClick}
                getRowAnimationClass={getRowAnimationClass}
                emptyState={
                  <EmptyState
                    icon={FolderKanban}
                    title={t('pm.noProjectsFound')}
                    description={t('pm.noProjectsMatchFilter', { defaultValue: 'Try adjusting your search or filters' })}
                  />
                }
              />
              <DataTablePagination table={table} defaultPageSize={defaultPageSize} />
            </>
          ) : (
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
              {sortedItems.map((project) => (
                <ProjectCard
                  key={project.id}
                  project={project}
                  onClick={() => handleProjectClick(project)}
                  t={t}
                  formatDate={formatDate}
                />
              ))}
            </div>
          )}

          {viewMode === 'grid' && data.totalPages > 1 && (
            <div className="flex items-center justify-center gap-2">
              <Button
                variant="outline"
                size="sm"
                disabled={params.page <= 1}
                onClick={() => setPage(params.page - 1)}
                className="cursor-pointer"
              >
                {t('buttons.previous', { defaultValue: 'Previous' })}
              </Button>
              <span className="text-sm text-muted-foreground tabular-nums">
                {params.page} / {data.totalPages}
              </span>
              <Button
                variant="outline"
                size="sm"
                disabled={params.page >= data.totalPages}
                onClick={() => setPage(params.page + 1)}
                className="cursor-pointer"
              >
                {t('buttons.next', { defaultValue: 'Next' })}
              </Button>
            </div>
          )}
        </>
      )}

      <ProjectDialog
        open={isCreateOpen}
        onOpenChange={onCreateOpenChange}
      />
    </div>
  )
}

export default ProjectsPage
