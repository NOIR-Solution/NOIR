import { useState, useEffect, useCallback } from 'react'
import { useTranslation } from 'react-i18next'
import { usePageContext } from '@/hooks/usePageContext'
import {
  Activity,
  Search,
  RefreshCw,
  AlertCircle,
  CheckCircle2,
  XCircle,
  Clock,
  Pencil,
  Trash2,
  Plus,
  Database,
  Fingerprint,
  X,
} from 'lucide-react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Switch } from '@/components/ui/switch'
import { Label } from '@/components/ui/label'
import { Skeleton } from '@/components/ui/skeleton'
import { Avatar } from '@/components/ui/avatar'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { Pagination } from '@/components/ui/pagination'
import { cn } from '@/lib/utils'
import {
  searchActivityTimeline,
  getPageContexts,
  type ActivityTimelineEntry,
} from '@/services/audit'
import { ActivityDetailsDialog } from './components/ActivityDetailsDialog'

// Operation type icons and colors
const operationConfig = {
  Create: { icon: Plus, color: 'bg-green-500', textColor: 'text-green-700', bgColor: 'bg-green-100 dark:bg-green-900/30' },
  Update: { icon: Pencil, color: 'bg-blue-500', textColor: 'text-blue-700', bgColor: 'bg-blue-100 dark:bg-blue-900/30' },
  Delete: { icon: Trash2, color: 'bg-red-500', textColor: 'text-red-700', bgColor: 'bg-red-100 dark:bg-red-900/30' },
}

// Format relative time
function formatRelativeTime(timestamp: string): string {
  const date = new Date(timestamp)
  const now = new Date()
  const diffMs = now.getTime() - date.getTime()
  const diffSecs = Math.floor(diffMs / 1000)
  const diffMins = Math.floor(diffSecs / 60)
  const diffHours = Math.floor(diffMins / 60)
  const diffDays = Math.floor(diffHours / 24)

  if (diffSecs < 60) return 'Just now'
  if (diffMins < 60) return `${diffMins}m ago`
  if (diffHours < 24) return `${diffHours}h ago`
  if (diffDays < 7) return `${diffDays}d ago`
  return date.toLocaleDateString()
}

// Timeline Entry Component - clickable card that opens details popup
function TimelineEntry({
  entry,
  isLast,
  onViewDetails,
}: {
  entry: ActivityTimelineEntry
  isLast: boolean
  onViewDetails: () => void
}) {
  const config = operationConfig[entry.operationType as keyof typeof operationConfig] || operationConfig.Update
  const Icon = config.icon

  return (
    <div className="relative flex gap-4">
      {/* Timeline line */}
      {!isLast && (
        <div className="absolute left-5 top-12 bottom-0 w-0.5 bg-border" />
      )}

      {/* Avatar with page context initials and status indicator */}
      <div className="relative z-10 flex-shrink-0 h-10 w-10">
        <Avatar
          fallback={entry.displayContext || 'System'}
          size="md"
          className={cn(
            'ring-4 ring-background',
            !entry.isSuccess && 'ring-red-100 dark:ring-red-900/30'
          )}
        />
        <span
          className={cn(
            'absolute bottom-0 right-0 h-4 w-4 rounded-full border-2 border-background flex items-center justify-center',
            entry.isSuccess ? 'bg-green-500' : 'bg-red-500'
          )}
        >
          {entry.isSuccess ? (
            <CheckCircle2 className="h-2.5 w-2.5 text-white" />
          ) : (
            <XCircle className="h-2.5 w-2.5 text-white" />
          )}
        </span>
      </div>

      {/* Content - clickable card */}
      <button
        type="button"
        onClick={onViewDetails}
        className={cn(
          'flex-1 min-w-0 rounded-lg border transition-all mb-3 text-left',
          'hover:shadow-md hover:border-primary/20 cursor-pointer',
          !entry.isSuccess && 'border-red-200 dark:border-red-800 bg-red-50/50 dark:bg-red-950/20'
        )}
      >
        <div className="p-4 flex items-start gap-3">
          {/* Operation Icon */}
          <div className={cn('p-2 rounded-lg', config.bgColor)}>
            <Icon className={cn('h-4 w-4', config.textColor)} />
          </div>

          {/* Main Content */}
          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-2 flex-wrap">
              <span className="font-medium text-sm">
                {entry.actionDescription || entry.displayContext}
              </span>
              <Badge variant="outline" className={cn('text-xs', config.textColor)}>
                {entry.operationType}
              </Badge>
              {entry.entityChangeCount > 0 && (
                <Badge variant="secondary" className="text-xs">
                  <Database className="h-3 w-3 mr-1" />
                  {entry.entityChangeCount} {entry.entityChangeCount === 1 ? 'change' : 'changes'}
                </Badge>
              )}
              {entry.targetDtoId && (
                <span className="font-mono text-xs text-muted-foreground bg-muted px-1.5 py-0.5 rounded">
                  {entry.targetDtoId}
                </span>
              )}
            </div>

            <div className="flex items-center gap-3 mt-1.5 text-xs text-muted-foreground">
              <span>{entry.userEmail || 'System'}</span>
              <span className="flex items-center gap-1">
                <Clock className="h-3 w-3" />
                {formatRelativeTime(entry.timestamp)}
              </span>
              {entry.durationMs && (
                <span className="text-muted-foreground/70">
                  {entry.durationMs}ms
                </span>
              )}
              {entry.correlationId && (
                <span className="flex items-center gap-1" title="Correlation ID">
                  <Fingerprint className="h-3 w-3" />
                  <span className="font-mono truncate max-w-[100px]">{entry.correlationId}</span>
                </span>
              )}
              {entry.targetDisplayName && (
                <span>
                  → <span className="font-medium">{entry.targetDisplayName}</span>
                </span>
              )}
            </div>
          </div>
        </div>
      </button>
    </div>
  )
}

