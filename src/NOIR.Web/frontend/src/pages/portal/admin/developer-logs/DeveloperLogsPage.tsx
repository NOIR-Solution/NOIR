/**
 * Developer Logs Page
 *
 * Real-time log viewer with SignalR streaming, syntax highlighting,
 * log level control, filtering, historical log browsing, and error clustering.
 */
import { useState, useEffect, useCallback, useMemo, useRef } from 'react'
import { useTranslation } from 'react-i18next'
import type { DateRange } from 'react-day-picker'
import { usePageContext } from '@/hooks/usePageContext'
import { useLogStream } from '@/hooks/useLogStream'
import {
  Terminal,
  Play,
  Pause,
  Trash2,
  RefreshCw,
  Search,
  AlertTriangle,
  Bug,
  Info,
  AlertCircle,
  Skull,
  MessageSquare,
  ChevronDown,
  ChevronRight,
  BarChart3,
  Wifi,
  WifiOff,
  Copy,
  Check,
  X,
  ArrowDown,
  ArrowUp,
  ArrowDownToLine,
  History,
  FileText,
  ChevronLeft,
  Loader2,
  Clock,
  Database,
  Maximize2,
} from 'lucide-react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Skeleton } from '@/components/ui/skeleton'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from '@/components/ui/collapsible'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { ScrollArea } from '@/components/ui/scroll-area'
import { DateRangePicker } from '@/components/ui/date-range-picker'
import { Pagination } from '@/components/ui/pagination'
import { Switch } from '@/components/ui/switch'
import { Label } from '@/components/ui/label'
import { JsonViewer } from '@/components/ui/json-viewer'
import { LogMessageFormatter } from '@/components/ui/log-message-formatter'
import {
  DropdownMenu,
  DropdownMenuCheckboxItem,
  DropdownMenuContent,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { cn } from '@/lib/utils'
import {
  getLogLevel,
  setLogLevel,
  clearBuffer,
  getAvailableLogDates,
  getHistoricalLogs,
  type LogEntryDto,
  type DevLogLevel,
  type LogBufferStatsDto,
  type ErrorClusterDto,
  type LogEntriesPagedResponse,
} from '@/services/developerLogs'
import { format, parseISO } from 'date-fns'

// Configuration constants
const LOG_STREAM_CONFIG = {
  MAX_ENTRIES: 1000,
  HISTORY_PAGE_SIZE: 100,
  AUTO_CONNECT: true,
} as const

// Log level configuration with colors and icons
const LOG_LEVELS: {
  value: DevLogLevel
  label: string
  icon: typeof Bug
  bgColor: string
  textColor: string
  borderColor: string
}[] = [
  {
    value: 'Verbose',
    label: 'VRB',
    icon: MessageSquare,
    bgColor: 'bg-slate-100 dark:bg-slate-800',
    textColor: 'text-slate-500 dark:text-slate-400',
    borderColor: 'border-slate-300 dark:border-slate-600',
  },
  {
    value: 'Debug',
    label: 'DBG',
    icon: Bug,
    bgColor: 'bg-purple-100 dark:bg-purple-900/30',
    textColor: 'text-purple-600 dark:text-purple-400',
    borderColor: 'border-purple-300 dark:border-purple-700',
  },
  {
    value: 'Information',
    label: 'INF',
    icon: Info,
    bgColor: 'bg-blue-100 dark:bg-blue-900/30',
    textColor: 'text-blue-600 dark:text-blue-400',
    borderColor: 'border-blue-300 dark:border-blue-700',
  },
  {
    value: 'Warning',
    label: 'WRN',
    icon: AlertTriangle,
    bgColor: 'bg-amber-100 dark:bg-amber-900/30',
    textColor: 'text-amber-600 dark:text-amber-400',
    borderColor: 'border-amber-300 dark:border-amber-700',
  },
  {
    value: 'Error',
    label: 'ERR',
    icon: AlertCircle,
    bgColor: 'bg-red-100 dark:bg-red-900/30',
    textColor: 'text-red-600 dark:text-red-400',
    borderColor: 'border-red-300 dark:border-red-700',
  },
  {
    value: 'Fatal',
    label: 'FTL',
    icon: Skull,
    bgColor: 'bg-red-200 dark:bg-red-900/50',
    textColor: 'text-red-700 dark:text-red-300',
    borderColor: 'border-red-500 dark:border-red-600',
  },
]

function getLevelConfig(level: DevLogLevel) {
  return LOG_LEVELS.find(l => l.value === level) || LOG_LEVELS[2] // Default to Information
}

// Format timestamp for display
function formatTimestamp(timestamp: string): string {
  const date = new Date(timestamp)
  return date.toLocaleTimeString('en-US', {
    hour12: false,
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
    fractionalSecondDigits: 3,
  })
}

// Format full timestamp with date
function formatFullTimestamp(timestamp: string): string {
  const date = new Date(timestamp)
  return date.toLocaleString('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour12: false,
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
  })
}

// Format bytes to human-readable
function formatBytes(bytes: number): string {
  const sizes = ['B', 'KB', 'MB', 'GB']
  if (bytes === 0) return '0 B'
  const i = Math.floor(Math.log(bytes) / Math.log(1024))
  return `${(bytes / Math.pow(1024, i)).toFixed(1)} ${sizes[i]}`
}

// Format date string for display
function formatDateDisplay(dateStr: string): string {
  try {
    const date = parseISO(dateStr)
    return format(date, 'MMM d, yyyy')
  } catch {
    return dateStr
  }
}

// Get displayable message from log entry (handles empty message field)
function getDisplayMessage(entry: LogEntryDto): string {
  // If message exists, use it
  if (entry.message && entry.message.trim()) {
    return entry.message
  }

  // Fallback to messageTemplate
  if (entry.messageTemplate && entry.messageTemplate.trim()) {
    return entry.messageTemplate
  }

  // Try to extract MessageTemplate from properties (Serilog format)
  if (entry.properties) {
    const props = entry.properties as Record<string, unknown>
    if (typeof props.MessageTemplate === 'string' && props.MessageTemplate.trim()) {
      return props.MessageTemplate
    }
    // Some formats nest it under Properties
    if (props.Properties && typeof props.Properties === 'object') {
      const nestedProps = props.Properties as Record<string, unknown>
      if (typeof nestedProps.MessageTemplate === 'string') {
        return nestedProps.MessageTemplate
      }
    }
  }

  return '(no message)'
}

