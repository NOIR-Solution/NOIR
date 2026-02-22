/**
 * HistoryTab Component
 *
 * Displays available historical log files in a grid with date range filtering.
 * When a file is selected, shows HistoryFileViewer with search, level filtering,
 * pagination, and fullscreen support.
 */
import { useState, useEffect, useCallback, useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import type { DateRange } from 'react-day-picker'
import {
  Search,
  ChevronDown,
  ChevronLeft,
  ChevronRight,
  X,
  ArrowDown,
  ArrowUp,
  History,
  FileText,
} from 'lucide-react'
import {
  Badge,
  Button,
  Card,
  CardContent,
  DateRangePicker,
  DropdownMenu,
  DropdownMenuCheckboxItem,
  DropdownMenuContent,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
  Input,
  Label,
  Pagination,
  EmptyState,
  Skeleton,
  Switch,
} from '@uikit'

import { cn } from '@/lib/utils'
import { parseISO } from 'date-fns'
import {
  getAvailableLogDates,
  getHistoricalLogs,
  type LogEntryDto,
  type DevLogLevel,
  type LogEntriesPagedResponse,
} from '@/services/developerLogs'
import { LogTable } from './LogTable'
import { LogDetailDialog } from './LogDetailDialog'
import { LOG_LEVELS, LOG_STREAM_CONFIG, getLevelConfig, formatDateDisplay } from './log-utils'

// History File Card Component
const HistoryFileCard = ({
  date,
  onView,
  isSelected,
}: {
  date: string
  onView: () => void
  isSelected: boolean
}) => {
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

// History File Viewer Component
const HistoryFileViewer = ({
  date,
  onBack,
}: {
  date: string
  onBack: () => void
}) => {
  const { t } = useTranslation('common')
  const [entries, setEntries] = useState<LogEntryDto[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [page, setPage] = useState(1)
  const [totalPages, setTotalPages] = useState(0)
  const [totalCount, setTotalCount] = useState(0)
  const [searchTerm, setSearchTerm] = useState('')
  const [selectedLevels, setSelectedLevels] = useState<Set<DevLogLevel>>(new Set())
  const [errorsOnly, setErrorsOnly] = useState(false)
  const [expandedEntries, setExpandedEntries] = useState<Set<number>>(new Set())
  const [sortOrder, setSortOrder] = useState<'newest' | 'oldest'>('newest')
  const [detailEntry, setDetailEntry] = useState<LogEntryDto | null>(null)
  const [isFullscreen, setIsFullscreen] = useState(false)

  const fetchLogs = useCallback(async () => {
    setIsLoading(true)
    try {
      // Determine levels to filter
      let levelsToFilter: DevLogLevel[] | undefined
      if (errorsOnly) {
        levelsToFilter = ['Error', 'Warning', 'Fatal']
      } else if (selectedLevels.size > 0) {
        levelsToFilter = Array.from(selectedLevels)
      }

      const response: LogEntriesPagedResponse = await getHistoricalLogs(date, {
        page,
        pageSize: LOG_STREAM_CONFIG.HISTORY_PAGE_SIZE,
        search: searchTerm || undefined,
        levels: levelsToFilter,
        sortOrder,
      })
      setEntries(response.items)
      setTotalPages(response.totalPages)
      setTotalCount(response.totalCount)
    } catch {
      // Error visible in network tab
    } finally {
      setIsLoading(false)
    }
  }, [date, page, searchTerm, selectedLevels, errorsOnly, sortOrder])

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

  return (
    <div className="flex flex-col h-full min-h-[400px]">
      {/* Header with back button */}
      <div className="flex items-center gap-3 pb-4 border-b flex-shrink-0">
        <Button variant="ghost" size="sm" onClick={onBack} className="gap-1">
          <ChevronLeft className="h-4 w-4" />
          {t('developerLogs.back')}
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
          onClick={() => {
            setSortOrder(sortOrder === 'newest' ? 'oldest' : 'newest')
            setPage(1)
          }}
          className="gap-1"
        >
          {sortOrder === 'newest' ? (
            <>
              <ArrowDown className="h-4 w-4" />
              {t('developerLogs.newestFirst')}
            </>
          ) : (
            <>
              <ArrowUp className="h-4 w-4" />
              {t('developerLogs.oldestFirst')}
            </>
          )}
        </Button>
      </div>

      {/* Filters - compact row */}
      <div className="flex items-center gap-2 py-4 flex-shrink-0">
        <div className="relative flex-1 max-w-sm">
          <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
          <Input
            placeholder={t('developerLogs.searchLogsPlaceholder')}
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
        {/* Errors only toggle */}
        <div className="flex items-center gap-2 px-3 py-1.5 bg-muted rounded-md">
          <Switch
            id="history-errors-only"
            checked={errorsOnly}
            onCheckedChange={(checked) => {
              setErrorsOnly(checked)
              if (checked) {
                setSelectedLevels(new Set())
              }
              setPage(1)
            }}
            className={cn(errorsOnly && 'data-[state=checked]:bg-destructive')}
          />
          <Label htmlFor="history-errors-only" className="text-sm cursor-pointer whitespace-nowrap">
            {t('developerLogs.errorsOnly')}
          </Label>
        </div>

        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="outline" size="sm" className="h-8 gap-2 cursor-pointer" disabled={errorsOnly} aria-label={t('developerLogs.filterByLevel', 'Filter by log level')}>
              <span className="text-muted-foreground">{t('developerLogs.filterColon')}</span>
              {selectedLevels.size === 0 ? (
                <span>{t('developerLogs.allLevels')}</span>
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
                  {t('developerLogs.clearFilters')}
                </Button>
              </>
            )}
          </DropdownMenuContent>
        </DropdownMenu>
      </div>

      {/* Log entries - terminal style */}
      <LogTable
        entries={entries}
        expandedEntries={expandedEntries}
        onToggleExpand={toggleEntryExpanded}
        onViewDetail={setDetailEntry}
        isLoading={isLoading}
        emptyMessage={t('developerLogs.noLogEntriesFound')}
        isFullscreen={isFullscreen}
        onFullscreenChange={setIsFullscreen}
        fullscreenTitle={`noir-${date}.json`}
      />

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

      {/* Log Detail Dialog */}
      <LogDetailDialog
        entry={detailEntry}
        open={!!detailEntry}
        onOpenChange={(open) => !open && setDetailEntry(null)}
      />
    </div>
  )
}

// Main History Tab Content Component
export const HistoryTab = () => {
  const { t } = useTranslation('common')
  const [availableDates, setAvailableDates] = useState<string[]>([])
  const [isLoadingDates, setIsLoadingDates] = useState(true)
  const [selectedDate, setSelectedDate] = useState<string | null>(null)
  const [dateRange, setDateRange] = useState<DateRange | undefined>(undefined)

  const fetchDates = useCallback(async () => {
    setIsLoadingDates(true)
    try {
      const dates = await getAvailableLogDates()
      setAvailableDates(dates)
    } catch {
      // Error visible in network tab
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
      {/* Filter Bar */}
      <div className="flex flex-wrap items-center gap-2">
        <Badge variant="secondary">
          {t('developerLogs.filesCount', { filtered: filteredDates.length, total: availableDates.length })}
        </Badge>

        <div className="flex-1" />

        <DateRangePicker
          value={dateRange}
          onChange={setDateRange}
          placeholder={t('developerLogs.filterByDateRange')}
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
        <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
          <CardContent className="p-0">
            <EmptyState
              icon={History}
              title={t('developerLogs.noHistoricalFiles')}
              description={dateRange?.from ? t('developerLogs.adjustDateRange') : t('developerLogs.logFilesCreatedDaily')}
              className="border-0 rounded-none px-4 py-12"
            />
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
