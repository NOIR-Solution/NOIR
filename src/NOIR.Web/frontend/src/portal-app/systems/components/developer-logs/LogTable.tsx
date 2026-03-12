/**
 * LogTable Component
 *
 * Terminal-style log viewer with a macOS-style traffic light header.
 * Uses TanStack Virtual for efficient rendering of large log entry lists.
 * Used by both Live Logs and History File Viewer.
 */
import { useEffect, useRef } from 'react'
import { useTranslation } from 'react-i18next'
import {
  Terminal,
  FileText,
  Maximize2,
  Pause,
  ArrowDownToLine,
  Loader2,
} from 'lucide-react'
import { Badge, Button, Dialog, DialogContent, DialogHeader, DialogTitle, EmptyState, useVirtualList } from '@uikit'

import { cn } from '@/lib/utils'
import type { LogEntryDto } from '@/services/developerLogs'
import { LogEntryRow } from './LogEntryRow'

const ESTIMATED_ROW_HEIGHT = 52

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
  /** Height class for the scroll container */
  scrollAreaClassName?: string
  /** Whether fullscreen is open */
  isFullscreen?: boolean
  /** Callback to toggle fullscreen */
  onFullscreenChange?: (open: boolean) => void
  /** Title for the fullscreen dialog */
  fullscreenTitle?: string
}

export const LogTable = ({
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
  scrollAreaClassName = 'h-[calc(100vh-330px)] min-h-[400px]',
  isFullscreen = false,
  onFullscreenChange,
  fullscreenTitle = 'Logs',
}: LogTableProps) => {
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
              <span className="flex items-center gap-1 text-green-700 dark:text-green-400">
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
        <div className={cn(
          'bg-card dark:bg-slate-950',
          scrollAreaClassName,
        )}>
          <LogTableContent
            entries={entries}
            expandedEntries={expandedEntries}
            onToggleExpand={onToggleExpand}
            onViewDetail={onViewDetail}
            isLoading={isLoading}
            emptyMessage={emptyMessage}
            emptySubMessage={emptySubMessage}
            autoScrollToEnd={autoScroll}
          />
        </div>
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
}

// Internal component for virtualized log content rendering
const LogTableContent = ({
  entries,
  expandedEntries,
  onToggleExpand,
  onViewDetail,
  isLoading,
  emptyMessage,
  emptySubMessage,
  autoScrollToEnd,
}: {
  entries: LogEntryDto[]
  expandedEntries: Set<number>
  onToggleExpand: (id: number) => void
  onViewDetail: (entry: LogEntryDto) => void
  isLoading: boolean
  emptyMessage: string
  emptySubMessage?: string
  autoScrollToEnd?: boolean
}) => {
  const { parentRef, virtualizer, virtualItems, totalSize } = useVirtualList({
    items: entries,
    estimateSize: ESTIMATED_ROW_HEIGHT,
    overscan: 5,
    getItemKey: (entry) => entry.id,
  })

  const lastEntryCountRef = useRef(entries.length)

  // Auto-scroll to the first entry (newest) when new entries arrive
  useEffect(() => {
    if (autoScrollToEnd && entries.length > lastEntryCountRef.current) {
      virtualizer.scrollToIndex(0, { align: 'start' })
    }
    lastEntryCountRef.current = entries.length
  }, [entries.length, autoScrollToEnd, virtualizer])

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
    <div ref={parentRef} className="overflow-auto" style={{ height: '100%' }}>
      <div style={{ height: `${totalSize}px`, width: '100%', position: 'relative' }}>
        {virtualItems.map((virtualItem) => {
          const entry = entries[virtualItem.index]
          return (
            <div
              key={virtualItem.key}
              data-index={virtualItem.index}
              ref={virtualizer.measureElement}
              style={{
                position: 'absolute',
                top: 0,
                left: 0,
                width: '100%',
                transform: `translateY(${virtualItem.start}px)`,
              }}
            >
              <LogEntryRow
                entry={entry}
                isExpanded={expandedEntries.has(entry.id)}
                onToggleExpand={() => onToggleExpand(entry.id)}
                onViewDetail={() => onViewDetail(entry)}
              />
            </div>
          )
        })}
      </div>
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
  const { parentRef, virtualizer, virtualItems, totalSize } = useVirtualList({
    items: entries,
    estimateSize: ESTIMATED_ROW_HEIGHT,
    overscan: 5,
    getItemKey: (entry) => entry.id,
  })

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
        <div className="flex-1 min-h-0 bg-card dark:bg-slate-950">
          {entries.length === 0 ? (
            <EmptyState
              icon={FileText}
              title={t('developerLogs.noLogEntries')}
              description=""
              className="border-0 rounded-none px-4 py-20"
            />
          ) : (
            <div ref={parentRef} className="overflow-auto" style={{ height: '100%' }}>
              <div style={{ height: `${totalSize}px`, width: '100%', position: 'relative' }}>
                {virtualItems.map((virtualItem) => {
                  const entry = entries[virtualItem.index]
                  return (
                    <div
                      key={virtualItem.key}
                      data-index={virtualItem.index}
                      ref={virtualizer.measureElement}
                      style={{
                        position: 'absolute',
                        top: 0,
                        left: 0,
                        width: '100%',
                        transform: `translateY(${virtualItem.start}px)`,
                      }}
                    >
                      <LogEntryRow
                        entry={entry}
                        isExpanded={expandedEntries.has(entry.id)}
                        onToggleExpand={() => onToggleExpand(entry.id)}
                        onViewDetail={() => onViewDetail(entry)}
                      />
                    </div>
                  )
                })}
              </div>
            </div>
          )}
        </div>
      </DialogContent>
    </Dialog>
  )
}