// Log Detail Dialog Component
function LogDetailDialog({
  entry,
  open,
  onOpenChange,
}: {
  entry: LogEntryDto | null
  open: boolean
  onOpenChange: (open: boolean) => void
}) {
  const [copied, setCopied] = useState(false)

  if (!entry) return null

  const config = getLevelConfig(entry.level)

  const handleCopyMessage = () => {
    navigator.clipboard.writeText(getDisplayMessage(entry))
    setCopied(true)
    setTimeout(() => setCopied(false), 2000)
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-4xl max-h-[90vh] overflow-hidden flex flex-col">
        <DialogHeader className="flex-shrink-0">
          <DialogTitle className="flex items-center gap-3">
            <Badge
              variant="outline"
              className={cn(
                'px-2 py-0.5 text-xs font-bold',
                config.bgColor,
                config.textColor
              )}
            >
              {config.label}
            </Badge>
            <span className="text-sm font-mono text-muted-foreground">
              {formatFullTimestamp(entry.timestamp)}
            </span>
          </DialogTitle>
        </DialogHeader>

        <div className="flex-1 overflow-y-auto space-y-4 pr-2">
          {/* Message */}
          <div className="space-y-2">
            <div className="flex items-center justify-between">
              <label className="text-sm font-medium text-muted-foreground">Message</label>
              <Button variant="ghost" size="sm" onClick={handleCopyMessage} className="h-7 gap-1.5">
                {copied ? <Check className="h-3 w-3" /> : <Copy className="h-3 w-3" />}
                Copy
              </Button>
            </div>
            <div className="p-3 bg-muted rounded-lg font-mono text-sm">
              <LogMessageFormatter message={getDisplayMessage(entry)} />
            </div>
          </div>

          {/* Metadata Grid */}
          <div className="grid grid-cols-2 gap-4">
            {entry.sourceContext && (
              <div className="space-y-1">
                <label className="text-xs font-medium text-muted-foreground">Source</label>
                <div className="p-2 bg-muted rounded text-sm font-mono truncate">
                  {entry.sourceContext}
                </div>
              </div>
            )}
            {entry.requestId && (
              <div className="space-y-1">
                <label className="text-xs font-medium text-muted-foreground">Request ID</label>
                <div className="p-2 bg-muted rounded text-sm font-mono truncate">
                  {entry.requestId}
                </div>
              </div>
            )}
            {entry.traceId && (
              <div className="space-y-1">
                <label className="text-xs font-medium text-muted-foreground">Trace ID</label>
                <div className="p-2 bg-muted rounded text-sm font-mono truncate">
                  {entry.traceId}
                </div>
              </div>
            )}
            {entry.userId && (
              <div className="space-y-1">
                <label className="text-xs font-medium text-muted-foreground">User ID</label>
                <div className="p-2 bg-muted rounded text-sm font-mono truncate">
                  {entry.userId}
                </div>
              </div>
            )}
            {entry.tenantId && (
              <div className="space-y-1">
                <label className="text-xs font-medium text-muted-foreground">Tenant ID</label>
                <div className="p-2 bg-muted rounded text-sm font-mono truncate">
                  {entry.tenantId}
                </div>
              </div>
            )}
          </div>

          {/* Exception */}
          {entry.exception && (
            <div className="space-y-2">
              <label className="text-sm font-medium text-red-600 dark:text-red-400">Exception</label>
              <div className="p-3 bg-red-50 dark:bg-red-950/50 rounded-lg border border-red-200 dark:border-red-800">
                <div className="font-semibold text-red-700 dark:text-red-300">
                  {entry.exception.type}
                </div>
                <div className="text-red-600 dark:text-red-400 mt-1">
                  {entry.exception.message}
                </div>
                {entry.exception.stackTrace && (
                  <pre className="mt-3 p-2 bg-red-100 dark:bg-red-900/50 rounded text-[11px] text-red-600/90 dark:text-red-400/90 whitespace-pre-wrap overflow-x-auto max-h-[300px] overflow-y-auto">
                    {entry.exception.stackTrace}
                  </pre>
                )}
              </div>
            </div>
          )}

          {/* Properties */}
          {entry.properties && Object.keys(entry.properties).length > 0 && (
            <div className="space-y-2">
              <label className="text-sm font-medium text-muted-foreground">Properties</label>
              <JsonViewer
                data={entry.properties}
                defaultExpanded={true}
                maxDepth={4}
                maxHeight="200px"
                allowFullscreen={true}
                title="Log Properties"
              />
            </div>
          )}

          {/* Raw JSON */}
          <div className="space-y-2">
            <label className="text-sm font-medium text-muted-foreground">Raw JSON</label>
            <JsonViewer
              data={entry}
              defaultExpanded={true}
              maxDepth={4}
              maxHeight="250px"
              allowFullscreen={true}
              title="Log Entry"
            />
          </div>
        </div>
      </DialogContent>
    </Dialog>
  )
}