export default function ActivityTimelinePage() {
  const { t } = useTranslation('common')
  usePageContext('Activity Timeline')

  // State
  const [entries, setEntries] = useState<ActivityTimelineEntry[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [pageContexts, setPageContexts] = useState<string[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [totalPages, setTotalPages] = useState(0)
  const [currentPage, setCurrentPage] = useState(1)

  // Filters
  const [searchTerm, setSearchTerm] = useState('')
  const [searchInput, setSearchInput] = useState('')
  const [pageContext, setPageContext] = useState<string>('')
  const [operationType, setOperationType] = useState<string>('')
  const [onlyFailed, setOnlyFailed] = useState(false)
  const [selectedEntry, setSelectedEntry] = useState<ActivityTimelineEntry | null>(null)

  const pageSize = 20

  // Fetch page contexts for filter dropdown
  useEffect(() => {
    getPageContexts()
      .then(setPageContexts)
      .catch(console.error)
  }, [])

  // Fetch activity timeline
  const fetchData = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const result = await searchActivityTimeline({
        pageContext: pageContext || undefined,
        operationType: operationType || undefined,
        searchTerm: searchTerm || undefined,
        onlyFailed: onlyFailed || undefined,
        page: currentPage,
        pageSize,
      })
      setEntries(result.items)
      setTotalCount(result.totalCount)
      setTotalPages(result.totalPages)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load activity timeline')
    } finally {
      setLoading(false)
    }
  }, [pageContext, operationType, searchTerm, onlyFailed, currentPage])

  useEffect(() => {
    fetchData()
  }, [fetchData])

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    setSearchTerm(searchInput)
    setCurrentPage(1)
  }

  const handleRefresh = () => {
    fetchData()
  }

  const handlePageChange = (page: number) => {
    setCurrentPage(page)
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <div className="p-2 bg-primary/10 rounded-lg">
            <Activity className="h-6 w-6 text-primary" />
          </div>
          <div>
            <h1 className="text-3xl font-bold tracking-tight">
              {t('activityTimeline.title', 'Activity Timeline')}
            </h1>
            <p className="text-muted-foreground">
              {t('activityTimeline.description', 'Monitor all user actions in real-time')}
            </p>
          </div>
        </div>
        <Button variant="outline" onClick={handleRefresh} disabled={loading}>
          <RefreshCw className={cn('mr-2 h-4 w-4', loading && 'animate-spin')} />
          {t('buttons.refresh', 'Refresh')}
        </Button>
      </div>

      {/* Filters Card */}
      <Card>
        <CardHeader className="pb-4">
          <div className="space-y-3">
            {/* Header with title and results count */}
            <div className="flex items-center justify-between">
              <div>
                <CardTitle className="text-lg">{t('activityTimeline.recentActivity', 'Recent Activity')}</CardTitle>
                <CardDescription className="text-xs">
                  {totalCount > 0
                    ? `${entries.length} of ${totalCount} entries`
                    : t('activityTimeline.noActivity', 'No activity found')}
                </CardDescription>
              </div>
            </div>

            {/* Filter Bar - Clean unified search */}
            <form onSubmit={handleSearchSubmit} className="space-y-2">
              {/* Main filter row */}
              <div className="flex flex-wrap items-center gap-2">
                {/* Unified search input */}
                <div className="relative flex-1 min-w-[280px]">
                  <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                  <Input
                    placeholder="Search by ID, user, handler, field name, value..."
                    value={searchInput}
                    onChange={(e) => setSearchInput(e.target.value)}
                    className="pl-9 h-9"
                    title="Search across: Entity ID, Correlation ID, user email, handler name, HTTP path, field names, values, and more"
                  />
                </div>

                {/* Context dropdown */}
                <Select
                  value={pageContext || 'all'}
                  onValueChange={(value) => {
                    setPageContext(value === 'all' ? '' : value)
                    setCurrentPage(1)
                  }}
                >
                  <SelectTrigger className="w-[130px] h-9">
                    <SelectValue placeholder="All Contexts" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all">All Contexts</SelectItem>
                    {pageContexts.map((ctx) => (
                      <SelectItem key={ctx} value={ctx}>
                        {ctx}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>

                {/* Action dropdown */}
                <Select
                  value={operationType || 'all'}
                  onValueChange={(value) => {
                    setOperationType(value === 'all' ? '' : value)
                    setCurrentPage(1)
                  }}
                >
                  <SelectTrigger className="w-[130px] h-9">
                    <SelectValue placeholder="All Actions" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all">All Actions</SelectItem>
                    <SelectItem value="Create">
                      <span className="flex items-center gap-2">
                        <Plus className="h-3 w-3 text-green-600" />
                        Create
                      </span>
                    </SelectItem>
                    <SelectItem value="Update">
                      <span className="flex items-center gap-2">
                        <Pencil className="h-3 w-3 text-blue-600" />
                        Update
                      </span>
                    </SelectItem>
                    <SelectItem value="Delete">
                      <span className="flex items-center gap-2">
                        <Trash2 className="h-3 w-3 text-red-600" />
                        Delete
                      </span>
                    </SelectItem>
                  </SelectContent>
                </Select>

                {/* Failed only toggle */}
                <div className="flex items-center gap-2 px-3 py-1.5 bg-muted rounded-md">
                  <Switch
                    id="only-failed"
                    checked={onlyFailed}
                    onCheckedChange={(checked: boolean) => {
                      setOnlyFailed(checked)
                      setCurrentPage(1)
                    }}
                    className={cn(onlyFailed && 'data-[state=checked]:bg-destructive')}
                  />
                  <Label htmlFor="only-failed" className="text-sm cursor-pointer whitespace-nowrap">
                    Failed only
                  </Label>
                </div>

                {/* Actions */}
                <div className="flex items-center gap-2">
                  {/* Clear button - only show when filters are active */}
                  {(searchInput || pageContext || operationType || onlyFailed) && (
                    <Button
                      type="button"
                      variant="ghost"
                      size="sm"
                      className="h-9 gap-1.5"
                      onClick={() => {
                        setSearchInput('')
                        setSearchTerm('')
                        setPageContext('')
                        setOperationType('')
                        setOnlyFailed(false)
                        setCurrentPage(1)
                      }}
                    >
                      <X className="h-3.5 w-3.5" />
                      Clear
                    </Button>
                  )}
                  <Button type="submit" size="sm" className="h-9 gap-1.5">
                    <Search className="h-3.5 w-3.5" />
                    {t('buttons.search', 'Search')}
                  </Button>
                </div>
              </div>

              {/* Search help text - more visible */}
              <div className="flex items-center gap-2 text-sm text-foreground/70 bg-muted/50 px-3 py-1.5 rounded-md border border-border/50">
                <Search className="h-3.5 w-3.5 text-primary flex-shrink-0" />
                <span>
                  Search by <span className="font-medium text-primary">entity ID</span>,{' '}
                  <span className="font-medium text-primary">correlation ID</span>,{' '}
                  <span className="font-medium text-primary">user email</span>,{' '}
                  <span className="font-medium text-primary">handler name</span>,{' '}
                  <span className="font-medium text-primary">HTTP path</span>,{' '}
                  <span className="font-medium text-primary">field names</span>, or{' '}
                  <span className="font-medium text-primary">values</span>
                </span>
              </div>
            </form>
          </div>
        </CardHeader>
        <CardContent>
          {error && (
            <div className="mb-4 p-4 bg-destructive/10 text-destructive rounded-md flex items-center gap-2">
              <AlertCircle className="h-4 w-4" />
              {error}
            </div>
          )}

          {/* Timeline */}
          <div className="pl-2">
            {loading ? (
              // Loading skeletons
              Array.from({ length: 5 }).map((_, i) => (
                <div key={i} className="flex gap-4 mb-4">
                  <Skeleton className="h-10 w-10 rounded-full flex-shrink-0" />
                  <div className="flex-1 space-y-2 p-4 border rounded-lg">
                    <Skeleton className="h-4 w-3/4" />
                    <Skeleton className="h-3 w-1/2" />
                  </div>
                </div>
              ))
            ) : entries.length === 0 ? (
              <div className="text-center py-12 text-muted-foreground">
                <Activity className="h-12 w-12 mx-auto mb-4 opacity-50" />
                <p>{t('activityTimeline.noActivity', 'No activity found')}</p>
              </div>
            ) : (
              entries.map((entry, index) => (
                <TimelineEntry
                  key={entry.id}
                  entry={entry}
                  isLast={index === entries.length - 1}
                  onViewDetails={() => setSelectedEntry(entry)}
                />
              ))
            )}
          </div>

          {/* Pagination */}
          {totalPages > 1 && (
            <Pagination
              currentPage={currentPage}
              totalPages={totalPages}
              totalItems={totalCount}
              pageSize={pageSize}
              onPageChange={handlePageChange}
              showPageSizeSelector={false}
              className="mt-4"
            />
          )}
        </CardContent>
      </Card>

      {/* Details Dialog */}
      <ActivityDetailsDialog
        entry={selectedEntry}
        open={!!selectedEntry}
        onOpenChange={(open) => !open && setSelectedEntry(null)}
      />
    </div>
  )
}
