/**
 * LogTable Component
 *
 * Terminal-style log viewer with a macOS-style traffic light header.
 * Renders a list of log entries with scrolling support and optional
 * fullscreen dialog. Used by both Live Logs and History File Viewer.
 */
import { forwardRef } from 'react'
import { useTranslation } from 'react-i18next'
import {
  Terminal,
  FileText,
  Maximize2,
  Pause,
  ArrowDownToLine,
  Loader2,
} from 'lucide-react'
import { Badge, Button, Dialog, DialogContent, DialogHeader, DialogTitle, EmptyState, ScrollArea } from '@uikit'

import type { LogEntryDto } from '@/services/developerLogs'
import { LogEntryRow } from './LogEntryRow'

export interface LogTableProps {
  entries: LogEntryDto[]
  expandedEntries: Set<number>
  onToggleExpand: (id: number) => void
  onViewDetail: (entry: LogEntryDto) => void
  /** If true, shows a loading spinner instead of entries */
  isLoading?: boolean
  /** Total unfiltered count for "filtered from N" display */
  totalEntries?: number
  /** Current search term (for filtered display) */
  searchTerm?: string
  /** Whether auto-scroll is enabled (live logs only) */
  autoScroll?: boolean
  /** Whether stream is paused (live logs only) */
  isPaused?: boolean
  /** Custom empty state message */
  emptyMessage?: string
  /** Custom empty sub-message */
  emptySubMessage?: string
  /** Whether to use ScrollArea (for live logs) vs flex overflow (for history) */
  useScrollArea?: boolean
  /** Height class for ScrollArea mode */
  scrollAreaClassName?: string
  /** Whether fullscreen is open */
  isFullscreen?: boolean
  /** Callback to toggle fullscreen */
  onFullscreenChange?: (open: boolean) => void
  /** Title for the fullscreen dialog */
  fullscreenTitle?: string
}

export const LogTable = forwardRef<HTMLDivElement, LogTableProps>((
  {
    entries,
    expandedEntries,
    onToggleExpand,
    onViewDetail,
    isLoading = false,
    totalEntries,
    searchTerm,
    autoScroll,
    isPaused,
    emptyMessage = 'No log entries',
    emptySubMessage,
    useScrollArea = false,
    scrollAreaClassName = 'h-[calc(100vh-330px)] min-h-[400px]',
    isFullscreen = false,
    onFullscreenChange,
    fullscreenTitle = 'Logs',
  },
  ref
) => {
  const { t } = useTranslation('common')
  const entryCount = entries.length
  const showFilteredInfo = searchTerm && totalEntries !== undefined && totalEntries !== entryCount

  return (
    <>
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
              {entryCount} {t('developerLogs.entries')}
              {showFilteredInfo && (
                <span className="opacity-70"> ({t('developerLogs.filteredFrom', { total: totalEntries })})</span>
              )}
            </span>
          </div>
          <div className="flex items-center gap-2 text-xs">
            {autoScroll && (
              <span className="flex items-center gap-1 text-green-600 dark:text-green-400">
                <ArrowDownToLine className="h-3 w-3" />
                {t('developerLogs.autoScrollLabel')}
              </span>
            )}
            {isPaused && (
              <span className="flex items-center gap-1 text-amber-600 dark:text-amber-400">
                <Pause className="h-3 w-3" />
                {t('developerLogs.paused')}
              </span>
            )}
            {onFullscreenChange && (
              <Button
                variant="ghost"
                size="sm"
                onClick={() => onFullscreenChange(true)}
                className="h-6 gap-1 text-xs px-2"
              >
                <Maximize2 className="h-3 w-3" />
                {t('developerLogs.expandLabel')}
              </Button>
            )}
          </div>
        </div>

        {/* Log content */}
        {useScrollArea ? (
          <ScrollArea
            ref={ref}
            className={`${scrollAreaClassName} bg-card dark:bg-slate-950`}
          >
            <LogTableContent
              entries={entries}
              expandedEntries={expandedEntries}
              onToggleExpand={onToggleExpand}
              onViewDetail={onViewDetail}
              isLoading={isLoading}
              emptyMessage={emptyMessage}
              emptySubMessage={emptySubMessage}
            />
          </ScrollArea>
        ) : (
          <div className="bg-card dark:bg-slate-950 flex-1 overflow-y-auto">
            <LogTableContent
              entries={entries}
              expandedEntries={expandedEntries}
              onToggleExpand={onToggleExpand}
              onViewDetail={onViewDetail}
              isLoading={isLoading}
              emptyMessage={emptyMessage}
              emptySubMessage={emptySubMessage}
            />
          </div>
        )}
      </div>

      {/* Fullscreen Log Dialog */}
      {onFullscreenChange && (
        <FullscreenLogDialog
          entries={entries}
          title={fullscreenTitle}
          open={isFullscreen}
          onOpenChange={onFullscreenChange}
          expandedEntries={expandedEntries}
          onToggleExpand={onToggleExpand}
          onViewDetail={onViewDetail}
        />
      )}
    </>
  )
})
LogTable.displayName = 'LogTable'

// Internal component for log content rendering
const LogTableContent = ({
  entries,
  expandedEntries,
  onToggleExpand,
  onViewDetail,
  isLoading,
  emptyMessage,
  emptySubMessage,
}: {
  entries: LogEntryDto[]
  expandedEntries: Set<number>
  onToggleExpand: (id: number) => void
  onViewDetail: (entry: LogEntryDto) => void
  isLoading: boolean
  emptyMessage: string
  emptySubMessage?: string
}) => {
  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    )
  }

  if (entries.length === 0) {
    return (
      <EmptyState
        icon={Terminal}
        title={emptyMessage}
        description={emptySubMessage || ''}
        className="border-0 rounded-none px-4 py-12 min-h-[400px]"
      />
    )
  }

  return (
    <div className="divide-y divide-border">
      {entries.map(entry => (
        <LogEntryRow
          key={entry.id}
          entry={entry}
          isExpanded={expandedEntries.has(entry.id)}
          onToggleExpand={() => onToggleExpand(entry.id)}
          onViewDetail={() => onViewDetail(entry)}
        />
      ))}
    </div>
  )
}

// Fullscreen Log Viewer Dialog — intentionally uses raw Dialog (not Credenza)
// because it needs near-fullscreen dimensions (95vw x 95vh), not a bottom drawer on mobile.
const FullscreenLogDialog = ({
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
}) => {
  const { t } = useTranslation('common')
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-[95vw] w-full max-h-[95vh] h-full flex flex-col p-0">
        <DialogHeader className="px-4 py-3 border-b flex-shrink-0">
          <DialogTitle className="flex items-center gap-2">
            <Terminal className="h-5 w-5" />
            {title}
            <Badge variant="secondary" className="ml-2">
              {entries.length} {t('developerLogs.entries')}
            </Badge>
          </DialogTitle>
        </DialogHeader>
        <div className="flex-1 overflow-y-auto bg-card dark:bg-slate-950">
          {entries.length === 0 ? (
            <EmptyState
              icon={FileText}
              title={t('developerLogs.noLogEntries')}
              description=""
              className="border-0 rounded-none px-4 py-20"
            />
          ) : (
            <div className="divide-y divide-border">
              {entries.map(entry => (
                <LogEntryRow
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