// Log Entry Component
function LogEntry({
  entry,
  isExpanded,
  onToggleExpand,
  onViewDetail,
}: {
  entry: LogEntryDto
  isExpanded: boolean
  onToggleExpand: () => void
  onViewDetail: () => void
}) {
  const config = getLevelConfig(entry.level)
  const [copied, setCopied] = useState(false)
  const hasException = !!entry.exception

  const handleCopy = (e: React.MouseEvent) => {
    e.stopPropagation()
    const text = JSON.stringify(entry, null, 2)
    navigator.clipboard.writeText(text)
    setCopied(true)
    setTimeout(() => setCopied(false), 2000)
  }

  return (
    <div
      className={cn(
        'group border-l-4 px-3 py-1.5 font-mono text-xs hover:bg-muted/50 dark:hover:bg-slate-800/50 transition-colors cursor-pointer',
        config.borderColor,
        hasException && 'bg-red-50 dark:bg-red-950/30'
      )}
      onClick={onViewDetail}
    >
      <div className="flex items-start gap-2">
        {/* Expand button for entries with exceptions */}
        {hasException ? (
          <button
            onClick={(e) => {
              e.stopPropagation()
              onToggleExpand()
            }}
            className="flex-shrink-0 mt-0.5 p-0.5 hover:bg-muted dark:hover:bg-slate-700 rounded text-muted-foreground"
          >
            {isExpanded ? (
              <ChevronDown className="h-3 w-3" />
            ) : (
              <ChevronRight className="h-3 w-3" />
            )}
          </button>
        ) : (
          <span className="w-4 flex-shrink-0" />
        )}

        {/* Timestamp */}
        <span className="flex-shrink-0 text-muted-foreground tabular-nums">
          {formatTimestamp(entry.timestamp)}
        </span>

        {/* Level badge */}
        <Badge
          variant="outline"
          className={cn(
            'flex-shrink-0 px-1.5 py-0 h-5 text-[10px] font-bold',
            config.bgColor,
            config.textColor
          )}
        >
          {config.label}
        </Badge>

        {/* Source context */}
        {entry.sourceContext && (
          <span className="flex-shrink-0 text-muted-foreground/70 truncate max-w-[200px]">
            [{entry.sourceContext.split('.').pop()}]
          </span>
        )}

        {/* Message - ALWAYS visible with syntax highlighting */}
        <span className="flex-1 text-foreground">
          <LogMessageFormatter message={getDisplayMessage(entry)} />
        </span>

        {/* Action buttons - always visible */}
        <div className="flex-shrink-0 flex items-center gap-1">
          <button
            onClick={handleCopy}
            className="p-1 hover:bg-muted dark:hover:bg-slate-700 rounded text-muted-foreground hover:text-foreground"
            title="Copy entry as JSON"
          >
            {copied ? (
              <Check className="h-3 w-3 text-green-600 dark:text-green-400" />
            ) : (
              <Copy className="h-3 w-3" />
            )}
          </button>
          <button
            onClick={(e) => {
              e.stopPropagation()
              onViewDetail()
            }}
            className="p-1 hover:bg-muted dark:hover:bg-slate-700 rounded text-muted-foreground hover:text-foreground"
            title="View full details"
          >
            <Maximize2 className="h-3 w-3" />
          </button>
        </div>
      </div>

      {/* Exception details */}
      {isExpanded && entry.exception && (
        <div
          className="mt-2 ml-6 p-2 bg-red-100 dark:bg-red-950/50 rounded border border-red-300 dark:border-red-800"
          onClick={(e) => e.stopPropagation()}
        >
          <div className="font-semibold text-red-700 dark:text-red-300">
            {entry.exception.type}
          </div>
          <div className="text-red-600 dark:text-red-400 mt-1">
            {entry.exception.message}
          </div>
          {entry.exception.stackTrace && (
            <pre className="mt-2 text-[10px] text-red-600/80 dark:text-red-400/80 whitespace-pre-wrap overflow-x-auto">
              {entry.exception.stackTrace}
            </pre>
          )}
        </div>
      )}
    </div>
  )
}

// Stats Card Component
function StatsCard({
  stats,
  onRefresh,
}: {
  stats: LogBufferStatsDto | null
  onRefresh: () => void
}) {
  if (!stats) {
    return (
      <div className="space-y-3">
        <Skeleton className="h-4 w-32" />
        <Skeleton className="h-8 w-full" />
        <Skeleton className="h-8 w-full" />
      </div>
    )
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <span className="text-sm font-medium">Buffer Statistics</span>
        <Button variant="ghost" size="sm" onClick={onRefresh}>
          <RefreshCw className="h-3 w-3" />
        </Button>
      </div>

      <div className="grid grid-cols-2 gap-3 text-sm">
        <div className="p-2 bg-muted rounded">
          <div className="text-muted-foreground text-xs">Entries</div>
          <div className="font-mono font-semibold">
            {stats.totalEntries.toLocaleString()} / {stats.maxCapacity.toLocaleString()}
          </div>
        </div>
        <div className="p-2 bg-muted rounded">
          <div className="text-muted-foreground text-xs">Memory</div>
          <div className="font-mono font-semibold">
            {formatBytes(stats.memoryUsageBytes)}
          </div>
        </div>
      </div>

      {/* Entries by level */}
      <div className="space-y-2">
        <div className="text-xs text-muted-foreground">By Level</div>
        <div className="space-y-1">
          {LOG_LEVELS.map(level => {
            const count = stats.entriesByLevel[level.value] || 0
            const percentage = stats.totalEntries > 0
              ? (count / stats.totalEntries) * 100
              : 0

            return (
              <div key={level.value} className="flex items-center gap-2 text-xs">
                <Badge
                  variant="outline"
                  className={cn('w-10 justify-center px-1 py-0', level.bgColor, level.textColor)}
                >
                  {level.label}
                </Badge>
                <div className="flex-1 h-2 bg-muted rounded overflow-hidden">
                  <div
                    className={cn('h-full transition-all', level.bgColor.replace('100', '400').replace('900/30', '600'))}
                    style={{ width: `${percentage}%` }}
                  />
                </div>
                <span className="w-12 text-right tabular-nums font-mono">
                  {count.toLocaleString()}
                </span>
              </div>
            )
          })}
        </div>
      </div>
    </div>
  )
}

// Error Clusters Component
function ErrorClusters({
  clusters,
  onRefresh,
}: {
  clusters: ErrorClusterDto[]
  onRefresh: () => void
}) {
  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <span className="text-sm font-medium">Error Patterns</span>
        <Button variant="ghost" size="sm" onClick={onRefresh}>
          <RefreshCw className="h-3 w-3" />
        </Button>
      </div>

      {clusters.length === 0 ? (
        <div className="text-center py-6 text-muted-foreground text-sm">
          No error patterns detected
        </div>
      ) : (
        <div className="space-y-2">
          {clusters.map(cluster => (
            <Collapsible key={cluster.id}>
              <CollapsibleTrigger className="w-full">
                <div className="flex items-center gap-2 p-2 bg-red-50 dark:bg-red-950/30 rounded border border-red-200 dark:border-red-800 text-left hover:bg-red-100 dark:hover:bg-red-950/50 transition-colors">
                  <Badge
                    variant={
                      cluster.severity === 'critical'
                        ? 'destructive'
                        : cluster.severity === 'high'
                        ? 'default'
                        : 'secondary'
                    }
                    className="flex-shrink-0"
                  >
                    {cluster.count}x
                  </Badge>
                  <span className="flex-1 text-xs font-mono truncate">
                    {cluster.pattern}
                  </span>
                  <ChevronRight className="h-4 w-4 flex-shrink-0" />
                </div>
              </CollapsibleTrigger>
              <CollapsibleContent>
                <div className="mt-1 p-2 bg-muted/50 rounded text-xs space-y-1">
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">First seen:</span>
                    <span className="font-mono">{new Date(cluster.firstSeen).toLocaleString()}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">Last seen:</span>
                    <span className="font-mono">{new Date(cluster.lastSeen).toLocaleString()}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">Severity:</span>
                    <Badge variant="outline" className="capitalize">{cluster.severity}</Badge>
                  </div>
                </div>
              </CollapsibleContent>
            </Collapsible>
          ))}
        </div>
      )}
    </div>
  )
}

// History File Card Component
function HistoryFileCard({
  date,
  onView,
  isSelected,
}: {
  date: string
  onView: () => void
  isSelected: boolean
}) {
  return (
    <button
      onClick={onView}
      className={cn(
        'w-full p-4 rounded-lg border text-left transition-all hover:shadow-md cursor-pointer',
        isSelected
          ? 'border-primary bg-primary/5 shadow-sm'
          : 'border-border hover:border-primary/50 bg-card'
      )}
    >
      <div className="flex items-center gap-3">
        <div className={cn(
          'p-2 rounded-lg',
          isSelected ? 'bg-primary/10' : 'bg-muted'
        )}>
          <FileText className={cn('h-5 w-5', isSelected ? 'text-primary' : 'text-muted-foreground')} />
        </div>
        <div className="flex-1 min-w-0">
          <div className="font-medium truncate">
            noir-{date}.json
          </div>
          <div className="text-xs text-muted-foreground mt-0.5">
            {formatDateDisplay(date)}
          </div>
        </div>
        <ChevronRight className={cn(
          'h-5 w-5 flex-shrink-0 transition-transform',
          isSelected ? 'text-primary rotate-90' : 'text-muted-foreground'
        )} />
      </div>
    </button>
  )
}

// Fullscreen Log Viewer Dialog
function FullscreenLogDialog({
  entries,
  title,
  open,
  onOpenChange,
  expandedEntries,
  onToggleExpand,
  onViewDetail,
}: {
  entries: LogEntryDto[]
  title: string
  open: boolean
  onOpenChange: (open: boolean) => void
  expandedEntries: Set<number>
  onToggleExpand: (id: number) => void
  onViewDetail: (entry: LogEntryDto) => void
}) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-[95vw] w-full max-h-[95vh] h-full flex flex-col p-0">
        <DialogHeader className="px-4 py-3 border-b flex-shrink-0">
          <DialogTitle className="flex items-center gap-2">
            <Terminal className="h-5 w-5" />
            {title}
            <Badge variant="secondary" className="ml-2">
              {entries.length} entries
            </Badge>
          </DialogTitle>
        </DialogHeader>
        <div className="flex-1 overflow-y-auto bg-card dark:bg-slate-950">
          {entries.length === 0 ? (
            <div className="flex flex-col items-center justify-center h-full text-muted-foreground py-20">
              <FileText className="h-12 w-12 mb-4 opacity-50" />
              <p>No log entries</p>
            </div>
          ) : (
            <div className="divide-y divide-border">
              {entries.map(entry => (
                <LogEntry
                  key={entry.id}
                  entry={entry}
                  isExpanded={expandedEntries.has(entry.id)}
                  onToggleExpand={() => onToggleExpand(entry.id)}
                  onViewDetail={() => onViewDetail(entry)}
                />
              ))}
            </div>
          )}
        </div>
      </DialogContent>
    </Dialog>
  )
}

// History File Viewer Component
function HistoryFileViewer({
  date,
  onBack,
}: {
  date: string
  onBack: () => void
}) {
  const [entries, setEntries] = useState<LogEntryDto[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [page, setPage] = useState(1)
  const [totalPages, setTotalPages] = useState(0)
  const [totalCount, setTotalCount] = useState(0)
  const [searchTerm, setSearchTerm] = useState('')
  const [selectedLevels, setSelectedLevels] = useState<Set<DevLogLevel>>(new Set())
  const [expandedEntries, setExpandedEntries] = useState<Set<number>>(new Set())
  const [sortOrder, setSortOrder] = useState<'newest' | 'oldest'>('newest')
  const [detailEntry, setDetailEntry] = useState<LogEntryDto | null>(null)
  const [isFullscreen, setIsFullscreen] = useState(false)

  const fetchLogs = useCallback(async () => {
    setIsLoading(true)
    try {
      const response: LogEntriesPagedResponse = await getHistoricalLogs(date, {
        page,
        pageSize: LOG_STREAM_CONFIG.HISTORY_PAGE_SIZE,
        search: searchTerm || undefined,
        levels: selectedLevels.size > 0 ? Array.from(selectedLevels) : undefined,
      })
      setEntries(response.items)
      setTotalPages(response.totalPages)
      setTotalCount(response.totalCount)
    } catch (error) {
      console.error('Failed to fetch historical logs:', error)
    } finally {
      setIsLoading(false)
    }
  }, [date, page, searchTerm, selectedLevels])

  useEffect(() => {
    fetchLogs()
  }, [fetchLogs])

  const toggleEntryExpanded = useCallback((id: number) => {
    setExpandedEntries(prev => {
      const next = new Set(prev)
      if (next.has(id)) {
        next.delete(id)
      } else {
        next.add(id)
      }
      return next
    })
  }, [])

  const sortedEntries = useMemo(() => {
    if (sortOrder === 'oldest') {
      return [...entries].reverse()
    }
    return entries
  }, [entries, sortOrder])

  return (
    <div className="flex flex-col h-full min-h-[400px]">
      {/* Header with back button */}
      <div className="flex items-center gap-3 pb-4 border-b flex-shrink-0">
        <Button variant="ghost" size="sm" onClick={onBack} className="gap-1">
          <ChevronLeft className="h-4 w-4" />
          Back
        </Button>
        <div className="flex-1">
          <h2 className="font-semibold">noir-{date}.json</h2>
          <p className="text-sm text-muted-foreground">
            {formatDateDisplay(date)} &middot; {totalCount.toLocaleString()} entries
          </p>
        </div>
        <Button
          variant="outline"
          size="sm"
          onClick={() => setSortOrder(sortOrder === 'newest' ? 'oldest' : 'newest')}
          className="gap-1"
        >
          {sortOrder === 'newest' ? (
            <>
              <ArrowDown className="h-4 w-4" />
              Newest First
            </>
          ) : (
            <>
              <ArrowUp className="h-4 w-4" />
              Oldest First
            </>
          )}
        </Button>
      </div>

      {/* Filters - compact row */}
      <div className="flex items-center gap-2 py-4 flex-shrink-0">
        <div className="relative flex-1 max-w-sm">
          <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
          <Input
            placeholder="Search logs..."
            value={searchTerm}
            onChange={(e) => {
              setSearchTerm(e.target.value)
              setPage(1)
            }}
            className="pl-8 h-8"
          />
          {searchTerm && (
            <button
              onClick={() => {
                setSearchTerm('')
                setPage(1)
              }}
              className="absolute right-2.5 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground"
            >
              <X className="h-4 w-4" />
            </button>
          )}
        </div>
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="outline" size="sm" className="h-8 gap-2">
              <span className="text-muted-foreground">Levels:</span>
              {selectedLevels.size === 0 ? (
                <span>All</span>
              ) : (
                <span className="flex items-center gap-1">
                  {Array.from(selectedLevels).slice(0, 2).map(level => {
                    const config = getLevelConfig(level)
                    return (
                      <Badge
                        key={level}
                        variant="outline"
                        className={cn('px-1.5 py-0 text-xs', config.textColor)}
                      >
                        {config.label}
                      </Badge>
                    )
                  })}
                  {selectedLevels.size > 2 && (
                    <span className="text-xs text-muted-foreground">+{selectedLevels.size - 2}</span>
                  )}
                </span>
              )}
              <ChevronDown className="h-3.5 w-3.5 opacity-50" />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            {LOG_LEVELS.map(level => (
              <DropdownMenuCheckboxItem
                key={level.value}
                checked={selectedLevels.has(level.value)}
                onSelect={(e) => e.preventDefault()}
                onCheckedChange={(checked) => {
                  setSelectedLevels(prev => {
                    const next = new Set(prev)
                    if (checked) {
                      next.add(level.value)
                    } else {
                      next.delete(level.value)
                    }
                    return next
                  })
                  setPage(1)
                }}
              >
                <level.icon className={cn('h-4 w-4 mr-2', level.textColor)} />
                <span className={level.textColor}>{level.value}</span>
              </DropdownMenuCheckboxItem>
            ))}
            {selectedLevels.size > 0 && (
              <>
                <DropdownMenuSeparator />
                <Button
                  variant="ghost"
                  size="sm"
                  className="w-full h-7 text-xs"
                  onClick={(e) => {
                    e.preventDefault()
                    setSelectedLevels(new Set())
                    setPage(1)
                  }}
                >
                  Clear filters
                </Button>
              </>
            )}
          </DropdownMenuContent>
        </DropdownMenu>
      </div>

      {/* Log entries - terminal style - fills available space */}
      <div className="rounded-lg border overflow-hidden flex-1 flex flex-col min-h-0">
        {/* Compact terminal header */}
        <div className="flex items-center justify-between px-3 py-1.5 bg-muted dark:bg-slate-900 border-b flex-shrink-0">
          <div className="flex items-center gap-3">
            <div className="flex items-center gap-1.5">
              <div className="w-2.5 h-2.5 rounded-full bg-red-500" />
              <div className="w-2.5 h-2.5 rounded-full bg-yellow-500" />
              <div className="w-2.5 h-2.5 rounded-full bg-green-500" />
            </div>
            <span className="text-xs font-mono text-muted-foreground">
              {sortedEntries.length} entries on this page
            </span>
          </div>
          <Button
            variant="ghost"
            size="sm"
            onClick={() => setIsFullscreen(true)}
            className="h-7 gap-1.5 text-xs"
          >
            <Maximize2 className="h-3.5 w-3.5" />
            Expand
          </Button>
        </div>
        {/* Log content - fills remaining space */}
        <div className="bg-card dark:bg-slate-950 flex-1 overflow-y-auto">
          {isLoading ? (
            <div className="flex items-center justify-center py-12">
              <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
            </div>
          ) : sortedEntries.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-12 text-muted-foreground">
              <FileText className="h-10 w-10 mb-3 opacity-50" />
              <p className="text-sm">No log entries found</p>
            </div>
          ) : (
            <div className="divide-y divide-border">
              {sortedEntries.map(entry => (
                <LogEntry
                  key={entry.id}
                  entry={entry}
                  isExpanded={expandedEntries.has(entry.id)}
                  onToggleExpand={() => toggleEntryExpanded(entry.id)}
                  onViewDetail={() => setDetailEntry(entry)}
                />
              ))}
            </div>
          )}
        </div>
      </div>

      {/* Pagination - sticky at bottom */}
      {totalPages > 1 && (
        <div className="pt-4 flex-shrink-0">
          <Pagination
            currentPage={page}
            totalPages={totalPages}
            totalItems={totalCount}
            pageSize={LOG_STREAM_CONFIG.HISTORY_PAGE_SIZE}
            onPageChange={setPage}
            showPageSizeSelector={false}
          />
        </div>
      )}

      {/* Fullscreen Log Dialog */}
      <FullscreenLogDialog
        entries={sortedEntries}
        title={`noir-${date}.json`}
        open={isFullscreen}
        onOpenChange={setIsFullscreen}
        expandedEntries={expandedEntries}
        onToggleExpand={toggleEntryExpanded}
        onViewDetail={setDetailEntry}
      />

      {/* Log Detail Dialog */}
      <LogDetailDialog
        entry={detailEntry}
        open={!!detailEntry}
        onOpenChange={(open) => !open && setDetailEntry(null)}
      />
    </div>
  )
}

// History Tab Content
function HistoryTabContent() {
  const [availableDates, setAvailableDates] = useState<string[]>([])
  const [isLoadingDates, setIsLoadingDates] = useState(true)
  const [selectedDate, setSelectedDate] = useState<string | null>(null)
  const [dateRange, setDateRange] = useState<DateRange | undefined>(undefined)

  const fetchDates = useCallback(async () => {
    setIsLoadingDates(true)
    try {
      const dates = await getAvailableLogDates()
      setAvailableDates(dates)
    } catch (error) {
      console.error('Failed to fetch available dates:', error)
    } finally {
      setIsLoadingDates(false)
    }
  }, [])

  useEffect(() => {
    fetchDates()
  }, [fetchDates])

  // Filter available dates by date range
  const filteredDates = useMemo(() => {
    if (!dateRange?.from) return availableDates

    return availableDates.filter(dateStr => {
      const date = parseISO(dateStr)
      const from = dateRange.from!
      const to = dateRange.to || dateRange.from!

      return date >= from && date <= to
    })
  }, [availableDates, dateRange])

  if (selectedDate) {
    return (
      <div className="h-full">
        <HistoryFileViewer
          date={selectedDate}
          onBack={() => setSelectedDate(null)}
        />
      </div>
    )
  }

  return (
    <div className="space-y-4">
      {/* Filter Bar - filter on right side */}
      <div className="flex flex-wrap items-center gap-2">
        <Badge variant="secondary">
          {filteredDates.length} of {availableDates.length} files
        </Badge>

        <div className="flex-1" />

        <DateRangePicker
          value={dateRange}
          onChange={setDateRange}
          placeholder="Filter by date range"
          className="h-9 w-[220px]"
          numberOfMonths={2}
        />
      </div>

      {/* Available log files */}
      {isLoadingDates ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3">
          {[1, 2, 3, 4, 5, 6].map(i => (
            <Skeleton key={i} className="h-20 w-full" />
          ))}
        </div>
      ) : filteredDates.length === 0 ? (
        <Card>
          <CardContent className="flex flex-col items-center justify-center py-12 text-muted-foreground">
            <History className="h-12 w-12 mb-4 opacity-50" />
            <p>No historical log files found</p>
            <p className="text-xs mt-1">
              {dateRange?.from ? 'Try adjusting the date range' : 'Log files are created daily'}
            </p>
          </CardContent>
        </Card>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3">
          {filteredDates.map(date => (
            <HistoryFileCard
              key={date}
              date={date}
              onView={() => setSelectedDate(date)}
              isSelected={false}
            />
          ))}
        </div>
      )}
    </div>
  )
}

export default function DeveloperLogsPage() {
  useTranslation('common')
  usePageContext('Developer Logs')

  // Log stream hook
  const {
    connectionState,
    entries,
    bufferStats,
    errorClusters,
    isPaused,
    isConnected,
    setPaused,
    clearEntries,
    requestErrorSummary,
    requestBufferStats,
  } = useLogStream({
    autoConnect: LOG_STREAM_CONFIG.AUTO_CONNECT,
    maxEntries: LOG_STREAM_CONFIG.MAX_ENTRIES,
  })

  // Local state
  const [serverLevel, setServerLevel] = useState<string>('Information')
  const [availableLevels, setAvailableLevels] = useState<string[]>([])
  const [searchTerm, setSearchTerm] = useState('')
  const [exceptionsOnly, setExceptionsOnly] = useState(false)
  const [liveSelectedLevels, setLiveSelectedLevels] = useState<Set<DevLogLevel>>(new Set())
  const [expandedEntries, setExpandedEntries] = useState<Set<number>>(new Set())
  const [isChangingLevel, setIsChangingLevel] = useState(false)
  const [autoScroll, setAutoScroll] = useState(true)
  const [sortOrder, setSortOrder] = useState<'newest' | 'oldest'>('newest')
  const [mainTab, setMainTab] = useState('live')
  const [detailEntry, setDetailEntry] = useState<LogEntryDto | null>(null)
  const [isLiveFullscreen, setIsLiveFullscreen] = useState(false)

  const scrollAreaRef = useRef<HTMLDivElement>(null)
  const lastEntryCountRef = useRef(entries.length)

  // Auto-scroll when new entries arrive
  useEffect(() => {
    if (autoScroll && entries.length > lastEntryCountRef.current && scrollAreaRef.current) {
      const viewport = scrollAreaRef.current.querySelector('[data-radix-scroll-area-viewport]')
      if (viewport) {
        viewport.scrollTop = 0 // Scroll to top since newest entries are at top
      }
    }
    lastEntryCountRef.current = entries.length
  }, [entries.length, autoScroll])

  // Fetch initial log level
  useEffect(() => {
    getLogLevel().then(response => {
      setServerLevel(response.level)
      setAvailableLevels(response.availableLevels)
    }).catch(console.error)
  }, [])

  // Handle log level change
  const handleLevelChange = async (level: string) => {
    setIsChangingLevel(true)
    try {
      const response = await setLogLevel(level)
      setServerLevel(response.level)
    } catch (error) {
      console.error('Failed to set log level:', error)
    } finally {
      setIsChangingLevel(false)
    }
  }

  // Handle clear buffer
  const handleClearBuffer = async () => {
    try {
      await clearBuffer()
      clearEntries()
      refreshStats()
    } catch (error) {
      console.error('Failed to clear buffer:', error)
    }
  }

  // Refresh stats via SignalR
  const refreshStats = useCallback(() => {
    requestBufferStats()
    requestErrorSummary()
  }, [requestBufferStats, requestErrorSummary])

  // Toggle entry expansion
  const toggleEntryExpanded = useCallback((id: number) => {
    setExpandedEntries(prev => {
      const next = new Set(prev)
      if (next.has(id)) {
        next.delete(id)
      } else {
        next.add(id)
      }
      return next
    })
  }, [])

  // Filter entries locally
  const filteredEntries = useMemo(() => {
    let result = entries.filter(entry => {
      // Level filter
      if (liveSelectedLevels.size > 0 && !liveSelectedLevels.has(entry.level)) {
        return false
      }

      // Search filter
      if (searchTerm) {
        const searchLower = searchTerm.toLowerCase()
        const matchesMessage = entry.message.toLowerCase().includes(searchLower)
        const matchesSource = entry.sourceContext?.toLowerCase().includes(searchLower)
        const matchesException = entry.exception?.message?.toLowerCase().includes(searchLower)
        if (!matchesMessage && !matchesSource && !matchesException) {
          return false
        }
      }

      // Exceptions only filter
      if (exceptionsOnly && !entry.exception) {
        return false
      }

      return true
    })

    // Apply sort order
    if (sortOrder === 'oldest') {
      result = [...result].reverse()
    }

    return result
  }, [entries, searchTerm, exceptionsOnly, sortOrder, liveSelectedLevels])

  return (
    <div className="flex flex-col h-[calc(100vh-120px)] overflow-hidden">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <div className="p-2 bg-primary/10 rounded-lg">
            <Terminal className="h-6 w-6 text-primary" />
          </div>
          <div>
            <h1 className="text-3xl font-bold tracking-tight">Developer Logs</h1>
            <p className="text-muted-foreground">
              Real-time server log streaming and analysis
            </p>
          </div>
        </div>

        {/* Connection status */}
        <div className="flex items-center gap-2">
          {isConnected ? (
            <Badge variant="outline" className="gap-1 bg-green-50 text-green-700 dark:bg-green-900/30 dark:text-green-400">
              <Wifi className="h-3 w-3" />
              Connected
            </Badge>
          ) : connectionState === 'connecting' || connectionState === 'reconnecting' ? (
            <Badge variant="outline" className="gap-1 bg-amber-50 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400">
              <RefreshCw className="h-3 w-3 animate-spin" />
              {connectionState === 'connecting' ? 'Connecting' : 'Reconnecting'}
            </Badge>
          ) : (
            <Badge variant="outline" className="gap-1 bg-red-50 text-red-700 dark:bg-red-900/30 dark:text-red-400">
              <WifiOff className="h-3 w-3" />
              Disconnected
            </Badge>
          )}
        </div>
      </div>

      {/* Main tabs */}
      <Tabs value={mainTab} onValueChange={setMainTab} className="flex-1 flex flex-col mt-4 overflow-hidden">
        <TabsList>
          <TabsTrigger value="live" className="gap-2">
            <Terminal className="h-4 w-4" />
            Live Logs
          </TabsTrigger>
          <TabsTrigger value="history" className="gap-2">
            <History className="h-4 w-4" />
            History Files
          </TabsTrigger>
          <TabsTrigger value="stats" className="gap-2">
            <BarChart3 className="h-4 w-4" />
            Statistics
          </TabsTrigger>
          <TabsTrigger value="errors" className="gap-2">
            <AlertCircle className="h-4 w-4" />
            Error Clusters
          </TabsTrigger>
        </TabsList>

        {/* Live Logs Tab */}
        <TabsContent value="live" className="space-y-4">
          {/* Unified Toolbar */}
          <Card>
            <CardContent className="p-4 space-y-3">
              {/* Row 1: Main controls */}
              <div className="flex items-center gap-2">
                {/* Playback Group */}
                <div className="flex items-center gap-1 pr-3 border-r">
                  <Button
                    variant={isPaused ? 'default' : 'secondary'}
                    size="sm"
                    onClick={() => setPaused(!isPaused)}
                    className="gap-1.5"
                  >
                    {isPaused ? (
                      <>
                        <Play className="h-4 w-4" />
                        Resume
                      </>
                    ) : (
                      <>
                        <Pause className="h-4 w-4" />
                        Pause
                      </>
                    )}
                  </Button>
                  <Button
                    variant={autoScroll ? 'secondary' : 'ghost'}
                    size="sm"
                    onClick={() => setAutoScroll(!autoScroll)}
                    className="gap-1.5"
                    title="Auto-scroll to new entries"
                  >
                    <ArrowDownToLine className={cn('h-4 w-4', autoScroll && 'text-primary')} />
                  </Button>
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => setSortOrder(sortOrder === 'newest' ? 'oldest' : 'newest')}
                    title={sortOrder === 'newest' ? 'Showing newest first' : 'Showing oldest first'}
                  >
                    {sortOrder === 'newest' ? (
                      <ArrowDown className="h-4 w-4" />
                    ) : (
                      <ArrowUp className="h-4 w-4" />
                    )}
                  </Button>
                </div>

                {/* Server Log Level - controls what logs are generated */}
                <Select
                  value={serverLevel}
                  onValueChange={handleLevelChange}
                  disabled={isChangingLevel}
                >
                  <SelectTrigger className="w-[130px] h-8" title="Server minimum log level">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {availableLevels.map(level => {
                      const config = getLevelConfig(level as DevLogLevel)
                      return (
                        <SelectItem key={level} value={level}>
                          <span className={cn('flex items-center gap-2', config.textColor)}>
                            <config.icon className="h-4 w-4" />
                            {level}
                          </span>
                        </SelectItem>
                      )
                    })}
                  </SelectContent>
                </Select>

                {/* Display Level Filter - filter which levels to show */}
                <DropdownMenu>
                  <DropdownMenuTrigger asChild>
                    <Button variant="outline" size="sm" className="h-8 gap-2">
                      <span className="text-muted-foreground">Filter:</span>
                      {liveSelectedLevels.size === 0 ? (
                        <span>All</span>
                      ) : (
                        <span className="flex items-center gap-1">
                          {Array.from(liveSelectedLevels).slice(0, 2).map(level => {
                            const config = getLevelConfig(level)
                            return (
                              <Badge
                                key={level}
                                variant="outline"
                                className={cn('px-1.5 py-0 text-xs', config.textColor)}
                              >
                                {config.label}
                              </Badge>
                            )
                          })}
                          {liveSelectedLevels.size > 2 && (
                            <span className="text-xs text-muted-foreground">+{liveSelectedLevels.size - 2}</span>
                          )}
                        </span>
                      )}
                      <ChevronDown className="h-3.5 w-3.5 opacity-50" />
                    </Button>
                  </DropdownMenuTrigger>
                  <DropdownMenuContent align="end">
                    {LOG_LEVELS.map(level => (
                      <DropdownMenuCheckboxItem
                        key={level.value}
                        checked={liveSelectedLevels.has(level.value)}
                        onSelect={(e) => e.preventDefault()}
                        onCheckedChange={(checked) => {
                          setLiveSelectedLevels(prev => {
                            const next = new Set(prev)
                            if (checked) {
                              next.add(level.value)
                            } else {
                              next.delete(level.value)
                            }
                            return next
                          })
                        }}
                      >
                        <level.icon className={cn('h-4 w-4 mr-2', level.textColor)} />
                        <span className={level.textColor}>{level.value}</span>
                      </DropdownMenuCheckboxItem>
                    ))}
                    {liveSelectedLevels.size > 0 && (
                      <>
                        <DropdownMenuSeparator />
                        <Button
                          variant="ghost"
                          size="sm"
                          className="w-full h-7 text-xs"
                          onClick={(e) => {
                            e.preventDefault()
                            setLiveSelectedLevels(new Set())
                          }}
                        >
                          Clear filters
                        </Button>
                      </>
                    )}
                  </DropdownMenuContent>
                </DropdownMenu>

                {/* Search - grows to fill space */}
                <div className="flex-1 min-w-[180px] max-w-md">
                  <div className="relative">
                    <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                    <Input
                      placeholder="Search logs..."
                      value={searchTerm}
                      onChange={(e) => setSearchTerm(e.target.value)}
                      className="pl-8 h-8"
                    />
                    {searchTerm && (
                      <button
                        onClick={() => setSearchTerm('')}
                        className="absolute right-2.5 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground"
                      >
                        <X className="h-4 w-4" />
                      </button>
                    )}
                  </div>
                </div>

                {/* Errors only toggle - styled like Activity Timeline */}
                <div className="flex items-center gap-2 px-3 py-1.5 bg-muted rounded-md">
                  <Switch
                    id="errors-only"
                    checked={exceptionsOnly}
                    onCheckedChange={setExceptionsOnly}
                    className={cn(exceptionsOnly && 'data-[state=checked]:bg-destructive')}
                  />
                  <Label htmlFor="errors-only" className="text-sm cursor-pointer whitespace-nowrap">
                    Errors only
                  </Label>
                </div>

                {/* Clear filters - only show when filters are active */}
                {(searchTerm || exceptionsOnly || liveSelectedLevels.size > 0) && (
                  <Button
                    variant="ghost"
                    size="sm"
                    className="h-9 gap-1.5"
                    onClick={() => {
                      setSearchTerm('')
                      setExceptionsOnly(false)
                      setLiveSelectedLevels(new Set())
                    }}
                  >
                    <X className="h-3.5 w-3.5" />
                    Clear
                  </Button>
                )}

                {/* Clear buffer */}
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={handleClearBuffer}
                  className="h-9 gap-1.5 text-muted-foreground hover:text-destructive"
                >
                  <Trash2 className="h-4 w-4" />
                  Clear Buffer
                </Button>
              </div>
            </CardContent>
          </Card>

          {/* Log entries - terminal style */}
          <div className="rounded-lg border overflow-hidden">
            {/* Compact terminal header */}
            <div className="flex items-center justify-between px-3 py-1.5 bg-muted dark:bg-slate-900 border-b">
              <div className="flex items-center gap-3">
                <div className="flex items-center gap-1.5">
                  <div className="w-2.5 h-2.5 rounded-full bg-red-500" />
                  <div className="w-2.5 h-2.5 rounded-full bg-yellow-500" />
                  <div className="w-2.5 h-2.5 rounded-full bg-green-500" />
                </div>
                <span className="text-xs font-mono text-muted-foreground">
                  {filteredEntries.length} entries
                  {searchTerm && <span className="opacity-70"> (filtered from {entries.length})</span>}
                </span>
              </div>
              <div className="flex items-center gap-2 text-xs">
                {autoScroll && (
                  <span className="flex items-center gap-1 text-green-600 dark:text-green-400">
                    <ArrowDownToLine className="h-3 w-3" />
                    Auto-scroll
                  </span>
                )}
                {isPaused && (
                  <span className="flex items-center gap-1 text-amber-600 dark:text-amber-400">
                    <Pause className="h-3 w-3" />
                    Paused
                  </span>
                )}
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => setIsLiveFullscreen(true)}
                  className="h-6 gap-1 text-xs px-2"
                >
                  <Maximize2 className="h-3 w-3" />
                  Expand
                </Button>
              </div>
            </div>
            {/* Log content - taller height since live logs don't have paging */}
            <ScrollArea
              ref={scrollAreaRef}
              className="h-[calc(100vh-400px)] min-h-[400px] bg-card dark:bg-slate-950"
            >
              {filteredEntries.length === 0 ? (
                <div className="flex flex-col items-center justify-center min-h-[400px] text-muted-foreground py-12">
                  <Terminal className="h-12 w-12 mb-4 opacity-40" />
                  <p className="text-base font-medium">No log entries</p>
                  <p className="text-sm mt-1 opacity-70">
                    {entries.length === 0
                      ? 'Waiting for incoming logs...'
                      : 'No entries match the current filters'}
                  </p>
                </div>
              ) : (
                <div className="divide-y divide-border">
                  {filteredEntries.map(entry => (
                    <LogEntry
                      key={entry.id}
                      entry={entry}
                      isExpanded={expandedEntries.has(entry.id)}
                      onToggleExpand={() => toggleEntryExpanded(entry.id)}
                      onViewDetail={() => setDetailEntry(entry)}
                    />
                  ))}
                </div>
              )}
            </ScrollArea>
          </div>

          {/* Fullscreen Log Dialog for Live Logs */}
          <FullscreenLogDialog
            entries={filteredEntries}
            title="Live Logs"
            open={isLiveFullscreen}
            onOpenChange={setIsLiveFullscreen}
            expandedEntries={expandedEntries}
            onToggleExpand={toggleEntryExpanded}
            onViewDetail={setDetailEntry}
          />

          {/* Log Detail Dialog */}
          <LogDetailDialog
            entry={detailEntry}
            open={!!detailEntry}
            onOpenChange={(open) => !open && setDetailEntry(null)}
          />
        </TabsContent>

        {/* History Files Tab */}
        <TabsContent value="history" className="flex-1 mt-4 overflow-hidden">
          <HistoryTabContent />
        </TabsContent>

        {/* Statistics Tab */}
        <TabsContent value="stats">
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            <Card className="lg:col-span-2">
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <Database className="h-5 w-5" />
                  Buffer Overview
                </CardTitle>
              </CardHeader>
              <CardContent>
                <StatsCard stats={bufferStats} onRefresh={refreshStats} />
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <Clock className="h-5 w-5" />
                  Time Range
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                {bufferStats ? (
                  <>
                    <div className="p-3 bg-muted rounded-lg">
                      <div className="text-xs text-muted-foreground mb-1">Oldest Entry</div>
                      <div className="font-mono text-sm">
                        {bufferStats.oldestEntry ? formatFullTimestamp(bufferStats.oldestEntry) : 'N/A'}
                      </div>
                    </div>
                    <div className="p-3 bg-muted rounded-lg">
                      <div className="text-xs text-muted-foreground mb-1">Newest Entry</div>
                      <div className="font-mono text-sm">
                        {bufferStats.newestEntry ? formatFullTimestamp(bufferStats.newestEntry) : 'N/A'}
                      </div>
                    </div>
                  </>
                ) : (
                  <div className="space-y-3">
                    <Skeleton className="h-16 w-full" />
                    <Skeleton className="h-16 w-full" />
                  </div>
                )}
              </CardContent>
            </Card>
          </div>
        </TabsContent>

        {/* Error Clusters Tab */}
        <TabsContent value="errors">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <AlertCircle className="h-5 w-5 text-red-500" />
                Error Pattern Analysis
              </CardTitle>
            </CardHeader>
            <CardContent>
              <ErrorClusters
                clusters={errorClusters}
                onRefresh={() => requestErrorSummary()}
              />
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  )
}
